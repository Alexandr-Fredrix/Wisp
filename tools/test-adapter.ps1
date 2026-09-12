param(
    [string]$Dll = (Join-Path $PSScriptRoot '../artifacts/Wisp/Wisp.dll'),
    [Parameter(Mandatory=$true)][string]$HollowKnightRefs,
    [Parameter(Mandatory=$true)][string]$BepInExRefs
)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
public static class WispTestResolver {
    public static string[] Directories;
    private static readonly HashSet<string> Resolving = new HashSet<string>();
    public static Assembly Resolve(object sender, ResolveEventArgs args) {
        string name = new AssemblyName(args.Name).Name;
        if (name.EndsWith(".resources") || !Resolving.Add(name)) return null;
        try {
            foreach (string directory in Directories) {
                string path = Path.Combine(directory, name + ".dll");
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        } finally { Resolving.Remove(name); }
    }
}
'@
[WispTestResolver]::Directories = @($HollowKnightRefs, $BepInExRefs)
$resolver = [ResolveEventHandler][WispTestResolver]::Resolve
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ('WispAdapter-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testDirectory) | Out-Null
try {
    $assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $Dll).Path)
    $flags = [Reflection.BindingFlags]'Instance,NonPublic'
    # Constructors would call Unity. The actual settings handler below requires no native engine calls.
    $mod = [Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([Wisp.WispMod])
    $guide = [Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([Wisp.UI.GuideWindow])
    $settings = [Wisp.Core.Preferences]::new()
    $progress = [Wisp.Core.SaveProgress]::new()
    $progress.Completed.Add('manual-a')
    $progress.VisitedChapters.Add('greenpath')
    $progress.RouteGoal = 'steel'; $progress.ChapterId = 'pdf-c1'; $progress.StepId = 'steel-start'
    [Wisp.WispMod].GetField('<Progress>k__BackingField',$flags).SetValue($mod,$progress)
    [Wisp.WispMod].GetField('<Settings>k__BackingField',$flags).SetValue($mod,$settings)
    [Wisp.WispMod].GetField('directory',$flags).SetValue($mod,$testDirectory)
    [Wisp.UI.GuideWindow].GetField('mod',$flags).SetValue($guide,$mod)
    [Wisp.UI.GuideWindow].GetField('journalStatuses',$flags).SetValue($guide,[Collections.Generic.Dictionary[string,Wisp.Core.JournalStatus]]::new())
    $stream = $assembly.GetManifestResourceStream('Wisp.english.json')
    $reader = [IO.StreamReader]::new($stream)
    try { $translations = $reader.ReadToEnd() | ConvertFrom-Json -AsHashtable } finally { $reader.Dispose() }
    foreach ($key in $translations.Keys) { [Wisp.Core.I18n]::Translations[$key] = $translations[$key] }
    $before = $progress | ConvertTo-Json -Depth 20
    [Wisp.Core.I18n]::English = $false
    $toggle = [Wisp.UI.GuideWindow].GetMethod('ToggleSetting',$flags)
    foreach ($expected in @('en','ru')) {
        $toggle.Invoke($guide,@([int]4)) | Out-Null
        if ($settings.Language -ne $expected) { throw 'Actual language handler failed' }
        if (($progress | ConvertTo-Json -Depth 20) -ne $before) { throw 'Actual handler changed populated progress' }
        if (![Object]::ReferenceEquals($mod.Progress,$progress)) { throw 'Progress object replaced' }
        $saved = Get-Content -LiteralPath (Join-Path $testDirectory 'settings.json') -Raw | ConvertFrom-Json
        if ($saved.Language -ne $expected) { throw 'Language preference not persisted' }
    }
    $catalog = [Wisp.Game.Catalog]::Load()
    if ($catalog.Enemies.Count -ne 164) { throw 'Catalog changed unexpectedly' }
    $decoder = [Wisp.WispMod].GetMethod('DecodeProgress',[Reflection.BindingFlags]'Static,NonPublic')
    foreach ($invalid in @('null','{}','{"unexpected":1}','{"Schema":2,"Completed":[]}','{"Completed":123}')) {
        $rejected = $false
        try { $decoder.Invoke($null,@($invalid)) | Out-Null } catch { $rejected = $true }
        if (!$rejected) { throw 'Invalid or unsupported progress accepted' }
    }
    $valid = $decoder.Invoke($null,@('{"Schema":1,"Completed":["manual"],"RouteGoal":"steel"}'))
    if ($valid.Completed[0] -ne 'manual') { throw 'Existing JSON progress not preserved' }
    'Actual UI language handler RU -> EN -> RU: populated progress, object identity and preference persistence passed.'
    'Actual JSON adapter: valid old progress accepted; malformed, empty and unsupported-schema data rejected.'
} finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
    $resolvedTest = [IO.Path]::GetFullPath($testDirectory)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (!$resolvedTest.StartsWith($tempRoot,[StringComparison]::OrdinalIgnoreCase) -or !([IO.Path]::GetFileName($resolvedTest)).StartsWith('WispAdapter-')) { throw 'Unsafe test cleanup path' }
    Remove-Item -LiteralPath $resolvedTest -Recurse -Force
}
