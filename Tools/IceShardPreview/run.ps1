param(
    [string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader',
    [string]$Dotnet = 'C:/Users/PC/.dotnet/dotnet.exe',
    [switch]$Combined
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$work = Join-Path $root '.vissandbox/ice-shard-preview'
$output = Join-Path $work 'out-stones-v2'
if ($Combined) { $output = Join-Path $work 'out-sprites-white' }
New-Item -ItemType Directory -Path $work -Force | Out-Null
[xml]$project = Get-Content -Raw (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Preview.csproj.txt')
$sources = if ($Combined) { @('ComboPreview.cs.txt', 'ComboReplay.cs.txt', 'IceShardScene.cs.txt') } else { @('Preview.cs.txt', 'IceShardScene.cs.txt') }
foreach ($source in $sources) {
    $node = $project.CreateElement('Compile')
    $node.SetAttribute('Include', (Join-Path $PSScriptRoot $source))
    $null = $project.Project.ItemGroup.AppendChild($node)
}
$project.Save((Join-Path $work 'Preview.csproj'))
& $Dotnet run --project (Join-Path $work 'Preview.csproj') "-p:TmlPath=$TmlPath" -- $output $root
if ($LASTEXITCODE -ne 0) { throw "Ice shard FNA preview failed: $LASTEXITCODE" }
$composer = if ($Combined) { 'compose_combo.py' } else { 'compose.py' }
python -I (Join-Path $PSScriptRoot $composer) $output
if ($LASTEXITCODE -ne 0) { throw "Ice shard composition failed: $LASTEXITCODE" }
