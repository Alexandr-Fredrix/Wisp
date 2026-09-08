# Development

## Checks without game files

Python 3.11+ and PowerShell 7:

```sh
python tools/check.py
python -m unittest discover -s tests
pwsh -File tools/test-core.ps1
```

CoreTests runs the actual C# progress/journal logic without Unity. CI runs these checks. It does not claim to test Unity UI or compile the game adapter without local references.

## Build with game references

Use a legitimate **1.5.12620** installation with BepInEx 5.4.23.5 x64. Do not mix old API files into a newer installation. Game libraries remain local and are never redistributed.

With a .NET SDK:

```sh
dotnet build src/Wisp/Wisp.csproj -c Release -p:HollowKnightRefs="/path/to/hollow_knight_Data/Managed" -p:BepInExRefs="/path/to/BepInEx/core"
```

PowerShell 7 also includes Roslyn, so a system SDK is not required for the alternative build:

```powershell
./tools/build.ps1 -HollowKnightRefs 'C:/path/to/hollow_knight_Data/Managed' -BepInExRefs 'C:/path/to/BepInEx/core'
```

Build output is artifacts/Wisp/Wisp.dll. PlayerData fields are checked and content is embedded in the DLL.

## Structure

- src/Wisp/Core: data models, progress normalization, read-only completion rules and journal-counter semantics.
- src/Wisp/Game: API/game readers and embedded-content loading.
- src/Wisp/UI: pause overlay, portraits and map-mesh rendering.
- content: original guide text, enemy identifiers/regions and source links; no game images.

Never set game flags to simulate completed steps. Missing or unknown fields are unavailable, never completed. Alternative choices remain manual. Wisp stores sidecar data after a successful game-save callback; preferences save on normal application quit.

Follow TESTING.md and RELEASING.md. Alpha releases list unverified behavior; CI does not replace a game test.
