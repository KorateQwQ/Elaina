param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [string]$Python = 'python')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$out = Join-Path $repo '.vissandbox/ashen-tooltip-preview'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$items = $project.SelectSingleNode('/Project/ItemGroup')
function Add-PreviewItem([string]$kind, [string]$path) {
    $node = $project.CreateElement($kind)
    $node.SetAttribute('Include', $path)
    [void]$items.AppendChild($node)
    return $node
}
foreach ($name in @('FNA','ReLogic')) { $null = Add-PreviewItem 'Reference' "$Tml/Libraries/$name/1.0.0/$name.dll" }
$null = Add-PreviewItem 'Reference' "$Tml/Libraries/hjson/3.0.0/lib/netstandard1.3/Hjson.dll"
$null = Add-PreviewItem 'Compile' "$repo/ElainaModAlchemy/Crafting/FacsimileCraftingTooltip.cs"
$null = Add-PreviewItem 'Compile' "$repo/ElainaModAlchemy/Crafting/FacsimileMaterialRule.cs"
$null = Add-PreviewItem 'Compile' "$repo/ElainaModAlchemy/Crafting/FacsimileTooltipEffect.cs"
$null = Add-PreviewItem 'Compile' "$repo/Tools/LocalizationChecks/PreviewLocalization.cs.txt"
$null = Add-PreviewItem 'Compile' "$PSScriptRoot/TooltipPreview.cs.txt"
$native = Add-PreviewItem 'None' "$Tml/Libraries/Native/Windows/*.dll"
$native.SetAttribute('Link', '%(Filename)%(Extension)')
$native.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
$path = Join-Path $out 'Preview.csproj'
$project.Save($path)
foreach ($culture in @('zh-Hans','en-US')) {
    dotnet run --project $path --configuration Release -- $repo $Tml $culture
    if ($LASTEXITCODE -ne 0) { throw "Tooltip FNA preview failed: $culture" }
    & $Python (Join-Path $PSScriptRoot 'assemble-fx.py') (Join-Path $out "frames-$culture") (Join-Path $out "tooltip-$culture.gif")
    if ($LASTEXITCODE -ne 0) { throw "Tooltip animation assembly failed: $culture" }
}
