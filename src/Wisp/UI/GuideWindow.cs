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
        private string notice = "";
        private float noticeUntil;
        private readonly Dictionary<string, int> lastKills = new Dictionary<string, int>();
        private bool cursorWasVisible;
        private CursorLockMode cursorLock;
        private bool open;
        private int chapterIndex;
        private int stepIndex;
        private int tab;
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
        private readonly AreaMap areaMap = new AreaMap();
        private Vector2 mapPan;
        private float mapZoom = 1;
        private bool mapTab;
        private bool mapDirty = true;

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
            mapDirty = true;
            areaMap.Dispose();
        }

        private Chapter[] RouteChapters { get { return catalog.Chapters.Where(c => c.Steps.Any(s => RouteGoals.Includes(mod.Progress.RouteGoal, s))).ToArray(); } }
        private Step[] RouteSteps { get { return Current.Steps.Where(s => RouteGoals.Includes(mod.Progress.RouteGoal, s)).ToArray(); } }
        private Chapter Current { get { return RouteChapters[Mathf.Clamp(chapterIndex, 0, RouteChapters.Length - 1)]; } }
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
                if (expandedEnemyMap) expandedEnemyMap = false;
                else if (tab == 0 && padPane > 0) padPane--;
                else Close();
                return;
            }
            if (Input.GetKeyDown(KeyCode.PageDown)) SelectStep(Math.Min(stepIndex + 1, RouteSteps.Length - 1));
            if (Input.GetKeyDown(KeyCode.PageUp)) SelectStep(Math.Max(0, stepIndex - 1));
            if (device.LeftBumper.WasPressed) { ChangeTab((tab + 2) % 3); }
            if (device.RightBumper.WasPressed) { ChangeTab((tab + 1) % 3); }
            bool leftStickMaps = (tab == 0 && mapTab && padPane == 2) || (tab == 1 && expandedEnemyMap);
            int vertical = device.DPadUp.IsPressed || (!leftStickMaps && device.LeftStickY.Value > .5f) ? -1 : device.DPadDown.IsPressed || (!leftStickMaps && device.LeftStickY.Value < -.5f) ? 1 : 0;
            int horizontal = device.DPadLeft.IsPressed || (!leftStickMaps && device.LeftStickX.Value < -.5f) ? -1 : device.DPadRight.IsPressed || (!leftStickMaps && device.LeftStickX.Value > .5f) ? 1 : 0;
            if (vertical == 0 && horizontal == 0) nextNavigation = 0;
            if (Time.unscaledTime >= nextNavigation && (vertical != 0 || horizontal != 0))
            {
                nextNavigation = Time.unscaledTime + .2f;
                if (tab == 0)
                {
                    padPane = Mathf.Clamp(padPane + horizontal, 0, 2);
                    if (padPane == 0 && vertical != 0) { SelectChapter(Mathf.Clamp(chapterIndex + vertical, 0, RouteChapters.Length - 1)); chapterScroll.y = Mathf.Max(0, chapterIndex * 96 - 192); }
                    else if (padPane == 1 && vertical != 0) { SelectStep(Mathf.Clamp(stepIndex + vertical, 0, RouteSteps.Length - 1)); stepScroll.y = Mathf.Max(0, stepIndex * 96 - 192); }
                    else if (padPane == 2 && vertical != 0) detailChoice = Mathf.Clamp(detailChoice + vertical, 0, 2);
                }
                else if (tab == 1) { enemyIndex = Mathf.Clamp(enemyIndex + vertical, 0, Math.Max(0, FilteredEnemies().Length - 1)); enemyScroll.y = Mathf.Max(0, enemyIndex * 80 - 160); enemyMapIndex = 0; mapPan = Vector2.zero; mapZoom = 1; }
                else padPane = Mathf.Clamp(padPane + vertical, 0, 4);
            }
            if (tab == 0 && Visible(Current))
            {
                if (device.Action3.WasPressed) { mapTab = !mapTab; detailChoice = mapTab ? 1 : 0; padPane = 2; mapDirty = true; }
                if (device.Action1.WasPressed)
                {
                    if (padPane < 2) { padPane++; detailChoice = mapTab ? 1 : 0; }
                    else if (detailChoice == 2) OpenRegionJournal();
                    else { mapTab = detailChoice == 1; mapDirty = true; }
                }
                if (device.Action4.WasPressed && padPane == 1 && !Completion.Confirmed(RouteSteps[stepIndex], player))
                    mod.Progress.Mark(RouteSteps[stepIndex].Id, !Done(RouteSteps[stepIndex]));
                if (mapTab && padPane == 2)
                {
                    mapPan += MapStick(device, leftStickMaps) * Time.unscaledDeltaTime * 350;
                    mapZoom = Mathf.Clamp(mapZoom + (device.RightTrigger.Value - device.LeftTrigger.Value) * Time.unscaledDeltaTime * 2, 1, 5);
                    if (device.Action4.WasPressed) { mapPan = Vector2.zero; mapZoom = 1; }
                }
                else if (padPane == 2) detailScroll.y = Mathf.Max(0, detailScroll.y - device.RightStickY.Value * Time.unscaledDeltaTime * 350);
            }
            if (tab == 1) {
                if (device.DPadRight.IsPressed) habitatScroll.y += Time.unscaledDeltaTime * 240;
                if (device.DPadLeft.IsPressed) habitatScroll.y = Mathf.Max(0, habitatScroll.y - Time.unscaledDeltaTime * 240);
                if (device.Action3.WasPressed) { allRegions = !allRegions; enemyIndex = 0; enemyScroll = Vector2.zero; }
                if (device.Action1.WasPressed) { enemyMapIndex++; mapPan = Vector2.zero; mapZoom = 1; }
                mapPan += MapStick(device, leftStickMaps) * Time.unscaledDeltaTime * 350;
                mapZoom = Mathf.Clamp(mapZoom + (device.RightTrigger.Value - device.LeftTrigger.Value) * Time.unscaledDeltaTime * 2, 1, 5);
                if (device.Action4.WasPressed) { mapPan = Vector2.zero; mapZoom = 1; }
            }
            if (tab == 2 && device.Action1.WasPressed) ToggleSetting(padPane);
        }

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
            open = true;
            padPane = 0; detailChoice = mapTab ? 1 : 0;
            GuideInputGuard.Capture();
            mapDirty = true;
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
                int index = Array.FindIndex(RouteChapters, c => c.Id == liveChapter);
                if (index >= 0) { SelectChapter(index); int next = Array.FindIndex(RouteSteps, s => !Done(s)); SelectStep(Math.Max(0, next)); }
            }
            if (previousRegion != liveRegion && !string.IsNullOrEmpty(liveRegion))
            {
                notice = "Новая область · " + liveRegion;
                noticeUntil = Time.unscaledTime + 7;
            }
            foreach (var enemy in catalog.Enemies.Where(e => e.Regions.Contains(liveRegion)))
            {
                var status = JournalStatus.Read(enemy, player);
                if (!status.Available) continue;
                int previous;
                if (lastKills.TryGetValue(enemy.Id, out previous) && status.Remaining < previous)
                {
                    notice = enemy.Name + (status.Complete ? " · запись завершена" : " · осталось: " + status.Remaining);
                    noticeUntil = Time.unscaledTime + 5;
                }
                lastKills[enemy.Id] = status.Remaining;
            }
            // Resolve save/journal state on the refresh tick, never in every IMGUI event.
            var hudChapter = catalog.Chapters.FirstOrDefault(c => c.Id == liveChapter);
            var hudNext = hudChapter == null ? null : hudChapter.Steps.FirstOrDefault(s => RouteGoals.Includes(mod.Progress.RouteGoal, s) && !Done(s) && (!s.Spoiler || mod.Settings.ShowSpoilers));
            hudTask = hudNext == null ? "" : hudNext.Title;
        }

        private void SelectChapter(int index)
        {
            chapterIndex = index;
            stepIndex = 0;
            stepScroll = detailScroll = Vector2.zero;
            mapDirty = true;
            mapPan = Vector2.zero;
            mapZoom = 1;
            Remember();
        }

        private void SelectStep(int index)
        {
            stepIndex = index;
            detailScroll = Vector2.zero;
            Remember();
        }

        private void Remember()
        {
            mod.Progress.ChapterId = Current.Id;
            mod.Progress.StepId = RouteSteps[stepIndex].Id;
        }

        private bool Done(Step step) { return Completion.Confirmed(step, player) || mod.Progress.Completed.Contains(step.Id); }
        private bool Visible(Chapter chapter) { return mod.Settings.ShowSpoilers || chapter.Id == liveChapter || mod.Progress.VisitedChapters.Contains(chapter.Id) || chapter.Id == "kings-pass"; }

        private void OnDestroy()
        {
            Close(); areaMap.Dispose();
            if (media != null) media.Dispose();
            foreach (var texture in new[] { panelTexture, buttonTexture, activeTexture, frameTexture, dividerTexture })
                if (texture != null) Destroy(texture);
            if (font != null) Destroy(font);
        }
        private void OnDisable() { Close(); GuideInputGuard.Clear(); }
    }
}
