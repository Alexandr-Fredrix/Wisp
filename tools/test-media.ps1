$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$sources = @((Get-ChildItem (Join-Path $root 'src/Wisp/Core') -Filter '*.cs').FullName)
$sources += Join-Path $root 'src/Wisp/UI/MediaLibrary.cs'
$sources += Join-Path $root 'tests/MediaHarness.cs'
Add-Type -Path $sources
[MediaHarness]::Run()
