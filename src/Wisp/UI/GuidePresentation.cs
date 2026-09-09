using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Wisp.Core;

namespace Wisp.UI
{
    public sealed partial class GuideWindow
    {
        private Texture2D frameTexture, dividerTexture;
        private GUIStyle small, centered;
        private MediaLibrary media;
        private int enemyIndex, enemyMapIndex;
        private string journalRegion = "";
        private bool fullReferenceMap = true;
        private bool expandedEnemyMap;
        private Vector2 habitatScroll;

        private string HabitatLabel(EnemyMedia entry, int index)
        {
            if (entry == null || index >= entry.MapCaptions.Length || string.IsNullOrEmpty(entry.MapCaptions[index])) return "Место обитания";
            return Habitat.Phase(entry.MapCaptions[index], entry.MapCaptions);
        }
        private const float CanvasWidth = 1280, CanvasHeight = 800;

        private void Styles()
        {
            if (text != null) return;
            try { font = BundledFont.Load(); }
            catch (Exception error) { mod.LogError("Wisp font: " + error); font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Arial" }, 18); }
            panelTexture = Solid(new Color(.025f, .032f, .047f, 1f));
            buttonTexture = Solid(Color.clear);
            activeTexture = new Texture2D(128, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 128; x++)
            {
                float fade = Mathf.Sin(Mathf.PI * x / 127f) * Mathf.Sin(Mathf.PI * y / 31f);
                activeTexture.SetPixel(x, y, new Color(.5f, .65f, .85f, fade * .16f));
            }
            activeTexture.Apply();
            text = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, wordWrap = true, richText = false, clipping = TextClipping.Clip, padding = new RectOffset(4, 4, 4, 4) };
            text.normal.textColor = new Color(.86f, .88f, .91f);
            heading = new GUIStyle(text) { fontSize = 23, fontStyle = FontStyle.Normal };
            muted = new GUIStyle(text) { fontSize = 16 };
            muted.normal.textColor = new Color(.58f, .66f, .75f);
            small = new GUIStyle(muted) { fontSize = 14 };
            centered = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(text) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(18, 12, 6, 6), fontSize = 18 };
            button.normal.background = buttonTexture;
            button.hover.background = activeTexture; button.hover.textColor = Color.white;
            button.active.background = activeTexture; button.focused.background = activeTexture;
            active = new GUIStyle(button); active.normal.background = activeTexture; active.normal.textColor = Color.white;
            frameTexture = Embedded("ui/frame.png"); dividerTexture = Embedded("ui/divider.png");
        }

