param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$Checks)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/kuma-preview'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$entries = ,@('Reference', (Join-Path $Tml 'Libraries/FNA/1.0.0/FNA.dll'))
foreach ($source in @('DollPetProjectile', 'KumaPetProjectile', 'DollControlSkill')) {
    $entries += ,@('Compile', (Join-Path $repo "ElainaModSkills/Skills/DollControl/$source.cs"))
}
foreach ($source in @('EngineStubs.cs.txt', 'Checks.cs.txt')) {
    $entries += ,@('Compile', (Join-Path $PSScriptRoot $source))
}
if (!$Checks) { $entries += ,@('Compile', (Join-Path $PSScriptRoot 'Preview.cs.txt')) }
$entries += ,@('None', (Join-Path $Tml 'Libraries/Native/Windows/*.dll'))
foreach ($entry in $entries) {
    $node = $project.CreateElement($entry[0])
    $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') {
        $node.SetAttribute('Link', '%(Filename)%(Extension)')
        $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$startup = $project.CreateElement('StartupObject')
$startup.InnerText = if ($Checks) { 'Checks' } else { 'Preview' }
[void]$project.Project.PropertyGroup.AppendChild($startup)
$projectPath = Join-Path $output 'Preview.csproj'
$project.Save($projectPath)
dotnet run --project $projectPath --verbosity quiet -- $repo $output
if ($LASTEXITCODE -ne 0) { throw 'Doll control checks/preview failed.' }
