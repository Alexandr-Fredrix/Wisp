# Validation scope — 1.0.0

Completed locally:

- 17 Python tests: route/media integrity, region regressions and complete English coverage for displayed text.
- 45 C# core assertions.
- Build against Hollow Knight 1.5.12620 / Unity 6 and BepInEx 5.4.23.5 x64.
- Packaged catalog switched Russian → English → Russian, preserving chapter/step identifiers and a progress object.
- User-supplied screenshots show the Russian route, map, Journal, atlas and settings before the language toggle was added.

Not yet exercised in a running game: the final bilingual DLL, live language-switch layout, and controller input after that switch. The automated checks do not certify every controller or screen resolution. No claim of a full 63-achievement playthrough is made.

For runtime verification: open each main tab, switch both languages, inspect long descriptions and illustrated references, scroll with a controller, change Journal regions, restart and verify language persistence, then save/reload and verify marks. Check achievement status against the game profile. Report the resolution, controller and exact step when reporting a problem.

Commands: `python -m unittest discover -s tests`, `python tools/check.py`, `pwsh -File tools/test-core.ps1`, and after building `pwsh -File tools/test-localization.ps1`.
