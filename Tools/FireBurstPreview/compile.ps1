param([string]$CompilerDirectory = '')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
if (!$CompilerDirectory) { $CompilerDirectory = Join-Path (Split-Path $repo -Parent) 'KL/Effects' }
$stage = Join-Path $repo '.vissandbox/fire-burst-preview/shader-compiler'
New-Item -ItemType Directory -Force -Path (Join-Path $stage 'Content') | Out-Null
Copy-Item -LiteralPath (Join-Path $CompilerDirectory 'XNBCompiler.exe') -Destination $stage -Force
Get-ChildItem -LiteralPath $CompilerDirectory -Filter '*.dll' | Copy-Item -Destination $stage -Force
$shaders = @('FireBurstFlame')
# Preview-only candidate shaders: sources live in this folder, output goes to .vissandbox (never into production Effects).
$previewShaders = @()
foreach ($name in $previewShaders) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "$name.fx") -Destination (Join-Path $stage "Content/$name.fx") -Force
    $old = Join-Path $stage "Content/$name.xnb"
    if (Test-Path -LiteralPath $old) { Remove-Item -LiteralPath $old }
}
foreach ($name in $shaders) {
    Copy-Item -LiteralPath (Join-Path $repo "Effects/Content/$name.fx") -Destination (Join-Path $stage "Content/$name.fx") -Force
    # Never accept a previous successful binary after a failed source compilation.
    $old = Join-Path $stage "Content/$name.xnb"
    if (Test-Path -LiteralPath $old) { Remove-Item -LiteralPath $old }
}
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
if (!$process.WaitForExit(30000)) { $process.Kill(); throw 'Fire burst shader compiler timed out.' }
$stdout.Result | Write-Output
$process.Dispose()
# This compiler ends with Console.ReadKey, which may fail with redirected stdin.
foreach ($name in $shaders) {
    $compiled = Join-Path $stage "Content/$name.xnb"
    if (!(Test-Path -LiteralPath $compiled)) {
        $compilerLog = Join-Path $stage 'logfile.txt'
        if (Test-Path -LiteralPath $compilerLog) { Get-Content -LiteralPath $compilerLog -Tail 8 | Write-Output }
        throw "No fresh $name shader output; see compiler diagnostics above."
    }
    Copy-Item -LiteralPath $compiled -Destination (Join-Path $repo "Effects/Content/$name.xnb") -Force
    Write-Output "Compiled production $name.xnb from the current FX source."
}
$previewOut = Join-Path $repo '.vissandbox/fire-burst-preview/preview-effects'
New-Item -ItemType Directory -Force -Path $previewOut | Out-Null
foreach ($name in $previewShaders) {
    $compiled = Join-Path $stage "Content/$name.xnb"
    if (!(Test-Path -LiteralPath $compiled)) {
        $compilerLog = Join-Path $stage 'logfile.txt'
        if (Test-Path -LiteralPath $compilerLog) { Get-Content -LiteralPath $compilerLog -Tail 8 | Write-Output }
        throw "No fresh preview $name shader output; see compiler diagnostics above."
    }
    Copy-Item -LiteralPath $compiled -Destination (Join-Path $previewOut "$name.xnb") -Force
    Write-Output "Compiled preview-only $name.xnb."
}
