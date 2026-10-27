param(
    [string]$Tml = 'D:/Steam/steamapps/common/tModLoader',
    [string]$ImproveGame = (Join-Path $PSScriptRoot '../../../ImproveGame')
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/player-pose-preview'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$items = $project.SelectSingleNode('/Project/ItemGroup')
function Add-Item([string]$kind, [string]$path) {
    $node = $project.CreateElement($kind); $node.SetAttribute('Include', $path)
    [void]$items.AppendChild($node); return $node
}
$null = Add-Item Reference "$Tml/tModLoader.dll"
$reference = Add-Item Reference "$Tml/Libraries/**/*.dll"
$reference.SetAttribute('Exclude', "$Tml/Libraries/Native/**;$Tml/Libraries/**/runtime*/**;$Tml/Libraries/**/*.resources.dll;$Tml/Libraries/tModCodeAssist/**")
foreach ($relative in @('System/ElainaDrawAlignment.cs','System/ElainaAppearanceSystem.cs','System/SleepLayer.cs','Items/Armors/新版身体/ElainaNewBodyLayer.cs','Items/accessories/ElainaHatLayer.cs','ElainaModAlchemy/UI/AlchemyBrewAnimation.cs','ElainaModAlchemy/Gameplay/AlchemyBrewSession.cs')) {
    $null = Add-Item Compile "$repo/$relative"
}
$null = Add-Item Compile "$PSScriptRoot/Host.cs.txt"
$null = Add-Item Compile "$repo/ElainaModAlchemy/Gameplay/AlchemyEfficiency.cs"
$null = Add-Item Compile "$PSScriptRoot/Preview.cs.txt"
$null = Add-Item Compile "$PSScriptRoot/AppearanceChecks.cs.txt"
$null = Add-Item Compile "$PSScriptRoot/SleepPortraitChecks.cs.txt"
$null = Add-Item Compile "$PSScriptRoot/ImproveGameChecks.cs.txt"
# Refresh the actual foreign hook on every run; only its config singleton is a fixture.
$compatSource = Join-Path $ImproveGame 'Content/Functions/MinorPatches.cs'
$compatText = Get-Content -LiteralPath $compatSource -Raw
$compatMatch = [regex]::Match($compatText, '(?s)    private class TranslucentInvisPotion : ModPlayer.*?(?=    private class NoPylonRestrictions)')
if (-not $compatMatch.Success) { throw "Cannot extract ImproveGame's invisibility hook from $compatSource" }
$compatClass = $compatMatch.Value.Replace('private class TranslucentInvisPotion', 'public class TranslucentInvisPotion')
$compatFixture = @"
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;
namespace ImproveGameFixture;
public sealed class UIConfigs
{
    public static UIConfigs Instance { get; } = new();
    public float InvisibleTransparency = 0.5f;
}
$compatClass
"@
$compatPath = Join-Path $output 'ImproveGameFixture.cs'
Set-Content -LiteralPath $compatPath -Value $compatFixture -Encoding utf8
$null = Add-Item Compile $compatPath
$native = Add-Item None "$Tml/Libraries/Native/Windows/*.dll"
$native.SetAttribute('Link', '%(Filename)%(Extension)'); $native.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
$path = Join-Path $output 'Preview.csproj'; $project.Save($path)
dotnet run --project $path --configuration Release --verbosity quiet -- $repo $output $Tml |
    Tee-Object -FilePath (Join-Path $output 'checks.log')
if ($LASTEXITCODE -ne 0) { throw 'Player pose preview failed.' }
