param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$Checks, [switch]$TransitionOnly)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/recall-preview'
New-Item -ItemType Directory -Force -Path $output | Out-Null

if (!$Checks -and !$TransitionOnly) {
    # Keep this file ASCII (PowerShell 5.1 reads it as ANSI). The host compiles production code and the KL bloom pipeline with Roslyn.
    $hostDir = Join-Path $output 'host'
    New-Item -ItemType Directory -Force -Path $hostDir | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Preview.csproj.txt') -Destination (Join-Path $hostDir 'Preview.csproj')
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Preview.cs.txt') -Destination (Join-Path $hostDir 'Preview.cs')
    dotnet run --project (Join-Path $hostDir 'Preview.csproj') --verbosity quiet "-p:TmlPath=$Tml" -- $repo $output $PSScriptRoot
    if ($LASTEXITCODE -ne 0) { throw 'Recall Preview failed.' }
    return
}

$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$sources = @('RecallPortalDrawing', 'RecallPortalScript', 'RecallPortalPattern', 'RecallSkill', 'RecallPortalSystem', 'RecallPortalPlayer', 'RecallPortalProjectile', 'RecallTransition')
$harness = @('Checks.cs.txt', 'NetworkChecks.cs.txt', 'EngineStubs.cs.txt')
if ($TransitionOnly) {
    $sources = @('RecallTransition')
    $harness = @('TransitionPreview.cs.txt')
}
$entries = ,@('Reference', (Join-Path $Tml 'Libraries/FNA/1.0.0/FNA.dll'))
foreach ($source in $sources) { $entries += ,@('Compile', (Join-Path $repo "ElainaModSkills/Skills/Recall/$source.cs")) }
foreach ($source in $harness) { $entries += ,@('Compile', (Join-Path $PSScriptRoot $source)) }
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
$projectPath = Join-Path $output $(if ($TransitionOnly) { 'Transition.csproj' } else { 'Checks.csproj' })
$project.Save($projectPath)
dotnet run --project $projectPath --verbosity quiet -- $output
if ($LASTEXITCODE -ne 0) { throw 'Recall checks or transition preview failed.' }
