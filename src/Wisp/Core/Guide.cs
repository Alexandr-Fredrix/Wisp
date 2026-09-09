using System;
using System.Collections.Generic;
using System.Linq;

namespace Wisp.Core
{
    public sealed class Chapter
    {
        public string Id = "";
        public string Title = "";
        public string English = "";
        public string Summary = "";
        public string Goal = "";
        public Step[] Steps = new Step[0];
    }

    public sealed class Step
    {
        public string Id = "";
        public string Title = "";
        public string Body = "";
        public string Warning = "";
        public string Source = "";
        public string MapChapter = "";
        public bool ReferenceOnly;
        public bool Spoiler;
        public Condition[] Conditions = new Condition[0];
    }

    public sealed class Condition
    {
        public string Field = "";
        public string Kind = "bool";
        public int Minimum = 1;
    }

    public interface IPlayerState
    {
        bool TryBool(string field, out bool value);
        bool TryInt(string field, out int value);
    }

    public static class Completion
    {
        public static bool Confirmed(Step step, IPlayerState player)
        {
            if (step.Conditions == null || step.Conditions.Length == 0) return false;
            foreach (var condition in step.Conditions)
            {
                bool flag;
                int count;
                if (condition.Kind == "bool")
                {
                    if (!player.TryBool(condition.Field, out flag) || !flag) return false;
                }
                else if (condition.Kind == "int")
                {
                    if (!player.TryInt(condition.Field, out count) || count < condition.Minimum) return false;
                }
                else return false;
            }
            return true;
        }
    }

    public sealed class SaveProgress
    {
        public int Schema = 1;
        public List<string> Completed = new List<string>();
        public List<string> VisitedChapters = new List<string>();
        public string ChapterId = "kings-pass";
        public string StepId = "movement";
        public string RouteGoal = "112";

        public void Normalize()
        {
            Completed = (Completed ?? new List<string>()).Where(x => !string.IsNullOrEmpty(x)).Distinct().Take(10000).ToList();
            VisitedChapters = (VisitedChapters ?? new List<string>()).Where(x => !string.IsNullOrEmpty(x)).Distinct().Take(1000).ToList();
            ChapterId = ChapterId ?? "kings-pass";
            StepId = StepId ?? "movement";
            if (RouteGoal != "112" && RouteGoal != "steel" && RouteGoal != "speed") RouteGoal = "112";
        }

        public void Mark(string id, bool done)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (done && !Completed.Contains(id)) Completed.Add(id);
            if (!done) Completed.RemoveAll(x => x == id);
        }
    }

    public sealed class Preferences
    {
        public string Language = "ru";
        public bool ShowSpoilers;
        public bool HideCompleted;
        public bool ShowPauseHint = true;
        public bool ShowHud = true;
    }

    public sealed class Enemy
    {
        public string Id = "";
        public string Name = "";
        public string JournalKey = "";
        public string[] Regions = new string[0];
        public string Source = "";
        public string Warning = "";
        public string BeforeInfection = "";
        public string AfterInfection = "";
        public bool Optional;
    }

    public sealed class JournalStatus
    {
        public bool Available;
        public bool Discovered;
        public int Remaining;
        public bool Complete { get { return Available && Discovered && Remaining <= 0; } }

        public static JournalStatus Read(Enemy enemy, IPlayerState player)
        {
            bool discovered;
            int remaining;
            if (string.IsNullOrEmpty(enemy.JournalKey) ||
                !player.TryBool("killed" + enemy.JournalKey, out discovered) ||
                !player.TryInt("kills" + enemy.JournalKey, out remaining)) return new JournalStatus();
            return new JournalStatus { Available = true, Discovered = discovered, Remaining = Math.Max(0, remaining) };
        }
    }
}
