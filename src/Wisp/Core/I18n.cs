using System.Collections.Generic;

namespace Wisp.Core
{
    public static class I18n
    {
        public static bool English;
        public static Dictionary<string, string> Translations = new Dictionary<string, string>();
        public static string T(string text)
        {
            string translated;
            return English && text != null && Translations.TryGetValue(text, out translated) ? translated : text;
        }
    }
}
