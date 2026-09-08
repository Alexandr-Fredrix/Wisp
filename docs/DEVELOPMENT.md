# Development

Python 3.11+ runs repository checks without third-party Python packages:

```sh
python tools/check.py
python -m unittest discover -s tests
python tools/package_source.py
```

## C# scaffold

Use a .NET SDK, a legitimate PC copy of Hollow Knight and a compatible Modding API. The initial net472 target follows the API's example, not a guarantee of compatibility with all patches.

```sh
dotnet build src/Wisp/Wisp.csproj -c Release -p:HollowKnightRefs="/absolute/path/to/hollow_knight_Data/Managed"
```

Game DLLs are local references only and are not copied to the output. The Framework reference-assemblies package is a compile-time dependency. The current entry point logs initialization only; it has not been validated in-game.

CI checks repository structure and source archives. It does not compile the mod without game references or perform in-game tests.

Keep game adapters/UI in src/Wisp, future original route data in content, and per-save progress separate. Do not write arbitrary game flags to simulate completed tasks.

Reference: https://github.com/hk-modding/api/tree/master/Examples
