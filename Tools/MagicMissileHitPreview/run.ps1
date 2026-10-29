param(
    [string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader',
    [string]$TerrariaImages = 'D:/Steam/steamapps/common/Terraria/Content/Images'
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$kl = (Resolve-Path (Join-Path $root '../KL')).Path
$work = Join-Path $root '.vissandbox/magic-missile-hit'
$out = Join-Path $work 'out'
New-Item -ItemType Directory -Path $work -Force | Out-Null
if (Test-Path $out) { Get-ChildItem -LiteralPath $out -Recurse -File | Remove-Item -Force }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Preview.csproj.txt') -Destination (Join-Path $work 'Preview.csproj')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Program.cs.txt') -Destination (Join-Path $work 'Program.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'TrailChecks.cs.txt') -Destination (Join-Path $work 'TrailChecks.cs')
dotnet run --project (Join-Path $work 'Preview.csproj') "-p:TmlPath=$TmlPath" "-p:ElainaPath=$root" -- $kl $out $root $TerrariaImages
if ($LASTEXITCODE -ne 0) { throw "Preview failed: $LASTEXITCODE" }
python -I (Join-Path $PSScriptRoot 'compose.py') $out
if ($LASTEXITCODE -ne 0) { throw "Compose failed: $LASTEXITCODE" }
