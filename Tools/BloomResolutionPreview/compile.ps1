param()
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$kl = (Resolve-Path "$repo/../KL").Path
$stage = Join-Path $repo '.vissandbox/bloom-resolution/production-compiler'
New-Item -ItemType Directory -Force "$stage/Content" | Out-Null
Copy-Item -LiteralPath "$kl/Effects/XNBCompiler.exe" -Destination $stage -Force
Get-ChildItem -LiteralPath "$kl/Effects" -Filter '*.dll' | Copy-Item -Destination $stage -Force
Copy-Item -LiteralPath "$kl/Effects/Content/Bloom.fx" -Destination "$stage/Content/Bloom.fx" -Force
$compiled = "$stage/Content/Bloom.xnb"
if (Test-Path -LiteralPath $compiled) { Remove-Item -LiteralPath $compiled }
$info = New-Object System.Diagnostics.ProcessStartInfo
$info.FileName = "$stage/XNBCompiler.exe"
$info.WorkingDirectory = $stage
$info.UseShellExecute = $false
$info.CreateNoWindow = $true
$info.RedirectStandardInput = $true
$info.RedirectStandardOutput = $true
$info.RedirectStandardError = $true
$process = [System.Diagnostics.Process]::Start($info)
$stdout = $process.StandardOutput.ReadToEndAsync()
$stderr = $process.StandardError.ReadToEndAsync()
if (!$process.WaitForExit(30000)) { $process.Kill(); throw 'Bloom compiler timed out.' }
$stdout.Result | Write-Output
# The legacy compiler calls Console.ReadKey after writing XNB; redirected stdin fails there.
# Accept only a newly produced binary, never a stale output from a previous run.
if (!(Test-Path -LiteralPath $compiled)) {
    $stderr.Result | Write-Output
    if (Test-Path "$stage/logfile.txt") { Get-Content "$stage/logfile.txt" -Tail 12 }
    throw 'No fresh Bloom.xnb was produced.'
}
$process.Dispose()
Copy-Item -LiteralPath $compiled -Destination "$kl/Effects/Content/Bloom.xnb" -Force
Write-Output 'Compiled production KL Bloom.xnb.'
