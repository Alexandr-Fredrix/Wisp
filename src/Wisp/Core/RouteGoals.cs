using System.Collections.Generic;

namespace Wisp.Core
{
    public static class RouteGoals
    {
        // A conservative no-skip route to the first ending, not a speedrun record route.
        private static readonly HashSet<string> Steel = new HashSet<string> {
            "movement", "reach-town", "well", "sly", "map-tools", "cornifer", "station",
            "false-knight", "vengeful-spirit", "enter-greenpath", "hornet-greenpath",
            "mantis-claw", "city-gate", "nail-upgrade", "soul-master", "city-transport",
            "lantern", "crystal-heart", "descend-resting", "dream-nail", "waterways-key",
            "dung-defender", "ismas-tear", "basin-bench", "broken-vessel", "herrah",
            "monomon", "lurien", "ending-a"
        };

        public static bool Includes(string goal, Step step) { return goal != "steel" || Steel.Contains(step.Id); }
        public static bool IsSteelSave(IPlayerState player)
        {
            int mode;
            return player.TryInt("permadeathMode", out mode) && mode == 1;
        }
    }
}
