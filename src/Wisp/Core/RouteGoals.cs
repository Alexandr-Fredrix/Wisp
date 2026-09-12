using System.Collections.Generic;

namespace Wisp.Core
{
    public static class RouteGoals
    {
        public static bool IsSteelSave(IPlayerState player)
        {
            int mode;
            return player.TryInt("permadeathMode", out mode) && mode == 1;
        }
    }
}
