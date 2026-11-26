param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$ChargeOnly)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$preview = Join-Path $repo '.vissandbox/resource-bars'
New-Item -ItemType Directory -Path $preview -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Preview.cs.txt') -Destination (Join-Path $preview 'Program.cs')
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><NoWarn>0436</NoWarn></PropertyGroup><ItemGroup/></Project>'
foreach ($entry in @(
    @('Reference', (Join-Path $Tml 'Libraries/FNA/1.0.0/FNA.dll')),
    @('Reference', (Join-Path $Tml 'Libraries/ReLogic/1.0.0/ReLogic.dll')),
    @('Compile', (Join-Path $repo 'ElainaAttribute/ResourceBarDrawing.cs')),
    @('Compile', (Join-Path $repo 'ElainaAttribute/ResourceBarState.cs')),
    @('Compile', (Join-Path $repo 'ElainaAttribute/PortraitHudDrawing.cs')),
    @('Compile', (Join-Path $repo 'ElainaAttribute/PortraitHudState.cs')),
    @('Compile', (Join-Path $repo 'ElainaAttribute/ChargePoints.cs')),
    @('Compile', (Join-Path $repo 'ElainaAttribute/ChargePointHud.cs')),
    @('Compile', (Join-Path $repo 'ElainaModSkills/UICore/UIRasterFont.cs')),
    @('Compile', (Join-Path $repo 'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/ConstellationFont.cs')),
    @('Compile', (Join-Path $PSScriptRoot 'FontStubs.cs.txt')),
    @('Compile', (Join-Path $repo 'Tools/ConstellationRasterChecks/TextQualityChecks.cs.txt')),
    @('None', (Join-Path $Tml 'Libraries/Native/Windows/*.dll')))) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') {
        $node.SetAttribute('Link', '%(Filename)%(Extension)')
        $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$project.Save((Join-Path $preview 'Preview.csproj'))
$extra = @(); if ($ChargeOnly) { $extra += '--charge' }
dotnet run --project (Join-Path $preview 'Preview.csproj') -- $repo (Join-Path $preview 'output') @extra
if ($LASTEXITCODE -ne 0) { throw 'Resource bar FNA preview failed.' }
