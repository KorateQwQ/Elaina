param([ValidateSet('ShardbindChargeSheet','IceCone3D')][string]$Name = 'ShardbindChargeSheet')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$effectDir = if ($Name -eq 'IceCone3D') { 'Effects/Content/ThreeD' } else { 'Effects/Content' }
$source = Join-Path $root "$effectDir/$Name.fx"
$worker = Join-Path $PSScriptRoot 'CompileSheet.cs.txt'
$compiler = Join-Path (Split-Path $root -Parent) 'KL/Effects/XNBCompiler.exe'
$output = Join-Path $root ".vissandbox/shardbind-preview/compiler-$Name"
$binary = Join-Path $output "$Name.xnb"
$packaged = Join-Path $root "$effectDir/$Name.xnb"
$stamp = Join-Path $output 'source.sha256'
$signature = ((Get-FileHash -LiteralPath $source, $worker, $PSCommandPath, $compiler).Hash -join ':')
if ((Test-Path -LiteralPath $binary) -and (Test-Path -LiteralPath $stamp) -and
    (Get-Content -Raw -LiteralPath $stamp).Trim() -eq $signature) {
    Copy-Item -LiteralPath $binary -Destination $packaged -Force
    return
}
New-Item -ItemType Directory -Path $output -Force | Out-Null
if (Test-Path -LiteralPath $binary) { Remove-Item -LiteralPath $binary }
$stage = Join-Path $output 'compiler'
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Get-ChildItem -LiteralPath (Split-Path $compiler -Parent) -Filter '*.dll' | Copy-Item -Destination $stage -Force
$runner = Join-Path $stage 'CompileSheet.exe'
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe') /nologo /platform:x86 "/out:$runner" $worker
if ($LASTEXITCODE -ne 0) { throw 'Failed to build the noninteractive shader compiler host.' }
$info = New-Object System.Diagnostics.ProcessStartInfo
$info.FileName = $runner
$info.WorkingDirectory = $stage
$info.Arguments = '"{0}" "{1}" "{2}"' -f $compiler, $source, $output
$info.UseShellExecute = $false
$info.CreateNoWindow = $true
$info.RedirectStandardOutput = $true
$info.RedirectStandardError = $true
$process = [Diagnostics.Process]::Start($info)
$stdout = $process.StandardOutput.ReadToEndAsync()
$stderr = $process.StandardError.ReadToEndAsync()
if (!$process.WaitForExit(30000)) { $process.Kill(); throw 'ChargeSheet compiler timed out.' }
$stdout.Result | Write-Output
$exitCode = $process.ExitCode
$diagnostics = $stderr.Result
$process.Dispose()
if ($exitCode -ne 0 -or !(Test-Path -LiteralPath $binary)) {
    throw "ChargeSheet compiler failed ($exitCode): $diagnostics"
}
if ($diagnostics) { Write-Host $diagnostics }
Set-Content -LiteralPath $stamp -Value $signature
Copy-Item -LiteralPath $binary -Destination $packaged -Force
Write-Host "Compiled $Name.fx for game and preview."
