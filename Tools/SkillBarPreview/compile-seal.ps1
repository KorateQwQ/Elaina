param()
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$scratch = Join-Path $repo '.vissandbox/seal-shader'
New-Item -ItemType Directory -Force -Path (Join-Path $scratch 'source') | Out-Null
Get-ChildItem -LiteralPath (Join-Path $repo 'Effects') -File |
    Where-Object { $_.Name -eq 'XNBCompiler.exe' -or $_.Extension -eq '.dll' -or $_.Name -eq 'XNBCompiler.exe.config' } |
    Copy-Item -Destination $scratch
$compiled = Join-Path $scratch 'SkillSeal.xnb'
$cache = Join-Path $scratch 'shader.cache'
foreach ($file in @($compiled, $cache)) {
    if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
}
Copy-Item -LiteralPath (Join-Path $repo 'Effects/Content/SkillSeal.fx') -Destination (Join-Path $scratch 'source/SkillSeal.fx')
Push-Location $scratch
try { & './XNBCompiler.exe' 'source' } finally { Pop-Location }
# The bundled legacy compiler may fault during shutdown; require a newly produced artifact.
# Actual loading/rendering is checked separately by run.ps1's FNA host.
if (!(Test-Path -LiteralPath $compiled) -or (Get-Item -LiteralPath $compiled).Length -lt 100) {
    throw 'No compiled SkillSeal.xnb was produced.'
}
Copy-Item -LiteralPath $compiled -Destination (Join-Path $repo 'Effects/Content/SkillSeal.xnb')
