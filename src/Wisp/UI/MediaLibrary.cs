using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Wisp.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Wisp.UI
{
    public sealed class StepImage { public string Label = ""; public string Url = ""; public bool Wide; }

    public sealed class EnemyMedia
    {
        public string Portrait = "";
        public string[] Maps = new string[0];
        public string[] MapCaptions = new string[0];
    }

    // Local reference images are embedded; remaining wiki artwork is cached on demand.
    public sealed class MediaLibrary : IDisposable
    {
        private readonly MonoBehaviour owner;
        private readonly Dictionary<string, Texture2D> images = new Dictionary<string, Texture2D>();
        private readonly HashSet<string> pending = new HashSet<string>();
        private readonly Dictionary<string, string> errors = new Dictionary<string, string>();
        private readonly Queue<string> order = new Queue<string>();
        private bool disposed;
        public readonly Dictionary<string, EnemyMedia> Enemies;
        public readonly Dictionary<string, string> Regions;
        public readonly Dictionary<string, string> EnglishRegions;
        public readonly Dictionary<string, StepImage[]> Steps;
        public string Cache { get; private set; }

        public MediaLibrary(MonoBehaviour owner)
        {
            this.owner = owner;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp.regions-en.json"))
            using (var reader = new StreamReader(stream))
                EnglishRegions = JsonConvert.DeserializeObject<Dictionary<string,string>>(reader.ReadToEnd());
            Cache = Path.Combine(Application.persistentDataPath, "WispCache");
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp.media.json"))
            using (var reader = new StreamReader(stream))
                Enemies = JsonConvert.DeserializeObject<Dictionary<string, EnemyMedia>>(reader.ReadToEnd());
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp.regions.json"))
            using (var reader = new StreamReader(stream))
                Regions = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp.step-media.json"))
            using (var reader = new StreamReader(stream))
                Steps = JsonConvert.DeserializeObject<Dictionary<string, StepImage[]>>(reader.ReadToEnd());
        }

        public Texture2D AchievementIcon(string key)
        {
            string id = "achievement-" + key;
            Texture2D image;
            if (images.TryGetValue(id, out image)) return image;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp.achievements/" + key + ".jpg"))
                if (stream != null) using (var buffer = new MemoryStream()) { stream.CopyTo(buffer); return Decode(id, buffer.ToArray()); }
            return null;
        }

        public Texture2D Region(string id)
        {
            string key = "region-" + id;
            if (I18n.English)
            {
                string englishMap;
                if (EnglishRegions.TryGetValue(id, out englishMap)) return Get(englishMap);
                return Regions.TryGetValue(id, out englishMap) ? Get(englishMap) : null;
            }
            Texture2D texture;
            if (images.TryGetValue(key, out texture)) return texture;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp.ui/" + key + ".png"))
                if (stream != null) using (var buffer = new MemoryStream()) { stream.CopyTo(buffer); return Decode(key, buffer.ToArray()); }
            texture = Local(key);
            if (texture != null) return texture;
            string url;
            return Regions.TryGetValue(id, out url) ? Get(url) : null;
        }
        public string RegionStatus(string id)
        { string url; return Regions.TryGetValue(id, out url) ? Status(url) : Wisp.Core.I18n.T("Для этой зоны справочная карта пока не добавлена."); }

        public static string Key(string url)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(url))).Replace("-", "").ToLowerInvariant();
        }

        public Texture2D Local(string key)
        {
            Texture2D result;
            if (images.TryGetValue(key, out result)) return result;
            if (errors.ContainsKey(key)) return null;
            try
            {
                var path = Path.Combine(Cache, key + ".png");
                if (File.Exists(path)) { var decoded = Decode(key, File.ReadAllBytes(path)); if (decoded != null) return decoded; }
            }
            catch (Exception) { }
            errors[key] = Wisp.Core.I18n.T("Изображение ещё не сохранено на этом компьютере.");
            return null;
        }

        public Texture2D Get(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (url.StartsWith("embedded:"))
            {
                Texture2D saved; if (images.TryGetValue(url, out saved)) return saved;
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp." + url.Substring(9)))
                    if (stream != null) using (var buffer = new MemoryStream()) { stream.CopyTo(buffer); return Decode(url, buffer.ToArray()); }
                return null;
            }
            string key = Key(url);
            var texture = Local(key);
            if (texture != null) return texture;
            if (!pending.Contains(key) && errors[key] == Wisp.Core.I18n.T("Изображение ещё не сохранено на этом компьютере.") && pending.Count < 3)
            {
                pending.Add(key);
                errors[key] = Wisp.Core.I18n.T("Загрузка изображения…");
                owner.StartCoroutine(Download(url, key));
            }
            return null;
        }

        public string Status(string url)
        {
            string result;
            return errors.TryGetValue(Key(url), out result) ? result : Wisp.Core.I18n.T("Загрузка изображения…");
        }

        public void Retry()
        {
            foreach (var key in new List<string>(errors.Keys))
                if (!pending.Contains(key)) errors.Remove(key);
        }

        private IEnumerator Download(string url, string key)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != "https" || uri.Host != "cdn.wikimg.net")
            { errors[key] = Wisp.Core.I18n.T("Неизвестный источник изображения."); pending.Remove(key); yield break; }
            using (var request = new UnityWebRequest())
            {
                request.url = url;
                request.method = "GET";
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 20;
                yield return request.SendWebRequest();
                if (!disposed)
                {
                    if (request.result != UnityWebRequest.Result.Success)
                        errors[key] = Wisp.Core.I18n.T("Нет связи с библиотекой изображений. Нажми «Повторить».");
                    else try
                    {
                        var data = request.downloadHandler.data;
                        if (data.Length > 16 * 1024 * 1024) throw new InvalidDataException();
                        var decoded = Decode(key, data);
                        if (decoded == null) throw new InvalidDataException();
                        Directory.CreateDirectory(Cache);
                        File.WriteAllBytes(Path.Combine(Cache, key + ".png"), data);
                    }
                    catch (Exception) { errors[key] = Wisp.Core.I18n.T("Не удалось прочитать или сохранить изображение."); }
                }
            }
            pending.Remove(key);
        }

        private Texture2D Decode(string key, byte[] data)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, data, true)) { UnityEngine.Object.Destroy(texture); return null; }
            while (images.Count >= 24 && order.Count > 0)
            {
                var oldest = order.Dequeue();
                UnityEngine.Object.Destroy(images[oldest]); images.Remove(oldest);
            }
            texture.filterMode = FilterMode.Bilinear;
            images[key] = texture; order.Enqueue(key); errors.Remove(key);
            return texture;
        }

        public void Dispose()
        {
            disposed = true;
            foreach (var image in images.Values) UnityEngine.Object.Destroy(image);
            images.Clear();
        }
    }
}
