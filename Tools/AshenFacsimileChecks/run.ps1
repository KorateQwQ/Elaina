param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$out = Join-Path $repo '.vissandbox/ashen-checks'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$items = $project.SelectSingleNode('/Project/ItemGroup')
function Add-Item([string]$kind, [string]$path) {
    $node = $project.CreateElement($kind)
    $node.SetAttribute('Include', $path)
    $null = $items.AppendChild($node)
    return $node
}
$null = Add-Item 'Reference' "$Tml/tModLoader.dll"
$reference = Add-Item 'Reference' "$Tml/Libraries/**/*.dll"
$reference.SetAttribute('Exclude', "$Tml/Libraries/Native/**;$Tml/Libraries/**/runtime*/**;$Tml/Libraries/**/*.resources.dll;$Tml/Libraries/tModCodeAssist/**")
$null = Add-Item 'Compile' "$repo/ElainaModAlchemy/Crafting/*.cs"
foreach ($path in @('AlchemyRecipe.cs','AlchemyIngredient.cs','item/AlchemyItem.cs','item/AshenFacsimileDust.cs')) {
    $null = Add-Item 'Compile' "$repo/ElainaModAlchemy/$path"
}
$checks = Add-Item 'Compile' "$PSScriptRoot/Checks.cs.txt"
$checks.SetAttribute('Link', 'Checks.cs')
$projectPath = Join-Path $out 'Checks.csproj'
$project.Save($projectPath)
dotnet run --project $projectPath --configuration Release -- $Tml $repo
if ($LASTEXITCODE -ne 0) { throw 'Ashen Facsimile Dust checks failed.' }

# Compile the entire mod against its existing dependency assemblies, without rebuilding or packaging them.
$project.Project.PropertyGroup.OutputType = 'Library'
$items.SelectNodes('Compile') | ForEach-Object { $null = $items.RemoveChild($_) }
$compile = Add-Item 'Compile' "$repo/**/*.cs"
$compile.SetAttribute('Exclude', "$repo/obj/**;$repo/bin/**;$repo/.vissandbox/**;$repo/Tools/**")
foreach ($assembly in @('../KL/bin/Debug/net8.0/KL.dll','../SilkyUIFramework.dll','../Homura/bin/Debug/net8.0/Homura.dll')) {
    $null = Add-Item 'Reference' (Join-Path $repo $assembly)
}
$property = $project.CreateElement('AssemblyName')
$property.InnerText = '伊蕾娜'
$null = $project.Project.PropertyGroup.AppendChild($property)
$fullPath = Join-Path $out 'FullMod.csproj'
$project.Save($fullPath)
dotnet build $fullPath --configuration Release --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Full mod compilation failed.' }
