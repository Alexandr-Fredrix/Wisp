using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Wisp.Core;
using Wisp.Game;

namespace Wisp.UI
{
    public sealed partial class GuideWindow : MonoBehaviour
    {
        private WispMod mod;
        private Catalog catalog;
        private readonly PlayerReader player = new PlayerReader();
        private readonly Dictionary<string, Sprite> portraits = new Dictionary<string, Sprite>();
        private EventSystem suspendedEvents;
        private bool navigationWasEnabled;
        private InControl.HollowKnightInputModule inputModule;
        private bool mouseWasEnabled;
        private bool ownsPause, opening, locating = true;
        private bool stickChordHeld;
        private float nextNavigation;
        private int padPane = 0;
        private int detailChoice;
        private bool contentFocus;
        private string notice = "";
        private float noticeUntil;
        private readonly Dictionary<string, int> lastKills = new Dictionary<string, int>();
        private bool cursorWasVisible;
        private CursorLockMode cursorLock;
        private bool open;
        private int chapterIndex;
        private int stepIndex;
        private int tab;
        private int primaryTab;
        private bool revealChapter = true, revealStep = true;
        private string query = "";
        private bool allRegions;
        private Vector2 chapterScroll, stepScroll, detailScroll, enemyScroll;
        private GUIStyle text, heading, button, active, muted;
        private Texture2D panelTexture, buttonTexture, activeTexture;
        private Font font;
        private string liveChapter = "";
        private string liveRegion = "";
        private float nextRefresh;
        private string hudTask = "";
        private Vector2 mapPan;
        private float mapZoom = 1;
        private bool mapTab;

        public void Initialize(WispMod owner, Catalog data)
        {
            mod = owner;
            catalog = data;
            media = new MediaLibrary(this);
            ResetView();
        }

        public void ResetView()
        {
            Close();
            if (catalog == null || mod == null) return;
            chapterIndex = Math.Max(0, Array.FindIndex(RouteChapters, c => c.Id == mod.Progress.ChapterId));
            stepIndex = Math.Max(0, Array.FindIndex(RouteSteps, s => s.Id == mod.Progress.StepId));
            detailScroll = stepScroll = chapterScroll = enemyScroll = Vector2.zero;
            liveChapter = liveRegion = "";
            locating = true;
            lastKills.Clear();
            nextRefresh = 0;
            portraits.Clear();


        }

        private Chapter[] RouteChapters { get { return catalog.PdfChapters.Where(c => c.Goal == mod.Progress.RouteGoal).ToArray(); } }
        private Step[] RouteSteps { get { return Current.Steps.Where(s => !s.ReferenceOnly || s.Id.EndsWith("-guide")).ToArray(); } }
        private Chapter Current { get { return RouteChapters[Mathf.Clamp(chapterIndex, 0, RouteChapters.Length - 1)]; } }
        private string CurrentMapId { get { return RouteSteps[Mathf.Clamp(stepIndex, 0, RouteSteps.Length - 1)].MapChapter; } }
        private bool InSession
        {
            get { var gm = GameManager.instance; return gm != null && (gm.gameState == GlobalEnums.GameState.PLAYING || gm.gameState == GlobalEnums.GameState.PAUSED) && HeroController.instance != null; }
        }
        private bool Paused
        {
            get { return GameManager.instance != null && HeroController.instance != null && GameManager.instance.IsGamePaused(); }
        }

