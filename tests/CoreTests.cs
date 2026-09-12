using System;
using System.Collections.Generic;
using Wisp.Core;

public static class CoreTests
{
    private sealed class State : IPlayerState, IAchievementState
    {
        public Dictionary<string, bool> Bools = new Dictionary<string, bool>();
        public Dictionary<string, int> Ints = new Dictionary<string, int>();
        public HashSet<string> Achievements = new HashSet<string>();
        public bool IsUnlocked(string key) { return Achievements.Contains(key); }
        public bool TryBool(string name, out bool value) { return Bools.TryGetValue(name, out value); }
        public bool TryInt(string name, out int value) { return Ints.TryGetValue(name, out value); }
    }
    private static int count;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        count++;
    }
    public static string Run()
    {
        count = 0;
        var state = new State();
        var step = new Step { Conditions = new[] { new Condition { Field = "boss" }, new Condition { Field = "spell", Kind = "int", Minimum = 1 } } };
        Check(!Completion.Confirmed(step, state), "Missing fields cannot complete a step");
        state.Bools["boss"] = true;
        state.Ints["spell"] = 0;
        Check(!Completion.Confirmed(step, state), "A defeated boss alone cannot confirm the spell reward");
        state.Ints["spell"] = 2;
        Check(Completion.Confirmed(step, state), "Upgraded spell must satisfy base-spell requirement");
        Check(!Completion.Confirmed(new Step(), state), "Manual tasks must not autocomplete");
        var profile = new State();
        var ending = new Step { Id = "ending-a" };
        Check(!ProfileAchievements.Confirmed(ending, profile), "Unavailable or locked profile achievements cannot confirm an ending");
        profile.Achievements.Add("ENDING_A");
        Check(ProfileAchievements.Confirmed(ending, profile), "An ending earned in another save is recognized from the game profile");
        Check(!Completion.Confirmed(ending, profile), "Profile completion is not current-save completion");
        Check(!ProfileAchievements.Confirmed(new Step { Id = "resume-save" }, profile), "An ending achievement cannot prove returning after credits");
        Check(!ProfileAchievements.Confirmed(new Step { Id = "ending-b" }, profile), "One ending cannot confirm another ending");
        profile.Achievements.Add("STEELSOUL");
        Check(!ProfileAchievements.Confirmed(new Step { Id = "steel-start" }, profile), "A profile achievement cannot change a save's mode");
        var profileImport = new SaveProgress();
        SaveDiscovery.Import(new[] { new Chapter { Id = "first-ending", Steps = new[] { ending } } }, profileImport, profile);
        Check(profileImport.Completed.Count == 0 && profileImport.VisitedChapters.Count == 0, "Profile achievements cannot fabricate save marks or visited areas");
        Check(!SaveDiscovery.IsLocation("first-ending") && SaveDiscovery.IsLocation("city"), "Only real locations receive exploration markers");
        Check(!Completion.Confirmed(new Step { Conditions = new[] { new Condition { Kind = "unknown" } } }, state), "Unknown rule type must fail closed");

        var first = new SaveProgress();
        var second = new SaveProgress();
        first.Mark("boss", true);
        first.Mark("boss", true);
        Check(first.Completed.Count == 1, "Repeated marks must be idempotent");
        Check(second.Completed.Count == 0, "Save slots must not share lists");
        first.Mark("boss", false);
        Check(first.Completed.Count == 0, "Unmark must be reversible");
        var loaded = new SaveProgress { Completed = new List<string> { "a", "a", null, "" }, VisitedChapters = null, ChapterId = null };
        loaded.Normalize();
        Check(loaded.Completed.Count == 1 && loaded.VisitedChapters.Count == 0 && loaded.ChapterId == "kings-pass", "Null/duplicate persisted data must migrate");

        var enemy = new Enemy { JournalKey = "Bouncer" };
        Check(!JournalStatus.Read(enemy, state).Available, "Missing counter must not appear as zero remaining");
        state.Bools["killedBouncer"] = false;
        state.Ints["killsBouncer"] = 0;
        Check(!JournalStatus.Read(enemy, state).Complete, "Zero alone cannot confirm an undiscovered entry");
        state.Bools["killedBouncer"] = true;
        state.Ints["killsBouncer"] = 17;
        Check(JournalStatus.Read(enemy, state).Remaining == 17 && !JournalStatus.Read(enemy, state).Complete, "Game counter is kills REMAINING");
        state.Ints["killsBouncer"] = -1;
        Check(JournalStatus.Read(enemy, state).Complete && JournalStatus.Read(enemy, state).Remaining == 0, "Completed counters are clamped");
        var chapters = new[] {
            new Chapter { Id = "city", Steps = new[] { new Step { Id = "unrecorded" } } },
            new Chapter { Id = "greenpath", Steps = new[] { step } },
            new Chapter { Id = "deepnest" }
        };
        state.Bools["visitedRuins"] = true;
        var imported = new SaveProgress();
        SaveDiscovery.Import(chapters, imported, state);
        Check(imported.VisitedChapters.Contains("city"), "An existing save restores visited areas without Wisp history");
        Check(imported.VisitedChapters.Contains("greenpath"), "Confirmed milestones reveal their own chapter");
        Check(SaveDiscovery.HasVisitEvidence("city", state), "A persisted visit flag confirms actual exploration");
        Check(!SaveDiscovery.HasVisitEvidence("greenpath", state), "A revealed chapter with a completed task is not proof of a visit");
        Check(!SaveDiscovery.HasVisitEvidence("city", new State()), "Visit evidence must not leak into another save");
        Check(!imported.VisitedChapters.Contains("deepnest"), "Unknown visit flags cannot reveal an area");
        Check(imported.Completed.Count == 0, "Import must not fabricate manual task completion");
        SaveDiscovery.Import(chapters, imported, state);
        Check(imported.VisitedChapters.Count == 2, "Repeated imports must be idempotent");
        var emptySave = new SaveProgress();
        SaveDiscovery.Import(chapters, emptySave, new State());
        Check(emptySave.VisitedChapters.Count == 0, "Another save cannot inherit discovery");
        var goalSave = new SaveProgress { RouteGoal = "steel", Completed = new List<string> { "manual" } };
        goalSave.Normalize();
        Check(goalSave.RouteGoal == "steel" && goalSave.Completed.Contains("manual"), "Goal survives normalization without dropping manual progress");
        Check(new SaveProgress().RouteGoal == "112", "Another slot defaults independently to full completion");
        goalSave.RouteGoal = "invalid"; goalSave.Normalize();
        Check(goalSave.RouteGoal == "112", "Unknown goals migrate safely");
        Check(!RouteGoals.IsSteelSave(state), "Selecting a goal cannot invent Steel Soul mode");
        state.Ints["permadeathMode"] = 1;
        Check(RouteGoals.IsSteelSave(state), "Read actual game mode");
        state.Ints["permadeathMode"] = 2;
        Check(!RouteGoals.IsSteelSave(state), "A dead Steel Soul save is not an active run");
        var captions = new[] { "Locations in the Forgotten Crossroads", "Location in the Infected Crossroads" };
        Check(Habitat.Phase(captions[0], captions) == "До заражения", "A paired original map is identified as pre-infection");
        Check(Habitat.Phase(captions[1], captions) == "После заражения", "An infected map is identified separately");
        Check(Habitat.Phase("Forgotten/Infected Crossroads", captions) == "До и после заражения", "Shared maps do not invent disappearance");
        Check(Habitat.Phase(captions[0], new[] { captions[0] }).Contains("проверь"), "Unqualified maps do not claim a lifecycle they cannot establish");
        var habitatEnemy = new Enemy { BeforeInfection = "раннее место", AfterInfection = "позднее место" };
        Check(Habitat.Description(habitatEnemy, state).Contains("недоступно"), "Unknown world state is not interpreted as pre-infection");
        state.Bools["crossroadsInfected"] = true;
        Check(Habitat.Description(habitatEnemy, state).Contains("Сейчас: Перепутье заражено."), "Read infection from the current save");
        Check(Habitat.Description(habitatEnemy, state).Contains("раннее место") && Habitat.Description(habitatEnemy, state).Contains("позднее место"), "Both conditions remain available for comparison");
        return count + " core assertions passed";
    }
}
