param([string]$Dll = (Join-Path $PSScriptRoot '../artifacts/Wisp/Wisp.dll'))
$ErrorActionPreference = 'Stop'
# Exercise the packaged resources and actual catalog code without starting Unity.
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $Dll).Path)
$stream = $assembly.GetManifestResourceStream('Wisp.english.json')
$reader = [IO.StreamReader]::new($stream)
try { $translations = $reader.ReadToEnd() | ConvertFrom-Json -AsHashtable } finally { $reader.Dispose() }
foreach ($key in $translations.Keys) { [Wisp.Core.I18n]::Translations[$key] = $translations[$key] }
[Wisp.Core.I18n]::English = $false
$ru = [Wisp.Game.Catalog]::Load()
[Wisp.Core.I18n]::English = $true
$en = [Wisp.Game.Catalog]::Load()
if ($en.PdfChapters[0].Title -ne 'A1 · Dirtmouth → Forgotten Crossroads') { throw 'English catalog was not translated' }
if ($ru.PdfChapters[0].Title -eq $en.PdfChapters[0].Title) { throw 'Russian catalog was mutated' }
for ($i = 0; $i -lt $ru.PdfChapters.Length; $i++) {
    $a = $ru.PdfChapters[$i]; $b = $en.PdfChapters[$i]
    if ($a.Id -ne $b.Id -or $a.Steps.Length -ne $b.Steps.Length) { throw 'Chapter identity changed' }
    for ($j = 0; $j -lt $a.Steps.Length; $j++) {
        if ($a.Steps[$j].Id -ne $b.Steps[$j].Id) { throw 'Step identity changed' }
    }
}
[Wisp.Core.I18n]::English = $false
$again = [Wisp.Game.Catalog]::Load()
if ($again.PdfChapters[0].Title -ne $ru.PdfChapters[0].Title) { throw 'Switching back failed' }
'Packaged catalog: Russian → English → Russian passed; chapter and step identifiers preserved. Actual UI progress persistence is covered by test-adapter.ps1.'
