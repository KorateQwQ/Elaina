param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$modName = Split-Path $repo -Leaf
$stage = Join-Path $repo '.vissandbox/bottled-rain-shader/staging'
$mods = Join-Path $stage 'Mods'
New-Item -ItemType Directory -Force -Path $mods | Out-Null
foreach ($dependency in @('KL', 'Homura')) {
    $source = Join-Path $repo "../../Mods/$dependency.tmod"
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $mods -Force }
}
dotnet msbuild (Join-Path $repo "$modName.csproj") '-t:Compile,CopyFilesToOutputDirectory' '-p:BuildProjectReferences=false' -v:quiet -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw 'Mod compilation failed.' }
# -build executes before -modpath is applied. Isolate the whole tML save root instead.
$arguments = @((Join-Path $Tml 'tModLoader.dll'), '-server', '-build', $repo,
    '-eac', (Join-Path $repo "bin/Debug/net8.0/$modName.dll"), '-tmlsavedirectory', $stage)
Push-Location $Tml
try {
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Staged mod packaging failed.' }
} finally { Pop-Location }
python (Join-Path $PSScriptRoot 'check-package.py') (Join-Path $mods "$modName.tmod")
if ($LASTEXITCODE -ne 0) { throw 'Staged package verification failed.' }
