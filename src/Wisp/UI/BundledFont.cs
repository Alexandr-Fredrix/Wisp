using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Wisp.UI
{
    internal static class BundledFont
    {
        internal static Font Load()
        {
            Font candidate = null;
            try
            {
                string target = Path.Combine(Application.persistentDataPath, "WispCache", "JetBrainsMonoNerdFont-Regular.ttf");
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                using (var source = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp.ui/JetBrainsMonoNerdFont-Regular.ttf"))
                {
                    if (source == null) throw new InvalidOperationException("Embedded font missing");
                    using (var output = File.Create(target)) source.CopyTo(output);
                }
                // An absolute file path makes Unity load the font data directly.
                // Private GDI registration does not make it discoverable by Unity 6.
                candidate = new Font(target);
                if (Usable(candidate))
                {
                    Debug.Log("[Wisp] JetBrains Mono loaded from bundled file; Latin/Cyrillic glyph checks passed.");
                    return candidate;
                }
                throw new InvalidOperationException("Bundled font failed glyph/texture validation");
            }
            catch (Exception error)
            {
                if (candidate != null) UnityEngine.Object.Destroy(candidate);
                Debug.LogWarning("[Wisp] Bundled font unavailable; using a validated fallback. " + error.Message);
            }
            foreach (string name in new[] { "Consolas", "Arial", "Georgia" })
            {
                candidate = null;
                try
                {
                    candidate = Font.CreateDynamicFontFromOSFont(name, 18);
                    if (Usable(candidate)) { Debug.Log("[Wisp] Fallback font: " + name); return candidate; }
                }
                catch (Exception) { }
                if (candidate != null) UnityEngine.Object.Destroy(candidate);
            }
            // Null lets IMGUI use its own default; never keep a failed font object.
            Debug.LogWarning("[Wisp] Using the default IMGUI font.");
            return null;
        }

        private static bool Usable(Font value)
        {
            if (value == null) return false;
            foreach (int size in new[] { 14, 15, 16, 17, 18, 20, 23, 26 })
            {
                const string sample = "Wisp0123РђР‘РЇР°Р±СЏРЃС‘";
                value.RequestCharactersInTexture(sample, size, FontStyle.Normal);
                foreach (char c in sample)
                {
                    CharacterInfo glyph;
                    if (!value.GetCharacterInfo(c, out glyph, size, FontStyle.Normal) || glyph.advance <= 0) return false;
                }
            }
            return value.material != null && value.material.mainTexture != null;
        }
    }
}
