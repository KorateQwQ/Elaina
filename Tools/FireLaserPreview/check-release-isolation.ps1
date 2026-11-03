param([string]$Dotnet = 'C:/Users/PC/.dotnet/dotnet.exe')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$work = Join-Path $repo '.vissandbox/fire-laser-preview'
$baseline = Join-Path $work 'isolation-baseline'
$variant = Join-Path $work 'isolation-variant'
$savedVariant = $env:FIRE_RELEASE_VARIANT
$savedFrames = $env:FIRE_FRAMES
$savedTransitions = $env:FIRE_TRANSITIONS
try {
    $env:FIRE_RELEASE_VARIANT = '0'
    $env:FIRE_FRAMES = '0'
    $env:FIRE_TRANSITIONS = '0'
    & (Join-Path $PSScriptRoot 'run.ps1') -Dotnet $Dotnet -SkipShader
    New-Item -ItemType Directory -Force -Path $baseline | Out-Null
    $files = @('entry-00.png', 'entry-02.png', 'entry-05.png', 'entry-07.png', 'entry-10.png', 'entry-14.png', 'entry-18.png', 'flow-60.png', 'exit-12.png')
    foreach ($file in $files) { Copy-Item -LiteralPath (Join-Path $work "out/$file") -Destination (Join-Path $baseline $file) }
    $env:FIRE_RELEASE_VARIANT = '1'
    & $Dotnet run --project (Join-Path $work 'Preview.csproj') -- $repo $variant
    if ($LASTEXITCODE -ne 0) { throw 'Variant preview failed.' }
    foreach ($file in $files | Where-Object { $_ -ne 'exit-12.png' }) {
        if ((Get-FileHash -LiteralPath (Join-Path $baseline $file)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $variant $file)).Hash) {
            throw "Release parameters changed opening/sustained output: $file"
        }
    }
    if ((Get-FileHash -LiteralPath (Join-Path $baseline 'exit-12.png')).Hash -eq (Get-FileHash -LiteralPath (Join-Path $variant 'exit-12.png')).Hash) {
        throw 'Variant did not change release output; comparison is inconclusive.'
    }
    Write-Output 'PASS: 7 opening frames and 1 sustained frame byte-identical under changed release parameters; release frame differs.'
}
finally {
    $env:FIRE_RELEASE_VARIANT = $savedVariant
    $env:FIRE_FRAMES = $savedFrames
    $env:FIRE_TRANSITIONS = $savedTransitions
}
