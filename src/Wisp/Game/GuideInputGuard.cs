using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace Wisp.Game
{
    // Own only gameplay dispatch, never InControl's device polling or bindings.
    internal static class GuideInputGuard
    {
        internal static bool Open;
        private static bool releasing;
        private static int releasedFrame;
        private static bool ownPause;
        internal static bool Captured { get { return Open || releasing; } }

        internal static void Capture() { Open = true; releasing = false; }
        internal static void Release() { Open = false; releasing = true; releasedFrame = Time.frameCount; }
        internal static void Clear() { Open = releasing = false; }
        internal static void Tick()
        {
            if (!releasing) return;
            var pad = InControl.InputManager.ActiveDevice;
            // Wait for the closing press to be released; a new press belongs to the game.
            bool held = pad.AnyButtonIsPressed || Input.anyKey;
            if (held) releasedFrame = Time.frameCount;
            else if (Time.frameCount > releasedFrame + 1) releasing = false;
        }
        internal static IEnumerator ToggleOwnedPause(GameManager manager)
        {
            // Harmony's iterator prefix runs when the iterator is created.
            ownPause = true;
            try { return manager.PauseGameToggle(); }
            finally { ownPause = false; }
        }
        internal static bool AllowPause { get { return ownPause || !Captured; } }
        internal static IEnumerator Empty() { yield break; }
    }

    [HarmonyPatch(typeof(InControl.HollowKnightInputModule), "Process")]
    internal static class BlockUnderlyingMenu
    {
        private static bool Prefix() { return !GuideInputGuard.Captured; }
    }
    [HarmonyPatch(typeof(GameManager), "PauseGameToggle")]
    internal static class BlockUnderlyingPause
    {
        private static bool Prefix(ref IEnumerator __result)
        {
            if (GuideInputGuard.AllowPause) return true;
            __result = GuideInputGuard.Empty();
            return false;
        }
    }
    [HarmonyPatch(typeof(UIManager), "TogglePauseGame")]
    internal static class BlockUnderlyingMenuPause
    {
        private static bool Prefix() { return !GuideInputGuard.Captured; }
    }
    [HarmonyPatch(typeof(HeroController), "LookForInput")]
    internal static class BlockHeroInput
    {
        private static bool Prefix() { return !GuideInputGuard.Captured; }
    }
    [HarmonyPatch(typeof(HeroController), "LookForQueueInput")]
    internal static class BlockQueuedHeroInput
    {
        private static bool Prefix() { return !GuideInputGuard.Captured; }
    }
    [HarmonyPatch(typeof(HeroController), "CanAttack")]
    internal static class BlockHeroAttack
    {
        private static bool Prefix(ref bool __result)
        {
            if (!GuideInputGuard.Captured) return true;
            __result = false;
            return false;
        }
    }
}
