param([string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader',
    [string]$Dotnet = 'C:/Users/PC/.dotnet/dotnet.exe',
    [switch]$BakeHalo,
    [ValidateSet('none', 'alpha', 'tangent', 'sampling', 'interruption')][string]$Fault = 'none')
$ErrorActionPreference = 'Stop'
if ($BakeHalo) { $Fault = 'bake-halo' }
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$work = Join-Path $root '.vissandbox/action-trail-preview'
New-Item -ItemType Directory -Force $work | Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/../BloomBackbufferPreview/Preview.csproj.txt" -Destination "$work/Preview.csproj"
Copy-Item -LiteralPath "$PSScriptRoot/../BloomBackbufferPreview/Program.cs.txt" -Destination "$work/Pipeline.cs"
Copy-Item -LiteralPath "$PSScriptRoot/Preview.cs.txt" -Destination "$work/Preview.cs"
(Get-Item -LiteralPath "$work/Preview.cs").LastWriteTime = Get-Date
& $Dotnet run --project "$work/Preview.csproj" '-p:StartupObject=ActionTrailPreview' "-p:TmlPath=$TmlPath" -- $root "$work/out" $Fault
if ($LASTEXITCODE -ne 0) { throw "Action trail preview failed: $LASTEXITCODE" }
