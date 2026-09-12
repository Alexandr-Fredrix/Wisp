# Build and validation

`src/`: C#; `content/`: routes/assets; `tests/`: checks/regression fixtures; `tools/`: build/packaging. Installable downloads are in Releases.

Use PowerShell 7, Python 3 with `tools/requirements-tests.txt`, and references from your Hollow Knight/BepInEx installation. Game assemblies are never committed or redistributed.

```powershell
pwsh -File tools/build.ps1 -HollowKnightRefs "<game>/hollow_knight_Data/Managed" -BepInExRefs "<game>/BepInEx/core" -OutputDirectory artifacts/Wisp
python tools/validate_candidate.py --pwsh pwsh --game "<game>/hollow_knight_Data/Managed" --api "<game>/BepInEx/core" --directory artifacts/Wisp
python tools/package_mod.py --dll artifacts/Wisp/Wisp.dll
```

Packaging rejects stale source manifests and validation. Keep `version.json`, the C# project and changelog consistent. Automated checks do not certify native rendering or physical controllers.

Report reproducible bugs in Issues. Do not post saves, credentials or private logs. Discuss code contributions with Alex first; current personal-use terms are in `LICENSE`, while earlier MIT grants remain effective.
