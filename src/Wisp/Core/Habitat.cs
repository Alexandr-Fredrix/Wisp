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
            if ((infected && forgotten) || caption.Contains("Forgotten/Infected")) return "До и после заражения";
            if (infected || caption.Contains("after it has become infected")) return "После заражения";
            // A generic Crossroads caption alone does not establish a pre-infection-only map.
            if (forgotten && (captions ?? new string[0]).Any(c => (c ?? "").Contains("Infected Crossroad"))) return "До заражения";
            if (forgotten) return "Перепутье · проверь условия ниже";
            if (caption.Contains("before defeating Broken Vessel")) return "До победы над Разбитым сосудом";
            return "Другая область";
        }
        public static string Description(Enemy enemy, IPlayerState player)
        {
            if (string.IsNullOrEmpty(enemy.BeforeInfection) && string.IsNullOrEmpty(enemy.AfterInfection)) return enemy.Warning;
            bool infected;
            string state = player.TryBool("crossroadsInfected", out infected) ? (infected ? "Сейчас: Перепутье заражено." : "Сейчас: Перепутье ещё не заражено.") : "Состояние заражения недоступно.";
            return state + "\nДо заражения: " + enemy.BeforeInfection + "\nПосле заражения: " + enemy.AfterInfection;
        }
    }
}
