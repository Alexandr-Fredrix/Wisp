using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wisp.Core;

namespace Wisp.UI
{
    public sealed partial class GuideWindow
    {
        private void DrawImageStatus(Rect rect, string url)
        {
            if (media.CanRetry(url)) { if (GUI.Button(rect, media.Status(url), button)) media.Retry(url); }
            else GUI.Label(rect, media.Status(url), small);
        }
        private bool RetryFocusedImages()
        {
            var urls = new List<string>();
            if (tab == 4) { var item = CurrentCollectionItem; if (item != null) { urls.Add(item.Icon); if (item.Maps.Length > 0) urls.Add(item.Maps[CollectionMapIndex].Url); } }
            else if (tab == 3) urls.Add(media.RegionSource(atlasIds[atlasIndex]));
            else if (tab == 1)
            {
                var enemies = FilteredEnemies();
                if (enemies.Length == 0) return false;
                var entry = MediaFor(enemies[Mathf.Clamp(enemyIndex, 0, enemies.Length - 1)]);
                if (entry == null) return false;
                if (padPane == 1) urls.Add(entry.Portrait);
                else if (padPane == 2 && entry.HabitatMaps.Length > 0)
                    urls.Add(entry.HabitatMaps[(enemyMapIndex % entry.HabitatMaps.Length + entry.HabitatMaps.Length) % entry.HabitatMaps.Length].Url);
            }
            else if (tab == 0)
            {
                if (mapTab && !expandedStepImage) urls.Add(media.RegionSource(CurrentMapId));
                else
                {
                    var step = RouteSteps[Mathf.Clamp(stepIndex, 0, RouteSteps.Length - 1)];
                    if (step.Spoiler && !mod.Settings.ShowSpoilers) return false;
                    StepImage[] targets;
                    if (media.TryStepImages(Current.Goal, Current.Id, step.Id, out targets) && targets.Length > 0)
                    {
                        if (expandedStepImage || targets.Any(t => t.Wide)) urls.Add(targets[(stepImageIndex % targets.Length + targets.Length) % targets.Length].Url);
                        else urls.AddRange(targets.Select(t => t.Url));
                    }
                    string achievement;
                    if (!expandedStepImage && ProfileAchievements.Steps.TryGetValue(step.Id, out achievement)) urls.Add("embedded:achievements/" + achievement + ".jpg");
                }
            }
            bool retried = false;
            foreach (var url in urls) if (media.CanRetry(url)) { media.Retry(url); retried = true; }
            return retried;
        }

        private Step descriptionStep;
        private bool descriptionEnglish, descriptionSpoilers;
        private string descriptionGoal;
        private string[] descriptionParagraphs;
        private float[] descriptionHeights;
        private float descriptionTitleHeight, descriptionBodyHeight;
        private void PrepareDescription(Step step)
        {
            if (descriptionStep == step && descriptionEnglish == I18n.English && descriptionSpoilers == mod.Settings.ShowSpoilers && descriptionGoal == mod.Progress.RouteGoal) return;
            descriptionStep = step; descriptionEnglish = I18n.English; descriptionSpoilers = mod.Settings.ShowSpoilers; descriptionGoal = mod.Progress.RouteGoal;
            string body = step.Spoiler && !mod.Settings.ShowSpoilers ? I18n.T("Описание содержит сюжетные спойлеры. Их можно включить в настройках.") : step.Body;
            if (step.Warning.Length > 0) body = I18n.T("ВАЖНО\n") + step.Warning + "\n\n" + body;
            if (mod.Progress.RouteGoal == "steel") body += I18n.T("\n\nСтальная душа: цель C — 100% без смерти и без ограничения времени. Проверяй набор на 100% в отдельном этапе.");
            body = System.Text.RegularExpressions.Regex.Replace(body, @"\s*Контекст этого этапа полностью приведён.*?PDF, стр\. \d+\.", "");
            descriptionParagraphs = body.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            descriptionHeights = descriptionParagraphs.Select(p => text.CalcHeight(new GUIContent(p), 564 * ContentScale)).ToArray();
            descriptionTitleHeight = heading.CalcHeight(new GUIContent(step.Title), 584 * ContentScale);
            descriptionBodyHeight = descriptionHeights.Sum(h => h + 18);
        }
    }
}
