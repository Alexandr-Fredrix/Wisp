using System.Collections.Generic;

namespace Wisp.Core
{
    public interface IAchievementState
    {
        bool IsUnlocked(string key);
    }

    public static class ProfileAchievements
    {
        // Only exact achievement objectives, never travel or preparation steps.
        public static readonly Dictionary<string, string> Steps = new Dictionary<string, string> {
            { "first-charm", "CHARMED" }, { "false-knight", "FK_DEFEAT" },
            { "hornet-greenpath", "HORNET_1" }, { "mantis-lords", "MANTIS_LORDS" },
            { "soul-master", "SOUL_MASTER_DEFEAT" }, { "dung-defender", "DUNG_DEFENDER" },
            { "broken-vessel", "BROKEN_VESSEL" }, { "lost-kin", "DREAM_BROKEN_VESSEL" },
            { "herrah", "BEAST" }, { "monomon", "TEACHER" }, { "lurien", "WATCHER" },
            { "quirrel", "QUIRREL_EPILOGUE" }, { "ending-a", "ENDING_A" },
            { "ending-b", "ENDING_B" }, { "ending-c", "ENDING_C" },
            { "hornet-sentinel", "HORNET_2" }, { "trial-one", "COLOSSEUM_1" },
            { "trial-two", "COLOSSEUM_2" }, { "trial-three", "COLOSSEUM_3" },
            { "zote-arena", "ZOTE" }, { "neglect", "NEGLECT" }, { "traitor", "TRAITOR_LORD" },
            { "essence-600", "ATTUNEMENT" }, { "awoken", "AWAKENING" },
            { "delicate-flower", "MOURNER" }, { "smith-spare", "NAILSMITH_SPARE" },
            { "smith-kill", "NAILSMITH_KILL" }, { "grimm-first", "GRIMM" },
            { "grimm-ritual", "NIGHTMARE_GRIMM" }, { "grimm-banish", "BANISHMENT" },
            { "failed-champion", "DREAM_FK" }, { "soul-tyrant", "DREAM_SOUL_MASTER_DEFEAT" },
            { "white-defender", "WHITE_DEFENDER" }, { "grey-prince", "GREY_PRINCE" },
            { "seer-final", "ASCENSION" }, { "half-charms", "ENCHANTED" },
            { "all-charms", "BLESSED" }, { "four-shards", "PROTECTED" },
            { "all-shards", "MASKED" }, { "three-vessels", "SOULFUL" },
            { "all-vessels", "WORLDSOUL" }, { "half-grubs", "GRUBFRIEND" },
            { "all-grubs", "METAMORPHOSIS" }, { "collector", "COLLECTOR" },
            { "half-stations", "STAG_STATION_HALF" }, { "all-stations", "STAG_STATION_ALL" },
            { "all-maps", "MAP" }, { "hunter-record", "HUNTER_1" }, { "hunter-mark", "HUNTER_2" },
            { "mushroom", "MR_MUSHROOM" }, { "complete-100", "COMPLETION" },
            { "complete-112", "COMPLETIONGG" }, { "pantheon-1", "PANTHEON1" },
            { "pantheon-2", "PANTHEON2" }, { "pantheon-3", "PANTHEON3" },
            { "pantheon-4", "PANTHEON4" }, { "pantheon-5", "ENDINGD" },
            { "steel-ending", "STEELSOUL" }, { "steel-100", "STEELSOUL_COMPLETION" },
            { "speed-10", "SPEEDRUN_1" }, { "speed-5", "SPEEDRUN_2" }, { "speed-100", "SPEED_COMPLETION" }
        };

        public static bool Confirmed(Step step, IAchievementState profile)
        {
            string key;
            return Steps.TryGetValue(step.Id, out key) && profile.IsUnlocked(key);
        }
    }
}
