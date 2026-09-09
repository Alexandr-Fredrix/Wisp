# Third-party materials

## Wisp reference content

The Russian route text was authored for the owner's Achievement Path project and adapted for Wisp. Enemy identifiers, regions and factual reference links are catalog data. `content/media.json` records original image URLs on the Hollow Knight Wiki CDN. The in-game viewer fetches portraits and habitat maps on demand and keeps a private local WispCache. Additional artwork is embedded for in-game reference; see the local reference assets section below. Team Cherry artwork and wiki map annotations retain their original rights; The Wisp license does not cover them. The viewer displays attribution to Hollow Knight Wiki / Team Cherry.

`content/ui/frame.png` and `content/ui/divider.png` are original decorative artwork generated with the built-in imagegen tool for this project. Prompts are recorded in `content/ui/PROMPTS.md`. These decorations contain no extracted game sprites or map data.

JetBrainsMono Nerd Font Regular is embedded in Wisp.dll. JetBrains Mono copyright 2020 The JetBrains Mono Project Authors; Nerd Fonts patched distribution. Licensed under SIL OFL 1.1, preserved at `content/ui/JetBrainsMono-OFL.txt` and embedded alongside the font. This font is excluded from Wisp’s personal-use restrictions. Unity loads the extracted TTF directly from WispCache; no system font registration or installation is used. The API/game DLLs used for local compilation are not included in the Wisp license or distributed output.

## General policy

Current original changes use the personal-use license. Earlier MIT grants remain effective for previously published versions. BepInEx and Harmony are separate dependencies, not bundled.

Hollow Knight, its characters, artwork, maps, audio and game assemblies belong to Team Cherry. Game DLLs are not included. Local compilation does not grant redistribution rights.

The community [Modding API](https://github.com/hk-modding/api) is a separate project, not bundled here. Review its terms and each dependency's license before distribution.

The owner's local installation may use a region reference cache prepared from Achievement Path's already downloaded wiki maps. That cache is not distributed. A source URL alone is not permission to redistribute an image. Before bundling third-party assets in a release, record file paths, author, source URL, license/permission, modifications and attribution here.

Do not incorporate third-party code without checking its terms.

Bundled font: JetBrainsMono Nerd Font Regular (SIL OFL 1.1). Game artwork embedded for local reference is listed below; game libraries are not bundled.


## Region maps

Region reference maps: Hollow Knight Wiki / Team Cherry. The two Russian maps of Queen’s Gardens and Fog Canyon were supplied by the user from the wiki and are included as reference artwork. These images are not covered by the Wisp code license. Source pages: https://hollowknight.wiki/w/Queen%27s_Gardens and https://hollowknight.wiki/w/Fog_Canyon. Other originals are referenced in content/regions.json.

## Bundled reference assets

`content/locations/`: 205 original maps and screenshots from Hollow Knight Wiki. Exact source URLs are recorded in `content/location-sources.json`; route associations and Russian captions are in `content/step-media.json`. Artwork: Team Cherry; map annotations: Hollow Knight Wiki contributors. Files retain their original image contents and filenames. The viewer attributes Hollow Knight Wiki / Team Cherry. These are included for the requested in-game reference, and are not covered by Wisp’s code license. The Wisp code license does not grant rights in these third-party materials.

`content/mushroom/`: seven maps and seven screenshots for the ordered Mister Mushroom encounters, from https://hollowknight.wiki/w/Mister_Mushroom_(Hollow_Knight). `content/ui/region-abyss.png`: Russian reference map supplied by the user. The same artwork attribution and license exclusion apply.

## English localization

English guide prose is a translation/adaptation of the Russian Wisp route. Canonical game names and achievement captions follow Team Cherry’s localization. English atlas sources are recorded in content/regions.json and content/regions-en.json. Wiki contributor annotations retain their applicable [CC BY-SA terms](https://hollowknight.wiki/w/Hollow_Knight_Wiki:Copyrights), separately from Team Cherry game artwork. User-supplied screenshots under docs/images show actual gameplay UI and remain outside the original-code license.
