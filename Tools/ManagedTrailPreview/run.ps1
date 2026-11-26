param([string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader', [switch]$WindAlignment, [switch]$IceBreak, [switch]$WindWeb)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$work = Join-Path $root '.vissandbox/managed-trail-preview'
if ($WindWeb) { $work = Join-Path $root '.vissandbox/wind-web-preview' }
New-Item -ItemType Directory -Path $work -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Preview.csproj.txt') -Destination (Join-Path $work 'Preview.csproj')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Program.cs.txt') -Destination (Join-Path $work 'Program.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'VisualBlend.cs.txt') -Destination (Join-Path $work 'VisualBlend.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'WindAlignment.cs.txt') -Destination (Join-Path $work 'WindAlignment.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'WindWeb.cs.txt') -Destination (Join-Path $work 'WindWeb.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'IceBreak.cs.txt') -Destination (Join-Path $work 'IceBreak.cs')
$output = if ($IceBreak) { Join-Path $work 'out/ice-break' } elseif ($WindAlignment) { Join-Path $work 'out/wind-alignment' } else { Join-Path $work 'out' }
$mode = if ($WindWeb) { 'wind-web' } elseif ($IceBreak) { 'ice-break' } elseif ($WindAlignment) { 'wind-alignment' } else { 'regression' }
$dotnet = 'dotnet'
$userDotnet = Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'
if (-not (dotnet --list-sdks | Select-String '^10\.') -and (Test-Path -LiteralPath $userDotnet)) {
    $dotnet = $userDotnet
}
& $dotnet run --project (Join-Path $work 'Preview.csproj') "-p:TmlPath=$TmlPath" -- $root $output $mode
if ($LASTEXITCODE -ne 0) { throw "Managed trail preview failed: $LASTEXITCODE" }
