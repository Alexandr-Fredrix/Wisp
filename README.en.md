<div align="center">

# Wisp

**Your guide through Hallownest — directly inside the game.**

A step-by-step companion for **112%**, every achievement, speedrun endings, **Steel Soul**, the Hunter's Journal and world maps.

[![Release](https://img.shields.io/badge/release-v1.0.0-2ea44f?style=flat-square&logo=github)](https://github.com/Alexandr-Fredrix/Wisp/releases)
[![Validate](https://github.com/Alexandr-Fredrix/Wisp/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Alexandr-Fredrix/Wisp/actions/workflows/ci.yml)
![Hollow Knight](https://img.shields.io/badge/Hollow%20Knight-1.5.12620-6f42c1?style=flat-square)
![Platform](https://img.shields.io/badge/platform-Windows%20x64-0078D4?style=flat-square&logo=windows)
![Languages](https://img.shields.io/badge/languages-EN%20%7C%20RU-2f81f7?style=flat-square)

[**Download**](https://github.com/Alexandr-Fredrix/Wisp/releases) · [**Русский**](README.md) · [**Install**](docs/INSTALL.md) · [**Report an issue**](https://github.com/Alexandr-Fredrix/Wisp/issues/new/choose)

</div>

---

## What Wisp does

- 🧭 **Progression routes** — a structured path through 112%, achievements, alternate choices and endings.
- 🏆 **Achievement companion** — achievements are tied to specific stages and actions for the current save.
- ⚡ **Speedrun and Steel Soul** — separate goals and routes without mixing progress between saves.
- 🗺️ **Maps and illustrations** — regional maps plus images for objectives, items, bosses and collections inside the game.
- 📖 **Hunter's Journal** — entry progress, region filtering and habitat maps.
- 🍄 **Mister Mushroom** — all seven encounters presented as one sequential route.
- 🌐 **English + Русский** — both languages are included in one download; changing language preserves progress.
- 🎮 **Mouse, keyboard and controller** — navigation, scrolling, galleries and map zoom/pan.

## Screenshots

![Route and stage instructions](docs/images/route.png)

<details>
<summary><b>Show more screenshots</b></summary>
<br>

| Route map | Hunter's Journal |
|---|---|
| ![Route map](docs/images/map.png) | ![Hunter's Journal](docs/images/journal.png) |

| Atlas | Settings |
|---|---|
| ![Atlas](docs/images/atlas.png) | ![Settings](docs/images/settings.png) |

![HUD](docs/images/hud.png)

</details>

## Quick start

1. Install [**BepInEx 5.4.23.5 x64**](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5) into the Hollow Knight directory.
2. Download **Wisp-1.0.0.zip** from [Releases](https://github.com/Alexandr-Fredrix/Wisp/releases).
3. Extract it into the game directory. You should get:

```text
Hollow Knight/
└─ BepInEx/
   └─ plugins/
      └─ Wisp/
         └─ Wisp.dll
```

4. Load a save and press **F8** or **both controller sticks**.

> Full installation, update, removal and controls: **[docs/INSTALL.md](docs/INSTALL.md)**.

## Controls

| Action | Control |
|---|---|
| Open Wisp | `F8` or press both sticks |
| Main tabs | `LB / RB` |
| Select panel / item | D-pad / directions |
| Open / back | `A / B` |
| Scroll description | right stick |
| Region selector in Journal | `X` |
| Next illustration | `Y` |
| Expand image | press `RS` |
| Map zoom | `LT / RT` |

With a mouse, click to select, use the wheel to scroll or zoom, and drag to pan maps.

## Progress model

Wisp reads achievements from the game profile while route conditions come from the **current save**. Actions that cannot be confirmed automatically can still be marked manually.

Wisp preferences and route marks are stored separately from the game's own saves. Changing language does not reset selected goals or progress. Choosing the Steel Soul route **does not change the actual save mode**.

## Compatibility

- **Hollow Knight:** `1.5.12620`
- **OS:** Windows x64
- **BepInEx:** `5.4.23.5 x64`
- **Wisp:** `1.0.0`
- **Languages:** English / Русский

Many images are embedded in the mod. Remaining wiki maps and portraits download on first view and stay in the local cache.

> **Validation status:** automated build, content, core-logic and localization checks pass. The final bilingual UI still needs a complete in-game validation pass. See [TESTING.md](docs/TESTING.md).

## Development

```text
src/       mod source
content/   routes, localization and embedded media
tests/     automated checks
tools/     build, validation and packaging
docs/      install and technical documentation
```

- [Development](docs/DEVELOPMENT.md)
- [Testing](docs/TESTING.md)
- [Releasing](docs/RELEASING.md)
- [Changelog](CHANGELOG.md)

## Project

Author: **Alex / [Alexandr-Fredrix](https://github.com/Alexandr-Fredrix)**

[Discussions](https://github.com/Alexandr-Fredrix/Wisp/discussions) · [Issues](https://github.com/Alexandr-Fredrix/Wisp/issues) · [License](LICENSE) · [Third-party materials](THIRD_PARTY_NOTICES.md)

Free personal play is permitted. Resale, republication and reuse of newly protected original material require permission under `LICENSE`. Earlier MIT releases retain their terms. **Hollow Knight** and game artwork belong to Team Cherry.
