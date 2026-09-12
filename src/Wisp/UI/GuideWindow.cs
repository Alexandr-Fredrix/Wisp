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
        private bool cursorWasVisible;
        private CursorLockMode cursorLock;
        private bool open;
        private int chapterIndex;
        private int stepIndex;
        private int tab;
        private bool revealChapter = true, revealStep = true;
        private bool allRegions;
        private Vector2 detailScroll, enemyScroll;
        private GUIStyle text, heading, button, active, muted;
        private Texture2D panelTexture, buttonTexture, activeTexture;
        private Font font;
        private string liveChapter = "";
        private string liveRegion = "";
        private float nextRefresh;
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
            InvalidateView();
            locating = !RouteView.Restore(RouteChapters, mod.Progress, out chapterIndex, out stepIndex);
            detailScroll = enemyScroll = Vector2.zero;
            liveChapter = liveRegion = "";
            nextRefresh = 0;
        }

        private Catalog cachedCatalog;
        private string cachedGoal;
        private Chapter[] cachedChapters;
        private readonly Dictionary<string, Step[]> cachedSteps = new Dictionary<string, Step[]>();
        private Chapter[] RouteChapters
        {
            get {
                if (cachedCatalog != catalog || cachedGoal != mod.Progress.RouteGoal) {
                    cachedCatalog = catalog; cachedGoal = mod.Progress.RouteGoal;
                    cachedChapters = catalog.PdfChapters.Where(c => c.Goal == cachedGoal).ToArray(); cachedSteps.Clear();
                }
                return cachedChapters;
            }
        }
        private Step[] RouteSteps { get { Step[] steps; var chapter = Current; if (!cachedSteps.TryGetValue(chapter.Id, out steps)) cachedSteps[chapter.Id] = steps = RouteView.VisibleSteps(chapter); return steps; } }
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
            bool retriedImage = device.Action3.WasPressed && RetryFocusedImages();
            if (Input.GetKeyDown(KeyCode.R)) RetryFocusedImages();
            if (expandedStepImage)
            {
                mapPan += MapStick(device, true) * Time.unscaledDeltaTime * 350;
                mapZoom = Mathf.Clamp(mapZoom + (device.RightTrigger.Value - device.LeftTrigger.Value) * Time.unscaledDeltaTime * 2, 1, 5);
                if (device.DPadLeft.WasPressed) { stepImageIndex--; mapPan = Vector2.zero; mapZoom = 1; }
                if (device.DPadRight.WasPressed) { stepImageIndex++; mapPan = Vector2.zero; mapZoom = 1; }
                if (device.Action4.WasPressed) { mapPan = Vector2.zero; mapZoom = 1; }
                return;
            }
            bool onMap = contentFocus && ((tab == 0 && mapTab) || (tab == 1 && padPane == 2) || tab == 3);
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
                        else if (padPane == 0 && dy != 0) { SelectChapter(Mathf.Clamp(chapterIndex + dy, 0, RouteChapters.Length - 1)); }
                        else if (padPane == 1 && dy != 0) { SelectStep(Mathf.Clamp(stepIndex + dy, 0, RouteSteps.Length - 1)); }
                    }
                    else if (dx != 0) { detailChoice = Mathf.Clamp(detailChoice + dx, 0, 2); if (detailChoice < 2) mapTab = detailChoice == 1; }
                    if (device.Action1.WasPressed)
                    {
                        if (padPane < 2 && Visible(Current)) padPane++;
                        else if (detailChoice == 2) OpenRegionJournal();
                        else contentFocus = true;
                    }
                    if (device.Action3.WasPressed && !retriedImage) { mapTab = true; detailChoice = 1; padPane = 2; }
                    if (device.Action4.WasPressed && padPane == 1 && !ProfileAchievements.Steps.ContainsKey(RouteSteps[stepIndex].Id) && !RouteSteps[stepIndex].ReferenceOnly && !Completion.Confirmed(RouteSteps[stepIndex], player) && !ProfileAchievements.Confirmed(RouteSteps[stepIndex], player))
                        { mod.Progress.Mark(RouteSteps[stepIndex].Id, !Done(RouteSteps[stepIndex])); InvalidateView(); }
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
            else if (tab == 1)
            {
                if (!contentFocus)
                {
                    if (padPane == 0 && dy != 0) { enemyIndex = Mathf.Clamp(enemyIndex + dy, 0, Math.Max(0, FilteredEnemies().Length - 1)); enemyScroll.y = Mathf.Max(0, enemyIndex * 96 - 192); enemyMapIndex = 0; habitatScroll = mapPan = Vector2.zero; mapZoom = 1; }
                    if (dx != 0) padPane = Mathf.Clamp(padPane + dx, 0, 2);
                    if (device.Action1.WasPressed) { if (padPane < 2) padPane++; else contentFocus = true; }
                }
                if (padPane == 1)
                {
                    float stick = Mathf.Abs(device.RightStickY.Value) > .18f ? device.RightStickY.Value : 0;
                    habitatScroll.y = Mathf.Max(0, habitatScroll.y + dy * 64 - stick * Time.unscaledDeltaTime * 350);
                }
                if (padPane == 2)
                {
                    if (device.RightStickButton.WasPressed) { expandedEnemyMap = !expandedEnemyMap; contentFocus = true; }
                    if (!contentFocus && device.Action4.WasPressed) { enemyMapIndex++; mapPan = Vector2.zero; mapZoom = 1; }
                    if (contentFocus && (device.DPadLeft.WasPressed || device.DPadRight.WasPressed)) { enemyMapIndex += device.DPadLeft.WasPressed ? -1 : 1; mapPan = Vector2.zero; mapZoom = 1; }
                }
                if (device.Action3.WasPressed && !retriedImage) OpenJournalRegions();
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
            Refresh(true);
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

        private bool? infected;
        private readonly Dictionary<string, JournalStatus> journalStatuses = new Dictionary<string, JournalStatus>();
        private void Refresh(bool force = false)
        {
            var scene = GameManager.instance == null ? "" : GameManager.instance.GetSceneNameString();
            SaveDiscovery.Import(catalog.Chapters, mod.Progress, player);
            string previousRegion = liveRegion;
            liveChapter = catalog.ChapterFor(player.Zone, scene);
            liveRegion = HabitatMaps.CurrentRegion(Catalog.RegionFor(player.Zone, scene), player);
            bool flag; infected = player.TryBool("crossroadsInfected", out flag) ? (bool?)flag : null;
            if (!string.IsNullOrEmpty(liveChapter) && !mod.Progress.VisitedChapters.Contains(liveChapter)) mod.Progress.VisitedChapters.Add(liveChapter);
            if (open || force || locating) player.RefreshAchievements();
            if (locating && !string.IsNullOrEmpty(liveChapter))
            {
                locating = false;
                int index = Array.FindIndex(RouteChapters, c => c.Steps.Any(s => s.MapChapter == liveChapter));
                if (index >= 0) { SelectChapter(index); int next = Array.FindIndex(RouteSteps, s => !s.ReferenceOnly && !DoneInSave(s)); SelectStep(Math.Max(0, next)); }
            }
            if (previousRegion != liveRegion && !string.IsNullOrEmpty(liveRegion) && string.IsNullOrEmpty(journalRegion) && !allRegions) ResetJournalRegion();
            if (open || force) {
                journalStatuses.Clear();
                foreach (var enemy in catalog.Enemies) journalStatuses[enemy.Id] = JournalStatus.Read(enemy, player);
                InvalidateView();
            }
        }
        private JournalStatus StatusOf(Enemy enemy) { JournalStatus status; if (!journalStatuses.TryGetValue(enemy.Id, out status)) journalStatuses[enemy.Id] = status = JournalStatus.Read(enemy, player); return status; }

        private void SelectChapter(int index)
        {
            chapterIndex = index; InvalidateView();
            revealChapter = revealStep = true;
            stepIndex = 0; stepImageIndex = 0;
            detailScroll = Vector2.zero;

            mapPan = Vector2.zero;
            mapZoom = 1;
            Remember();
        }

        private void SelectStep(int index)
        {
            stepIndex = index; stepImageIndex = 0; InvalidateView();
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

        // Profile achievements and current-save prerequisites remain independent.
        private bool DoneInSave(Step step) { return Completion.Confirmed(step, player) || mod.Progress.Completed.Contains(step.Id); }
        private bool Done(Step step) { return ProfileAchievements.Steps.ContainsKey(step.Id) ? ProfileAchievements.Confirmed(step, player) : DoneInSave(step); }
        private bool Visible(Chapter chapter) { return mod.Settings.ShowSpoilers || chapter.Id.Contains("-ref-") || chapter.Id == liveChapter || mod.Progress.VisitedChapters.Contains(chapter.Id) || chapter.Id == "kings-pass" || chapter.Id == "pdf-a1" || chapter.Goal == "speed" || chapter.Goal == "steel" || chapter.Steps.Any(s => SaveDiscovery.HasVisitEvidence(s.MapChapter, player)) || chapter.Steps.Any(s => ProfileAchievements.Confirmed(s, player)); }

        private void OnDestroy()
        {
            Close();
            if (media != null) media.Dispose();
            foreach (var texture in new[] { panelTexture, buttonTexture, activeTexture, scrollThumb })
                if (texture != null) Destroy(texture);
            if (font != null) Destroy(font);
            if (flatSkin != null) Destroy(flatSkin);

        }
        private void OnDisable() { Close(); GuideInputGuard.Clear(); }
    }
}
