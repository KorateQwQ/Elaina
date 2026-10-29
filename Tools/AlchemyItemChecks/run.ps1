param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$PotionsOnly, [switch]$RainOnly, [switch]$RevealingOnly)
$ErrorActionPreference = 'Stop'
if ($RainOnly) {
    & (Join-Path $PSScriptRoot '../BottledRainAimPreview/check.ps1') -Tml $Tml
    return
}
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/alchemy-item-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$items = $project.SelectSingleNode('/Project/ItemGroup')
function Add-CheckItem([string]$kind, [string]$path) {
    $node = $project.CreateElement($kind)
    $node.SetAttribute('Include', $path)
    $null = $items.AppendChild($node)
    return $node
}
$null = Add-CheckItem 'Reference' "$Tml/tModLoader.dll"
$reference = Add-CheckItem 'Reference' "$Tml/Libraries/**/*.dll"
$reference.SetAttribute('Exclude', "$Tml/Libraries/Native/**;$Tml/Libraries/**/runtime*/**;$Tml/Libraries/**/*.resources.dll;$Tml/Libraries/tModCodeAssist/**")
$null = Add-CheckItem 'Compile' "$repo/Localization/UIText.cs"
$null = Add-CheckItem 'Compile' "$repo/Tools/AlchemyGameplayChecks/SkillFixture.cs.txt"
foreach ($path in @('AlchemyRecipe.cs', 'AlchemyIngredient.cs', 'UI/AlchemyCatalog.cs', 'UI/AlchemyNotebookSnapshot.cs', 'Gameplay/*.cs', 'Crafting/*.cs', 'Buffs/*.cs', 'Rain/*.cs', 'Revealing/*.cs', 'Purification/*.cs')) {
    $source = Add-CheckItem 'Compile' "$repo/ElainaModAlchemy/$path"
    # Food compatibility depends on the full player and Happy Buff; run-integration.ps1 covers it.
    if ($path -eq 'Gameplay/*.cs') { $source.SetAttribute('Exclude', "$repo/ElainaModAlchemy/Gameplay/ImproveGameFoodCompatibility.cs") }
    if ($path -eq 'Buffs/*.cs') { $source.SetAttribute('Exclude', "$repo/ElainaModAlchemy/Buffs/MoonDewElixirBuff.cs") }
}
$itemSources = Add-CheckItem 'Compile' "$repo/ElainaModAlchemy/item/**/*.cs"
# Character-dependent items require the full player implementation, checked by run-integration.ps1.
$itemSources.SetAttribute('Exclude', "$repo/ElainaModAlchemy/item/Potions/MoonDewElixir.cs;$repo/ElainaModAlchemy/item/Foods/HoneyBread.cs;$repo/ElainaModAlchemy/item/AlchemyCatalogItemTypes.cs")
$check = Add-CheckItem 'Compile' "$PSScriptRoot/Checks.cs.txt"
$check.SetAttribute('Link', 'Checks.cs')
$check = Add-CheckItem 'Compile' "$PSScriptRoot/PotionBehaviorChecks.cs.txt"
$check.SetAttribute('Link', 'PotionBehaviorChecks.cs')
$check = Add-CheckItem 'Compile' "$PSScriptRoot/RecipeScrollChecks.cs.txt"
$check.SetAttribute('Link', 'RecipeScrollChecks.cs')
$check = Add-CheckItem 'Compile' "$PSScriptRoot/ShimmerTransferChecks.cs.txt"
$check.SetAttribute('Link', 'ShimmerTransferChecks.cs')
$null = Add-CheckItem 'Compile' "$PSScriptRoot/BottledRainChecks.cs.txt"
$null = Add-CheckItem 'Compile' "$PSScriptRoot/RevealingDustChecks.cs.txt"
$protocol = [regex]::Match([IO.File]::ReadAllText("$repo/伊蕾娜.cs"), 'internal enum MessageType : byte\s*\{[^}]*\}').Value
[IO.File]::WriteAllText("$output/Protocol.cs", "namespace 伊蕾娜 { public class 伊蕾娜 { $protocol } }")
$null = Add-CheckItem 'Compile' "$output/Protocol.cs"
$projectPath = Join-Path $output 'Checks.csproj'
$project.Save($projectPath)
$checkArguments = @($repo)
if ($PotionsOnly) { $checkArguments += '--potions-only' }
if ($RainOnly) { $checkArguments += '--rain-only' }
if ($RevealingOnly) { $checkArguments += '--revealing-only' }
dotnet run --project $projectPath --configuration Release -- @checkArguments
if ($LASTEXITCODE -ne 0) { throw 'Alchemy item checks failed.' }
