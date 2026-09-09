using System;
using System.Linq;

namespace Wisp.Core
{
    public static class Habitat
    {
        public static string Phase(string caption, string[] captions)
        {
            caption = caption ?? "";
            bool infected = caption.IndexOf("Infected Crossroad", StringComparison.OrdinalIgnoreCase) >= 0;
            bool forgotten = caption.Contains("Forgotten Crossroad");
            if ((infected && forgotten) || caption.Contains("Forgotten/Infected")) return Wisp.Core.I18n.T("До и после заражения");
            if (infected || caption.Contains("after it has become infected")) return Wisp.Core.I18n.T("После заражения");
            // A generic Crossroads caption alone does not establish a pre-infection-only map.
            if (forgotten && (captions ?? new string[0]).Any(c => (c ?? "").Contains("Infected Crossroad"))) return Wisp.Core.I18n.T("До заражения");
            if (forgotten) return Wisp.Core.I18n.T("Перепутье · проверь условия ниже");
            if (caption.Contains("before defeating Broken Vessel")) return Wisp.Core.I18n.T("До победы над Разбитым сосудом");
            return Wisp.Core.I18n.T("Другая область");
        }
        public static string Description(Enemy enemy, IPlayerState player)
        {
            if (string.IsNullOrEmpty(enemy.BeforeInfection) && string.IsNullOrEmpty(enemy.AfterInfection)) return enemy.Warning;
            bool infected;
            string state = player.TryBool("crossroadsInfected", out infected) ? (infected ? Wisp.Core.I18n.T("Сейчас: Перепутье заражено.") : Wisp.Core.I18n.T("Сейчас: Перепутье ещё не заражено.")) : Wisp.Core.I18n.T("Состояние заражения недоступно.");
            return state + Wisp.Core.I18n.T("\nДо заражения: ") + enemy.BeforeInfection + Wisp.Core.I18n.T("\nПосле заражения: ") + enemy.AfterInfection;
        }
    }
}
