# Alpha.3 local validation — 2026-09-08

Environment: Windows, Hollow Knight 1.5.78.11833, Modding API v77, 2560×1440. Only the pre-existing disposable slot 4 copy was loaded; slots 1 and 2 were not used.

Observed in the game:

- F8 opens Wisp from gameplay and closes back to gameplay after the native pause transition.
- Generated frame and divider render, text wraps inside bounded columns, long station instructions have a vertical scroll area.
- Crawlid displays its actual portrait and localized Russian name. The habitat map displays marked locations, expands, zooms with the wheel and pans by dragging.
- The full Crossroads reference image displays from the separately prepared local region cache.
- Selecting Steel Soul goal shortens the route and shows a notice that this is an ordinary game save. After normal exit, slot 4 mod data contains RouteGoal=steel, retains 16 visited chapters and has no fabricated manual marks.
- Logs show no Wisp exception during these checks.
- 27 core assertions, 6 Python tests, metadata validation and compilation passed.

After the initial visual check: made the background fully opaque, moved footer text clear of the corner ornaments, isolated expanded-map input from the list behind it, restored HUD journal notifications and made the reference map the default. These refinements compile; they need a fresh final visual check.

Image cache: all 375 unique remote image URLs downloaded and decoded successfully, plus 20 locally prepared region images. The catalog covers 164 portraits and habitat maps for 160 of 164 records. Missing maps are reported explicitly. Images are private cache contents, not packaged artwork.

Remaining: physical Flydigi gamepad input, other resolutions, cold-cache downloading inside Unity, all-map visual review and full playthrough. The native discovered-room map produced no pieces for this test save; the reference image viewer works. Do not describe this as a complete runtime certification.