        private void Update()
        {
            if (mod == null) return;
            GuideInputGuard.Tick();
            if (!InSession) { if (opening) return; if (open) Close(); GuideInputGuard.Clear(); return; }
            if (Time.unscaledTime >= nextRefresh)
            {
                Refresh();
                nextRefresh = Time.unscaledTime + 1f;
            }
            var device = InControl.InputManager.ActiveDevice;
            bool chord = device.LeftStickButton.IsPressed && device.RightStickButton.IsPressed;
            bool toggle = Input.GetKeyDown(KeyCode.F8) || (chord && !stickChordHeld);
            stickChordHeld = chord;
            if (toggle) { if (open) Close(); else if (!opening) StartCoroutine(OpenFromGame()); }
            if (open && !Paused) { Close(); return; }
            if (!open) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (device.Action2.WasPressed)
            {
                if (expandedStepImage) expandedStepImage = false;
                else if (choosingJournalRegion) choosingJournalRegion = false;
                else if (contentFocus || expandedEnemyMap) { contentFocus = false; expandedEnemyMap = false; }
                else if (padPane > 0) padPane--;
                else Close();
                return;
            }
            if (device.LeftBumper.WasPressed) CycleMainTab(-1);
            if (device.RightBumper.WasPressed) CycleMainTab(1);
            if (expandedStepImage)
            {
                mapPan += MapStick(device, true) * Time.unscaledDeltaTime * 350;
                mapZoom = Mathf.Clamp(mapZoom + (device.RightTrigger.Value - device.LeftTrigger.Value) * Time.unscaledDeltaTime * 2, 1, 5);
                if (device.DPadLeft.WasPressed) { stepImageIndex--; mapPan = Vector2.zero; mapZoom = 1; }
                if (device.DPadRight.WasPressed) { stepImageIndex++; mapPan = Vector2.zero; mapZoom = 1; }
                if (device.Action4.WasPressed) { mapPan = Vector2.zero; mapZoom = 1; }
                return;
            }
            bool onMap = contentFocus && ((tab == 0 && mapTab) || tab == 1 || tab == 3);
            int vertical = device.DPadUp.IsPressed || (!onMap && device.LeftStickY.Value > .5f) ? -1 : device.DPadDown.IsPressed || (!onMap && device.LeftStickY.Value < -.5f) ? 1 : 0;
            int horizontal = device.DPadLeft.IsPressed || (!onMap && device.LeftStickX.Value < -.5f) ? -1 : device.DPadRight.IsPressed || (!onMap && device.LeftStickX.Value > .5f) ? 1 : 0;
            int trigger = !contentFocus && device.LeftTrigger.WasPressed ? -1 : !contentFocus && device.RightTrigger.WasPressed ? 1 : 0;
            if (vertical == 0 && horizontal == 0) nextNavigation = 0;
            bool repeat = Time.unscaledTime >= nextNavigation && (vertical != 0 || horizontal != 0);
            if (repeat) nextNavigation = Time.unscaledTime + .2f;
            int dx = trigger != 0 ? trigger : repeat ? horizontal : 0;
            int dy = repeat ? vertical : 0;
            if (choosingJournalRegion)
            {
                if (dy != 0) { journalRegionIndex = Mathf.Clamp(journalRegionIndex + dy, 0, JournalRegions.Length - 1); revealJournalRegion = true; }
                if (device.Action1.WasPressed) SelectJournalRegion(journalRegionIndex);
                return;
            }
            if (tab == 0)
            {
                if (!contentFocus)
                {
                    if (padPane < 2)
                    {
                        if (dx != 0) padPane = Mathf.Clamp(padPane + dx, 0, 2);
                        else if (padPane == 0 && dy != 0) { SelectChapter(Mathf.Clamp(chapterIndex + dy, 0, RouteChapters.Length - 1)); chapterScroll.y = Mathf.Max(0, chapterIndex * 112 - 224); }
                        else if (padPane == 1 && dy != 0) { SelectStep(Mathf.Clamp(stepIndex + dy, 0, RouteSteps.Length - 1)); stepScroll.y = Mathf.Max(0, stepIndex * 112 - 224); }
                    }
                    else if (dx != 0) { detailChoice = Mathf.Clamp(detailChoice + dx, 0, 2); if (detailChoice < 2) mapTab = detailChoice == 1; }
                    if (device.Action1.WasPressed)
                    {
                        if (padPane < 2 && Visible(Current)) padPane++;
                        else if (detailChoice == 2) OpenRegionJournal();
                        else contentFocus = true;
                    }
                    if (device.Action3.WasPressed) { mapTab = true; detailChoice = 1; padPane = 2; }
                    if (device.Action4.WasPressed && padPane == 1 && !ProfileAchievements.Steps.ContainsKey(RouteSteps[stepIndex].Id) && !RouteSteps[stepIndex].ReferenceOnly && !Completion.Confirmed(RouteSteps[stepIndex], player) && !ProfileAchievements.Confirmed(RouteSteps[stepIndex], player))
                        mod.Progress.Mark(RouteSteps[stepIndex].Id, !Done(RouteSteps[stepIndex]));
                }
                // The selected description is scrollable without an extra confirm press.
                if (!mapTab && padPane == 2 && detailChoice == 0)
                {
                    if (device.Action4.WasPressed) stepImageIndex++;
                    if (device.RightStickButton.WasPressed) OpenStepImage();
                    float stick = Mathf.Abs(device.RightStickY.Value) > .18f ? device.RightStickY.Value : 0;
                    detailScroll.y = Mathf.Max(0, detailScroll.y + dy * 64 - stick * Time.unscaledDeltaTime * 350);
                }
            }
            else if (tab == 1 && !contentFocus)
            {
                if (padPane == 0)
                {
                    if (dy != 0) { enemyIndex = Mathf.Clamp(enemyIndex + dy, 0, Math.Max(0, FilteredEnemies().Length - 1)); enemyScroll.y = Mathf.Max(0, enemyIndex * 96 - 192); enemyMapIndex = 0; mapPan = Vector2.zero; mapZoom = 1;  }
                    if (dx > 0 || device.Action1.WasPressed) padPane = 1;
                }
                else
                {
                    if (dx < 0) padPane = 0;
                    if (device.Action1.WasPressed) contentFocus = true;
                    if (device.Action4.WasPressed) { enemyMapIndex++;  mapPan = Vector2.zero; mapZoom = 1; }
                }
                if (device.Action3.WasPressed) { OpenJournalRegions(); }
            }
            else if (tab == 3)
            {
                if (!contentFocus && dy != 0) SelectAtlas(atlasIndex + dy);
                if (!contentFocus && (device.Action1.WasPressed || dx > 0)) contentFocus = true;
            }
            else if (tab == 2)
            {
                if (dy != 0) padPane = Mathf.Clamp(padPane + dy, 0, 4);
                if (padPane == 0 && dx != 0) CycleGoal(dx);
                if (device.Action1.WasPressed) ToggleSetting(padPane);
            }
            if (onMap)
            {
                mapPan += MapStick(device, true) * Time.unscaledDeltaTime * 350;
                mapZoom = Mathf.Clamp(mapZoom + (device.RightTrigger.Value - device.LeftTrigger.Value) * Time.unscaledDeltaTime * 2, 1, 5);
                if (device.Action4.WasPressed) { mapPan = Vector2.zero; mapZoom = 1; }
            }
        }

