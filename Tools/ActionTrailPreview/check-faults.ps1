param([string]$Dotnet = 'C:/Users/PC/.dotnet/dotnet.exe')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$work = Join-Path $root '.vissandbox/action-trail-preview'
New-Item -ItemType Directory -Force $work | Out-Null
$expected = @{
    alpha = 'Opacity was applied more than once'
    tangent = 'Nonfinite trail vertex'
    sampling = 'Render cadence changed'
    interruption = 'Interruption did not retain'
}
foreach ($fault in @('alpha', 'tangent', 'sampling', 'interruption')) {
    $log = Join-Path $work "fault-$fault.log"
    & powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot/run.ps1" -Dotnet $Dotnet -Fault $fault *> $log
    if ($LASTEXITCODE -eq 0 -or !(Select-String -LiteralPath $log -SimpleMatch $expected[$fault] -Quiet)) {
        Get-Content -LiteralPath $log -Tail 25
        throw "Regression did not reject the injected fault: $fault"
    }
    Write-Output "PASS: rejected $fault fault"
}
& "$PSScriptRoot/run.ps1" -Dotnet $Dotnet
