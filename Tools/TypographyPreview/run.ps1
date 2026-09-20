param([string]$Tml = 'D:\Steam\steamapps\common\tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
python (Join-Path $PSScriptRoot 'prepare.py') --tml $Tml
if ($LASTEXITCODE -ne 0) { throw 'Drawing extraction failed.' }
dotnet run --project (Join-Path $repo '.vissandbox\typography-preview\Preview.csproj') -- $repo
if ($LASTEXITCODE -ne 0) { throw 'FNA typography preview failed.' }