        private void CycleMainTab(int direction)
        { int[] order = { 0, 1, 3, 2 }; ChangeTab(order[(Array.IndexOf(order, tab) + direction + order.Length) % order.Length]); }

        private static Vector2 MapStick(InControl.InputDevice device, bool includeLeft)
        {
            var input = new Vector2(device.RightStickX.Value, -device.RightStickY.Value);
            if (includeLeft) input += new Vector2(device.LeftStickX.Value, -device.LeftStickY.Value);
            return input.magnitude < .18f ? Vector2.zero : Vector2.ClampMagnitude(input, 1);
        }

        private IEnumerator OpenFromGame()
        {
            opening = true;
            GuideInputGuard.Capture();
            var manager = GameManager.instance;
            if (!Paused && manager != null && manager.gameState == GlobalEnums.GameState.PLAYING && manager.inputHandler.pauseAllowed)
            {
                yield return manager.StartCoroutine(GuideInputGuard.ToggleOwnedPause(manager));
                ownsPause = Paused;
            }
            if (Paused) Open();
            else GuideInputGuard.Release();
            opening = false;
        }

        private void Open()
        {
            if (!Paused || open) return;
            Refresh();
            ResetJournalRegion();
            open = true; expandedStepImage = false;
            contentFocus = false; padPane = 0; detailChoice = mapTab ? 1 : 0;
            GuideInputGuard.Capture();

            suspendedEvents = EventSystem.current;
            // Never disable the shared EventSystem: doing so unregisters gamepad UI input.
            if (suspendedEvents != null) { navigationWasEnabled = suspendedEvents.sendNavigationEvents; suspendedEvents.sendNavigationEvents = false; }
            inputModule = UIManager.instance == null ? null : UIManager.instance.inputModule;
            if (inputModule != null) { mouseWasEnabled = inputModule.allowMouseInput; inputModule.allowMouseInput = false; }
            cursorWasVisible = Cursor.visible;
            cursorLock = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            foreach (var entry in Resources.FindObjectsOfTypeAll<JournalEntryStats>())
                if (!string.IsNullOrEmpty(entry.playerDataName) && entry.sprite != null)
                    portraits[entry.playerDataName] = entry.sprite;
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            GuideInputGuard.Release();
            if (suspendedEvents != null) suspendedEvents.sendNavigationEvents = navigationWasEnabled;
            if (inputModule != null) inputModule.allowMouseInput = mouseWasEnabled;
            inputModule = null;
            suspendedEvents = null;
            Cursor.visible = cursorWasVisible;
            Cursor.lockState = cursorLock;
            bool resume = ownsPause;
            ownsPause = false;
            if (resume && Paused) GameManager.instance.StartCoroutine(GuideInputGuard.ToggleOwnedPause(GameManager.instance));
        }

