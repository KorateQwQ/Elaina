param(
    [string]$Generator = 'D:\泰拉原版贴图与代码与其他工具\FontBuilder\DynamicFontGenerator\DynamicFontGenerator.exe',
    [string]$HarmonyFont = 'D:\泰拉原版贴图与代码与其他工具\FontBuilder\HarmonyOS Sans\HarmonyOS_Sans_SC\HarmonyOS_Sans_SC_Regular.ttf',
    [string]$NotoFont = '',
    [string]$GelasioFont = ''
)
# Bakes the KL UI fonts at the exact pixel sizes the constellation panel draws at 1920x1080 / 100%,
# 2560x1440 / 150% and 3840x2160 / 200%, in zh-Hans and en-US (run.ps1 -FontSizesOnly [-Culture en-US]).
# Other sizes use the 48px master. The Noto Serif master itself is also rebaked with only the needed characters.
$ErrorActionPreference = 'Stop'
$sizes = [ordered]@{
    'HarmonyOS_Sans_SC' = @(9..28)
    'NotoSerifSC' = @(9) + 11..18 + 20..26 + 28
    'Gelasio' = @(8..18) + 20 + 22..26
}
# Characters per family (describe.py): body text may name vanilla items; serif size copies only show the mod's text.
$charsets = @{ 'HarmonyOS_Sans_SC' = 'items'; 'NotoSerifSC' = 'mod'; 'Gelasio' = 'mod' }
# Trimmed 48px masters: the full description stays as the source; a missing character shows the default character.
$masters = @{ 'NotoSerifSC' = 'items' }
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$fontDir = (Resolve-Path (Join-Path $repo '..\KL\Fonts')).Path
if (-not $NotoFont) { $NotoFont = Join-Path $repo '.vissandbox\font-integration\regular\NotoSerifSC-Regular.ttf' }
if (-not $GelasioFont) { $GelasioFont = Join-Path $repo '.vissandbox\gelasio-build\KLGelasio.ttf' }
foreach ($file in @($HarmonyFont, $NotoFont, $GelasioFont, $Generator)) {
    if (-not (Test-Path -LiteralPath $file)) { throw "Missing input: $file (see Tools/FontTiers/README.md)" }
}
$scratch = Join-Path $repo '.vissandbox\font-tiers'
if (Test-Path -LiteralPath $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force }
New-Item -ItemType Directory -Path $scratch -Force | Out-Null

$describe = Join-Path $PSScriptRoot 'describe.py'
foreach ($family in $sizes.Keys) {
    & python -I $describe (Join-Path $fontDir "$family.dynamicfont") $scratch $family $charsets[$family] @($sizes[$family])
    if ($LASTEXITCODE -ne 0) { throw "Describing $family failed." }
}
foreach ($family in $masters.Keys) {
    & python -I $describe (Join-Path $fontDir "$family.dynamicfont") $scratch "${family}_master" $masters[$family] 48
    if ($LASTEXITCODE -ne 0) { throw "Describing the $family master failed." }
}

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $csc /nologo /platform:x86 "/out:$scratch\GenerateHost.exe" (Join-Path $PSScriptRoot 'GenerateHost.cs.txt')
if ($LASTEXITCODE -ne 0) { throw 'Generator host compilation failed.' }
Push-Location $scratch
try {
    & (Join-Path $scratch 'GenerateHost.exe') $Generator $HarmonyFont $NotoFont $GelasioFont
    if ($LASTEXITCODE -ne 0) { throw 'DynamicFontGenerator failed.' }
} finally { Pop-Location }

# Replace the previous set so sizes dropped from the table do not linger in the mod.
foreach ($family in $sizes.Keys) {
    Get-ChildItem -LiteralPath $fontDir -Filter "${family}_*.xnb" | Remove-Item
}
$total = 0
foreach ($family in $sizes.Keys) {
    foreach ($px in $sizes[$family]) {
        $xnb = Join-Path $scratch "${family}_$px.xnb"
        if (-not (Test-Path -LiteralPath $xnb)) { throw "Generator produced no $xnb" }
        Copy-Item -LiteralPath $xnb -Destination (Join-Path $fontDir "${family}_$px.xnb")
        $total += (Get-Item -LiteralPath $xnb).Length
    }
}
foreach ($family in $masters.Keys) {
    $xnb = Join-Path $scratch "${family}_master_48.xnb"
    if (-not (Test-Path -LiteralPath $xnb)) { throw "Generator produced no $xnb" }
    Copy-Item -LiteralPath $xnb -Destination (Join-Path $fontDir "$family.xnb")
    $total += (Get-Item -LiteralPath $xnb).Length
}
Write-Host ("Copied to {0}: {1:N1} MB" -f $fontDir, ($total / 1MB))
