param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/purification-dew'
$stage = Join-Path $output 'package-save'
& "$PSScriptRoot/compile.ps1"
dotnet build "$repo/伊蕾娜.csproj" -c Debug '-t:Compile;CopyFilesToOutputDirectory' '-p:BuildProjectReferences=false' -v:q *> "$output/package-compile.log"
if ($LASTEXITCODE -ne 0) { Get-Content "$output/package-compile.log" -Tail 25; throw 'Mod compilation failed.' }
New-Item -ItemType Directory -Force -Path "$stage/Mods" | Out-Null
foreach ($dependency in @('KL', 'SilkyUIFramework')) {
    Copy-Item -LiteralPath "$repo/../../Mods/$dependency.tmod" -Destination "$stage/Mods" -Force
}
Push-Location -LiteralPath $Tml
try {
    dotnet tModLoader.dll -server -build $repo -eac "$repo/bin/Debug/net8.0/伊蕾娜.dll" -tmlsavedirectory $stage *> "$output/staged-build.log"
    if ($LASTEXITCODE -ne 0) { Get-Content "$output/staged-build.log" -Tail 25; throw 'Isolated package build failed.' }
} finally { Pop-Location }
python "$PSScriptRoot/check-package.py" "$stage/Mods/伊蕾娜.tmod"
if ($LASTEXITCODE -ne 0) { throw 'Package verification failed.' }