        private void Refresh()
        {
            player.RefreshAchievements();
            var scene = GameManager.instance == null ? "" : GameManager.instance.GetSceneNameString();
            SaveDiscovery.Import(catalog.Chapters, mod.Progress, player);
            string previousRegion = liveRegion;
            liveChapter = catalog.ChapterFor(player.Zone, scene);
            liveRegion = Catalog.RegionFor(player.Zone, scene);
            if (!string.IsNullOrEmpty(liveChapter) && !mod.Progress.VisitedChapters.Contains(liveChapter))
                mod.Progress.VisitedChapters.Add(liveChapter);
            if (locating && !string.IsNullOrEmpty(liveChapter))
            {
                locating = false;
                int index = Array.FindIndex(RouteChapters, c => c.Steps.Any(s => s.MapChapter == liveChapter));
                if (index >= 0) { SelectChapter(index); int next = Array.FindIndex(RouteSteps, s => !s.ReferenceOnly && !DoneInSave(s)); SelectStep(Math.Max(0, next)); }
            }
            if (previousRegion != liveRegion && !string.IsNullOrEmpty(liveRegion))
            {
                ResetJournalRegion();
                notice = Wisp.Core.I18n.T("Новая область · ") + liveRegion;
                noticeUntil = Time.unscaledTime + 7;
            }
            foreach (var enemy in catalog.Enemies.Where(e => e.Regions.Contains(liveRegion)))
            {
                var status = JournalStatus.Read(enemy, player);
                if (!status.Available) continue;
                int previous;
                if (lastKills.TryGetValue(enemy.Id, out previous) && status.Remaining < previous)
                {
                    notice = enemy.Name + (status.Complete ? Wisp.Core.I18n.T(" · запись завершена") : Wisp.Core.I18n.T(" · осталось: ") + status.Remaining);
                    noticeUntil = Time.unscaledTime + 5;
                }
                lastKills[enemy.Id] = status.Remaining;
            }
            // Resolve save/journal state on the refresh tick, never in every IMGUI event.
            var hudChapter = RouteChapters.FirstOrDefault(c => c.Steps.Any(s => s.MapChapter == liveChapter));
            var hudNext = hudChapter == null ? null : hudChapter.Steps.FirstOrDefault(s => !s.ReferenceOnly && !DoneInSave(s) && (!s.Spoiler || mod.Settings.ShowSpoilers));
            hudTask = hudNext == null ? "" : hudNext.Title;
        }

        private void SelectChapter(int index)
        {
            chapterIndex = index;
            revealChapter = revealStep = true;
            stepIndex = 0; stepImageIndex = 0;
            stepScroll = detailScroll = Vector2.zero;

            mapPan = Vector2.zero;
            mapZoom = 1;
            Remember();
        }

        private void SelectStep(int index)
        {
            stepIndex = index; stepImageIndex = 0;
            revealStep = true;
             mapPan = Vector2.zero; mapZoom = 1;
            detailScroll = Vector2.zero;
            Remember();
        }

        private void Remember()
        {
            mod.Progress.ChapterId = Current.Id;
            mod.Progress.StepId = RouteSteps[stepIndex].Id;
        }

        // A profile achievement does not remove prerequisites from the current run's HUD.
        private bool DoneInSave(Step step) { return Completion.Confirmed(step, player) || mod.Progress.Completed.Contains(step.Id); }
        private bool Done(Step step) { return ProfileAchievements.Steps.ContainsKey(step.Id) ? ProfileAchievements.Confirmed(step, player) : DoneInSave(step); }
        private bool Visible(Chapter chapter) { return mod.Settings.ShowSpoilers || chapter.Id.Contains("-ref-") || chapter.Id == liveChapter || mod.Progress.VisitedChapters.Contains(chapter.Id) || chapter.Id == "kings-pass" || chapter.Id == "pdf-a1" || chapter.Goal == "speed" || chapter.Goal == "steel" || chapter.Steps.Any(s => SaveDiscovery.HasVisitEvidence(s.MapChapter, player)) || chapter.Steps.Any(s => ProfileAchievements.Confirmed(s, player)); }

        private void OnDestroy()
        {
            Close();
            if (media != null) media.Dispose();
            foreach (var texture in new[] { panelTexture, buttonTexture, activeTexture, frameTexture, dividerTexture, scrollThumb })
                if (texture != null) Destroy(texture);
            if (font != null) Destroy(font);
            if (flatSkin != null) Destroy(flatSkin);

        }
        private void OnDisable() { Close(); GuideInputGuard.Clear(); }
    }
}
