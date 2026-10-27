param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/doll-garden-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
# Link the production loot delivery / save region so chest cargo, deposits and loot all run their real code.
$source = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'ElainaModSkills/Skills/DollControl/DollCommandPlayer.cs')
$lootStart = $source.IndexOf('    #region ' + [char]0x6536 + [char]0x96C6 + [char]0x7269)
$lootEnd = $source.IndexOf('    #endregion', $lootStart)
if ($lootStart -lt 0 -or $lootEnd -lt 0) { throw 'Production loot region not found.' }
$generated = "using System;`nusing Terraria.Audio;`nusing Terraria.DataStructures;`nusing Terraria.ID;`nusing Terraria.ModLoader.IO;`nnamespace " + [char]0x4F0A + [char]0x857E + [char]0x5A1C + ".ElainaModSkills.Skills.DollControl;`npublic sealed partial class DollCommandPlayer {`n" +
    $source.Substring($lootStart, $lootEnd - $lootStart) + "    #endregion`n}"
Set-Content -LiteralPath (Join-Path $output 'LootPlayer.cs') -Value $generated -Encoding utf8
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><DefineConstants>GARDEN</DefineConstants><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(
    @('Reference', "$Tml/tModLoader.dll"),
    @('Reference', "$Tml/Libraries/**/*.dll"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollPlanterBuilding.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollHerbPlanting.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollGardenSystem.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollGardenTask.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollTileBreaker.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollMiningRules.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollWorkAssignments.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollPetProjectile.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollControlProgression.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollSummonVisuals.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollChestReach.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollChestReachSystem.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollChestCursorSystem.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollChestCursorDrawing.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollChestCargo.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollChestDeposit.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollChestTransferSystem.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollMiningCargo.cs"),
    @('Compile', "$repo/Localization/UIText.cs"),
    @('Compile', "$output/LootPlayer.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/KumaPetProjectile.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollPlacementTask.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollPlacementSystem.cs"),
    @('Compile', "$PSScriptRoot/GardenNativeHost.cs.txt"),
    @('Compile', "$PSScriptRoot/GardenNativeChecks.cs.txt"),
    @('Compile', "$PSScriptRoot/LootNativeChecks.cs.txt"),
    @('Compile', "$PSScriptRoot/ChestNativeChecks.cs.txt"),
    @('Compile', "$PSScriptRoot/ChestCursorNativeChecks.cs.txt"),
    @('Compile', "$PSScriptRoot/ChestDepositNativeChecks.cs.txt"),
    @('Compile', "$PSScriptRoot/MiningNativeChecks.cs.txt"),
    @('Compile', "$PSScriptRoot/PlanterFixture.cs.txt"),
    @('Compile', "$PSScriptRoot/PlanterNativeChecks.cs.txt"))) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[1].EndsWith('**/*.dll')) {
        $node.SetAttribute('Exclude', "$Tml/Libraries/Native/**;$Tml/Libraries/**/runtime*/**;$Tml/Libraries/**/*.resources.dll;$Tml/Libraries/tModCodeAssist/**")
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$path = Join-Path $output 'Checks.csproj'; $project.Save($path)
dotnet run --project $path --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Native planter placement checks failed.' }
