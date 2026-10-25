param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/fixed-texture-bake'
$stage = Join-Path $output 'package-save'
& "$PSScriptRoot/run.ps1" -Tml $Tml -Check
dotnet build "$repo/伊蕾娜.csproj" -c Debug '-t:Compile;CopyFilesToOutputDirectory' '-p:BuildProjectReferences=false' -v:q *> "$output/package-build.log"
if ($LASTEXITCODE -ne 0) { Get-Content "$output/package-build.log" -Tail 25; throw 'Mod compilation failed.' }
New-Item -ItemType Directory -Force -Path "$stage/Mods" | Out-Null
foreach ($dependency in @('KL', 'SilkyUIFramework')) {
    Copy-Item -LiteralPath "$repo/../../Mods/$dependency.tmod" -Destination "$stage/Mods" -Force
}
Push-Location -LiteralPath $Tml
try {
    dotnet tModLoader.dll -server -build $repo -eac "$repo/bin/Debug/net8.0/伊蕾娜.dll" -tmlsavedirectory $stage *> "$output/package.log"
    if ($LASTEXITCODE -ne 0) { Get-Content "$output/package.log" -Tail 25; throw 'Isolated package build failed.' }
} finally { Pop-Location }
python "$PSScriptRoot/check_package.py" "$stage/Mods/伊蕾娜.tmod"
if ($LASTEXITCODE -ne 0) { throw 'Baked texture package check failed.' }
