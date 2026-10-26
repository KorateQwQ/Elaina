param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/doll-mining-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
# Exercise actual mining completion, loot acceptance and persistence; only unrelated player systems are stubbed.
$source = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'ElainaModSkills/Skills/DollControl/DollCommandPlayer.cs')
$hitStart = $source.IndexOf('    internal void Hit(')
$hitEnd = $source.IndexOf('    #endregion', $hitStart)
$saveStart = $source.IndexOf('    public override void SaveData(')
$saveEnd = $source.IndexOf('    #endregion', $saveStart)
$markStart = $source.IndexOf('    public static bool IsMarkable(')
$markEnd = $source.IndexOf('    /// <summary>', $markStart)
$deliverStart = $source.IndexOf('    public void DeliverLoot(')
$deliverEnd = $source.IndexOf('    // 收集物存进角色存档', $deliverStart)
if ($hitStart -lt 0 -or $hitEnd -lt 0 -or $saveStart -lt 0 -or $saveEnd -lt 0 -or $markStart -lt 0 -or $markEnd -lt 0) { throw 'Production mining methods not found.' }
$generated = "using System;`nusing Terraria.ID;`nusing Terraria.ModLoader.IO;`nusing Terraria.Audio;`nusing Terraria.DataStructures;`nnamespace 伊蕾娜.ElainaModSkills.Skills.DollControl;`npublic sealed partial class DollCommandPlayer {`n" +
    $source.Substring($hitStart, $hitEnd - $hitStart) + $source.Substring($saveStart, $saveEnd - $saveStart) + $source.Substring($markStart, $markEnd - $markStart) + $source.Substring($deliverStart, $deliverEnd - $deliverStart) + "`n}"
Set-Content -LiteralPath (Join-Path $output 'MiningPlayer.cs') -Value $generated -Encoding utf8
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(
    @('Reference', "$Tml/tModLoader.dll"),
    @('Reference', "$Tml/Libraries/**/*.dll"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollTileBreaker.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollMiningNetwork.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollMiningCargo.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollTileSelection.cs"),
    @('Compile', "$output/MiningPlayer.cs"),
    @('Compile', "$PSScriptRoot/MiningNetworkHost.cs.txt"),
    @('Compile', "$PSScriptRoot/MiningNetworkChecks.cs.txt"),
    @('Compile', "$PSScriptRoot/FurnitureMiningChecks.cs.txt"))) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[1].EndsWith('**/*.dll')) {
        $node.SetAttribute('Exclude', "$Tml/Libraries/Native/**;$Tml/Libraries/**/runtime*/**;$Tml/Libraries/**/*.resources.dll;$Tml/Libraries/tModCodeAssist/**")
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$path = Join-Path $output 'Checks.csproj'; $project.Save($path)
dotnet run --project $path --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Native furniture mining checks failed.' }
