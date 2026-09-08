# Unreleased — controller focus

- Keep an unexplored (?) indicator beside chapter progress even when spoilers reveal the name; require visit evidence independently of completed tasks.
- Press the right stick in the map pane to switch reference/save maps; label the shortcut beside the controls and explain unavailable save maps.
- Separate the currently open item (underline) from controller focus (silver corners).
- Add LB/RB hints beside main tabs and contextual navigation help.
- Keep the left stick in chapter/step lists until focus enters the map pane.
- A enters panels or opens the highlighted detail tab; Y marks steps; B returns one panel.
- Reset route focus when changing main tabs; remove large filled selection boxes.
- Local build installed and guide opening observed; physical controller flow still needs verification. Published alpha.1 archive is unchanged.

# 0.3.0-alpha.1

- Port to Hollow Knight 1.5.12620 / BepInEx 5, without replacing game assemblies.
- One installable Wisp.dll; routes and generated interface art embedded.
- Compact task HUD, illustrated journal and pan/zoom habitat maps.
- Per-save 112% / short Steel Soul goal; import legacy Wisp marks.
- Personal-use terms for newly published changes; earlier MIT grants preserved.

## 0.2.0-alpha.3

Bounded ornamental interface, in-game habitat maps and per-save route goals. See docs/releases/0.2.0-alpha.3.md.

# Changelog

## 0.2.0-alpha.2

- Restore visited areas from existing saves and add 29 automatic milestone rules.
- Preserve the shared EventSystem; support opening from gameplay and controller navigation.
- Add a contextual gameplay HUD and journal notifications; restyle the guide as a framed, silver-blue journal.
- Add six tests for save discovery and isolation. Physical controller verification remains pending.

## Unreleased

### 0.2.0-alpha.1 development implementation

- Pause-screen guide (F8), 22 chapters / 98 steps, per-save manual progress and 23 read-only automatic rules.
- 164 journal records with 163 game-counter mappings, game portraits, location filters and contextual reminders.
- Area-map rendering from existing game meshes, pan/zoom and spoiler filtering.
- Core C# tests, content validation, local Roslyn build and development-archive tooling.
- Runtime testing is pending. This is not a verified public gameplay release.

## 0.1.0-alpha.1 — 2026-09-08

Initial source preview, **not an installable mod**.

- Russian/English documentation, MIT license and third-party policy.
- C# entry-point scaffold; game compatibility not tested.
- Repository checks, source packaging, checksums and prerelease automation.
- Contribution guidelines and issue templates.

No game binaries or external artwork are distributed.
