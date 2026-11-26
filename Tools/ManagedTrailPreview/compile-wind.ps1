param([switch]$FringeOnly)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$kl = (Resolve-Path (Join-Path $root '../KL')).Path
$stageName = if ($FringeOnly) { 'fringe-compiler' } else { 'wind-compiler' }
$stage = Join-Path $root ".vissandbox/managed-trail-preview/$stageName"
New-Item -ItemType Directory -Path (Join-Path $stage 'Content') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $kl 'Effects/XNBCompiler.exe') -Destination $stage -Force
Get-ChildItem -LiteralPath (Join-Path $kl 'Effects') -Filter '*.dll' | Copy-Item -Destination $stage -Force
$names = if ($FringeOnly) { 'WindSlashFringe' } else { 'CrescentFan', 'ArcFlame' }
$assetRoot = if ($FringeOnly) { $root } else { $kl }
foreach ($name in $names) {
    Copy-Item -LiteralPath (Join-Path $assetRoot "Effects/Content/$name.fx") -Destination (Join-Path $stage "Content/$name.fx") -Force
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
if (-not $process.WaitForExit(30000)) { $process.Kill(); throw 'Shader compiler timed out.' }
$stdout.Result | Write-Output
# The legacy compiler can fail on its final redirected Console.ReadKey; validate actual outputs.
foreach ($name in $names) {
    $compiled = Join-Path $stage "Content/$name.xnb"
    $source = Join-Path $stage "Content/$name.fx"
    if (-not (Test-Path -LiteralPath $compiled)) { throw ("No compiled $name.xnb: " + $stderr.Result) }
    if ((Get-Item -LiteralPath $compiled).LastWriteTimeUtc -lt (Get-Item -LiteralPath $source).LastWriteTimeUtc) { throw "$name shader output is stale." }
}
$process.Dispose()
foreach ($name in $names) {
    Copy-Item -LiteralPath (Join-Path $stage "Content/$name.xnb") -Destination (Join-Path $assetRoot "Effects/Content/$name.xnb") -Force
}
