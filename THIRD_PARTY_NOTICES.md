# Third-party materials

## Wisp 0.2 development content

The Russian route text was authored for the owner's Achievement Path project and adapted for Wisp. Enemy identifiers, regions and factual reference links are catalog data. `content/media.json` records original image URLs on the Hollow Knight Wiki CDN. The in-game viewer fetches portraits and habitat maps on demand and keeps a private local WispCache. Downloaded artwork is excluded from the repository and release archives. The game also supplies its map meshes at runtime. Team Cherry artwork and wiki map annotations retain their original rights; The Wisp license does not cover them. The viewer displays attribution to Hollow Knight Wiki / Team Cherry.

`content/ui/frame.png` and `content/ui/divider.png` are original decorative artwork generated with the built-in imagegen tool for this project. Prompts are recorded in `content/ui/PROMPTS.md`. These decorations contain no extracted game sprites or map data.

JetBrainsMono Nerd Font Regular is embedded in Wisp.dll. JetBrains Mono copyright 2020 The JetBrains Mono Project Authors; Nerd Fonts patched distribution. Licensed under SIL OFL 1.1, preserved at `content/ui/JetBrainsMono-OFL.txt` and embedded alongside the font. This font is excluded from Wisp’s personal-use restrictions. The Windows loader registers it privately for the game process; it does not install a system font. The API/game DLLs used for local compilation are not included in the Wisp license or distributed output.

## General policy

Current original changes use the personal-use license. Earlier MIT grants remain effective for previously published versions. BepInEx and Harmony are separate dependencies, not bundled.

Hollow Knight, its characters, artwork, maps, audio and game assemblies belong to Team Cherry. Game DLLs are not included. Local compilation does not grant redistribution rights.

The community [Modding API](https://github.com/hk-modding/api) is a separate project, not bundled here. Review its terms and each dependency's license before distribution.

The owner's local installation may use a region reference cache prepared from Achievement Path's already downloaded wiki maps. That cache is not distributed. A source URL alone is not permission to redistribute an image. Before bundling third-party assets in a release, record file paths, author, source URL, license/permission, modifications and attribution here.

Do not incorporate third-party code without checking its terms.

Bundled font: JetBrainsMono Nerd Font Regular (SIL OFL 1.1). Bundled third-party game artwork and libraries: **none**.
