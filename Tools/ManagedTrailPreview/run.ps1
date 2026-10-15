param([string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader', [switch]$WindAlignment, [switch]$IceBreak)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$work = Join-Path $root '.vissandbox/managed-trail-preview'
New-Item -ItemType Directory -Path $work -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Preview.csproj.txt') -Destination (Join-Path $work 'Preview.csproj')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Program.cs.txt') -Destination (Join-Path $work 'Program.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'VisualBlend.cs.txt') -Destination (Join-Path $work 'VisualBlend.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'WindAlignment.cs.txt') -Destination (Join-Path $work 'WindAlignment.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'IceBreak.cs.txt') -Destination (Join-Path $work 'IceBreak.cs')
$output = if ($IceBreak) { Join-Path $work 'out/ice-break' } elseif ($WindAlignment) { Join-Path $work 'out/wind-alignment' } else { Join-Path $work 'out' }
$mode = if ($IceBreak) { 'ice-break' } elseif ($WindAlignment) { 'wind-alignment' } else { 'regression' }
dotnet run --project (Join-Path $work 'Preview.csproj') "-p:TmlPath=$TmlPath" -- $root $output $mode
if ($LASTEXITCODE -ne 0) { throw "Managed trail preview failed: $LASTEXITCODE" }
