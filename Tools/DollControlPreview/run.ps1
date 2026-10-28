param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$Checks)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/kuma-preview'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$entries = ,@('Reference', (Join-Path $Tml 'Libraries/FNA/1.0.0/FNA.dll'))
foreach ($source in @('DollPetProjectile', 'KumaPetProjectile', 'AdditionalDollProjectiles', 'DollControlSkill', 'DollControlProgression', 'DollWorkAssignments', 'DollSummonVisuals')) {
    $entries += ,@('Compile', (Join-Path $repo "ElainaModSkills/Skills/DollControl/$source.cs"))
}
$entries += ,@('Compile', (Join-Path $repo 'ElainaModSkills/SkillBalanceConfig.cs'))
# Link freshly extracted production target selectors; only world/tool readiness is stubbed.
$production = Join-Path $repo 'ElainaModSkills/Skills/DollControl'
$mining = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $production 'DollCommandPlayer.cs')
$placing = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $production 'DollPlacementTask.cs')
$mineStart = $mining.IndexOf('    internal bool TrySelectTarget(')
$mineEnd = $mining.IndexOf('    private bool InRange(', $mineStart)
$placeStart = $placing.IndexOf('    internal bool TrySelectPlacementTarget(')
# Stop before CompletePlacement's doc comment; replaying vanilla placement needs the real engine (garden.ps1).
$placeEnd = $placing.LastIndexOf('    ///', $placing.IndexOf('    internal void CompletePlacement(', $placeStart))
if ($mineStart -lt 0 -or $mineEnd -lt 0 -or $placeStart -lt 0 -or $placeEnd -lt $placeStart) { throw 'Production selectors were not found.' }
$selectors = "using System;`nusing System.Linq;`nusing System.Collections.Generic;`nnamespace " + [char]0x4F0A + [char]0x857E + [char]0x5A1C + ".ElainaModSkills.Skills.DollControl;`npublic sealed partial class DollCommandPlayer {`n" +
    $mining.Substring($mineStart, $mineEnd - $mineStart) + $placing.Substring($placeStart, $placeEnd - $placeStart) + "`n}"
$garden = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $production 'DollGardenTask.cs')
$nearStart = $garden.IndexOf('    internal bool HasNearbyTask =>')
$nearEnd = $garden.IndexOf(';', $nearStart) + 1
$selectors += "`npublic sealed partial class DollCommandPlayer {`n" + $garden.Substring($nearStart, $nearEnd - $nearStart) + "`n}"
$selectorPath = Join-Path $output 'TargetSelectors.cs'
Set-Content -LiteralPath $selectorPath -Value $selectors -Encoding utf8
$entries += ,@('Compile', $selectorPath)
foreach ($source in @('EngineStubs.cs.txt', 'Checks.cs.txt', 'TeamChecks.cs.txt', 'ProgressionChecks.cs.txt')) {
    $entries += ,@('Compile', (Join-Path $PSScriptRoot $source))
}
if (!$Checks) { $entries += ,@('Compile', (Join-Path $PSScriptRoot 'Preview.cs.txt')) }
$entries += ,@('None', (Join-Path $Tml 'Libraries/Native/Windows/*.dll'))
foreach ($entry in $entries) {
    $node = $project.CreateElement($entry[0])
    $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') {
        $node.SetAttribute('Link', '%(Filename)%(Extension)')
        $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$startup = $project.CreateElement('StartupObject')
$startup.InnerText = if ($Checks) { 'Checks' } else { 'Preview' }
[void]$project.Project.PropertyGroup.AppendChild($startup)
$projectPath = Join-Path $output 'Preview.csproj'
$project.Save($projectPath)
dotnet run --project $projectPath --verbosity quiet -- $repo $output
if ($LASTEXITCODE -ne 0) { throw 'Doll control checks/preview failed.' }
