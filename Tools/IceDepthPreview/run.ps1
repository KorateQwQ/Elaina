param([string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader', [switch]$Single, [switch]$Bloom, [switch]$Trail, [switch]$Tracking, [switch]$Launch)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$work = Join-Path $root '.vissandbox/ice-depth-preview'
New-Item -ItemType Directory -Path $work -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Preview.csproj.txt') -Destination (Join-Path $work 'Preview.csproj')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Program.cs.txt') -Destination (Join-Path $work 'Program.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Single.cs.txt') -Destination (Join-Path $work 'Single.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Bloom.cs.txt') -Destination (Join-Path $work 'Bloom.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Trail.cs.txt') -Destination (Join-Path $work 'Trail.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Launch.cs.txt') -Destination (Join-Path $work 'Launch.cs')
$mode = if ($Launch) { 'launch' } elseif ($Tracking) { 'tracking' } elseif ($Trail) { 'trail' } elseif ($Bloom) { 'bloom' } elseif ($Single) { 'single' } else { 'depth' }
dotnet run --project (Join-Path $work 'Preview.csproj') "-p:TmlPath=$TmlPath" -- $root (Join-Path $work 'out') $mode
if ($LASTEXITCODE -ne 0) { throw "Ice depth preview failed: $LASTEXITCODE" }
