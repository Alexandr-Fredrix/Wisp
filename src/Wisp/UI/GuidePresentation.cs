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
            font = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "Arial" }, 16);
            panelTexture = Solid(new Color(.025f, .032f, .047f, 1f));
            buttonTexture = Solid(Color.clear);
            activeTexture = new Texture2D(128, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 128; x++)
            {
                float fade = Mathf.Sin(Mathf.PI * x / 127f) * Mathf.Sin(Mathf.PI * y / 31f);
                activeTexture.SetPixel(x, y, new Color(.5f, .65f, .85f, fade * .16f));
            }
            activeTexture.Apply();
            text = new GUIStyle(GUI.skin.label) { font = font, fontSize = 19, wordWrap = true, richText = false, clipping = TextClipping.Clip, padding = new RectOffset(4, 4, 4, 4) };
            text.normal.textColor = new Color(.86f, .88f, .91f);
            heading = new GUIStyle(text) { fontSize = 25, fontStyle = FontStyle.Normal };
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
            tab = target; padPane = 0; expandedEnemyMap = false;
            detailChoice = mapTab ? 1 : 0; detailScroll = Vector2.zero;
        }

        private void OpenRegionJournal()
        {
            journalRegion = Current.English.Replace('’', '\''); allRegions = false;
            enemyIndex = 0; enemyScroll = Vector2.zero; ChangeTab(1);
        }

        private string NavigationHint()
        {
            if (tab == 2) return "↑ ↓ Выбор настройки     A Изменить     B Закрыть";
            if (tab == 1) return "↑ ↓ Враг     A Следующее место     X Фильтр области     RS Карта     LT / RT Масштаб     B Назад";
            if (padPane == 0) return "↑ ↓ Область     A / → К шагам     B Закрыть";
            if (padPane == 1) return "↑ ↓ Шаг     A / → К описанию и карте     Y Отметка     B / ← К областям";
            return mapTab ? "↑ ↓ Выбрать вкладку · A Открыть     LS / RS Двигать карту     LT / RT Масштаб     Y Вписать     B / ← К шагам"
                : "↑ ↓ Выбрать вкладку · A Открыть     RS Прокрутить текст     X Карта     B / ← К шагам";
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
                if (Choose(new Rect(1040, 57, 145, 38), "Закрыть · F8")) Close();
                KeyHint(new Rect(84, 107, 36, 28), "LB");
                KeyHint(new Rect(682, 107, 36, 28), "RB");
                if (Choose(new Rect(126, 103, 150, 38), "Маршрут", tab == 0)) ChangeTab(0);
                if (Choose(new Rect(286, 103, 220, 38), "Дневник охотника", tab == 1)) ChangeTab(1);
                if (Choose(new Rect(516, 103, 160, 38), "Настройки", tab == 2)) ChangeTab(2);
                if (Choose(new Rect(750, 103, 430, 38), "Цель: " + (mod.Progress.RouteGoal == "steel" ? "Быстрая Стальная душа" : "112% · полное прохождение"))) { tab = 2; padPane = 0; }
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
            chapterScroll = GUI.BeginScrollView(new Rect(0, 34, 216, 496), chapterScroll, new Rect(0, 0, 192, chapters.Length * 96));
            for (int i = 0; i < chapters.Length; i++)
            {
                bool visible = Visible(chapters[i]);
                var included = chapters[i].Steps.Where(s => RouteGoals.Includes(mod.Progress.RouteGoal, s)).ToArray();
                if (Choose(new Rect(0, i * 96, 192, 90), (visible ? chapters[i].Title : "Неизученная область") + (visible ? "\n" + included.Count(Done) + " / " + included.Length : ""), chapterIndex == i, chapterIndex == i && padPane == 0)) { padPane = 0; SelectChapter(i); }
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(238, 0, 238, 28), padPane == 1 ? "ШАГИ · ВЫБОР" : "ШАГИ", muted);
            if (!Visible(Current)) { Paragraph(new Rect(504, 30, 596, 440), "Область ещё не открыта", "Посети эту область или включи спойлеры в настройках."); return; }
            stepScroll = GUI.BeginScrollView(new Rect(238, 34, 240, 496), stepScroll, new Rect(0, 0, 216, steps.Length * 96));
            float sy = 0;
            for (int i = 0; i < steps.Length; i++)
            {
                if (mod.Settings.HideCompleted && Done(steps[i]) && i != stepIndex) continue;
                if (Choose(new Rect(0, sy, 216, 90), (Done(steps[i]) ? "✓ " : "○ ") + steps[i].Title, stepIndex == i, stepIndex == i && padPane == 1)) { padPane = 1; SelectStep(i); }
                sy += 96;
            }
            GUI.EndScrollView();
            if (Choose(new Rect(500, 0, 168, 32), "Описание", !mapTab, padPane == 2 && detailChoice == 0)) { mapTab = false; detailChoice = 0; padPane = 2; }
            if (Choose(new Rect(680, 0, 150, 32), "Карта", mapTab, padPane == 2 && detailChoice == 1)) { mapTab = true; detailChoice = 1; padPane = 2; }
            if (Choose(new Rect(844, 0, 240, 32), "Враги области", false, padPane == 2 && detailChoice == 2)) OpenRegionJournal();
            KeyHint(new Rect(216, 2, 22, 26), "↔");
            KeyHint(new Rect(478, 2, 22, 26), "↔");
            if (padPane == 2) Rule(new Rect(500, 36, 612, 2), new Color(.66f, .77f, .87f));
            if (mapTab) DrawAreaMap(new Rect(500, 44, 612, 428));
            else
            {
                var selected = steps[Mathf.Clamp(stepIndex, 0, steps.Length - 1)];
                string body = selected.Spoiler && !mod.Settings.ShowSpoilers ? "Описание содержит сюжетные спойлеры. Их можно включить в настройках." : selected.Body;
                if (selected.Warning.Length > 0) body = "ВАЖНО\n" + selected.Warning + "\n\n" + body;
                if (mod.Progress.RouteGoal == "steel") body += "\n\nСтальная душа: здесь важна концовка без смерти. Дневник и необязательные задания можно оставить; лечение и подготовку к боссу не пропускай ради времени.";
                float titleHeight = heading.CalcHeight(new GUIContent(selected.Title), 584);
                float bodyHeight = text.CalcHeight(new GUIContent(body), 584);
                detailScroll = GUI.BeginScrollView(new Rect(500, 44, 612, 384), detailScroll, new Rect(0, 0, 588, titleHeight + bodyHeight + 28));
                GUI.Label(new Rect(0, 0, 584, titleHeight), selected.Title, heading);
                GUI.Label(new Rect(0, titleHeight + 20, 584, bodyHeight), body, text);
                GUI.EndScrollView();
                if (Completion.Confirmed(selected, player)) GUI.Label(new Rect(502, 434, 604, 40), "✓ Подтверждено этим сохранением", muted);
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
            GUI.Label(new Rect(0, 0, 70, 34), "Поиск", muted);
            string previousQuery = query;
            query = GUI.TextField(new Rect(72, 0, 194, 34), query, new GUIStyle(button) { normal = { background = activeTexture }, padding = new RectOffset(8, 8, 5, 5) });
            if (previousQuery != query) { enemyIndex = 0; enemyScroll = habitatScroll = Vector2.zero; }
            if (MapButton(new Rect(280, 0, 268, 34), allRegions ? "Все открытые области" : "Выбранная область")) { allRegions = !allRegions; enemyIndex = 0; }
            if (MapButton(new Rect(560, 0, 252, 34), "К текущей области")) { journalRegion = ""; allRegions = false; enemyIndex = 0; }
            if (MapButton(new Rect(900, 0, 202, 34), "Обновить картинки")) media.Retry();
            var enemies = FilteredEnemies();
            if (enemies.Length == 0) { GUI.Label(new Rect(0, 66, 1100, 80), "Нет врагов по этому фильтру. Очисти поиск или выбери все открытые области.", text); return; }
            enemyIndex = Mathf.Clamp(enemyIndex, 0, enemies.Length - 1);
            enemyScroll = GUI.BeginScrollView(new Rect(0, 54, 216, 476), enemyScroll, new Rect(0, 0, 190, enemies.Length * 80));
            for (int i = 0; i < enemies.Length; i++)
                if (Choose(new Rect(0, i * 80, 190, 74), (JournalStatus.Read(enemies[i], player).Complete ? "✓ " : "○ ") + EnemyName(enemies[i]), i == enemyIndex, i == enemyIndex))
                { enemyIndex = i; enemyMapIndex = 0; mapPan = Vector2.zero; mapZoom = 1; habitatScroll = Vector2.zero; }
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
            GUI.Label(new Rect(660, 68, 260, 34), "Где искать", heading);
            if (entry == null || entry.Maps.Length == 0)
            { GUI.Label(new Rect(660, 128, 422, 160), "Отдельной карты пока нет. Известные области перечислены слева.", text); return; }
            if (MapButton(new Rect(936, 68, 150, 34), "Развернуть")) expandedEnemyMap = true;
            enemyMapIndex %= entry.Maps.Length;
            string url = entry.Maps[enemyMapIndex];
            if (MapButton(new Rect(660, 116, 426, 52), HabitatLabel(entry, enemyMapIndex) + " · " + (enemyMapIndex + 1) + "/" + entry.Maps.Length + " ›"))
            { enemyMapIndex = (enemyMapIndex + 1) % entry.Maps.Length; mapPan = Vector2.zero; mapZoom = 1; url = entry.Maps[enemyMapIndex]; }
            MapControls(new Rect(660, 182, 322, 34));
            DrawMapTexture(new Rect(660, 228, 426, 250), media.Get(url), media.Status(url));
            GUI.Label(new Rect(660, 484, 426, 40), "Hollow Knight Wiki / Team Cherry", small);
        }

        private void DrawAreaMap(Rect viewport)
        {
            if (mapDirty && Event.current.type == EventType.Repaint)
            {
                mapDirty = false;
                try { areaMap.Load(Current.Id, mod.Settings.ShowSpoilers); }
                catch (Exception error) { areaMap.Dispose(); mod.LogError("Map preview: " + error); }
            }
            MapControls(new Rect(viewport.x, viewport.y, 322, 34));
            if (MapButton(new Rect(viewport.x + 310, viewport.y, 146, 32), "Справочная", fullReferenceMap)) { fullReferenceMap = true; mapPan = Vector2.zero; mapZoom = 1; mapDirty = true; }
            if (MapButton(new Rect(viewport.x + 462, viewport.y, 146, 32), "Изученная", !fullReferenceMap)) { fullReferenceMap = false; mapPan = Vector2.zero; mapZoom = 1; mapDirty = true; }
            Texture texture = fullReferenceMap ? (Texture)media.Local("region-" + Current.Id) : areaMap.Texture;
            string message = fullReferenceMap ? "Справочная карта не сохранена. Используй игровую карту или раздел «Враги области» с картами мест обитания." : areaMap.Message;
            DrawMapTexture(new Rect(viewport.x, viewport.y + 42, viewport.width, viewport.height - 76), texture, message);
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
            int index = Array.FindIndex(RouteChapters, c => c.Id == liveChapter);
            SelectChapter(Math.Max(0, index));
            chapterScroll = stepScroll = detailScroll = Vector2.zero;
        }

        private void DrawSettings()
        {
            GUI.Label(new Rect(12, 0, 1090, 36), "Цель этого сохранения", heading);
            if (Choose(new Rect(12, 45, 470, 46), "112% · полное прохождение", mod.Progress.RouteGoal == "112", padPane == 0 && mod.Progress.RouteGoal == "112")) SetGoal("112");
            if (Choose(new Rect(520, 45, 572, 46), "Быстрое прохождение Стальной души", mod.Progress.RouteGoal == "steel", padPane == 0 && mod.Progress.RouteGoal == "steel")) SetGoal("steel");
            string goalText = mod.Progress.RouteGoal == "steel" ? "Короткий маршрут к первой концовке: способности, три Сновидца и Полый рыцарь. Без фарма дневника, Белого дворца и пантеонов. Время не гарантируется; это маршрут без сложных пропусков." : "Полный путеводитель с коллекциями и дневником. Некоторые дополнительные цели, включая пятый пантеон и дневник, сами по себе не дают процентов.";
            if (mod.Progress.RouteGoal == "steel" && !RouteGoals.IsSteelSave(player)) goalText += "\nЭто обычное сохранение: выбор цели Wisp не включает режим Стальной души. Его нужно выбрать при создании новой игры.";
            GUI.Label(new Rect(16, 101, 1080, 104), goalText, muted);
            string[] labels = { "Спойлеры: " + (mod.Settings.ShowSpoilers ? "показаны" : "скрыты"), "Выполненные задачи: " + (mod.Settings.HideCompleted ? "скрыты" : "показаны"), "Подсказка на паузе: " + (mod.Settings.ShowPauseHint ? "включена" : "выключена"), "Проводник поверх игры: " + (mod.Settings.ShowHud ? "включён" : "выключен") };
            for (int i = 0; i < labels.Length; i++) if (Choose(new Rect(12, 218 + i * 46, 1080, 42), labels[i], false, padPane == i + 1)) ToggleSetting(i + 1);
            GUI.Label(new Rect(16, 418, 1080, 48), "Цель и отметки хранятся отдельно для каждого сейва и сохраняются вместе с игрой. Смена цели не удаляет прогресс.", muted);
            GUI.Label(new Rect(16, 475, 1080, 48), "Карты и портреты открываются здесь. При первом просмотре изображений врагов нужен интернет; затем они доступны из локального кэша.", small);
        }

        private void ToggleSetting(int index)
        {
            if (index == 0) SetGoal(mod.Progress.RouteGoal == "112" ? "steel" : "112");
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
