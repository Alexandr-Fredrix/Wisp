using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using UnityEngine;
using Wisp.Core;
using Wisp.UI;
using Wisp.Game;
namespace Wisp
{
    [BepInPlugin("com.alexandr-fredrix.wisp", "Wisp", "0.4.0")]
    public sealed class WispMod : BaseUnityPlugin
    {
        internal static WispMod Instance;
        public SaveProgress Progress { get; private set; } = new SaveProgress();
        public Preferences Settings { get; private set; } = new Preferences();
        private GuideWindow guide;
        private Harmony patches;
        private int slot;
        private string directory;
        public void LogError(object message) { Logger.LogError(message); }
        private void Awake()
        {
            Instance = this;
            directory = Path.Combine(Application.persistentDataPath, "Wisp");
            Directory.CreateDirectory(directory);
            Settings = Read<Preferences>(Path.Combine(directory, "settings.json")) ?? new Preferences();
            guide = gameObject.AddComponent<GuideWindow>();
            guide.Initialize(this, Catalog.Load());
            patches = new Harmony("com.alexandr-fredrix.wisp");
            patches.PatchAll(typeof(WispMod).Assembly);
            Logger.LogInfo("Wisp 0.4.0-alpha.1 / Unity 6. F8 or both sticks opens the guide. No input backend changes.");
        }
        private T Read<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            try { return JsonConvert.DeserializeObject<T>(File.ReadAllText(path)); }
            catch (Exception e) { Logger.LogWarning("Cannot read Wisp data: " + e.Message); return null; }
        }
        internal void Loaded(int id, bool fresh)
        {
            slot = id;
            Progress = fresh ? new SaveProgress() : Read<SaveProgress>(Path.Combine(directory, "user" + id + ".json"));
            if (Progress == null)
            {
                // Read only Wisp's old section; never rewrite the shared legacy file.
                var legacy = Read<JObject>(Path.Combine(Application.persistentDataPath, "user" + id + ".modded.json"));
                try { Progress = legacy?["modData"]?["Wisp"]?.ToObject<SaveProgress>(); }
                catch (Exception e) { Logger.LogWarning("Legacy Wisp import skipped: " + e.Message); }
            }
            Progress = Progress ?? new SaveProgress();
            Progress.Normalize();
            guide.ResetView();
            Logger.LogInfo("Guide loaded for slot " + id + (fresh ? " (new game)" : ""));
        }
        private void Write(string path, object value)
        {
            try
            {
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonConvert.SerializeObject(value, Formatting.Indented));
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
            }
            catch (Exception e) { Logger.LogError("Cannot save Wisp data: " + e.Message); }
        }
        internal void Saved(int id)
        {
            if (slot == id && id > 0) Write(Path.Combine(directory, "user" + id + ".json"), Progress);
            Write(Path.Combine(directory, "settings.json"), Settings);
        }
        private void OnApplicationQuit() { Write(Path.Combine(directory, "settings.json"), Settings); }
        private void OnDestroy()
        {
            if (guide != null) { guide.Close(); Destroy(guide); }
            patches?.UnpatchSelf();
            if (Instance == this) Instance = null;
        }
    }
    [HarmonyPatch(typeof(GameManager), "LoadGame", new Type[] { typeof(int), typeof(Action<bool>) })]
    internal static class LoadGuide
    {
        private static void Prefix(int __0, ref Action<bool> __1)
        {
            int id = __0; var callback = __1;
            __1 = success => { try { if (success && WispMod.Instance != null) WispMod.Instance.Loaded(id, false); } catch (Exception e) { WispMod.Instance?.LogError(e); } finally { callback?.Invoke(success); } };
        }
    }
    [HarmonyPatch(typeof(GameManager), "SaveGame", new Type[] { typeof(int), typeof(Action<bool>) })]
    internal static class SaveGuide
    {
        private static void Prefix(int __0, ref Action<bool> __1)
        {
            int id = __0; var callback = __1;
            __1 = success => { try { if (success && WispMod.Instance != null) WispMod.Instance.Saved(id); } catch (Exception e) { WispMod.Instance?.LogError(e); } finally { callback?.Invoke(success); } };
        }
    }
    [HarmonyPatch(typeof(GameManager), "StartNewGame", new Type[] { typeof(bool), typeof(bool) })]
    internal static class NewGuide
    {
        private static void Prefix(GameManager __instance) { WispMod.Instance?.Loaded(__instance.profileID, true); }
    }
}
