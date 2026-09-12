# Guide content

`route-pdf.json` is the active three-goal guide. `route.json` retains geographic metadata and compatible objective definitions. Stable task IDs must not change during editorial updates.

`enemies.json` contains enemy names, regions and PlayerData counter mappings. `media.json` contains portraits and explicit `habitatMaps` records (URL, source caption, established regions and infection phase). Empty regions and `unknown` phase mean the source did not establish that information; they are never inferred from a neighbouring entrance.

Reference maps, screenshots, achievement icons and the font are embedded or downloaded into the private WispCache. Bundled PNGs may be losslessly recompressed; their decoded pixels remain identical. See THIRD_PARTY_NOTICES.md for attribution. Game assemblies and player saves are never packaged.

`export_review.py` produces an independent offline editorial snapshot and refuses to overwrite existing files. It is not an automatic Markdown importer. `review_changes.py` only reports differences, including shared task IDs requiring review.
