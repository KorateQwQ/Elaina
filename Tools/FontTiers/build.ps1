param(
    [string]$Generator = 'D:\泰拉原版贴图与代码与其他工具\FontBuilder\DynamicFontGenerator\DynamicFontGenerator.exe',
    [string]$HarmonyFont = 'D:\泰拉原版贴图与代码与其他工具\FontBuilder\HarmonyOS Sans\HarmonyOS_Sans_SC\HarmonyOS_Sans_SC_Regular.ttf',
    [string]$NotoFont = '',
    [string]$GelasioFont = ''
)
# Retained for rebuilding the trimmed Noto Serif 48px master only.
# Small XNB tiers were retired; use Tools/ConstellationRasterChecks/run.ps1 -Label bake-fonts.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fontDir = (Resolve-Path (Join-Path $repo '../KL/Fonts')).Path
if (-not $NotoFont) { $NotoFont = Join-Path $repo '.vissandbox/font-integration/regular/NotoSerifSC-Regular.ttf' }
if (-not $GelasioFont) { $GelasioFont = Join-Path $repo '.vissandbox/gelasio-build/KLGelasio.ttf' }
foreach ($file in @($HarmonyFont, $NotoFont, $GelasioFont, $Generator)) {
    if (-not (Test-Path -LiteralPath $file)) { throw "Missing input: $file (see Tools/FontTiers/README.md)" }
}
# Use a fresh directory so old small-font descriptions can never be rebuilt accidentally.
$scratch = Join-Path $repo ('.vissandbox/font-master-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $scratch | Out-Null
& python -I (Join-Path $PSScriptRoot 'describe.py') (Join-Path $fontDir 'NotoSerifSC.dynamicfont') $scratch NotoSerifSC_master items 48
if ($LASTEXITCODE -ne 0) { throw 'Describing the Noto Serif master failed.' }
$csc = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
& $csc /nologo /platform:x86 "/out:$scratch/GenerateHost.exe" (Join-Path $PSScriptRoot 'GenerateHost.cs.txt')
if ($LASTEXITCODE -ne 0) { throw 'Generator host compilation failed.' }
Push-Location $scratch
try {
    & (Join-Path $scratch 'GenerateHost.exe') $Generator $HarmonyFont $NotoFont $GelasioFont
    if ($LASTEXITCODE -ne 0) { throw 'DynamicFontGenerator failed.' }
} finally { Pop-Location }
$xnb = Join-Path $scratch 'NotoSerifSC_master_48.xnb'
if (-not (Test-Path -LiteralPath $xnb)) { throw "Generator produced no $xnb" }
Copy-Item -LiteralPath $xnb -Destination (Join-Path $fontDir 'NotoSerifSC.xnb')
Write-Host 'Rebuilt NotoSerifSC.xnb (48px master); no small-font XNB copies generated.'
