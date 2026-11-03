param([string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader', [string]$Dotnet = 'dotnet', [switch]$Flight)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$work = Join-Path $root '.vissandbox/water-charge-preview'
New-Item -ItemType Directory -Path $work -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Preview.csproj.txt') -Destination (Join-Path $work 'Preview.csproj')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Program.cs.txt') -Destination (Join-Path $work 'Program.cs')
Copy-Item -LiteralPath (Join-Path $root 'ElainaModSkills/Skills/Water/WaterChargeFlowRenderer.cs') -Destination (Join-Path $work 'WaterChargeFlowRenderer.cs')
$outputName = if ($Flight) { 'out-flight' } else { 'out' }
$previewMode = if ($Flight) { 'flight' } else { 'charge' }
& $Dotnet run --project (Join-Path $work 'Preview.csproj') "-p:TmlPath=$TmlPath" -- $root (Join-Path $work $outputName) $previewMode
if ($LASTEXITCODE -ne 0) { throw "Water charge preview failed: $LASTEXITCODE" }
$composeScript = if ($Flight) { 'compose-flight.py' } else { 'compose.py' }
python (Join-Path $PSScriptRoot $composeScript)
if ($LASTEXITCODE -ne 0) { throw 'Preview frames rendered, but assembling the GIF failed (requires Python/Pillow).' }
