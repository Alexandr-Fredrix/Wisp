using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wisp.Core;

public static class RegressionTests
{
    private sealed class State : IPlayerState
    {
        public readonly HashSet<string> True = new HashSet<string>();
        public bool TryBool(string key, out bool value) { value = True.Contains(key); return true; }
        public bool TryInt(string key, out int value) { value = 0; return false; }
    }
    private static int count;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); count++; }
    public static string Run()
    {
        count = 0;
        var state = new State();
        Check(HabitatMaps.CurrentRegion("Forgotten Crossroads", state) == "Forgotten Crossroads", "Before infection");
        state.True.Add("crossroadsInfected");
        Check(HabitatMaps.CurrentRegion("Forgotten Crossroads", state) == "Infected Crossroads", "After infection");
        Check(HabitatMaps.CurrentRegion("Greenpath", state) == "Greenpath", "Other areas unaffected");
        state.True.Add("visitedRoyalGardens");
        Check(!SaveDiscovery.HasVisitEvidence("abyss", state), "Gardens cannot reveal Abyss");
        Check(SaveDiscovery.HasVisitEvidence("queens-gardens", state), "Gardens still recognized");
        state.True.Add("visitedWhitePalace");
        Check(!SaveDiscovery.HasVisitEvidence("abyss", state), "Palace cannot reveal Abyss");
        state.True.Add("visitedAbyssLower");
        Check(SaveDiscovery.HasVisitEvidence("abyss", state), "Real Abyss visit recognized");
        var maps = new[] {
            new HabitatMap { Url="other", Regions=new[]{"City of Tears"} },
            new HabitatMap { Url="before", Regions=new[]{"Greenpath"}, Phase="before-infection" },
            new HabitatMap { Url="after", Regions=new[]{"Greenpath"}, Phase="after-infection" },
            new HabitatMap { Url="unknown" }
        };
        var ordered = HabitatMaps.Order(maps, "Greenpath", true);
        Check(ordered[0].Url == "after", "Prefer region and phase");
        Check(ordered.Length == 4 && ordered.Any(m=>m.Url=="unknown"), "Unknown maps are retained");
        Check(maps[0].Url == "other", "Ordering does not mutate shared catalog");
        var loads = new ImageLoads();
        Check(loads.TryStart("a") && loads.TryStart("b") && loads.TryStart("c"), "Three requests start");
        Check(!loads.TryStart("d") && loads.State("d") == ImageLoadState.Queued, "Fourth request queues");
        I18n.English = true; loads.Finish("a",true);
        Check(loads.TryStart("d"), "Queued request survives language switch");
        loads.Finish("b",false);
        Check(!loads.TryStart("b"), "Failed request does not retry each frame");
        loads.Retry("b"); Check(loads.TryStart("b"), "Explicit retry starts failed request");
        Check(!loads.TryStart("b"), "No duplicate active request");
        loads.Retry("c"); Check(loads.Active == 3, "Retry cannot cancel another request");
        loads.Finish("b",true); loads.Finish("c",false); loads.Finish("d",true);
        Check(loads.Active == 0, "All request slots released");
        loads.Evicted("a"); Check(loads.TryStart("a"), "Evicted asset can be obtained again");
        I18n.English = false;
        var budget = new ImageBudget(100,3);
        budget.Admit("a",40);budget.Admit("b",40);budget.Touch("a");
        Check(budget.Admit("c",40).SequenceEqual(new[]{"b"}), "LRU preserves recently viewed image");
        Check(budget.Bytes == 80, "Bytes counted after eviction");
        Check(!budget.Fits(101), "Oversized image rejected");
        var countBudget = new ImageBudget(1000,2);
        countBudget.Admit("a",1);countBudget.Admit("b",1);
        Check(countBudget.Admit("c",1).SequenceEqual(new[]{"a"}), "Count cap also enforced");
        budget.Clear();Check(budget.Bytes == 0, "Disposal releases budget");
        var progress = new SaveProgress { ChapterId="c", StepId="last", Completed=new List<string>{"mark"}, RouteGoal="steel" };
        int chapter, step;
        Check(RouteView.Restore(new[]{new Chapter { Id="c",Steps=new[]{new Step{Id="first"},new Step{Id="last"}}}},progress,out chapter,out step) && step==1, "Restore last selected task");
        Check(!RouteView.Restore(new[]{new Chapter{Id="other"}},progress,out chapter,out step), "Unknown position uses auto location");
        Check(progress.Completed.SequenceEqual(new[]{"mark"}), "Position lookup preserves marks");
        string directory=Path.Combine(Path.GetTempPath(),"WispTests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var store=new ProgressStore(s=> { if(!s.StartsWith("valid:"))throw new InvalidDataException();return new SaveProgress{Completed=new List<string>{s.Substring(6)}}; }, p=>"valid:"+p.Completed[0]);
            string path=Path.Combine(directory,"user1.json"),legacy=Path.Combine(directory,"legacy.json");
            Func<string,SaveProgress> legacyDecode=s=>s=="legacy"?new SaveProgress{Completed=new List<string>{"legacy-mark"}}:null;
            File.WriteAllText(path,"damaged");File.WriteAllText(path+".bak","valid:backup-mark");
            var read=store.Load(path,legacy,legacyDecode,false);
            Check(read.Recovered && read.WriteAllowed && read.Data.Completed[0]=="backup-mark","Recover backup");
            store.Save(path,read);
            Check(File.ReadAllText(path)=="valid:backup-mark" && File.ReadAllText(path+".bak")=="valid:backup-mark","Keep good backup on recovery write");
            Check(Directory.GetFiles(directory,"*.corrupt-*").Length==1,"Preserve damaged original");
            File.WriteAllText(path,"broken again");File.WriteAllText(path+".bak","broken backup");
            read=store.Load(path,legacy,legacyDecode,false);
            Check(!read.WriteAllowed,"Block destructive empty recovery");
            bool rejected=false;try{store.Save(path,read);}catch(InvalidOperationException){rejected=true;}
            Check(rejected && File.ReadAllText(path)=="broken again","Blocked write leaves original intact");
            File.WriteAllText(legacy,"legacy");read=store.Load(path,legacy,legacyDecode,false);
            Check(read.Data.Completed[0]=="legacy-mark" && read.WriteAllowed,"Legacy fallback after both files fail");
            string second=Path.Combine(directory,"user2.json");
            var independent=store.Load(second,Path.Combine(directory,"none"),legacyDecode,false);
            Check(independent.WriteAllowed && independent.Data.Completed.Count==0,"New slot does not inherit marks");
            read=store.Load(path,legacy,legacyDecode,true);
            Check(read.WriteAllowed && read.Data.Completed.Count==0,"Explicit new game ignores old slot data");
        }
        finally { Directory.Delete(directory,true); }
        return count+" regression assertions passed";
    }
}
