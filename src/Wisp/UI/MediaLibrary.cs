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
        public HabitatMap[] HabitatMaps = new HabitatMap[0];
    }
    public sealed class MediaLibrary : IDisposable
    {
        private readonly MonoBehaviour owner;
        private readonly Dictionary<string, Texture2D> images = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, string> keys = new Dictionary<string, string>();
        private readonly Dictionary<string, string> regionSources = new Dictionary<string, string>();
        private readonly HashSet<string> checkedLocal = new HashSet<string>();
        private readonly HashSet<UnityWebRequest> requests = new HashSet<UnityWebRequest>();
        private readonly ImageLoads loads = new ImageLoads();
        private readonly ImageBudget budget = new ImageBudget();
        private bool disposed;
        public readonly Dictionary<string, EnemyMedia> Enemies;
        public readonly Dictionary<string, string> Regions;
        public readonly Dictionary<string, string> EnglishRegions;
        public readonly Dictionary<string, StepImage[]> Steps;
        public readonly CollectionGroup[] Collections;
        public string Cache { get; private set; }
        public long CachedBytes { get { return budget.Bytes; } }
        public int CachedCount { get { return images.Count; } }
        public MediaLibrary(MonoBehaviour owner)
        {
            this.owner = owner;
            Cache = Path.Combine(Application.persistentDataPath, "WispCache");
            EnglishRegions = Read<Dictionary<string, string>>("regions-en.json");
            Regions = Read<Dictionary<string, string>>("regions.json");
            Enemies = Read<Dictionary<string, EnemyMedia>>("media.json");
            Steps = Read<Dictionary<string, StepImage[]>>("step-media.json");
            Collections = Read<CollectionGroup[]>("collections.json");
        }
        private static T Read<T>(string name)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp." + name))
            using (var reader = new StreamReader(stream)) return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
        }
        public bool TryStepImages(string goal, string chapter, string step, out StepImage[] result)
        {
            // An empty occurrence override intentionally hides the inherited gallery.
            return Steps.TryGetValue(goal + "/" + chapter + "/" + step, out result) || Steps.TryGetValue(step, out result);
        }
        public Texture2D AchievementIcon(string key) { return Get("embedded:achievements/" + key + ".jpg"); }
        public string RegionSource(string id)
        {
            string url;
            string key = (I18n.English ? "en:" : "ru:") + id;
            if (regionSources.TryGetValue(key, out url)) return url;
            if (I18n.English && EnglishRegions.TryGetValue(id, out url)) return regionSources[key] = url;
            // Packaged maps work in both languages, including a fresh offline install.
            if (Regions.TryGetValue(id, out url)) return regionSources[key] = url;
            if (File.Exists(Path.Combine(Cache, "region-" + id + ".png"))) return regionSources[key] = "local:region-" + id;
            return regionSources[key] = "";
        }
        public Texture2D Region(string id) { return Get(RegionSource(id)); }
        public string RegionStatus(string id) { return Status(RegionSource(id)); }
        public static string Key(string url)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(url))).Replace("-", "").ToLowerInvariant();
        }
        private string CacheKey(string url)
        { string key; if (!keys.TryGetValue(url, out key)) keys[url] = key = Key(url); return key; }
        public Texture2D Get(string url)
        {
            if (disposed || string.IsNullOrEmpty(url)) return null;
            string key = CacheKey(url);
            Texture2D texture;
            if (images.TryGetValue(key, out texture)) { budget.Touch(key); return texture; }
            if (loads.State(key) == ImageLoadState.Failed) return null;
            if (checkedLocal.Add(key))
            {
                try
                {
                    if (url.StartsWith("embedded:", StringComparison.Ordinal))
                    {
                        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp." + url.Substring(9)))
                        {
                            if (stream == null) throw new FileNotFoundException();
                            using (var buffer = new MemoryStream()) { stream.CopyTo(buffer); texture = Decode(key, buffer.ToArray()); }
                        }
                    }
                    else
                    {
                        string path = Path.Combine(Cache, (url.StartsWith("local:", StringComparison.Ordinal) ? url.Substring(6) : key) + ".png");
                        texture = File.Exists(path) ? Decode(key, File.ReadAllBytes(path)) : null;
                    }
                    if (texture != null) return texture;
                }
                catch (Exception e) { Debug.LogWarning("[Wisp] Image cache read failed: " + e.Message); }
                if (url.StartsWith("embedded:", StringComparison.Ordinal) || url.StartsWith("local:", StringComparison.Ordinal))
                { loads.Finish(key, false); return null; }
            }
            if (loads.TryStart(key)) owner.StartCoroutine(Download(url, key));
            return null;
        }
        public string Status(string url)
        {
            if (string.IsNullOrEmpty(url)) return I18n.T("Для этой зоны справочная карта пока не добавлена.");
            return loads.State(CacheKey(url)) == ImageLoadState.Failed ? I18n.T("Не удалось загрузить изображение. Повторить · X") : I18n.T("Загрузка изображения…");
        }
        public bool CanRetry(string url) { return !string.IsNullOrEmpty(url) && loads.State(CacheKey(url)) == ImageLoadState.Failed; }
        public void Retry(string url)
        {
            if (!CanRetry(url)) return;
            string key = CacheKey(url); loads.Retry(key); checkedLocal.Remove(key); Get(url);
        }
        private IEnumerator Download(string url, string key)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != "https" || uri.Host != "cdn.wikimg.net")
            { loads.Finish(key, false); yield break; }
            var request = UnityWebRequest.Get(url);
            request.timeout = 20;
            requests.Add(request);
            bool success = false;
            try
            {
                yield return request.SendWebRequest();
                if (!disposed && request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        byte[] data = request.downloadHandler.data;
                        if (data.Length > 16 * 1024 * 1024) throw new InvalidDataException("Image exceeds download limit");
                        success = Decode(key, data) != null;
                        if (success)
                            try { Directory.CreateDirectory(Cache); string path = Path.Combine(Cache, key + ".png"); File.WriteAllBytes(path + ".tmp", data); if (File.Exists(path)) File.Replace(path + ".tmp", path, null); else File.Move(path + ".tmp", path); }
                            catch (Exception e) { Debug.LogWarning("[Wisp] Image is available, but disk cache failed: " + e.Message); }
                    }
                    catch (Exception e) { Debug.LogWarning("[Wisp] Image decode failed: " + e.Message); }
                }
            }
            finally { requests.Remove(request); request.Dispose(); loads.Finish(key, success); }
        }
        private Texture2D Decode(string key, byte[] data)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, data, true)) throw new InvalidDataException("Invalid image");
                long bytes = (long)texture.width * texture.height * 4;
                if (!budget.Fits(bytes)) throw new InvalidDataException("Decoded image exceeds memory budget");
                foreach (var oldest in budget.Admit(key, bytes))
                { UnityEngine.Object.Destroy(images[oldest]); images.Remove(oldest); checkedLocal.Remove(oldest); loads.Evicted(oldest); }
                texture.filterMode = FilterMode.Bilinear; images[key] = texture;
                if (loads.State(key) != ImageLoadState.Loading) loads.Finish(key, true);
                return texture;
            }
            catch { UnityEngine.Object.Destroy(texture); throw; }
        }
        public void Dispose()
        {
            disposed = true;
            foreach (var request in requests) request.Abort();
            foreach (var image in images.Values) UnityEngine.Object.Destroy(image);
            images.Clear(); budget.Clear();
        }
    }
}
