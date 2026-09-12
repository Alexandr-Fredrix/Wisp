# Validation scope — local 1.0.1 candidate

Automated checks are run by `tools/validate_candidate.py` and recorded beside the DLL in `validation.json`, with hashed logs and a build manifest. They cover source metadata/content, core behavior, progress file recovery, image queue and cache limits, the actual MediaLibrary with deterministic engine/network doubles, packaged catalog resources, and the real UI language handler with populated progress and preference persistence.

Two obsolete assertions for the unused Steel route filter were removed; the active three-goal catalog is covered by content tests. The catalog-only localization test no longer claims to verify an unrelated empty progress object.

The real game, native texture allocation, live network transport and physical controller are not certified by these tests. No final candidate was installed or published.

## Manual acceptance on a disposable test save

1. Open/close from gameplay and native pause. Verify mouse, F8 and stick chord; restore the original pause and cursor state.
2. Switch RU/EN in all tabs. Read long route text and journal information, expand maps with RS, select locations with directional controls and return with B.
3. Compare current-region enemies before/after Crossroads infection. In Greenpath select Vengefly and verify its first known habitat map. Unknown-region maps remain available later in the gallery.
4. Disconnect networking with a cold test cache; check clickable retry and X/R on the relevant pane, then restore networking. Check a full queue during language switching.
5. Save/reload marks and selected task on a test slot; change language and goal. Never use an important save for corruption tests (automated tests already use temporary directories).
6. Compare baseline and candidate at the same resolution, scene and sequence of opened images: 60 seconds closed, 60 seconds route, 60 seconds journal, then repeated gallery cycles. Record frame times, GC allocations, native texture memory and peak process memory in Unity Profiler or equivalent instrumentation.

The 128 MiB limit applies to cached decoded textures; download buffers, transient decoding and Unity deferred destruction are separate. Do not infer an FPS improvement from archive size reduction.
