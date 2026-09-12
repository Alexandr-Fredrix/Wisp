using System;
using System.Linq;
namespace Wisp.Core
{
    public sealed class HabitatMap
    {
        public string Url = "";
        public string Caption = "";
        public string[] Regions = new string[0];
        public string Phase = "unknown";
    }
    public static class HabitatMaps
    {
        public static string CurrentRegion(string region, IPlayerState player)
        {
            bool infected;
            return region == "Forgotten Crossroads" && player.TryBool("crossroadsInfected", out infected) && infected ? "Infected Crossroads" : region;
        }
        public static HabitatMap[] Order(HabitatMap[] maps, string region, bool? infected)
        { return maps.OrderBy(m => !m.Regions.Contains(region)).ThenBy(m => PhaseRank(m, infected)).ToArray(); }
        private static int PhaseRank(HabitatMap map, bool? infected)
        {
            if (!infected.HasValue || map.Phase == "both") return 0;
            if (map.Phase == (infected.Value ? "after-infection" : "before-infection")) return 0;
            return map.Phase == "unknown" ? 1 : 2;
        }
    }
}
