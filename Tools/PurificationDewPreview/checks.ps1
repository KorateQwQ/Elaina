param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$Benchmark)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/purification-dew/checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
foreach ($source in @("$repo/伊蕾娜.csproj")) {
    dotnet build $source -c Debug '-t:Compile;CopyFilesToOutputDirectory' '-p:BuildProjectReferences=false' -v:q *> "$output/build.log"
    if ($LASTEXITCODE -ne 0) { Get-Content "$output/build.log" -Tail 25; throw 'Compilation failed.' }
}
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$items = $project.SelectSingleNode('/Project/ItemGroup')
function Add-CheckItem([string]$kind, [string]$path) {
    $node = $project.CreateElement($kind)
    $node.SetAttribute('Include', $path)
    $null = $items.AppendChild($node)
    return $node
}
foreach ($path in @("$Tml/tModLoader.dll", "$repo/../KL/bin/Debug/net8.0/KL.dll", "$repo/bin/Debug/net8.0/伊蕾娜.dll", "$repo/../SilkyUIFramework/bin/Debug/net8.0/SilkyUIFramework.dll")) {
    $null = Add-CheckItem 'Reference' $path
}
$reference = Add-CheckItem 'Reference' "$Tml/Libraries/**/*.dll"
$reference.SetAttribute('Exclude', "$Tml/Libraries/Native/**;$Tml/Libraries/**/runtime*/**;$Tml/Libraries/**/*.resources.dll;$Tml/Libraries/tModCodeAssist/**")
$check = Add-CheckItem 'Compile' "$PSScriptRoot/Checks.cs.txt"
$check.SetAttribute('Link', 'Checks.cs')
$null = Add-CheckItem 'Compile' "$PSScriptRoot/TerrainBenchmark.cs.txt"
$null = Add-CheckItem 'Compile' "$PSScriptRoot/MapChecks.cs.txt"
$null = Add-CheckItem 'Compile' "$PSScriptRoot/ProtectionChecks.cs.txt"
$projectPath = Join-Path $output 'Checks.csproj'
$project.Save($projectPath)
$arguments = @($repo)
if ($Benchmark) { $arguments += '--benchmark' }
dotnet run --project $projectPath --configuration Release --verbosity quiet -- @arguments *> "$output/checks.log"
if ($LASTEXITCODE -ne 0) { Get-Content "$output/checks.log" -Tail 20; throw 'Purification Dew checks failed.' }
Get-Content "$output/checks.log" -Tail 65

