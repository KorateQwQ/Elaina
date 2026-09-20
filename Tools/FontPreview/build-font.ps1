param(
    [string]$Font = 'C:\Windows\Fonts\NotoSerifSC-VF.ttf',
    [string]$Generator = 'D:\泰拉原版贴图与代码与其他工具\FontBuilder\DynamicFontGenerator\DynamicFontGenerator.exe',
    [string]$Python = 'python'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$scratch = Join-Path $repo '.vissandbox\font-integration\regular'
$fontDir = Join-Path $repo '..\KL\Fonts'
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
& $Python (Join-Path $PSScriptRoot 'prepare.py') --font $Font --output $scratch
if ($LASTEXITCODE -ne 0) { throw 'Font preparation failed (requires fonttools).' }
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $csc /nologo /platform:x86 "/out:$scratch\Generate.exe" (Join-Path $PSScriptRoot 'Generate.cs.txt')
if ($LASTEXITCODE -ne 0) { throw 'Generator host compilation failed.' }
Push-Location $scratch
try {
    & (Join-Path $scratch 'Generate.exe') $Generator (Join-Path $scratch 'NotoSerifSC-Regular.ttf')
    if ($LASTEXITCODE -ne 0) { throw 'DynamicFontGenerator failed.' }
} finally { Pop-Location }
Copy-Item -LiteralPath (Join-Path $scratch 'NotoSerifSC.xnb') -Destination (Join-Path $fontDir 'NotoSerifSC.xnb')
Copy-Item -LiteralPath (Join-Path $scratch 'NotoSerifSC.dynamicfont') -Destination (Join-Path $fontDir 'NotoSerifSC.dynamicfont')
Copy-Item -LiteralPath (Join-Path $scratch 'source.json') -Destination (Join-Path $fontDir 'NotoSerifSC.source.json')
