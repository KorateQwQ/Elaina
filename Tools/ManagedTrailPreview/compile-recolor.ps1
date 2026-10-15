$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$kl = (Resolve-Path (Join-Path $root '../KL')).Path
$stage = Join-Path $root '.vissandbox/managed-trail-preview/compiler'
New-Item -ItemType Directory -Path (Join-Path $stage 'Content') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $kl 'Effects/XNBCompiler.exe') -Destination $stage -Force
Get-ChildItem -LiteralPath (Join-Path $kl 'Effects') -Filter '*.dll' | Copy-Item -Destination $stage -Force
$source = Join-Path $stage 'Content/ReColor.fx'
Copy-Item -LiteralPath (Join-Path $kl 'Effects/Content/ReColor.fx') -Destination $source -Force
$info = New-Object System.Diagnostics.ProcessStartInfo
$info.FileName = Join-Path $stage 'XNBCompiler.exe'
$info.WorkingDirectory = $stage
$info.UseShellExecute = $false
$info.CreateNoWindow = $true
$info.RedirectStandardInput = $true
$info.RedirectStandardOutput = $true
$info.RedirectStandardError = $true
$process = [System.Diagnostics.Process]::Start($info)
$stdout = $process.StandardOutput.ReadToEndAsync()
$stderr = $process.StandardError.ReadToEndAsync()
if (-not $process.WaitForExit(30000)) { $process.Kill(); throw 'Shader compiler timed out.' }
$stdout.Result | Write-Output
# The legacy compiler can fail on its final redirected Console.ReadKey; check the actual output.
$compiled = Join-Path $stage 'Content/ReColor.xnb'
if (-not (Test-Path -LiteralPath $compiled)) { throw ('No compiled ReColor.xnb: ' + $stderr.Result) }
if ((Get-Item -LiteralPath $compiled).LastWriteTimeUtc -lt (Get-Item -LiteralPath $source).LastWriteTimeUtc) { throw 'Compiled shader is stale.' }
$process.Dispose()
Copy-Item -LiteralPath $compiled -Destination (Join-Path $kl 'Effects/Content/ReColor.xnb') -Force
