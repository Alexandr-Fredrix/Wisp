using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Wisp.UI
{
    // Private to this process: no system-wide font installation or registry changes.
    internal static class BundledFont
    {
        private static string path;
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern int AddFontResourceEx(string name, uint flags, IntPtr reserved);
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool RemoveFontResourceEx(string name, uint flags, IntPtr reserved);

        internal static Font Load()
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer && path == null)
            {
                string target = Path.Combine(Application.persistentDataPath, "WispCache", "JetBrainsMonoNerdFont-Regular.ttf");
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                using (var source = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp.ui/JetBrainsMonoNerdFont-Regular.ttf"))
                {
                    if (source == null) throw new InvalidOperationException("Embedded JetBrains font missing");
                    using (var output = File.Create(target)) source.CopyTo(output);
                }
                if (AddFontResourceEx(target, 0x10, IntPtr.Zero) == 0) throw new InvalidOperationException("Private font registration failed");
                path = target;
            }
            return Font.CreateDynamicFontFromOSFont(new[] { "JetBrainsMono NF", "JetBrainsMono Nerd Font", "JetBrains Mono", "Consolas", "Arial" }, 18);
        }
        internal static void Release()
        {
            if (path == null) return;
            RemoveFontResourceEx(path, 0x10, IntPtr.Zero);
            path = null;
        }
    }
}
