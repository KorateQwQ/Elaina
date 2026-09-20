param([string]$Tml = 'D:\Steam\steamapps\common\tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$preview = Join-Path $repo '.vissandbox\skillbar-preview'
New-Item -ItemType Directory -Path $preview -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Preview.cs.txt') -Destination (Join-Path $preview 'Program.cs')
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup/></Project>'
# XML APIs preserve paths containing spaces and non-ASCII characters.
foreach ($entry in @(
    @('Reference', (Join-Path $Tml 'Libraries\FNA\1.0.0\FNA.dll')),
    @('Reference', (Join-Path $Tml 'Libraries\ReLogic\1.0.0\ReLogic.dll')),
    @('Compile', (Join-Path $repo 'ElainaModSkills\ElainaSkillUI\SkillBarDrawing.cs')),
    @('None', (Join-Path $Tml 'Libraries\Native\Windows\*.dll')))) {
    $node = $project.CreateElement($entry[0])
    $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') {
        $node.SetAttribute('Link', '%(Filename)%(Extension)')
        $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$project.Save((Join-Path $preview 'Preview.csproj'))
dotnet run --project (Join-Path $preview 'Preview.csproj') -- $repo (Join-Path $preview 'output')
if ($LASTEXITCODE -ne 0) { throw 'Skill bar FNA preview failed.' }
