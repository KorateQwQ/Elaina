param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/alchemy-chest-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
dotnet build "$repo/伊蕾娜.csproj" -c Debug '-t:Compile;CopyFilesToOutputDirectory' '-p:BuildProjectReferences=false' -v:q *> "$output/build.log"
if ($LASTEXITCODE -ne 0) { Get-Content "$output/build.log" -Tail 30; throw 'Mod compilation failed.' }
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$items = $project.SelectSingleNode('/Project/ItemGroup')
function Add-Item([string]$kind, [string]$path) {
    $node = $project.CreateElement($kind)
    $node.SetAttribute('Include', $path)
    $null = $items.AppendChild($node)
    return $node
}
foreach ($path in @("$Tml/tModLoader.dll", "$repo/../KL/bin/Debug/net8.0/KL.dll", "$repo/bin/Debug/net8.0/伊蕾娜.dll", "$repo/../SilkyUIFramework/bin/Debug/net8.0/SilkyUIFramework.dll")) {
    $null = Add-Item 'Reference' $path
}
$reference = Add-Item 'Reference' "$Tml/Libraries/**/*.dll"
$reference.SetAttribute('Exclude', "$Tml/Libraries/Native/**;$Tml/Libraries/**/runtime*/**;$Tml/Libraries/**/*.resources.dll;$Tml/Libraries/tModCodeAssist/**")
$null = Add-Item 'Compile' "$PSScriptRoot/Checks.cs.txt"
$projectPath = Join-Path $output 'Checks.csproj'
$project.Save($projectPath)
dotnet run --project $projectPath -c Release --verbosity quiet *> "$output/checks.log"
Get-Content "$output/checks.log" -Tail 22
if ($LASTEXITCODE -ne 0) { throw 'Alchemy chest checks failed.' }
