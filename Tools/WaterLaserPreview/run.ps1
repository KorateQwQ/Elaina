param([string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader', [string]$Dotnet = 'dotnet',
    [ValidateSet('Original', 'Clear')][string]$ParticleVariant = 'Clear', [switch]$BallFrames)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$work = Join-Path $root '.vissandbox/water-laser-preview'
New-Item -ItemType Directory -Path $work -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Preview.csproj.txt') -Destination (Join-Path $work 'Preview.csproj')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Program.cs.txt') -Destination (Join-Path $work 'Program.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Probe.cs.txt') -Destination (Join-Path $work 'Probe.cs')
$env:WATER_BALL_FRAMES = if ($BallFrames) { '1' } else { '' }
$outName = if ($ParticleVariant -eq 'Clear') { 'out-clear' } else { 'out' }
& $Dotnet run --project (Join-Path $work 'Preview.csproj') "-p:TmlPath=$TmlPath" -- $root (Join-Path $work $outName) $ParticleVariant
if ($LASTEXITCODE -ne 0) { throw "Water laser preview failed: $LASTEXITCODE" }
