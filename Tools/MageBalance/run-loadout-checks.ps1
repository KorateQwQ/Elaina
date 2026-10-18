param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$modName = Split-Path $repo -Leaf
$output = Join-Path $repo '.vissandbox/loadout-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
if (-not $SkipBuild) {
    dotnet build "$repo/$modName.csproj" -c Debug '-t:Compile;CopyFilesToOutputDirectory' '-p:BuildProjectReferences=false' -v:q *> "$output/build.log"
    if ($LASTEXITCODE -ne 0) { Get-Content "$output/build.log" -Tail 30; throw 'Mod compilation failed.' }
}
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$items = $project.SelectSingleNode('/Project/ItemGroup')
function Add-CheckItem([string]$kind, [string]$path) {
    $node = $project.CreateElement($kind)
    $node.SetAttribute('Include', $path)
    $null = $items.AppendChild($node)
    return $node
}
foreach ($path in @("$Tml/tModLoader.dll", "$repo/../KL/bin/Debug/net8.0/KL.dll", "$repo/bin/Debug/net8.0/$modName.dll", "$repo/../SilkyUIFramework/bin/Debug/net8.0/SilkyUIFramework.dll")) {
    $null = Add-CheckItem 'Reference' $path
}
$reference = Add-CheckItem 'Reference' "$Tml/Libraries/**/*.dll"
$reference.SetAttribute('Exclude', "$Tml/Libraries/Native/**;$Tml/Libraries/**/runtime*/**;$Tml/Libraries/**/*.resources.dll;$Tml/Libraries/tModCodeAssist/**")
$check = Add-CheckItem 'Compile' "$PSScriptRoot/LoadoutChecks.cs.txt"
$check.SetAttribute('Link', 'Checks.cs')
$projectPath = Join-Path $output 'Checks.csproj'
$project.Save($projectPath)
$fixture = Join-Path $output 'browser-loadouts.json'
node (Join-Path $PSScriptRoot 'export-loadout-fixture.cjs') $fixture
if ($LASTEXITCODE -ne 0) { throw 'Browser loadout fixture export failed.' }
dotnet run --project $projectPath --configuration Release --verbosity quiet -- (Join-Path $repo 'ElainaModSkills/Data/SkillBalance.json') $fixture
if ($LASTEXITCODE -ne 0) { throw 'Loadout integration checks failed.' }
