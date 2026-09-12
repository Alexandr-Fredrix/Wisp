param(
    [Parameter(Mandatory=$true)][string]$HollowKnightRefs,
    [Parameter(Mandatory=$true)][string]$BepInExRefs,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../artifacts/Wisp'),
    [string]$Python = 'python'
)
$ErrorActionPreference = 'Stop'
# PowerShell 7 ships Roslyn. This path supports building without installing a system SDK.
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$game = (Resolve-Path $HollowKnightRefs).Path
$api = (Resolve-Path $BepInExRefs).Path
$references = [System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
$seen = @{}
foreach ($directory in @($api, $game)) {
    foreach ($file in (Get-ChildItem -LiteralPath $directory -Filter '*.dll')) {
        if ($file.Name -ne '0Harmony20.dll' -and !$seen.ContainsKey($file.Name)) {
            try {
                [Reflection.AssemblyName]::GetAssemblyName($file.FullName) | Out-Null
                $reference = [Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($file.FullName)
                $references.Add($reference)
                $seen[$file.Name] = $true
            } catch [System.BadImageFormatException] { }
        }
    }
}
$trees = [System.Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
foreach ($source in (Get-ChildItem (Join-Path $root 'src/Wisp') -Recurse -Filter '*.cs' | Where-Object FullName -NotMatch '[\\/](obj|bin)[\\/]')) {
    $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($source.FullName)))
}
$version = (Get-Content (Join-Path $root 'version.json') -Raw | ConvertFrom-Json).version
$assemblyVersion = ($version -split '-')[0] + '.0'
$trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText('[assembly:System.Reflection.AssemblyVersion("' + $assemblyVersion + '")][assembly:System.Reflection.AssemblyInformationalVersion("' + $version + '")]'))
$options = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$options = $options.WithOptimizationLevel([Microsoft.CodeAnalysis.OptimizationLevel]::Release).WithDeterministic($true)
$compilation = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('Wisp', $trees, $references, $options)
if ($null -eq $compilation.GetTypeByMetadataName('BepInEx.BaseUnityPlugin')) { throw 'BepInEx 5 core references missing.' }
$playerData = $compilation.GetTypeByMetadataName('PlayerData')
$fields = @{}
$membersMethod = [Microsoft.CodeAnalysis.INamespaceOrTypeSymbol].GetMethod('GetMembers', [Type[]]@())
foreach ($member in $membersMethod.Invoke($playerData, @())) { $fields[$member.Name] = $member }
$route = @((Get-Content (Join-Path $root 'content/route.json') -Raw | ConvertFrom-Json)) + @((Get-Content (Join-Path $root 'content/route-pdf.json') -Raw | ConvertFrom-Json))
foreach ($condition in $route.steps.conditions) {
    if (!$fields.ContainsKey($condition.field)) { throw "Unknown PlayerData field: $($condition.field)" }
    $fieldType = ([Microsoft.CodeAnalysis.IFieldSymbol]$fields[$condition.field]).Type.SpecialType
    $expected = if ($condition.kind -eq 'bool') { [Microsoft.CodeAnalysis.SpecialType]::System_Boolean } elseif ($condition.kind -eq 'int') { [Microsoft.CodeAnalysis.SpecialType]::System_Int32 } else { throw 'Unknown condition kind' }
    if ($fieldType -ne $expected) { throw "Wrong PlayerData type: $($condition.field)" }
}
$enemies = Get-Content (Join-Path $root 'content/enemies.json') -Raw | ConvertFrom-Json
foreach ($enemy in $enemies) {
    if ($enemy.journalKey -and (!$fields.ContainsKey('killed'+$enemy.journalKey) -or !$fields.ContainsKey('kills'+$enemy.journalKey))) {
        throw "Unknown journal fields for $($enemy.id)"
    }
}
$resources = [System.Collections.Generic.List[Microsoft.CodeAnalysis.ResourceDescription]]::new()
foreach ($name in @('english.json','regions-en.json','achievements.json','step-media.json','regions.json','ui/region-abyss.png','ui/region-queens-gardens.png','ui/region-fog-canyon.png','route.json','route-pdf.json','enemies.json','media.json','ui/JetBrainsMonoNerdFont-Regular.ttf','ui/JetBrainsMono-OFL.txt')) {
    $resourcePath = Join-Path $root "content/$name"
    $factory = { [IO.File]::OpenRead($resourcePath) }.GetNewClosure()
    $resources.Add([Microsoft.CodeAnalysis.ResourceDescription]::new("Wisp.$name", [Func[IO.Stream]]$factory, $true))
}
foreach ($image in (Get-ChildItem (Join-Path $root 'content/achievements') -Filter '*.jpg')) {
    $path = $image.FullName
    $factory = { [IO.File]::OpenRead($path) }.GetNewClosure()
    $resources.Add([Microsoft.CodeAnalysis.ResourceDescription]::new("Wisp.achievements/$($image.Name)", [Func[IO.Stream]]$factory, $true))
}
foreach ($image in (Get-ChildItem (Join-Path $root 'content/mushroom') -Filter '*.png')) {
    $path = $image.FullName
    $factory = { [IO.File]::OpenRead($path) }.GetNewClosure()
    $resources.Add([Microsoft.CodeAnalysis.ResourceDescription]::new("Wisp.mushroom/$($image.Name)", [Func[IO.Stream]]$factory, $true))
}
foreach ($image in (Get-ChildItem (Join-Path $root 'content/locations') -File)) {
    $path = $image.FullName
    $factory = { [IO.File]::OpenRead($path) }.GetNewClosure()
    $resources.Add([Microsoft.CodeAnalysis.ResourceDescription]::new("Wisp.locations/$($image.Name)", [Func[IO.Stream]]$factory, $true))
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$output = [IO.File]::Create((Join-Path (Resolve-Path $OutputDirectory) 'Wisp.dll'))
try {
    $result = $compilation.Emit($output, $null, $null, $null, $resources)
} finally { $output.Dispose() }
foreach ($diagnostic in $result.Diagnostics) { Write-Output $diagnostic.ToString() }
if (!$result.Success) { throw 'Wisp compilation failed.' }
Write-Output "Built Wisp $version in $OutputDirectory"
& $Python -B (Join-Path $PSScriptRoot 'build_manifest.py') $OutputDirectory --capture
if ($LASTEXITCODE -ne 0) { throw 'Build manifest failed.' }
