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
        private GUIStyle small, columnHeading, keyStyle, cardStyle, pageStyle, mapStyle, regionStyle, zoomStyle;
        private GUISkin flatSkin;
        private int chapterPage, stepPage;
        private Texture2D scrollThumb;
        private MediaLibrary media;
        private int enemyIndex, enemyMapIndex;
        private string journalRegion = "";
        private bool expandedEnemyMap;
        private Vector2 habitatScroll;

        private string HabitatLabel(EnemyMedia entry, int index)
        {
            if (entry == null || entry.HabitatMaps.Length == 0) return I18n.T("Место обитания");
            var map = entry.HabitatMaps[index];
            string area = map.Regions.Length == 0 ? I18n.T("Место обитания") : string.Join(" · ", map.Regions.Select(RegionName));
            return area + (map.Phase == "before-infection" ? " · " + I18n.T("До заражения") : map.Phase == "after-infection" ? " · " + I18n.T("После заражения") : "");
        }
        private const float ContentScale = 1358f / 1112f;
        private static Rect ContentRect(float x, float y, float w, float h) { return new Rect(x * ContentScale, y, w * ContentScale, h); }

        private const float CanvasWidth = 1422, CanvasHeight = 800;

        private void Styles()
        {
            if (text != null) return;
            font = BundledFont.Load();

            panelTexture = Solid(new Color(.031f, .051f, .071f, 1f));
            buttonTexture = Solid(Color.clear);
            activeTexture = Solid(new Color(.11f, .18f, .23f, 1f));
            text = new GUIStyle(GUI.skin.label) { font = font, fontSize = 20, wordWrap = true, richText = false, clipping = TextClipping.Clip, padding = new RectOffset(4, 4, 4, 4) };
            text.normal.textColor = new Color(.91f, .95f, .97f);
            heading = new GUIStyle(text) { fontSize = 26, fontStyle = FontStyle.Normal };
            muted = new GUIStyle(text) { fontSize = 17 };
            muted.normal.textColor = new Color(.65f, .76f, .82f);
            small = new GUIStyle(muted) { fontSize = 15 };
            columnHeading = new GUIStyle(muted) { alignment = TextAnchor.MiddleLeft };
            button = new GUIStyle(text) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 10, 4, 4), fontSize = 20 };
            button.normal.background = buttonTexture;
            button.hover.background = buttonTexture; button.hover.textColor = Color.white;
            button.active.background = buttonTexture; button.focused.background = buttonTexture;
            active = new GUIStyle(button); active.normal.background = activeTexture; active.hover.background = activeTexture; active.active.background = activeTexture; active.normal.textColor = Color.white;
            flatSkin = Instantiate(GUI.skin);
            scrollThumb = Solid(new Color(.35f, .49f, .57f));
            foreach (var track in new[] { flatSkin.verticalScrollbar, flatSkin.horizontalScrollbar })
            {
                track.normal.background = buttonTexture; track.hover.background = buttonTexture;
                track.active.background = buttonTexture; track.border = new RectOffset();
                track.margin = new RectOffset(); track.padding = new RectOffset();
            }
            flatSkin.verticalScrollbar.fixedWidth = 7;
            flatSkin.horizontalScrollbar.fixedHeight = 7;
            foreach (var thumb in new[] { flatSkin.verticalScrollbarThumb, flatSkin.horizontalScrollbarThumb })
            {
                thumb.normal.background = scrollThumb; thumb.hover.background = scrollThumb;
                thumb.active.background = scrollThumb; thumb.border = new RectOffset();
            }
            foreach (var arrow in new[] { flatSkin.verticalScrollbarUpButton, flatSkin.verticalScrollbarDownButton, flatSkin.horizontalScrollbarLeftButton, flatSkin.horizontalScrollbarRightButton })
            {
                arrow.normal.background = buttonTexture; arrow.hover.background = buttonTexture;
                arrow.active.background = buttonTexture; arrow.fixedHeight = arrow.fixedWidth = 0;
            }
            keyStyle = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, wordWrap = false, padding = new RectOffset(0,0,0,0) };
            cardStyle = new GUIStyle(small) { alignment = TextAnchor.UpperCenter };
            pageStyle = new GUIStyle(muted) { alignment = TextAnchor.MiddleCenter, wordWrap = false, padding = new RectOffset(0,0,0,0) };
            zoomStyle = new GUIStyle(muted) { alignment = TextAnchor.MiddleCenter };
            mapStyle = new GUIStyle(button) { alignment = TextAnchor.MiddleCenter, fontSize = 17, padding = new RectOffset(4,4,2,2) };
            regionStyle = new GUIStyle(button) { fontSize = 16, alignment = TextAnchor.MiddleLeft, wordWrap = true, padding = new RectOffset(4,4,0,0) };
        }

        private static Texture2D Solid(Color color)
        { var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, color); texture.Apply(); return texture; }

        // The open item has an underline; only controller focus gets silver brackets.
        private bool Choose(Rect rect, string label, bool selected = false, bool focused = false)
        {
            bool clicked = GUI.Button(rect, label, focused ? active : button);
            if (selected) Underline(rect, label, button, new Color(.65f, .76f, .85f));
            if (focused) FocusCorners(rect);
            return clicked;
        }

        private void Underline(Rect rect, string label, GUIStyle style, Color tint)
        {
            float available = rect.width - style.padding.horizontal;
            float width = Mathf.Min(available, style.CalcSize(new GUIContent(label)).x - style.padding.horizontal);
            bool wrapped = label.Contains("\n") || style.CalcHeight(new GUIContent(label), rect.width) > style.lineHeight + style.padding.vertical + 2;
            float x = style.alignment == TextAnchor.MiddleCenter ? rect.center.x - width / 2 : rect.x + style.padding.left;
            float y = wrapped ? rect.yMax - 5 : rect.center.y + style.lineHeight / 2 + 4;
            Rule(new Rect(x, y, Mathf.Max(1, width), 1), tint);
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
            Rule(rect, new Color(.075f, .12f, .16f));
            Border(rect, new Color(.38f, .52f, .60f));
            GUI.Label(rect, key, keyStyle);
        }

        private void ChangeTab(int target, bool routeRegion = false)
        {
            if (target == 1 && !routeRegion) ResetJournalRegion();
            expandedCollectionMap = false; collectionInfo = false; expandedStepImage = false; choosingJournalRegion = false; tab = target; padPane = 0; expandedEnemyMap = false; contentFocus = false;
             detailChoice = mapTab ? 1 : 0; detailScroll = Vector2.zero;
        }

        private void OpenRegionJournal()
        {
            var location = catalog.Chapters.FirstOrDefault(c => c.Id == CurrentMapId);
            journalRegion = location == null ? liveRegion : location.English.Replace('’', '\''); allRegions = false;
            enemyIndex = 0; enemyScroll = Vector2.zero; ChangeTab(1, true);
        }

        private string NavigationHint()
        {
            if (tab == 4) return CollectionHint();
            if (expandedStepImage) return Wisp.Core.I18n.T("Стики: перемещение · LT/RT: масштаб · ←→: иллюстрация · X: повтор · Y: вписать · B: описание");
            if (choosingJournalRegion) return Wisp.Core.I18n.T("↑↓: локация · A: выбрать · B: назад");
            if (tab == 3) return contentFocus ? Wisp.Core.I18n.T("Стики: карта · LT/RT: масштаб · X: повтор · Y: вписать · B: к зонам") : Wisp.Core.I18n.T("↑↓: зона · A / →: карта · B: закрыть");
            if (contentFocus && tab == 1) return I18n.T("Стики: карта · ←→: место · LT/RT: масштаб · RS: развернуть · X: повтор · Y: вписать · B: назад");
            if (contentFocus) return (tab == 1 || mapTab) ? Wisp.Core.I18n.T("Стики: карта · LT/RT: масштаб · X: повтор · Y: вписать · B: к вкладкам") : Wisp.Core.I18n.T("↑↓ / RS: текст · B: к вкладкам");
            if (tab == 2) return Wisp.Core.I18n.T("↑↓: настройка · ←→ / LT/RT: цель · A: изменить · B: назад");
            if (tab == 1) return padPane == 0 ? I18n.T("↑↓: враг · A / →: сведения · X: фильтр · B: закрыть") : padPane == 1 ? I18n.T("↑↓ / RS: сведения · A / →: карта · X: повтор / фильтр · B: враги") : I18n.T("A: карта · RS: развернуть · Y: другое место · X: повтор / фильтр · B: сведения");
            if (padPane == 0) return Wisp.Core.I18n.T("↑↓: область · A / →: шаги · B: закрыть · ?: посещение не подтверждено");
            if (padPane == 1) return Wisp.Core.I18n.T("↑↓: шаг · A / →: вкладки · Y: отметка · B / ←: области");
            if (tab == 0 && padPane == 2 && !mapTab && RouteSteps[stepIndex].Collection.Length > 0) return I18n.T("↑↓ / RS: текст · Y: открыть коллекцию · B: к шагам");
            if (!mapTab && detailChoice == 0) return Wisp.Core.I18n.T("↑↓ / RS: прокрутка · Y: иллюстрация · RS: развернуть · X: повтор / карта · B: к шагам");
            return Wisp.Core.I18n.T("←→ / LT/RT: вкладка · A: открыть · X: карта · B: к шагам");
        }

        private void Divider(Rect rect)
        {
            Rule(new Rect(rect.x, rect.center.y, rect.width, 1), new Color(.20f, .30f, .35f));
        }

        private void OnGUI()
        {
            if (mod == null || !InSession) return;
            Styles();
            var oldSkin = GUI.skin;
            GUI.skin = flatSkin;
            var matrix = GUI.matrix; int depth = GUI.depth; bool enabled = GUI.enabled; var color = GUI.color;
            float scale = Mathf.Min(Screen.width / (CanvasWidth + 24), Screen.height / (CanvasHeight + 24));
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - CanvasWidth * scale) / 2, (Screen.height - CanvasHeight * scale) / 2), Quaternion.identity, new Vector3(scale, scale, 1));
            try
            {
                GUI.color = Color.white;
                if (!open)
                {
                    if (Paused && mod.Settings.ShowPauseHint) GUI.Label(new Rect(40, 752, 900, 28), Wisp.Core.I18n.T("WISP · F8 / оба стика — проводник"), muted);
                    return;
                }
                GUI.depth = -1000;
                // Uniform screen-wide backing eliminates the exposed rectangular panel behind the frame.
                var backdropMatrix = GUI.matrix;
                GUI.matrix = Matrix4x4.identity;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), panelTexture);
                GUI.matrix = backdropMatrix;


                if (mod.ProgressSaveBlocked || mod.ProgressRecovered)
                    GUI.Label(new Rect(32, 76, 1358, 26), mod.ProgressSaveBlocked ? I18n.T("Отметки не удалось восстановить. Запись отключена; исходные файлы сохранены.") : I18n.T("Отметки восстановлены из резервной копии; исходные файлы сохранены."), small);
                // Every section has its own fixed viewport; no child can widen its parent.
                GUI.Label(new Rect(32, 32, 960, 42), Wisp.Core.I18n.T("WISP  /  Атлас Халлоунеста"), heading);
                if (Choose(new Rect(1184, 32, 206, 42), Wisp.Core.I18n.T("Закрыть · F8"))) Close();
                KeyHint(new Rect(32, 112, 38, 28), "LB");
                KeyHint(new Rect(1352, 112, 38, 28), "RB");
                if (Choose(new Rect(86, 105, 150, 42), I18n.T("Маршрут"), tab == 0)) ChangeTab(0);
                if (Choose(new Rect(244, 105, 285, 42), I18n.T("Дневник охотника"), tab == 1)) ChangeTab(1);
                if (Choose(new Rect(537, 105, 205, 42), I18n.T("Коллекции"), tab == 4)) ChangeTab(4);
                if (Choose(new Rect(750, 105, 130, 42), I18n.T("Атлас"), tab == 3)) ChangeTab(3);
                if (Choose(new Rect(888, 105, 195, 42), I18n.T("Настройки"), tab == 2)) ChangeTab(2);
                GUI.Label(new Rect(1100, 105, 240, 42), I18n.T("Цель: ") + (mod.Progress.RouteGoal == "steel" ? "C" : mod.Progress.RouteGoal == "speed" ? "B" : "A · 112%"), columnHeading);
                Divider(new Rect(32, 155, 1358, 2));
                GUI.BeginGroup(new Rect(32, 182, 1358, 530));
                if (tab == 4) DrawCollections(); else if (tab == 0) DrawRoute(); else if (tab == 1) DrawJournal(); else if (tab == 3) DrawAtlas(); else DrawSettings();
                Rule(new Rect(0, 42, 1358, 1), new Color(.20f, .30f, .35f));
                GUI.EndGroup();
                Divider(new Rect(32, 730, 1358, 2));
                GUI.Label(new Rect(32, 747, 1358, 36), NavigationHint(), small);
            }
            finally { GUI.skin = oldSkin; GUI.matrix = matrix; GUI.depth = depth; GUI.enabled = enabled; GUI.color = color; }
        }

        private int routeLabelRevision = -1;
        private string[] cachedChapterLabels;
        private int stepLabelRevision = -1;
        private Step[] labelledSteps;
        private string[] cachedStepLabels;
        private bool[] cachedHiddenSteps;
        private void DrawRoute()
        {
            if (DrawExpandedStepImage()) return;
            var chapters = RouteChapters;
            var steps = RouteSteps;
            GUI.Label(ContentRect(30, 0, 180, 34), padPane == 0 ? Wisp.Core.I18n.T("ЭТАПЫ · ВЫБОР") : Wisp.Core.I18n.T("ЭТАПЫ"), columnHeading);
            if (routeLabelRevision != viewRevision) {
            cachedChapterLabels = chapters.Select(c => {
                bool visible = Visible(c);
                var tasks = c.Steps.Where(t => !t.ReferenceOnly).ToArray();
                bool visited = c.Steps.Any(t => SaveDiscovery.HasVisitEvidence(t.MapChapter, player));
                return visible ? c.Title + (tasks.Length == 0 ? Wisp.Core.I18n.T("\nСправочник") : "\n" + tasks.Count(Done) + " / " + tasks.Length) + (!visited && !c.Id.Contains("-ref-") ? "  ?" : "") : Wisp.Core.I18n.T("Неизученный этап");
            }).ToArray();
            routeLabelRevision = viewRevision; }
            PagedList(ContentRect(0, 54, 216, 476), cachedChapterLabels, chapterIndex, ref chapterPage, ref revealChapter,
                padPane == 0 && !contentFocus, index => { contentFocus = false; padPane = 0; SelectChapter(index); });
            GUI.Label(ContentRect(238, 0, 238, 34), padPane == 1 ? Wisp.Core.I18n.T("ШАГИ · ВЫБОР") : Wisp.Core.I18n.T("ШАГИ"), columnHeading);
            if (!Visible(Current)) { Paragraph(ContentRect(504, 30, 596, 440), Wisp.Core.I18n.T("Область ещё не открыта"), Wisp.Core.I18n.T("Посети эту область или включи спойлеры в настройках.")); return; }
            steps = RouteSteps;
            if (labelledSteps != steps || stepLabelRevision != viewRevision)
            {
                cachedStepLabels = steps.Select(t => (t.ReferenceOnly ? (t.Id.StartsWith("mushroom-") ? "≡ " + t.Title : Wisp.Core.I18n.T("≡ Обзор этапа")) : (Done(t) ? "✓ " : "○ ") + t.Title)).ToArray();
                cachedHiddenSteps = steps.Select(t => mod.Settings.HideCompleted && Done(t) && !t.ReferenceOnly).ToArray();
                labelledSteps = steps; stepLabelRevision = viewRevision;
            }
            PagedList(ContentRect(238, 54, 240, 476), cachedStepLabels, stepIndex, ref stepPage, ref revealStep,
                padPane == 1 && !contentFocus, index => { contentFocus = false; padPane = 1; SelectStep(index); }, cachedHiddenSteps);
            if (Choose(ContentRect(538, 0, 144, 34), Wisp.Core.I18n.T("Описание"), detailChoice == 0, !contentFocus && padPane == 2 && detailChoice == 0)) { contentFocus = false; mapTab = false; detailChoice = 0; padPane = 2; }
            if (Choose(ContentRect(696, 0, 128, 34), Wisp.Core.I18n.T("Карта"), detailChoice == 1, !contentFocus && padPane == 2 && detailChoice == 1)) { contentFocus = false; mapTab = true; detailChoice = 1; padPane = 2; }
            if (Choose(ContentRect(830, 0, 240, 34), Wisp.Core.I18n.T("Враги области"), detailChoice == 2, !contentFocus && padPane == 2 && detailChoice == 2)) OpenRegionJournal();
            KeyHint(ContentRect(0, 2, 24, 26), "←");
            KeyHint(ContentRect(500, 2, 30, 26), "LT");
            KeyHint(ContentRect(1080, 2, 30, 26), "RT");
            KeyHint(ContentRect(464, 2, 24, 26), "→");
            if (contentFocus) FocusCorners(ContentRect(500, 42, 612, 434));

            if (detailChoice == 2) Paragraph(ContentRect(500, 54, 612, 418), Wisp.Core.I18n.T("Враги области"), Wisp.Core.I18n.T("Нажми A, чтобы открыть дневник для выбранного места. B возвращает к шагам."));
            else if (mapTab) DrawAreaMap(ContentRect(500, 54, 612, 418));
            else
            {
                var selected = steps[Mathf.Clamp(stepIndex, 0, steps.Length - 1)];
                PrepareDescription(selected);
                StepImage[] targets;
                if ((selected.Spoiler && !mod.Settings.ShowSpoilers) || !media.TryStepImages(Current.Goal, Current.Id, selected.Id, out targets)) targets = new StepImage[0];
                float galleryHeight = targets.Length == 0 ? 0 : (targets.Any(t => t.Wide) ? 392 : ((targets.Length + 2) / 3) * 156 + 32);
                string achievementKey;
                bool hasAchievement = ProfileAchievements.Steps.TryGetValue(selected.Id, out achievementKey) && (!selected.Spoiler || mod.Settings.ShowSpoilers);
                float achievementHeight = hasAchievement ? 126 : 0;
                float titleHeight = descriptionTitleHeight;
                string[] paragraphs = descriptionParagraphs;
                float bodyHeight = descriptionBodyHeight;
                detailScroll = GUI.BeginScrollView(ContentRect(500, 54, 612, 374), detailScroll, ContentRect(0, 0, 588, titleHeight + achievementHeight + galleryHeight + bodyHeight + 28));
                GUI.Label(ContentRect(0, 0, 584, titleHeight), selected.Title, heading);
                if (hasAchievement) DrawAchievement(achievementKey, titleHeight + 12);
                bool pagedGallery = targets.Any(t => t.Wide);
                float paragraphY = titleHeight + achievementHeight + 20;
                int startParagraph = 0;
                if (pagedGallery && paragraphs.Length > 0)
                {
                    float leadHeight = descriptionHeights[0];
                    GUI.Label(ContentRect(8, paragraphY, 564, leadHeight), paragraphs[0], text);
                    paragraphY += leadHeight + 18; startParagraph = 1;
                }
                if (targets.Length > 0) DrawStepImages(targets, paragraphY);
                paragraphY += galleryHeight;
                for (int paragraphIndex = startParagraph; paragraphIndex < paragraphs.Length; paragraphIndex++)
                {
                    string part = paragraphs[paragraphIndex];
                    float ph = descriptionHeights[paragraphIndex];
                    GUI.Label(ContentRect(8, paragraphY, 564, ph), part, text);
                    paragraphY += ph + 18;
                }
                GUI.EndScrollView();
                if (selected.ReferenceOnly) GUI.Label(ContentRect(502, 434, 604, 40), Wisp.Core.I18n.T("Обзор · не входит в счётчик задач"), muted);
                else if (ProfileAchievements.Steps.ContainsKey(selected.Id)) GUI.Label(ContentRect(502, 434, 604, 40), Wisp.Core.I18n.T("Достижение проверяется автоматически · игровой профиль"), muted);
                else if (Completion.Confirmed(selected, player)) GUI.Label(ContentRect(502, 434, 604, 40), Wisp.Core.I18n.T("✓ Подтверждено этим сохранением"), muted);
                else if (ProfileAchievements.Confirmed(selected, player)) GUI.Label(ContentRect(502, 434, 604, 40), Wisp.Core.I18n.T("✓ Получено в игровом профиле"), muted);
                else if (Choose(ContentRect(500, 434, 612, 40), Done(selected) ? Wisp.Core.I18n.T("✓ Снять ручную отметку") : Wisp.Core.I18n.T("○ Отметить выполненным"))) { mod.Progress.Mark(selected.Id, !Done(selected)); InvalidateView(); }
            }
            var collectionId = steps[Mathf.Clamp(stepIndex, 0, steps.Length - 1)].Collection;
            if (collectionId.Length > 0 && MapButton(ContentRect(665, 498, 276, 32), I18n.T("Коллекция · Y"))) OpenCollection(collectionId);
            GUI.enabled = stepIndex > 0;
            if (MapButton(ContentRect(500, 498, 145, 32), Wisp.Core.I18n.T("‹ Назад"))) SelectStep(stepIndex - 1);
            GUI.enabled = stepIndex < steps.Length - 1;
            if (MapButton(ContentRect(967, 498, 145, 32), Wisp.Core.I18n.T("Далее ›"))) SelectStep(stepIndex + 1);
            GUI.enabled = true;
        }

        private System.Collections.Generic.Dictionary<string, AchievementCaption> achievementCaptions;
        private sealed class AchievementCaption { public string Title = ""; public string Description = ""; }
        private void DrawAchievement(string key, float top)
        {
            if (achievementCaptions == null)
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Wisp.achievements.json"))
                using (var reader = new StreamReader(stream))
                    achievementCaptions = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, AchievementCaption>>(reader.ReadToEnd());
            AchievementCaption fallback; achievementCaptions.TryGetValue(key, out fallback);
            string title = "";
            if (fallback != null) title = I18n.T(fallback.Title);
            else if (string.IsNullOrEmpty(title) || title.StartsWith("#")) title = Wisp.Core.I18n.T("Достижение");
            Rule(ContentRect(8, top, 564, 114), new Color(.06f, .10f, .13f));
            var icon = media.AchievementIcon(key);
            if (icon != null) GUI.DrawTexture(ContentRect(18, top + 20, 60, 60), icon, ScaleMode.ScaleToFit);
            else DrawImageStatus(ContentRect(18, top + 20, 60, 60), "embedded:achievements/" + key + ".jpg");
            GUI.Label(ContentRect(88, top + 4, 468, 24), Wisp.Core.I18n.T("ДОСТИЖЕНИЕ"), small);
            GUI.Label(ContentRect(88, top + 28, 468, 48), title, text);
            string status = !player.AchievementKnown(key) ? Wisp.Core.I18n.T("Статус профиля пока недоступен") : player.IsUnlocked(key) ? Wisp.Core.I18n.T("✓ Получено в игровом профиле") : Wisp.Core.I18n.T("○ Ещё не получено");
            GUI.Label(ContentRect(88, top + 79, 468, 30), status, small);
        }

        private int stepImageIndex;
        private bool expandedStepImage;
        private void OpenStepImage() { expandedStepImage = true; mapPan = Vector2.zero; mapZoom = 1; }
        private bool DrawExpandedStepImage()
        {
            if (!expandedStepImage) return false;
            StepImage[] targets;
            var selected = RouteSteps[Mathf.Clamp(stepIndex, 0, RouteSteps.Length - 1)];
            if ((selected.Spoiler && !mod.Settings.ShowSpoilers) || !media.TryStepImages(Current.Goal, Current.Id, selected.Id, out targets) || targets.Length == 0)
            { expandedStepImage = false; return false; }
            stepImageIndex = (stepImageIndex % targets.Length + targets.Length) % targets.Length;
            var target = targets[stepImageIndex];
            if (MapButton(ContentRect(0, 0, 160, 34), Wisp.Core.I18n.T("‹ К описанию"))) expandedStepImage = false;
            GUI.Label(ContentRect(180, 0, 900, 34), I18n.T(target.Label), columnHeading);
            MapControls(ContentRect(0, 48, 360, 32));
            if (MapButton(ContentRect(630, 48, 80, 32), "‹")) { stepImageIndex--; mapPan = Vector2.zero; mapZoom = 1; }
            GUI.Label(ContentRect(720, 48, 260, 32), (stepImageIndex + 1) + " / " + targets.Length, pageStyle);
            if (MapButton(ContentRect(1015, 48, 80, 32), "›")) { stepImageIndex++; mapPan = Vector2.zero; mapZoom = 1; }
            DrawMapTexture(ContentRect(0, 90, 1112, 405), target.Url);
            GUI.Label(ContentRect(0, 502, 1000, 26), "Hollow Knight Wiki / Team Cherry", small);
            return true;
        }

        private void DrawStepImages(StepImage[] targets, float top)
        {
            if (targets.Any(t => t.Wide))
            {
                stepImageIndex = (stepImageIndex % targets.Length + targets.Length) % targets.Length;
                if (MapButton(ContentRect(8, top, 48, 32), "‹")) stepImageIndex = (stepImageIndex + targets.Length - 1) % targets.Length;
                if (MapButton(ContentRect(522, top, 48, 32), "›")) stepImageIndex = (stepImageIndex + 1) % targets.Length;
                GUI.Label(ContentRect(66, top, 450, 32), Wisp.Core.I18n.T("Иллюстрация ") + (stepImageIndex + 1) + " / " + targets.Length + Wisp.Core.I18n.T(" · Y: следующая"), pageStyle);
                var target = targets[stepImageIndex];
                bool caption = !string.IsNullOrWhiteSpace(target.Label);
                if (caption) GUI.Label(ContentRect(8, top + 36, 564, 48), I18n.T(target.Label), muted);
                float imageTop = caption ? 88 : 44;
                var picture = media.Get(target.Url);
                if (picture != null) GUI.DrawTexture(ContentRect(8, top + imageTop, 564, 358 - imageTop), picture, ScaleMode.ScaleToFit);
                else DrawImageStatus(ContentRect(8, top + imageTop, 564, 80), target.Url);
                if (MapButton(ContentRect(8, top + 360, 564, 32), Wisp.Core.I18n.T("Развернуть · нажатие RS"))) OpenStepImage();
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                float x = 8 + (i % 3) * 188, y = top + (i / 3) * 156;
                var card = ContentRect(x, y, 180, 148);
                Rule(card, new Color(.048f, .077f, .10f));
                var texture = media.Get(targets[i].Url);
                if (texture != null) GUI.DrawTexture(ContentRect(x + 12, y + 8, 156, 96), texture, ScaleMode.ScaleToFit);
                else DrawImageStatus(ContentRect(x + 8, y + 8, 164, 96), targets[i].Url);
                GUI.Label(ContentRect(x + 6, y + 108, 168, 40), I18n.T(targets[i].Label), cardStyle);
            }
            GUI.Label(ContentRect(8, top + ((targets.Length + 2) / 3) * 156, 564, 26), Wisp.Core.I18n.T("Изображения: Hollow Knight Wiki / Team Cherry"), small);
        }

        private void PagedList(Rect viewport, string[] labels, int selected, ref int page, ref bool reveal, bool focused, Action<int> select, bool[] hidden = null)
        {
            const int count = 5;
            int[] indices = Enumerable.Range(0, labels.Length).Where(i => hidden == null || !hidden[i] || i == selected).ToArray();
            int selectedPosition = Array.IndexOf(indices, selected);
            int last = Math.Max(0, (indices.Length - 1) / count);
            if (reveal) { page = Math.Max(0, selectedPosition) / count; reveal = false; }
            var evt = Event.current;
            if (evt.type == EventType.ScrollWheel && viewport.Contains(evt.mousePosition))
            { page = Mathf.Clamp(page + (evt.delta.y > 0 ? 1 : -1), 0, last); evt.Use(); }
            page = Mathf.Clamp(page, 0, last);
            const float footerHeight = 32, footerGap = 12, cardGap = 8;
            float cardHeight = (viewport.height - footerHeight - footerGap - cardGap * (count - 1)) / count;
            float rowHeight = cardHeight + cardGap;
            for (int row = 0; row < count; row++)
            {
                int position = page * count + row;
                if (position >= indices.Length) break;
                int index = indices[position];
                Rect cell = new Rect(viewport.x, viewport.y + row * rowHeight, viewport.width, cardHeight);
                string label = labels[index];
                string suffix = "";
                int split = label.LastIndexOf('\n');
                if (split >= 0) { suffix = label.Substring(split); label = label.Substring(0, split); }
                bool shortened = false;
                while (label.Length > 1 && button.CalcHeight(new GUIContent(label + (shortened ? "…" : "") + suffix), cell.width) > cell.height - 4)
                { label = label.Substring(0, label.Length - 1); shortened = true; }
                Rule(cell, index == selected ? new Color(.085f,.14f,.18f) : new Color(.048f,.077f,.10f));
                Border(cell, new Color(.12f,.20f,.25f));
                if (Choose(cell, label + (shortened ? "…" : "") + suffix, index == selected, focused && index == selected)) select(index);
            }
            float bottom = viewport.yMax - footerHeight;
            GUI.enabled = page > 0;
            if (MapButton(new Rect(viewport.x, bottom, footerHeight, footerHeight), "‹")) page--;
            GUI.enabled = page < last;
            if (MapButton(new Rect(viewport.xMax - footerHeight, bottom, footerHeight, footerHeight), "›")) page++;
            GUI.enabled = true;
            GUI.Label(new Rect(viewport.x + 40, bottom, viewport.width - 80, footerHeight), (page + 1) + " / " + (last + 1), pageStyle);
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

        private void ResetJournalRegion()
        {
            cachedEnemies = null; mapSelectionKey = null;
            journalRegion = ""; allRegions = false; choosingJournalRegion = false;
            enemyIndex = enemyMapIndex = 0;
            enemyScroll = habitatScroll = mapPan = Vector2.zero;
            mapZoom = 1;  expandedEnemyMap = false;
        }

        private static string RegionName(string region)
        {
            switch (region)
            {
                case "Forgotten Crossroads": return Wisp.Core.I18n.T("Забытое перепутье");
                case "Howling Cliffs": return Wisp.Core.I18n.T("Воющие утесы");
                case "King's Pass": return Wisp.Core.I18n.T("Королевская тропа");
                case "Greenpath": return Wisp.Core.I18n.T("Зеленая тропа");
                case "City of Tears": return Wisp.Core.I18n.T("Город слез");
                case "Colosseum of Fools": return Wisp.Core.I18n.T("Колизей глупцов");
                case "Infected Crossroads": return Wisp.Core.I18n.T("Заражённое перепутье");
                case "Ancient Basin": return Wisp.Core.I18n.T("Древний котлован");
                case "Deepnest": return Wisp.Core.I18n.T("Глубинное гнездо");
                case "Fungal Wastes": return Wisp.Core.I18n.T("Грибные пустоши");
                case "Royal Waterways": return Wisp.Core.I18n.T("Королевские стоки");
                case "Crystal Peak": return Wisp.Core.I18n.T("Кристальный пик");
                case "Resting Grounds": return Wisp.Core.I18n.T("Земли упокоения");
                case "Queen's Gardens": return Wisp.Core.I18n.T("Сады королевы");
                case "Fog Canyon": return Wisp.Core.I18n.T("Туманный каньон");
                case "Godhome": return Wisp.Core.I18n.T("Божий кров");
                case "Kingdom's Edge": return Wisp.Core.I18n.T("Край королевства");
                case "The Abyss": return Wisp.Core.I18n.T("Бездна ");
                case "The Hive": return Wisp.Core.I18n.T("Улей");
                case "Dirtmouth": return Wisp.Core.I18n.T("Грязьмут");
                case "Grey Prince Zote": return Wisp.Core.I18n.T("Серый принц Зот");
                case "Warrior Dreams": return Wisp.Core.I18n.T("Духи-воители");
                case "The Grimm Troupe": return Wisp.Core.I18n.T("Мрачная труппа");
                case "White Palace": return Wisp.Core.I18n.T("Белый дворец");
                case "Story": return Wisp.Core.I18n.T("Сюжет");
                default: return Wisp.Core.I18n.T("Область не определена");
            }
        }

        private string JournalRegionLabel()
        {
            string region = string.IsNullOrEmpty(journalRegion) ? liveRegion : journalRegion;
            return RegionName(region).Trim();
        }

        private int viewRevision;
        private int enemyRevision = -1;
        private string enemyFilter;
        private Enemy[] cachedEnemies;
        private string mapSelectionKey;
        private EnemyMedia orderedMedia;
        private void InvalidateView() { viewRevision++; }
        private Enemy[] FilteredEnemies()
        {
            string region = string.IsNullOrEmpty(journalRegion) ? liveRegion : journalRegion;
            string filter = region + "|" + allRegions + "|" + mod.Settings.HideCompleted + "|" + mod.Settings.ShowSpoilers;
            if (cachedEnemies == null || enemyRevision != viewRevision || enemyFilter != filter) {
                string selected = cachedEnemies != null && enemyIndex >= 0 && enemyIndex < cachedEnemies.Length ? cachedEnemies[enemyIndex].Id : null;
                cachedEnemies = catalog.Enemies.Where(e => (allRegions || e.Regions.Contains(region)) &&
                    (!allRegions || mod.Settings.ShowSpoilers || StatusOf(e).Discovered || e.Regions.Contains(liveRegion)) &&
                    (!mod.Settings.HideCompleted || !StatusOf(e).Complete)).OrderBy(e => e.Optional).ThenBy(e => StatusOf(e).Complete).ToArray();
                int found = Array.FindIndex(cachedEnemies, e => e.Id == selected);
                enemyIndex = found >= 0 ? found : Mathf.Clamp(enemyIndex, 0, Math.Max(0, cachedEnemies.Length - 1));
                enemyRevision = viewRevision; enemyFilter = filter;
            }
            return cachedEnemies;
        }
        private EnemyMedia MediaFor(Enemy enemy)
        {
            string region = string.IsNullOrEmpty(journalRegion) ? liveRegion : journalRegion;
            bool? phase = region == "Infected Crossroads" ? true : region == "Forgotten Crossroads" ? false : infected;
            string key = enemy.Id + "|" + region + "|" + phase;
            if (key != mapSelectionKey) {
                EnemyMedia source; media.Enemies.TryGetValue(enemy.Id, out source);
                orderedMedia = source == null ? null : new EnemyMedia { Portrait = source.Portrait, HabitatMaps = HabitatMaps.Order(source.HabitatMaps, region, phase) };
                mapSelectionKey = key; enemyMapIndex = 0; mapPan = Vector2.zero; mapZoom = 1;
            }
            return orderedMedia;
        }
        private string EnemyName(Enemy enemy)
        {
            return enemy.Name;
        }

        private bool DrawExpandedEnemyMap()
        {
            if (!expandedEnemyMap) return false;
            var enemies = FilteredEnemies();
            if (enemies.Length == 0) { expandedEnemyMap = false; return false; }
            enemyIndex = Mathf.Clamp(enemyIndex, 0, enemies.Length - 1);
            var enemy = enemies[enemyIndex];
            var entry = MediaFor(enemy);
            if (expandedEnemyMap && entry != null && entry.HabitatMaps.Length > 0)
            {
                enemyMapIndex = (enemyMapIndex % entry.HabitatMaps.Length + entry.HabitatMaps.Length) % entry.HabitatMaps.Length;
                string expandedUrl = entry.HabitatMaps[enemyMapIndex].Url;
                GUI.DrawTexture(ContentRect(0, 0, 1112, 530), panelTexture);
                if (Choose(ContentRect(0, 0, 210, 34), Wisp.Core.I18n.T("‹ К записи врага"))) expandedEnemyMap = false;
                GUI.Label(ContentRect(220, 0, 530, 34), HabitatLabel(entry, enemyMapIndex), text);
                MapControls(ContentRect(770, 0, 322, 34));
                DrawMapTexture(ContentRect(0, 44, 1112, 450), expandedUrl);
                if (Choose(ContentRect(0, 497, 450, 30), Wisp.Core.I18n.T("Следующее место обитания ›"))) { enemyMapIndex++; mapZoom = 1; mapPan = Vector2.zero; }
                GUI.Label(ContentRect(500, 497, 610, 30), Wisp.Core.I18n.T("Hollow Knight Wiki / Team Cherry · справочная карта"), columnHeading);
                return true;
            }
            expandedEnemyMap = false;
            return false;
        }

        private bool choosingJournalRegion;
        private int journalRegionIndex, journalRegionPage;
        private bool revealJournalRegion = true;
        private string[] cachedJournalRegions;
        private string[] JournalRegions { get { return cachedJournalRegions ?? (cachedJournalRegions = new[] { "", "*" }.Concat(catalog.Enemies.SelectMany(e => e.Regions).Distinct().Where(r => r != "Story" && r != "Warrior Dreams" && r != "Grey Prince Zote" && r != "The Grimm Troupe").OrderBy(RegionName)).ToArray()); } }
        private void OpenJournalRegions()
        {
            var regions = JournalRegions;
            journalRegionIndex = Math.Max(0, Array.IndexOf(regions, allRegions ? "*" : journalRegion));
            revealJournalRegion = true; choosingJournalRegion = true; contentFocus = false;
        }
        private void SelectJournalRegion(int index)
        {
            string selected = JournalRegions[index]; ResetJournalRegion();
            allRegions = selected == "*"; journalRegion = allRegions ? "" : selected;
            padPane = 0;
        }
        private void DrawJournalRegions()
        {
            GUI.Label(ContentRect(0, 0, 850, 34), Wisp.Core.I18n.T("ЛОКАЦИЯ · ВЫБОР"), columnHeading);
            if (Choose(ContentRect(930, 0, 170, 34), Wisp.Core.I18n.T("Назад · B"))) choosingJournalRegion = false;
            var labels = JournalRegions.Select(r => r == "" ? Wisp.Core.I18n.T("Текущая область · ") + RegionName(liveRegion) : r == "*" ? Wisp.Core.I18n.T("Все области") : RegionName(r)).ToArray();
            PagedList(ContentRect(0, 54, 650, 476), labels, journalRegionIndex, ref journalRegionPage, ref revealJournalRegion, true, SelectJournalRegion);
            GUI.Label(ContentRect(700, 70, 390, 200), Wisp.Core.I18n.T("Выбери область, чтобы посмотреть её врагов.\n\nТекущая область следует за местоположением героя."), text);
        }

        private void DrawJournal()
        {
            if (choosingJournalRegion) { DrawJournalRegions(); return; }
            if (DrawExpandedEnemyMap()) return;
            GUI.Label(ContentRect(30, 0, 180, 34), padPane == 0 ? Wisp.Core.I18n.T("ВРАГИ · ВЫБОР") : Wisp.Core.I18n.T("ВРАГИ"), columnHeading);
            KeyHint(ContentRect(0, 2, 24, 26), "←");
            KeyHint(ContentRect(464, 2, 24, 26), "→");
            if (Choose(ContentRect(238, 0, 220, 34), I18n.T("СВЕДЕНИЯ"), padPane == 1)) { padPane = 1; contentFocus = false; }
            if (Choose(ContentRect(500, 0, 556, 34), Wisp.Core.I18n.T("Места обитания"), true, padPane == 2 && !contentFocus)) { padPane = 2; contentFocus = false; }
            // Long area names wrap inside their own space; the input hint never wraps with them.
            if (GUI.Button(ContentRect(0, 48, 180, 48), allRegions ? Wisp.Core.I18n.T("Все области") : JournalRegionLabel(), regionStyle)) OpenJournalRegions();
            if (MapButton(ContentRect(186, 58, 28, 28), "X")) OpenJournalRegions();
            var enemies = FilteredEnemies();
            if (enemies.Length == 0) { GUI.Label(ContentRect(0, 104, 1100, 80), Wisp.Core.I18n.T("Нет врагов по этому фильтру. Выбери другую область или покажи выполненные записи."), text); return; }
            enemyIndex = Mathf.Clamp(enemyIndex, 0, enemies.Length - 1);
            enemyScroll = GUI.BeginScrollView(ContentRect(0, 100, 216, 430), enemyScroll, ContentRect(0, 0, 190, enemies.Length * 96));
            for (int i = Mathf.Max(0, Mathf.FloorToInt(enemyScroll.y / 96)); i < Mathf.Min(enemies.Length, Mathf.CeilToInt((enemyScroll.y + 430) / 96)); i++)
                if (Choose(ContentRect(0, i * 96, 190, 90), (StatusOf(enemies[i]).Complete ? "✓ " : "○ ") + EnemyName(enemies[i]), i == enemyIndex, i == enemyIndex && padPane == 0 && !contentFocus))
                { padPane = 0; contentFocus = false; enemyIndex = i; enemyMapIndex = 0;  mapPan = Vector2.zero; mapZoom = 1; habitatScroll = Vector2.zero; }
            GUI.EndScrollView();

            var enemy = enemies[enemyIndex]; var status = StatusOf(enemy);
            var entry = MediaFor(enemy);
            var portrait = entry == null ? null : media.Get(entry.Portrait);
            if (portrait != null) GUI.DrawTexture(ContentRect(250, 108, 152, 102), portrait, ScaleMode.ScaleToFit);
            else DrawImageStatus(ContentRect(250, 108, 152, 102), entry == null ? "" : entry.Portrait);
            GUI.Label(ContentRect(238, 54, 240, 58), EnemyName(enemy), heading);
            GUI.Label(ContentRect(238, 214, 240, 54), !status.Available ? Wisp.Core.I18n.T("Счётчик недоступен") : status.Complete ? Wisp.Core.I18n.T("✓ Запись завершена") : Wisp.Core.I18n.T("Осталось победить: ") + status.Remaining, muted);
            Divider(ContentRect(238, 280, 240, 1));
            string warning = Habitat.Description(enemy, player);
            if (string.IsNullOrEmpty(warning)) warning = enemy.Optional ? Wisp.Core.I18n.T("Дополнительная запись дневника.") : Wisp.Core.I18n.T("Выбери карту справа. Точки мест обитания уже нанесены на изображение.");
            string regions = string.Join(" · ", enemy.Regions.Select(r => { var chapter = catalog.Chapters.FirstOrDefault(c => c.English.Replace('’', '\'') == r); return chapter == null ? RegionName(r) : chapter.Title; }));
            string body = warning + Wisp.Core.I18n.T("\n\nОбласти: ") + regions;
            float bodyHeight = text.CalcHeight(new GUIContent(body), 218 * ContentScale);
            habitatScroll = GUI.BeginScrollView(ContentRect(238, 294, 240, 236), habitatScroll, ContentRect(0, 0, 218, bodyHeight));
            GUI.Label(ContentRect(0, 0, 218, bodyHeight), body, text);
            GUI.EndScrollView();
            if (contentFocus && padPane == 2) FocusCorners(ContentRect(500, 54, 612, 476));
            if (padPane == 1) FocusCorners(ContentRect(238, 294, 240, 236));
            GUI.Label(ContentRect(500, 54, 260, 34), Wisp.Core.I18n.T("Где искать"), heading);
            if (entry == null || entry.HabitatMaps.Length == 0)
            { GUI.Label(ContentRect(500, 120, 612, 160), Wisp.Core.I18n.T("Отдельной карты пока нет. Известные области перечислены слева."), text); return; }
            if (MapButton(ContentRect(950, 54, 150, 34), Wisp.Core.I18n.T("Развернуть"))) { expandedEnemyMap = true; contentFocus = true; padPane = 2; }
            enemyMapIndex = (enemyMapIndex % entry.HabitatMaps.Length + entry.HabitatMaps.Length) % entry.HabitatMaps.Length;
            string url = entry.HabitatMaps[enemyMapIndex].Url;
            if (MapButton(ContentRect(500, 92, 612, 40), HabitatLabel(entry, enemyMapIndex) + " · " + (enemyMapIndex + 1) + "/" + entry.HabitatMaps.Length + " ›"))
            { enemyMapIndex = (enemyMapIndex + 1) % entry.HabitatMaps.Length; mapPan = Vector2.zero; mapZoom = 1; url = entry.HabitatMaps[enemyMapIndex].Url; }
            MapControls(ContentRect(500, 140, 322, 34));
            DrawMapTexture(ContentRect(500, 184, 612, 294), url);
            GUI.Label(ContentRect(500, 490, 612, 38), "Hollow Knight Wiki / Team Cherry", small);
        }

        private int atlasIndex, atlasPage;
        private bool revealAtlas = true;
        private readonly string[] atlasIds = { "dirtmouth", "kings-pass", "crossroads", "greenpath", "fog-canyon", "fungal", "city", "waterways", "crystal", "resting", "deepnest", "basin", "abyss", "edge", "queens-gardens" };
        private string[] atlasNames { get { return new[] { Wisp.Core.I18n.T("Дёртмаут"), Wisp.Core.I18n.T("Воющие утёсы"), Wisp.Core.I18n.T("Забытое перепутье"), Wisp.Core.I18n.T("Зелёная тропа"), Wisp.Core.I18n.T("Туманный каньон"), Wisp.Core.I18n.T("Грибные пустоши"), Wisp.Core.I18n.T("Город слёз"), Wisp.Core.I18n.T("Королевские стоки"), Wisp.Core.I18n.T("Кристальный пик"), Wisp.Core.I18n.T("Земли упокоения"), Wisp.Core.I18n.T("Глубинное гнездо"), Wisp.Core.I18n.T("Древний котлован"), Wisp.Core.I18n.T("Бездна"), Wisp.Core.I18n.T("Край королевства"), Wisp.Core.I18n.T("Сады королевы") }; } }
        private void SelectAtlas(int index)
        { atlasIndex = Mathf.Clamp(index, 0, atlasIds.Length - 1); revealAtlas = true;  mapPan = Vector2.zero; mapZoom = 1; }
        private void DrawAtlas()
        {
            GUI.Label(ContentRect(0, 0, 230, 34), Wisp.Core.I18n.T("ЗОНЫ ХАЛЛОУНЕСТА"), columnHeading);
            GUI.Label(ContentRect(260, 0, 450, 34), atlasNames[atlasIndex], text);
            PagedList(ContentRect(0, 54, 240, 476), atlasNames, atlasIndex, ref atlasPage, ref revealAtlas, !contentFocus,
                index => { SelectAtlas(index); contentFocus = false; });
            MapControls(ContentRect(260, 54, 322, 34));
            DrawMapTexture(ContentRect(260, 105, 840, 374), media.RegionSource(atlasIds[atlasIndex]));
            GUI.Label(ContentRect(260, 490, 840, 36), Wisp.Core.I18n.T("Hollow Knight Wiki / Team Cherry · полная область"), small);
        }

        private void DrawAreaMap(Rect viewport)
        {
            MapControls(new Rect(viewport.x, viewport.y, 322, 34));
            GUI.Label(new Rect(viewport.x, viewport.y + 35, viewport.width, 26), Wisp.Core.I18n.T("A: управление · B: назад"), small);
            DrawMapTexture(new Rect(viewport.x, viewport.y + 66, viewport.width, viewport.height - 100), media.RegionSource(CurrentMapId));
            GUI.Label(new Rect(viewport.x, viewport.y + viewport.height - 30, viewport.width, 30), Wisp.Core.I18n.T("Hollow Knight Wiki / Team Cherry · полная область, включая спойлеры"), small);
        }

        private void MapControls(Rect rect)
        {
            if (MapButton(new Rect(rect.x, rect.y, 34, 32), "−")) mapZoom = Mathf.Max(1, mapZoom / 1.25f);
            GUI.Label(new Rect(rect.x + 38, rect.y, 55, 32), Mathf.RoundToInt(mapZoom * 100) + "%", zoomStyle);
            if (MapButton(new Rect(rect.x + 97, rect.y, 34, 32), "+")) mapZoom = Mathf.Min(5, mapZoom * 1.25f);
            if (MapButton(new Rect(rect.x + 139, rect.y, 152, 32), Wisp.Core.I18n.T("Вписать · Y"))) { mapPan = Vector2.zero; mapZoom = 1; }
        }

        private bool MapButton(Rect rect, string label, bool selected = false)
        {
            var style = mapStyle;
            if (selected) Underline(rect, label, style, new Color(.65f, .76f, .85f));
            return GUI.Button(rect, label, style);
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

        private void DrawMapTexture(Rect viewport, string url)
        {
            var texture = media.Get(url);
            if (texture == null) { DrawImageStatus(viewport, url); return; }
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
            detailScroll = Vector2.zero;
        }

        private void DrawSettings()
        {
            KeyHint(ContentRect(0, 0, 36, 32), "LT");
            string[] goals = { "112", "speed", "steel" };
            string[] names = { Wisp.Core.I18n.T("A · 112% и достижения"), Wisp.Core.I18n.T("B · Скорость и выборы"), Wisp.Core.I18n.T("C · Стальная душа") };
            for (int i = 0; i < 3; i++)
                if (Choose(ContentRect(42 + i * 342, 0, 338, 38), names[i], mod.Progress.RouteGoal == goals[i], padPane == 0 && mod.Progress.RouteGoal == goals[i])) SetGoal(goals[i]);
            KeyHint(ContentRect(1076, 0, 36, 32), "RT");
            string goalText = mod.Progress.RouteGoal == "steel" ? Wisp.Core.I18n.T("C · Стальная душа до 100%, без лимита времени. Сначала обычный финал, затем добор по списку маршрута. Режим Steel Soul нужно выбрать при создании игры.") : mod.Progress.RouteGoal == "speed" ? Wisp.Core.I18n.T("B · Отдельное обычное сохранение: финал до 5 часов, затем 100% до 20 часов. Зота не спасать; Кузнеца убить; труппу изгнать.") : Wisp.Core.I18n.T("A · Основное обычное сохранение. Этапы A1–A15: 112%, коллекции, Сады королевы, концовки и пантеоны. Серый принц Зот — после пятого пантеона.");
            if (mod.Progress.RouteGoal == "steel" && !RouteGoals.IsSteelSave(player)) goalText += Wisp.Core.I18n.T("\nВнимание: этот слот не в режиме Стальной души.");
            GUI.Label(ContentRect(16, 66, 1080, 138), goalText, text);
            string[] labels = { Wisp.Core.I18n.T("Спойлеры: ") + (mod.Settings.ShowSpoilers ? Wisp.Core.I18n.T("показаны") : Wisp.Core.I18n.T("скрыты")), Wisp.Core.I18n.T("Выполненные задачи: ") + (mod.Settings.HideCompleted ? Wisp.Core.I18n.T("скрыты") : Wisp.Core.I18n.T("показаны")), Wisp.Core.I18n.T("Подсказка на паузе: ") + (mod.Settings.ShowPauseHint ? Wisp.Core.I18n.T("включена") : Wisp.Core.I18n.T("выключена")), I18n.English ? "Language: English" : "Язык: Русский" };
            for (int i = 0; i < labels.Length; i++) if (Choose(ContentRect(12, 150 + i * 54, 720, 46), labels[i], false, padPane == i + 1)) ToggleSetting(i + 1);
            GUI.Label(ContentRect(16, 398, 1080, 58), Wisp.Core.I18n.T("Цель и отметки хранятся отдельно для каждого сейва и сохраняются вместе с игрой. Смена цели не удаляет прогресс."), muted);
            GUI.Label(ContentRect(16, 472, 1080, 48), Wisp.Core.I18n.T("Карты и портреты открываются здесь. При первом просмотре изображений врагов нужен интернет; затем они доступны из локального кэша."), small);
        }

        private void CycleGoal(int direction)
        {
            string[] goals = { "112", "speed", "steel" };
            int index = Array.IndexOf(goals, mod.Progress.RouteGoal);
            SetGoal(goals[Mathf.Clamp(index + direction, 0, goals.Length - 1)]);
        }

        private void ToggleSetting(int index)
        {
            InvalidateView();
            if (index == 0) CycleGoal(1);
            if (index == 1) { mod.Settings.ShowSpoilers = !mod.Settings.ShowSpoilers;  }
            if (index == 2) { mod.Settings.HideCompleted = !mod.Settings.HideCompleted; revealStep = true; }
            if (index == 3) mod.Settings.ShowPauseHint = !mod.Settings.ShowPauseHint;
            if (index == 4)
            {
                I18n.English = !I18n.English;
                mod.Settings.Language = I18n.English ? "en" : "ru";
                catalog = Wisp.Game.Catalog.Load();
                cachedJournalRegions = null; mapSelectionKey = null; cachedEnemies = null; journalStatuses.Clear();
                detailScroll = habitatScroll = Vector2.zero;
                revealChapter = revealStep = revealAtlas = revealJournalRegion = true;
                mod.SavePreferences();
            }
        }

    }
}
