param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$Checks, [string]$TerrariaContent = '')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/experience-preview'
if (!$TerrariaContent) { $TerrariaContent = Join-Path (Split-Path $Tml -Parent) 'Terraria/Content' }
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$sources = @('ExperienceRules', 'ExperienceOrbDrawing')
$harness = @('Preview.cs.txt')
if ($Checks) {
    $sources += @('ExperienceDropNPC', 'ExperienceOrb', 'ExperienceNetwork')
    $harness = @('Checks.cs.txt', 'EngineStubs.cs.txt')
}
$entries = ,@('Reference', (Join-Path $Tml 'Libraries/FNA/1.0.0/FNA.dll'))
foreach ($name in $sources) { $entries += ,@('Compile', (Join-Path $repo "System/Experience/$name.cs")) }
if ($Checks) { $entries += ,@('Compile', (Join-Path $repo 'System/ElainaStatePlayer.cs')) }
foreach ($name in $harness) { $entries += ,@('Compile', (Join-Path $PSScriptRoot $name)) }
$entries += ,@('None', (Join-Path $Tml 'Libraries/Native/Windows/*.dll'))
foreach ($entry in $entries) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') {
        $node.SetAttribute('Link', '%(Filename)%(Extension)')
        $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$projectPath = Join-Path $output $(if ($Checks) { 'Checks.csproj' } else { 'Preview.csproj' })
$project.Save($projectPath)
dotnet run --project $projectPath --configuration Release --verbosity quiet -- $output $TerrariaContent
if ($LASTEXITCODE -ne 0) { throw 'Experience checks or preview failed.' }