        private static Texture2D Embedded(string name)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp." + name))
            {
                if (stream == null) return null;
                using (var buffer = new MemoryStream())
                { stream.CopyTo(buffer); var texture = new Texture2D(2, 2); ImageConversion.LoadImage(texture, buffer.ToArray(), true); return texture; }
            }
        }

        private static Texture2D Solid(Color color)
        { var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, color); texture.Apply(); return texture; }

        // The open item has an underline; only controller focus gets silver brackets.
        private bool Choose(Rect rect, string label, bool selected = false, bool focused = false)
        {
            bool clicked = GUI.Button(rect, label, focused ? active : button);
            if (selected) Rule(new Rect(rect.x + 18, rect.yMax - 5, rect.width - 36, 1), new Color(.34f, .43f, .52f));
            if (focused) FocusCorners(rect);
            return clicked;
        }

        private static void Rule(Rect rect, Color tint)
        {
            var old = GUI.color; GUI.color = tint;
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }

        private static void FocusCorners(Rect rect)
        {
            var silver = new Color(.82f, .9f, .96f);
            foreach (float x in new[] { rect.x, rect.xMax - 1 })
            {
                Rule(new Rect(x, rect.y + 6, 1, 12), silver);
                Rule(new Rect(x, rect.yMax - 18, 1, 12), silver);
            }
            foreach (float y in new[] { rect.y + 6, rect.yMax - 7 })
            {
                Rule(new Rect(rect.x, y, 10, 1), silver);
                Rule(new Rect(rect.xMax - 10, y, 10, 1), silver);
            }
        }

        private void KeyHint(Rect rect, string key)
        {
            GUI.Label(rect, key, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            Rule(new Rect(rect.x + 5, rect.yMax - 2, rect.width - 10, 1), new Color(.38f, .48f, .58f));
        }

        private void ChangeTab(int target)
        {
            primaryTab = target; tab = Math.Min(target, 2); padPane = 0; expandedEnemyMap = false; contentFocus = false;
            detailChoice = mapTab ? 1 : 0; detailScroll = Vector2.zero;
        }

        private void OpenRegionJournal()
        {
            var location = catalog.Chapters.FirstOrDefault(c => c.Id == CurrentMapId);
            journalRegion = location == null ? liveRegion : location.English.Replace('’', '\''); allRegions = false;
            enemyIndex = 0; enemyScroll = Vector2.zero; ChangeTab(1);
        }

        private string NavigationHint()
        {
            if (contentFocus) return (tab == 1 || mapTab) ? "Стики: карта · LT/RT: масштаб · Y: вписать · B: к вкладкам" : "↑↓ / RS: текст · B: к вкладкам";
            if (tab == 2) return "↑↓: настройка · ←→ / LT/RT: цель · A: изменить · B: назад";
            if (tab == 1) return padPane == 0 ? "↑↓: враг · → / A: карты · X: фильтр · B: закрыть" : "←→ / LT/RT: тип карты · A: управлять · Y: другое место · B: к врагам";
            if (padPane == 0) return "↑↓: область · A / →: шаги · B: закрыть · ?: посещение не подтверждено";
            if (padPane == 1) return "↑↓: шаг · A / →: вкладки · Y: отметка · B / ←: области";
            return "←→ / LT/RT: вкладка · A: открыть · X: карта · B: к шагам";
        }

        private void Divider(Rect rect)
        {
            if (dividerTexture == null) return;
            // The generated texture includes transparent margins; only sample its central band.
            GUI.DrawTextureWithTexCoords(rect, dividerTexture, new Rect(0, .31f, 1, .38f));
        }

        private void OnGUI()
        {
            if (mod == null || !InSession) return;
            Styles();
            var matrix = GUI.matrix; int depth = GUI.depth; bool enabled = GUI.enabled; var color = GUI.color;
            float scale = Mathf.Min(Screen.width / (CanvasWidth + 24), Screen.height / (CanvasHeight + 24));
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - CanvasWidth * scale) / 2, (Screen.height - CanvasHeight * scale) / 2), Quaternion.identity, new Vector3(scale, scale, 1));
            try
            {
                if (!open)
                {
                    if (Paused && mod.Settings.ShowPauseHint) GUI.Label(new Rect(40, 752, 900, 28), "WISP · F8 / оба стика — проводник", muted);
                    else if (!Paused && mod.Settings.ShowHud) DrawHud(CanvasWidth);
                    return;
                }
                GUI.depth = -1000;
                // Uniform screen-wide backing eliminates the exposed rectangular panel behind the frame.
                var backdropMatrix = GUI.matrix;
                GUI.matrix = Matrix4x4.identity;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), panelTexture);
                GUI.matrix = backdropMatrix;
                Border(new Rect(36, 26, 1208, 748), new Color(.36f, .46f, .57f));
                Divider(new Rect(528, 28, 224, 12));
                // Every section has its own fixed viewport; no child can widen its parent.
                GUI.Label(new Rect(112, 57, 870, 38), "WISP  /  Атлас Халлоунеста", heading);
                if (Choose(new Rect(990, 57, 200, 38), "Закрыть · F8")) Close();
                KeyHint(new Rect(84, 107, 36, 28), "LB");
                KeyHint(new Rect(1156, 107, 36, 28), "RB");
                if (Choose(new Rect(126, 103, 150, 38), "Маршрут", tab == 0)) ChangeTab(0);
                if (Choose(new Rect(286, 103, 220, 38), "Дневник охотника", tab == 1)) ChangeTab(1);
                if (Choose(new Rect(516, 103, 160, 38), "Настройки", primaryTab == 2)) ChangeTab(2);
                if (Choose(new Rect(724, 103, 424, 38), "Цель: " + (mod.Progress.RouteGoal == "steel" ? "C · Стальная душа" : mod.Progress.RouteGoal == "speed" ? "B · Скорость" : "A · 112%"), primaryTab == 3)) { ChangeTab(3); }
                Divider(new Rect(92, 147, 1096, 15));
                GUI.BeginGroup(new Rect(84, 176, 1112, 530));
                if (tab == 0) DrawRoute(); else if (tab == 1) DrawJournal(); else DrawSettings();
                GUI.EndGroup();
                Divider(new Rect(340, 721, 600, 14));
                GUI.Label(new Rect(90, 740, 1100, 32), NavigationHint(), small);
            }
            finally { GUI.matrix = matrix; GUI.depth = depth; GUI.enabled = enabled; GUI.color = color; }
        }

        private void DrawRoute()
        {
            var chapters = RouteChapters;
            var steps = RouteSteps;
            GUI.Label(new Rect(0, 0, 210, 28), padPane == 0 ? "ОБЛАСТИ · ВЫБОР" : "ОБЛАСТИ", muted);
            string[] chapterLabels = chapters.Select(c => {
                bool visible = Visible(c);
                var tasks = c.Steps.Where(t => !t.ReferenceOnly).ToArray();
                bool visited = c.Steps.Any(t => SaveDiscovery.HasVisitEvidence(t.MapChapter, player));
                return visible ? c.Title + (tasks.Length == 0 ? "\nСправочник" : "\n" + tasks.Count(Done) + " / " + tasks.Length) + (!visited && !c.Id.Contains("-ref-") ? "  ?" : "") : "Неизученный этап";
            }).ToArray();
            float[] chapterHeights = chapterLabels.Select(t => Mathf.Max(92, button.CalcHeight(new GUIContent(t), 192) + 12)).ToArray();
            float cy = 0;
            if (revealChapter) { chapterScroll.y = Mathf.Max(0, chapterHeights.Take(chapterIndex).Sum() - 130); revealChapter = false; }
            chapterScroll = GUI.BeginScrollView(new Rect(0, 42, 216, 488), chapterScroll, new Rect(0, 0, 192, chapterHeights.Sum()));
            for (int i = 0; i < chapters.Length; i++)
            {
                if (Choose(new Rect(0, cy, 192, chapterHeights[i] - 6), chapterLabels[i], chapterIndex == i, chapterIndex == i && padPane == 0 && !contentFocus)) { contentFocus = false; padPane = 0; SelectChapter(i); }
                cy += chapterHeights[i];
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(238, 0, 238, 28), padPane == 1 ? "ШАГИ · ВЫБОР" : "ШАГИ", muted);
            if (!Visible(Current)) { Paragraph(new Rect(504, 30, 596, 440), "Область ещё не открыта", "Посети эту область или включи спойлеры в настройках."); return; }
            string[] stepLabels = steps.Select(t => (t.ReferenceOnly ? "≡ " : Done(t) ? "✓ " : "○ ") + t.Title).ToArray();
            float[] heights = stepLabels.Select((t, i) => mod.Settings.HideCompleted && Done(steps[i]) && i != stepIndex && !steps[i].ReferenceOnly ? 0 : Mathf.Max(84, button.CalcHeight(new GUIContent(t), 216) + 12)).ToArray();
            if (revealStep) { stepScroll.y = Mathf.Max(0, heights.Take(stepIndex).Sum() - 120); revealStep = false; }
            stepScroll = GUI.BeginScrollView(new Rect(238, 42, 240, 488), stepScroll, new Rect(0, 0, 216, heights.Sum()));
            float sy = 0;
            for (int i = 0; i < steps.Length; i++)
            {
                // Keep the selected row visible even when completed rows are collapsed.
                if (mod.Settings.HideCompleted && Done(steps[i]) && i != stepIndex && !steps[i].ReferenceOnly) continue;
                if (Choose(new Rect(0, sy, 216, heights[i] - 6), stepLabels[i], stepIndex == i, stepIndex == i && padPane == 1 && !contentFocus)) { contentFocus = false; padPane = 1; SelectStep(i); }
                sy += heights[i];
            }
            GUI.EndScrollView();
            if (Choose(new Rect(500, 0, 168, 32), "Описание", detailChoice == 0, !contentFocus && padPane == 2 && detailChoice == 0)) { contentFocus = false; mapTab = false; detailChoice = 0; padPane = 2; }
            if (Choose(new Rect(680, 0, 150, 32), "Карта", detailChoice == 1, !contentFocus && padPane == 2 && detailChoice == 1)) { contentFocus = false; mapTab = true; detailChoice = 1; padPane = 2; }
            if (Choose(new Rect(844, 0, 248, 32), "Враги области", detailChoice == 2, !contentFocus && padPane == 2 && detailChoice == 2)) OpenRegionJournal();
            KeyHint(new Rect(216, 2, 22, 26), "↔");
            KeyHint(new Rect(478, 2, 22, 26), "↔");
            if (contentFocus) FocusCorners(new Rect(500, 42, 612, 434));
            if (padPane == 2) Rule(new Rect(500, 36, 612, 2), new Color(.66f, .77f, .87f));
            if (detailChoice == 2) Paragraph(new Rect(500, 44, 612, 428), "Враги области", "Нажми A, чтобы открыть дневник для выбранного места. B возвращает к шагам.");
            else if (mapTab) DrawAreaMap(new Rect(500, 44, 612, 428));
            else
            {
                var selected = steps[Mathf.Clamp(stepIndex, 0, steps.Length - 1)];
                string body = selected.Spoiler && !mod.Settings.ShowSpoilers ? "Описание содержит сюжетные спойлеры. Их можно включить в настройках." : selected.Body;
                if (selected.Warning.Length > 0) body = "ВАЖНО\n" + selected.Warning + "\n\n" + body;
                if (ProfileAchievements.Confirmed(selected, player) && !Completion.Confirmed(selected, player))
                    body = "Достижение уже получено в игровом профиле — возможно, в другом сохранении. Это не выдаёт предметы и не открывает пути в текущем сейве.\n\n" + body;
                if (mod.Progress.RouteGoal == "steel") body += "\n\nСтальная душа: цель C — 100% без смерти и без ограничения времени. Проверяй набор на 100% в отдельном этапе.";
                float titleHeight = heading.CalcHeight(new GUIContent(selected.Title), 584);
                float bodyHeight = text.CalcHeight(new GUIContent(body), 584);
                detailScroll = GUI.BeginScrollView(new Rect(500, 44, 612, 384), detailScroll, new Rect(0, 0, 588, titleHeight + bodyHeight + 28));
                GUI.Label(new Rect(0, 0, 584, titleHeight), selected.Title, heading);
                GUI.Label(new Rect(0, titleHeight + 20, 584, bodyHeight), body, text);
                GUI.EndScrollView();
                if (selected.ReferenceOnly) GUI.Label(new Rect(502, 434, 604, 40), "Справка по PDF · не входит в счётчик задач", muted);
                else if (Completion.Confirmed(selected, player)) GUI.Label(new Rect(502, 434, 604, 40), "✓ Подтверждено этим сохранением", muted);
                else if (ProfileAchievements.Confirmed(selected, player)) GUI.Label(new Rect(502, 434, 604, 40), "✓ Получено в игровом профиле", muted);
                else if (Choose(new Rect(500, 434, 612, 40), Done(selected) ? "✓ Снять ручную отметку" : "○ Отметить выполненным")) mod.Progress.Mark(selected.Id, !Done(selected));
            }
            GUI.enabled = stepIndex > 0;
            if (Choose(new Rect(500, 486, 145, 36), "‹ Назад")) SelectStep(stepIndex - 1);
            GUI.enabled = stepIndex < steps.Length - 1;
            if (Choose(new Rect(960, 486, 145, 36), "Далее ›")) SelectStep(stepIndex + 1);
            GUI.enabled = true;
        }

        private void Paragraph(Rect viewport, string title, string body)
        {
            float h = heading.CalcHeight(new GUIContent(title), viewport.width - 24);
            float b = text.CalcHeight(new GUIContent(body), viewport.width - 24);
            detailScroll = GUI.BeginScrollView(viewport, detailScroll, new Rect(0, 0, viewport.width - 24, h + b + 20));
            GUI.Label(new Rect(0, 0, viewport.width - 24, h), title, heading);
            GUI.Label(new Rect(0, h + 16, viewport.width - 24, b), body, text);
            GUI.EndScrollView();
        }

        private Enemy[] FilteredEnemies()
        {
            string region = string.IsNullOrEmpty(journalRegion) ? liveRegion : journalRegion;
            return catalog.Enemies.Where(e => (allRegions || e.Regions.Contains(region)) &&
                (!allRegions || mod.Settings.ShowSpoilers || JournalStatus.Read(e, player).Discovered || e.Regions.Contains(liveRegion)) &&
                (!mod.Settings.HideCompleted || !JournalStatus.Read(e, player).Complete) &&
                (query.Length == 0 || EnemyName(e).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || e.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
        }

        private string EnemyName(Enemy enemy)
        {
            string translated = TeamCherry.Localization.Language.Get("NAME_" + enemy.JournalKey.ToUpperInvariant(), "Journal");
            return string.IsNullOrEmpty(translated) || translated.StartsWith("#") ? enemy.Name : translated;
        }

        private bool DrawExpandedEnemyMap()
        {
            if (!expandedEnemyMap) return false;
            var enemies = FilteredEnemies();
            if (enemies.Length == 0) { expandedEnemyMap = false; return false; }
            enemyIndex = Mathf.Clamp(enemyIndex, 0, enemies.Length - 1);
            var enemy = enemies[enemyIndex];
            EnemyMedia entry; media.Enemies.TryGetValue(enemy.Id, out entry);
            if (expandedEnemyMap && entry != null && entry.Maps.Length > 0)
            {
                enemyMapIndex %= entry.Maps.Length;
                string expandedUrl = entry.Maps[enemyMapIndex];
                GUI.DrawTexture(new Rect(0, 0, 1112, 530), panelTexture);
                if (Choose(new Rect(0, 0, 210, 34), "‹ К записи врага")) expandedEnemyMap = false;
                GUI.Label(new Rect(220, 0, 530, 34), HabitatLabel(entry, enemyMapIndex), text);
                MapControls(new Rect(770, 0, 322, 34));
                DrawMapTexture(new Rect(0, 44, 1112, 450), media.Get(expandedUrl), media.Status(expandedUrl));
                if (Choose(new Rect(0, 497, 450, 30), "Следующее место обитания ›")) { enemyMapIndex++; mapZoom = 1; mapPan = Vector2.zero; }
                GUI.Label(new Rect(500, 497, 610, 30), "Hollow Knight Wiki / Team Cherry · справочная карта", small);
                return true;
            }
            expandedEnemyMap = false;
            return false;
        }

        private void DrawJournal()
        {
            if (DrawExpandedEnemyMap()) return;
            GUI.Label(new Rect(0, 0, 216, 34), padPane == 0 ? "ВРАГИ · ВЫБОР" : "ВРАГИ", muted);
            KeyHint(new Rect(216, 0, 28, 30), "→");
            GUI.Label(new Rect(250, 0, 370, 34), "СВЕДЕНИЯ", muted);
            if (Choose(new Rect(644, 0, 250, 34), "Места обитания", !journalSaveMap, padPane == 1 && !journalSaveMap && !contentFocus)) { journalSaveMap = false; padPane = 1; contentFocus = false; }
            if (Choose(new Rect(896, 0, 206, 34), "Карта сейва", journalSaveMap, padPane == 1 && journalSaveMap && !contentFocus)) { journalSaveMap = true; padPane = 1; contentFocus = false; mapDirty = true; }
            if (MapButton(new Rect(0, 40, 216, 34), allRegions ? "Все области · X" : "Область · X")) { allRegions = !allRegions; enemyIndex = 0; }
            var enemies = FilteredEnemies();
            if (enemies.Length == 0) { GUI.Label(new Rect(0, 66, 1100, 80), "Нет врагов по этому фильтру. Очисти поиск или выбери все открытые области.", text); return; }
            enemyIndex = Mathf.Clamp(enemyIndex, 0, enemies.Length - 1);
            enemyScroll = GUI.BeginScrollView(new Rect(0, 82, 216, 448), enemyScroll, new Rect(0, 0, 190, enemies.Length * 96));
            for (int i = 0; i < enemies.Length; i++)
                if (Choose(new Rect(0, i * 96, 190, 90), (JournalStatus.Read(enemies[i], player).Complete ? "✓ " : "○ ") + EnemyName(enemies[i]), i == enemyIndex, i == enemyIndex && padPane == 0 && !contentFocus))
                { padPane = 0; contentFocus = false; enemyIndex = i; enemyMapIndex = 0; mapPan = Vector2.zero; mapZoom = 1; habitatScroll = Vector2.zero; }
            GUI.EndScrollView();
            // Two fixed journal pages: portrait/conditions and an uninterrupted habitat map.
            Border(new Rect(234, 54, 398, 476), new Color(.26f, .34f, .42f));
            Border(new Rect(644, 54, 458, 476), new Color(.26f, .34f, .42f));
            Divider(new Rect(638, 70, 1, 444));
            var enemy = enemies[enemyIndex]; var status = JournalStatus.Read(enemy, player);
            EnemyMedia entry; media.Enemies.TryGetValue(enemy.Id, out entry);
            var portrait = entry == null ? null : media.Get(entry.Portrait);
            if (portrait != null) GUI.DrawTexture(new Rect(250, 74, 120, 132), portrait, ScaleMode.ScaleToFit);
            else GUI.Label(new Rect(250, 100, 120, 90), entry == null ? "Нет портрета" : media.Status(entry.Portrait), muted);
            GUI.Label(new Rect(382, 74, 232, 88), EnemyName(enemy), heading);
            GUI.Label(new Rect(382, 164, 232, 62), !status.Available ? "Счётчик недоступен" : status.Complete ? "✓ Запись завершена" : "Осталось победить: " + status.Remaining, muted);
            Divider(new Rect(260, 226, 346, 9));
            string warning = Habitat.Description(enemy, player);
            if (string.IsNullOrEmpty(warning)) warning = enemy.Optional ? "Дополнительная запись дневника." : "Выбери карту справа. Точки мест обитания уже нанесены на изображение.";
            string regions = string.Join(" · ", enemy.Regions.Select(r => { var chapter = catalog.Chapters.FirstOrDefault(c => c.English.Replace('’', '\'') == r); return chapter == null ? r : chapter.Title; }));
            string body = warning + "\n\nОбласти: " + regions;
            float bodyHeight = text.CalcHeight(new GUIContent(body), 344);
            habitatScroll = GUI.BeginScrollView(new Rect(250, 244, 366, 264), habitatScroll, new Rect(0, 0, 344, bodyHeight));
            GUI.Label(new Rect(0, 0, 344, bodyHeight), body, text);
            GUI.EndScrollView();
            if (contentFocus) FocusCorners(new Rect(650, 58, 450, 470));
            if (journalSaveMap)
            {
                if (mapDirty && Event.current.type == EventType.Repaint)
                {
                    mapDirty = false;
                    var region = catalog.Chapters.FirstOrDefault(c => enemy.Regions.Contains(c.English.Replace('’', '\'')));
                    try { areaMap.Load(region == null ? Current.Id : region.Id, false); }
                    catch (Exception error) { areaMap.Dispose(); mod.LogError("Journal map: " + error); }
                }
                MapControls(new Rect(660, 78, 322, 34));
                DrawMapTexture(new Rect(660, 128, 426, 348), areaMap.Texture, areaMap.Message);
                GUI.Label(new Rect(660, 484, 426, 40), "Карта текущего сохранения", small);
                return;
            }
            GUI.Label(new Rect(660, 68, 260, 34), "Где искать", heading);
            if (entry == null || entry.Maps.Length == 0)
            { GUI.Label(new Rect(660, 128, 422, 160), "Отдельной карты пока нет. Известные области перечислены слева.", text); return; }
            if (MapButton(new Rect(936, 68, 150, 34), "Развернуть")) { expandedEnemyMap = true; contentFocus = true; padPane = 1; }
            enemyMapIndex %= entry.Maps.Length;
            string url = entry.Maps[enemyMapIndex];
            if (MapButton(new Rect(660, 116, 426, 52), HabitatLabel(entry, enemyMapIndex) + " · " + (enemyMapIndex + 1) + "/" + entry.Maps.Length + " ›"))
            { enemyMapIndex = (enemyMapIndex + 1) % entry.Maps.Length; mapPan = Vector2.zero; mapZoom = 1; url = entry.Maps[enemyMapIndex]; }
            MapControls(new Rect(660, 182, 322, 34));
            DrawMapTexture(new Rect(660, 228, 426, 250), media.Get(url), media.Status(url));
            GUI.Label(new Rect(660, 484, 426, 40), "Hollow Knight Wiki / Team Cherry", small);
        }

        private void SetMapMode(bool reference)
        {
            fullReferenceMap = reference;
            mapPan = Vector2.zero;
            mapZoom = 1;
            mapDirty = true;
            padPane = 2;
            detailChoice = 1;
        }

        private void DrawAreaMap(Rect viewport)
        {
            if (mapDirty && Event.current.type == EventType.Repaint)
            {
                mapDirty = false;
                try { areaMap.Load(CurrentMapId, false); }
                catch (Exception error) { areaMap.Dispose(); mod.LogError("Map preview: " + error); }
            }
            MapControls(new Rect(viewport.x, viewport.y, 322, 34));
            if (MapButton(new Rect(viewport.x + 310, viewport.y, 146, 32), "Справочная", fullReferenceMap)) SetMapMode(true);
            if (MapButton(new Rect(viewport.x + 462, viewport.y, 146, 32), "Карта сейва", !fullReferenceMap)) SetMapMode(false);
            GUI.Label(new Rect(viewport.x, viewport.y + 35, viewport.width, 26), "RS: тип карты · A: управление · B: назад", small);
            Texture texture = fullReferenceMap ? (Texture)media.Local("region-" + CurrentMapId) : areaMap.Texture;
            string message = fullReferenceMap ? "Справочная карта не сохранена. Используй игровую карту или раздел «Враги области» с картами мест обитания." : areaMap.Message;
            if (!fullReferenceMap && texture == null)
                message = "Карта сейва сейчас недоступна.\n\n" + (string.IsNullOrEmpty(message) ? "Wisp не смог построить изображение игровой карты." : message) + "\n\nНажми правый стик (RS), чтобы вернуться к справочной карте.";
            DrawMapTexture(new Rect(viewport.x, viewport.y + 66, viewport.width, viewport.height - 100), texture, message);
            GUI.Label(new Rect(viewport.x, viewport.y + viewport.height - 30, viewport.width, 30), fullReferenceMap ? "Hollow Knight Wiki / Team Cherry · полная область, включая спойлеры" : "Карта текущего сохранения · колёсико — масштаб · потяни для перемещения", small);
        }

        private void MapControls(Rect rect)
        {
            if (MapButton(new Rect(rect.x, rect.y, 34, 32), "−")) mapZoom = Mathf.Max(1, mapZoom / 1.25f);
            GUI.Label(new Rect(rect.x + 38, rect.y, 55, 32), Mathf.RoundToInt(mapZoom * 100) + "%", new GUIStyle(muted) { alignment = TextAnchor.MiddleCenter });
            if (MapButton(new Rect(rect.x + 97, rect.y, 34, 32), "+")) mapZoom = Mathf.Min(5, mapZoom * 1.25f);
            if (MapButton(new Rect(rect.x + 139, rect.y, 152, 32), "Вписать · Y")) { mapPan = Vector2.zero; mapZoom = 1; }
        }

        private bool MapButton(Rect rect, string label, bool selected = false)
        {
            Rule(new Rect(rect.x + 4, rect.yMax - 2, rect.width - 8, selected ? 2 : 1), selected ? new Color(.65f, .76f, .85f) : new Color(.23f, .30f, .37f));
            return GUI.Button(rect, label, new GUIStyle(button) { alignment = TextAnchor.MiddleCenter, fontSize = 16, padding = new RectOffset(4, 4, 2, 2) });
        }

        private static void Border(Rect rect, Color tint)
        {
            Color old = GUI.color; GUI.color = tint;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 1, rect.y, 1, rect.height), Texture2D.whiteTexture);
            GUI.color = old;
        }

        private void DrawMapTexture(Rect viewport, Texture texture, string message)
        {
            if (texture == null) { GUI.Label(viewport, string.IsNullOrEmpty(message) ? "Карта пока недоступна." : message, text); return; }
            var evt = Event.current;
            if (viewport.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.ScrollWheel) { mapZoom = Mathf.Clamp(mapZoom * Mathf.Pow(1.15f, -evt.delta.y), 1, 5); evt.Use(); }
                if (evt.type == EventType.MouseDrag && evt.button == 0) { mapPan += evt.delta; evt.Use(); }
            }
            float fit = Mathf.Min(viewport.width / texture.width, viewport.height / texture.height);
            float w = texture.width * fit * mapZoom, h = texture.height * fit * mapZoom;
            mapPan.x = Mathf.Clamp(mapPan.x, -Mathf.Max(w * .25f, (w - viewport.width) / 2), Mathf.Max(w * .25f, (w - viewport.width) / 2));
            mapPan.y = Mathf.Clamp(mapPan.y, -Mathf.Max(h * .25f, (h - viewport.height) / 2), Mathf.Max(h * .25f, (h - viewport.height) / 2));
            GUI.BeginGroup(viewport);
            GUI.DrawTexture(new Rect((viewport.width - w) / 2 + mapPan.x, (viewport.height - h) / 2 + mapPan.y, w, h), texture);
            GUI.EndGroup();
        }

        private void SetGoal(string goal)
        {
            mod.Progress.RouteGoal = goal;
            chapterIndex = 0; stepIndex = 0;
            int index = Array.FindIndex(RouteChapters, c => c.Steps.Any(s => s.MapChapter == liveChapter));
            SelectChapter(Math.Max(0, index));
            chapterScroll = stepScroll = detailScroll = Vector2.zero;
        }

        private void DrawSettings()
        {
            KeyHint(new Rect(0, 0, 36, 32), "LT");
            string[] goals = { "112", "speed", "steel" };
            string[] names = { "A · 112% и достижения", "B · Скорость и выборы", "C · Стальная душа" };
            for (int i = 0; i < 3; i++)
                if (Choose(new Rect(42 + i * 342, 0, 338, 38), names[i], mod.Progress.RouteGoal == goals[i], padPane == 0 && mod.Progress.RouteGoal == goals[i])) SetGoal(goals[i]);
            KeyHint(new Rect(1076, 0, 36, 32), "RT");
            string goalText = mod.Progress.RouteGoal == "steel" ? "C · Стальная душа до 100%, без лимита времени. Сначала обычный финал, затем добор по набору PDF. Режим Steel Soul нужно выбрать при создании игры." : mod.Progress.RouteGoal == "speed" ? "B · Отдельное обычное сохранение: финал до 5 часов, затем 100% до 20 часов. Зота не спасать; Кузнеца убить; труппу изгнать." : "A · Основное обычное сохранение. A1–A15 по PDF: 112%, коллекции, Сады королевы, концовки и пантеоны. Серый принц Зот — после пятого пантеона.";
            if (mod.Progress.RouteGoal == "steel" && !RouteGoals.IsSteelSave(player)) goalText += "\nВнимание: этот слот не в режиме Стальной души.";
            GUI.Label(new Rect(16, 66, 1080, 138), goalText, text);
            string[] labels = { "Спойлеры: " + (mod.Settings.ShowSpoilers ? "показаны" : "скрыты"), "Выполненные задачи: " + (mod.Settings.HideCompleted ? "скрыты" : "показаны"), "Подсказка на паузе: " + (mod.Settings.ShowPauseHint ? "включена" : "выключена"), "Проводник поверх игры: " + (mod.Settings.ShowHud ? "включён" : "выключен") };
            for (int i = 0; i < labels.Length; i++) if (Choose(new Rect(12, 218 + i * 46, 1080, 42), labels[i], false, padPane == i + 1)) ToggleSetting(i + 1);
            GUI.Label(new Rect(16, 418, 1080, 48), "Цель и отметки хранятся отдельно для каждого сейва и сохраняются вместе с игрой. Смена цели не удаляет прогресс.", muted);
            GUI.Label(new Rect(16, 475, 1080, 48), "Карты и портреты открываются здесь. При первом просмотре изображений врагов нужен интернет; затем они доступны из локального кэша.", small);
        }

        private void CycleGoal(int direction)
        {
            string[] goals = { "112", "speed", "steel" };
            int index = Array.IndexOf(goals, mod.Progress.RouteGoal);
            SetGoal(goals[Mathf.Clamp(index + direction, 0, goals.Length - 1)]);
        }

        private void ToggleSetting(int index)
        {
            if (index == 0) CycleGoal(1);
            if (index == 1) { mod.Settings.ShowSpoilers = !mod.Settings.ShowSpoilers; mapDirty = true; }
            if (index == 2) mod.Settings.HideCompleted = !mod.Settings.HideCompleted;
            if (index == 3) mod.Settings.ShowPauseHint = !mod.Settings.ShowPauseHint;
            if (index == 4) mod.Settings.ShowHud = !mod.Settings.ShowHud;
        }

        private void DrawHud(float width)
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(hudTask)) return;
            float x = width - 280;
            float height = text.CalcHeight(new GUIContent(hudTask), 228);
            string extra = Time.unscaledTime < noticeUntil && !notice.StartsWith("Новая область") ? notice : "";
            float extraHeight = extra.Length == 0 ? 0 : muted.CalcHeight(new GUIContent(extra), 228) + 8;
            Rect card = new Rect(x, 46, 252, height + extraHeight + 30);
            GUI.DrawTexture(card, panelTexture);
            Border(card, new Color(.48f, .60f, .72f));
            GUI.Label(new Rect(x + 12, 60, 228, height), hudTask, text);
            if (extraHeight > 0) GUI.Label(new Rect(x + 12, 66 + height, 228, extraHeight), extra, muted);
        }
    }
}
