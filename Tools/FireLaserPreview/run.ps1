param([string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader', [string]$Dotnet = 'dotnet', [switch]$SkipShader)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$work = Join-Path $root '.vissandbox/fire-laser-preview'
New-Item -ItemType Directory -Path $work -Force | Out-Null
if (!$SkipShader) { & (Join-Path $PSScriptRoot 'compile.ps1') }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Preview.csproj.txt') -Destination (Join-Path $work 'Preview.csproj')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Program.cs.txt') -Destination (Join-Path $work 'Program.cs')
& $Dotnet run --project (Join-Path $work 'Preview.csproj') "-p:TmlPath=$TmlPath" -- $root (Join-Path $work 'out')
if ($LASTEXITCODE -ne 0) { throw "Fire laser preview failed: $LASTEXITCODE" }
