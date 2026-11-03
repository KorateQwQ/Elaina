param([string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader', [string]$Dotnet = 'C:/Users/PC/.dotnet/dotnet.exe')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$kl = (Resolve-Path (Join-Path $root '../KL')).Path
$work = Join-Path $root '.vissandbox/line-sparkle-preview'
New-Item -ItemType Directory -Path $work -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Preview.csproj.txt') -Destination (Join-Path $work 'Preview.csproj')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Program.cs.txt') -Destination (Join-Path $work 'Pipeline.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Preview.cs.txt') -Destination (Join-Path $work 'Preview.cs')
& $Dotnet run --project (Join-Path $work 'Preview.csproj') '-p:StartupObject=LineSparklePreview' "-p:TmlPath=$TmlPath" -- $kl (Join-Path $work 'out')
if ($LASTEXITCODE -ne 0) { throw "Line sparkle preview failed: $LASTEXITCODE" }
