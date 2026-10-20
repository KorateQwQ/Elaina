param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$Checks, [string]$TerrariaContent)
$ErrorActionPreference = 'Stop'
if (!$Checks) {
    if (!$TerrariaContent) { $TerrariaContent = Join-Path (Split-Path $Tml -Parent) 'Terraria/Content' }
    if (!(Test-Path -LiteralPath (Join-Path $TerrariaContent 'Images/MagicPixel.xnb'))) {
        throw 'Specify -TerrariaContent pointing to the real Terraria Content directory (Images/MagicPixel.xnb is required).'
    }
}
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/recall-preview'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$sources = @('RecallPortalDrawing')
$harness = @('Preview.cs.txt')
if ($Checks) {
    $sources += @('RecallSkill', 'RecallPortalSystem', 'RecallPortalPlayer', 'RecallPortalProjectile')
    $harness = @('Checks.cs.txt', 'NetworkChecks.cs.txt', 'EngineStubs.cs.txt')
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
$name = if ($Checks) { 'Checks' } else { 'Preview' }
$projectPath = Join-Path $output "$name.csproj"
$project.Save($projectPath)
dotnet run --project $projectPath --verbosity quiet -- $output $TerrariaContent
if ($LASTEXITCODE -ne 0) { throw "Recall $name failed." }
