param(
    [string]$Tml = 'D:/Steam/steamapps/common/tModLoader',
    [string]$VanillaItems = 'D:/泰拉原版贴图与代码与其他工具/1.4贴图/Item'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/doll-cursor'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(
    @('Reference', "$Tml/Libraries/FNA/1.0.0/FNA.dll"),
    @('Reference', "$Tml/Libraries/ReLogic/1.0.0/ReLogic.dll"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollChestCursorDrawing.cs"),
    @('Compile', "$PSScriptRoot/Preview.cs.txt"),
    @('None', "$Tml/Libraries/Native/Windows/*.dll"))) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') {
        $node.SetAttribute('Link', '%(Filename)%(Extension)'); $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$path = Join-Path $output 'Preview.csproj'; $project.Save($path)
dotnet run --project $path --verbosity quiet -- $repo $output $VanillaItems
if ($LASTEXITCODE -ne 0) { throw 'Chest cursor FNA preview failed.' }
