param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/healing-behavior'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(
    @('Reference', (Join-Path $Tml 'Libraries/FNA/1.0.0/FNA.dll')),
    @('Compile', (Join-Path $PSScriptRoot 'BehaviorChecks.cs.txt')),
    @('Compile', (Join-Path $repo 'ElainaModSkills/Skills/Heal/HealSkill.cs')),
    @('Compile', (Join-Path $repo 'ElainaModSkills/Skills/Heal/HealSkill.Values.cs')),
    @('Compile', (Join-Path $repo 'ElainaModSkills/SkillBalanceConfig.cs')),
    @('Compile', (Join-Path $repo 'ElainaModSkills/Skills/Heal/HealProj.Healing.cs')))) {
    $node = $project.CreateElement($entry[0])
    $node.SetAttribute('Include', $entry[1])
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$projectPath = Join-Path $output 'Checks.csproj'
$project.Save($projectPath)
dotnet run --project $projectPath --verbosity quiet -- $repo
if ($LASTEXITCODE -ne 0) { throw 'Healing behavior checks failed.' }
