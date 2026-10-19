param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$PotionsOnly)
$ErrorActionPreference = 'Stop'
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
foreach ($path in @('AlchemyRecipe.cs', 'AlchemyIngredient.cs', 'UI/AlchemyCatalog.cs', 'UI/AlchemyNotebookSnapshot.cs', 'Gameplay/*.cs', 'Crafting/*.cs', 'Buffs/*.cs')) {
    $source = Add-CheckItem 'Compile' "$repo/ElainaModAlchemy/$path"
    if ($path -eq 'Buffs/*.cs') { $source.SetAttribute('Exclude', "$repo/ElainaModAlchemy/Buffs/MoonDewElixirBuff.cs") }
}
$itemSources = Add-CheckItem 'Compile' "$repo/ElainaModAlchemy/item/**/*.cs"
# Moon dew requires the full player implementation and is covered by run-integration.ps1.
$itemSources.SetAttribute('Exclude', "$repo/ElainaModAlchemy/item/Potions/MoonDewElixir.cs;$repo/ElainaModAlchemy/item/AlchemyCatalogItemTypes.cs")
$check = Add-CheckItem 'Compile' "$PSScriptRoot/Checks.cs.txt"
$check.SetAttribute('Link', 'Checks.cs')
$check = Add-CheckItem 'Compile' "$PSScriptRoot/PotionBehaviorChecks.cs.txt"
$check.SetAttribute('Link', 'PotionBehaviorChecks.cs')
$projectPath = Join-Path $output 'Checks.csproj'
$project.Save($projectPath)
$checkArguments = @($repo)
if ($PotionsOnly) { $checkArguments += '--potions-only' }
dotnet run --project $projectPath --configuration Release -- @checkArguments
if ($LASTEXITCODE -ne 0) { throw 'Alchemy item checks failed.' }
