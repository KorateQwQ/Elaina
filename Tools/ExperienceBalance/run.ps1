param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/experience-balance'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$items = $project.SelectSingleNode('/Project/ItemGroup')
function Add-BalanceItem([string]$kind, [string]$path) {
    $node = $project.CreateElement($kind); $node.SetAttribute('Include', $path)
    $null = $items.AppendChild($node)
    return $node
}
foreach ($path in @("$Tml/tModLoader.dll")) {
    $null = Add-BalanceItem 'Reference' $path
}
$reference = Add-BalanceItem 'Reference' "$Tml/Libraries/**/*.dll"
$reference.SetAttribute('Exclude', "$Tml/Libraries/Native/**;$Tml/Libraries/**/runtime*/**;$Tml/Libraries/**/*.resources.dll;$Tml/Libraries/tModCodeAssist/**")
foreach ($path in @("$repo/System/Experience/ExperienceRules.cs", "$repo/System/Experience/ExperienceNpcRules.cs", "$PSScriptRoot/Probe.cs.txt")) {
    $null = Add-BalanceItem 'Compile' $path
}
$projectPath = Join-Path $output 'Probe.csproj'
$project.Save($projectPath)
dotnet run --project $projectPath --configuration Release --verbosity quiet -- $PSScriptRoot
if ($LASTEXITCODE -ne 0) { throw 'Experience balance probe failed.' }
