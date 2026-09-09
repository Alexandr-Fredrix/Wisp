using System.Collections.Generic;
using System.Linq;

namespace Wisp.Core
{
    public static class SaveDiscovery
    {
        // Persisted game flags, not an assumption that all earlier route steps are done.
        private static readonly Dictionary<string, string[]> Visits = new Dictionary<string, string[]>
        {
            { "kings-pass", new[] { "visitedDirtmouth" } },
            { "dirtmouth", new[] { "visitedDirtmouth" } },
            { "crossroads", new[] { "visitedCrossroads", "visitedCrossroadsInfected" } },
            { "greenpath", new[] { "visitedGreenpath" } },
            { "fungal", new[] { "visitedFungus" } },
            { "city", new[] { "visitedRuins" } },
            { "crystal", new[] { "visitedMines" } },
            { "resting", new[] { "visitedRestingGrounds" } },
            { "waterways", new[] { "visitedWaterways" } },
            { "basin", new[] { "visitedAbyss" } },
            { "deepnest", new[] { "visitedDeepnest" } },
            { "edge", new[] { "visitedOutskirts" } },
            { "abyss", new[] { "visitedAbyssLower", "visitedWhitePalace", "visitedRoyalGardens" } },
            { "godhome", new[] { "visitedGodhome" } },
            { "queens-gardens", new[] { "visitedRoyalGardens" } },
            { "white-palace", new[] { "visitedWhitePalace" } }
        };

        public static bool HasVisitEvidence(string chapterId, IPlayerState state)
        {
            string[] flags;
            return Visits.TryGetValue(chapterId, out flags) && flags.Any(f => IsTrue(state, f));
        }

        public static bool IsLocation(string chapterId) { return Visits.ContainsKey(chapterId); }

        public static void Import(Chapter[] chapters, SaveProgress progress, IPlayerState state)
        {
            foreach (var chapter in chapters)
            {
                string[] flags;
                bool visited = Visits.TryGetValue(chapter.Id, out flags) && flags.Any(f => IsTrue(state, f));
                if ((visited || chapter.Steps.Any(s => Completion.Confirmed(s, state))) && !progress.VisitedChapters.Contains(chapter.Id))
                    progress.VisitedChapters.Add(chapter.Id);
            }
        }

        private static bool IsTrue(IPlayerState state, string field)
        {
            bool value;
            return state.TryBool(field, out value) && value;
        }
    }
}
