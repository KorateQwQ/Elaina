param([string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$work = Join-Path $repo '.vissandbox/magic-barrier-preview'
New-Item -ItemType Directory -Force -Path $work | Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/../BloomBackbufferPreview/Preview.csproj.txt" -Destination "$work/Preview.csproj"
Copy-Item -LiteralPath "$PSScriptRoot/Preview.cs.txt" -Destination "$work/Program.cs"
dotnet run --project "$work/Preview.csproj" "-p:TmlPath=$TmlPath" -- $repo "$work/out"
if ($LASTEXITCODE -ne 0) { throw 'Magic barrier FNA preview failed.' }
python "$PSScriptRoot/compose.py" "$work/out"
if ($LASTEXITCODE -ne 0) { throw 'Contact sheet composition failed.' }
