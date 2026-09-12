using System;
using System.Linq;

namespace Wisp.Core
{
    public static class RouteView
    {
        public static Step[] VisibleSteps(Chapter chapter)
        { return chapter.Steps.Where(s => !s.ReferenceOnly || s.Id.EndsWith("-guide", StringComparison.Ordinal)).ToArray(); }
        public static bool Restore(Chapter[] chapters, SaveProgress progress, out int chapter, out int step)
        {
            chapter = Array.FindIndex(chapters, c => c.Id == progress.ChapterId);
            step = chapter < 0 ? -1 : Array.FindIndex(VisibleSteps(chapters[chapter]), s => s.Id == progress.StepId);
            bool valid = chapter >= 0 && step >= 0;
            chapter = Math.Max(0, chapter); step = Math.Max(0, step);
            return valid;
        }
    }
}
