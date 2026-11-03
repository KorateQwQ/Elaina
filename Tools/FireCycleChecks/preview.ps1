param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$Water)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/fire-cycle-progress-preview'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [IO.File]::ReadAllText("$PSScriptRoot/../BloomBackbufferPreview/Preview.csproj.txt").Replace('net10.0', 'net9.0')
[IO.File]::WriteAllText("$output/Preview.csproj", $project)
Copy-Item -LiteralPath "$PSScriptRoot/ProgressPreview.cs.txt" -Destination "$output/Program.cs"
$previewArgs = @($repo, $Tml)
if ($Water) { $previewArgs += '--water' }
dotnet run --project "$output/Preview.csproj" "-p:TmlPath=$Tml" -- @previewArgs
if ($LASTEXITCODE -ne 0) { throw 'Fire progress FNA preview failed.' }
if (!$Water) { python "$PSScriptRoot/compose_progress.py" "$repo/.vissandbox/fire-cycle-checks/progress-preview" }
if ($LASTEXITCODE -ne 0) { throw 'Fire progress contact sheet failed.' }
