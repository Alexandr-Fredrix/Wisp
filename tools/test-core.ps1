$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$sources = @((Get-ChildItem (Join-Path $root 'src/Wisp/Core') -Filter '*.cs').FullName)
$sources += Join-Path $root 'tests/CoreTests.cs'
$sources += Join-Path $root 'tests/RegressionTests.cs'
Add-Type -Path $sources
[CoreTests]::Run()
[RegressionTests]::Run()
