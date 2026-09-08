using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Wisp.UI
{
    public sealed class EnemyMedia
    {
        public string Portrait = "";
        public string[] Maps = new string[0];
        public string[] MapCaptions = new string[0];
    }

    // Artwork stays in a local cache; release archives contain reference URLs only.
    public sealed class MediaLibrary : IDisposable
    {
        private readonly MonoBehaviour owner;
        private readonly Dictionary<string, Texture2D> images = new Dictionary<string, Texture2D>();
        private readonly HashSet<string> pending = new HashSet<string>();
        private readonly Dictionary<string, string> errors = new Dictionary<string, string>();
        private readonly Queue<string> order = new Queue<string>();
        private bool disposed;
        public readonly Dictionary<string, EnemyMedia> Enemies;
        public string Cache { get; private set; }

        public MediaLibrary(MonoBehaviour owner)
        {
            this.owner = owner;
            Cache = Path.Combine(Application.persistentDataPath, "WispCache");
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp.media.json"))
            using (var reader = new StreamReader(stream))
                Enemies = JsonConvert.DeserializeObject<Dictionary<string, EnemyMedia>>(reader.ReadToEnd());
        }

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
            errors[key] = "Изображение ещё не сохранено на этом компьютере.";
            return null;
        }

        public Texture2D Get(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            string key = Key(url);
            var texture = Local(key);
            if (texture != null) return texture;
            if (!pending.Contains(key) && errors[key] == "Изображение ещё не сохранено на этом компьютере." && pending.Count < 3)
            {
                pending.Add(key);
                errors[key] = "Загрузка изображения…";
                owner.StartCoroutine(Download(url, key));
            }
            return null;
        }

        public string Status(string url)
        {
            string result;
            return errors.TryGetValue(Key(url), out result) ? result : "Загрузка изображения…";
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
            { errors[key] = "Неизвестный источник изображения."; pending.Remove(key); yield break; }
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
                        errors[key] = "Нет связи с библиотекой изображений. Нажми «Повторить».";
                    else try
                    {
                        var data = request.downloadHandler.data;
                        if (data.Length > 16 * 1024 * 1024) throw new InvalidDataException();
                        var decoded = Decode(key, data);
                        if (decoded == null) throw new InvalidDataException();
                        Directory.CreateDirectory(Cache);
                        File.WriteAllBytes(Path.Combine(Cache, key + ".png"), data);
                    }
                    catch (Exception) { errors[key] = "Не удалось прочитать или сохранить изображение."; }
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
