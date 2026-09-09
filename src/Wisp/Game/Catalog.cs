using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Wisp.Core;

namespace Wisp.Game
{
    public sealed class Catalog
    {
        public Chapter[] Chapters;
        public Chapter[] PdfChapters;
        public Enemy[] Enemies;

        public static Catalog Load()
        {
            var result = new Catalog { Chapters = Read<Chapter[]>("route.json"), PdfChapters = Read<Chapter[]>("route-pdf.json"), Enemies = Read<Enemy[]>("enemies.json") };
            if (result.Chapters.Length == 0 || result.Chapters.SelectMany(x => x.Steps).Any(x => string.IsNullOrEmpty(x.Id)))
                throw new InvalidDataException("Wisp route is empty or invalid");
            return result;
        }

        private static T Read<T>(string name)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp." + name))
            {
                if (stream == null) throw new FileNotFoundException("Missing embedded Wisp content", name);
                using (var reader = new StreamReader(stream))
                    return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
            }
        }

        public string ChapterFor(string zone, string scene)
        {
            zone = NormalizeZone(zone);
            if (scene == "Tutorial_01") return "kings-pass";
            switch (zone)
            {
                case "TOWN": return "dirtmouth";
                case "CROSSROADS": return "crossroads";
                case "GREEN_PATH": return "greenpath";
                case "FUNGAL_WASTES": return "fungal";
                case "CITY": return "city";
                case "MINES": return "crystal";
                case "RESTING_GROUNDS": return "resting";
                case "WATERWAYS": return "waterways";
                case "ABYSS": return "basin";
                case "DEEP_NEST": return "deepnest";
                case "OUTSKIRTS": return "edge";
                case "ROYAL_GARDENS": return "queens-gardens";
                case "WHITE_PALACE": return "white-palace";
                case "GODS_GLORY": return "godhome";
                default: return "";
            }
        }

        public static string RegionFor(string zone, string scene)
        {
            zone = NormalizeZone(zone);
            if (scene == "Tutorial_01") return "King's Pass";
            switch (zone)
            {
                case "TOWN": return "Dirtmouth";
                case "CROSSROADS": return "Forgotten Crossroads";
                case "GREEN_PATH": return "Greenpath";
                case "FUNGAL_WASTES": return "Fungal Wastes";
                case "CITY": return "City of Tears";
                case "MINES": return "Crystal Peak";
                case "RESTING_GROUNDS": return "Resting Grounds";
                case "WATERWAYS": return "Royal Waterways";
                case "ABYSS": return "Ancient Basin";
                case "DEEP_NEST": return "Deepnest";
                case "OUTSKIRTS": return "Kingdom's Edge";
                case "ROYAL_GARDENS": return "Queen's Gardens";
                case "CLIFFS": return "Howling Cliffs";
                case "FOG_CANYON": return "Fog Canyon";
                case "HIVE": return "The Hive";
                case "WHITE_PALACE": return "White Palace";
                default: return "";
            }
        }

        private static string NormalizeZone(string zone)
        {
            switch (zone)
            {
                case "KINGS_PASS": return "CLIFFS";
                case "WASTES": case "QUEENS_STATION": case "MANTIS_VILLAGE": return "FUNGAL_WASTES";
                case "DEEPNEST": case "BEASTS_DEN": case "DISTANT_VILLAGE": case "RUINED_TRAMWAY": return "DEEP_NEST";
                case "KINGS_STATION": case "MAGE_TOWER": case "LURIENS_TOWER": case "LOVE_TOWER": case "ROYAL_QUARTER": return "CITY";
                case "SHAMAN_TEMPLE": case "FINAL_BOSS": return "CROSSROADS";
                case "ABYSS_DEEP": case "PALACE_GROUNDS": return "ABYSS";
                case "ISMAS_GROVE": return "WATERWAYS";
                case "CRYSTAL_MOUND": case "PEAK": return "MINES";
                case "GLADE": return "RESTING_GROUNDS";
                case "COLOSSEUM": return "OUTSKIRTS";
                case "NOEYES_TEMPLE": return "GREEN_PATH";
                case "MONOMON_ARCHIVE": case "OVERGROWN_MOUND": return "FOG_CANYON";
                case "GODSEEKER_WASTE": return "GODS_GLORY";
                default: return zone;
            }
        }
    }
}
