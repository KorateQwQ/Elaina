param([string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$kl = (Resolve-Path (Join-Path $root '../KL')).Path
$work = Join-Path $root '.vissandbox/bloom-backbuffer-preview'
New-Item -ItemType Directory -Path $work -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Preview.csproj.txt') -Destination (Join-Path $work 'Preview.csproj')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Program.cs.txt') -Destination (Join-Path $work 'Program.cs')
dotnet run --project (Join-Path $work 'Preview.csproj') "-p:TmlPath=$TmlPath" -- $kl (Join-Path $work 'out')
if ($LASTEXITCODE -ne 0) { throw "Preview failed: $LASTEXITCODE" }
