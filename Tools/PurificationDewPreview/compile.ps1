param([string]$CompilerDirectory = '')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
if (!$CompilerDirectory) { $CompilerDirectory = Join-Path (Split-Path $repo -Parent) 'KL/Effects' }
$stage = Join-Path $repo '.vissandbox/purification-dew/production-compiler'
New-Item -ItemType Directory -Force -Path (Join-Path $stage 'Content') | Out-Null
Copy-Item -LiteralPath (Join-Path $CompilerDirectory 'XNBCompiler.exe') -Destination $stage -Force
Get-ChildItem -LiteralPath $CompilerDirectory -Filter '*.dll' | Copy-Item -Destination $stage -Force
$source = Join-Path $stage 'Content/PurificationDew.fx'
$compiled = Join-Path $stage 'Content/PurificationDew.xnb'
Copy-Item -LiteralPath (Join-Path $repo 'Effects/Content/PurificationDew.fx') -Destination $source -Force
# Never accept a previous successful binary after a failed source compilation.
if (Test-Path -LiteralPath $compiled) { Remove-Item -LiteralPath $compiled }
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
if (!$process.WaitForExit(30000)) { $process.Kill(); throw 'Dew shader compiler timed out.' }
$stdout.Result | Write-Output
# This compiler ends with Console.ReadKey, which may fail with redirected stdin.
if (!(Test-Path -LiteralPath $compiled)) {
    $compilerLog = Join-Path $stage 'logfile.txt'
    if (Test-Path -LiteralPath $compilerLog) { Get-Content -LiteralPath $compilerLog -Tail 8 | Write-Output }
    $process.Dispose()
    throw 'No fresh dew shader output; see compiler diagnostics above.'
}
$process.Dispose()
Copy-Item -LiteralPath $compiled -Destination (Join-Path $repo 'Effects/Content/PurificationDew.xnb') -Force
Write-Output 'Compiled production PurificationDew.xnb from the current FX source.'
