param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$GeometryOnly)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$modName = Split-Path $repo -Leaf
$output = Join-Path $repo '.vissandbox/water-laser-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
dotnet build "$repo/$modName.csproj" -c Debug '-t:Compile;CopyFilesToOutputDirectory' '-p:BuildProjectReferences=false' -v:q *> "$output/build.log"
if ($LASTEXITCODE -ne 0) { Get-Content "$output/build.log" -Tail 30; throw 'Mod compilation failed.' }
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
$check = Add-CheckItem 'Compile' "$PSScriptRoot/Checks.cs.txt"
$check.SetAttribute('Link', 'Checks.cs')
$native = Add-CheckItem 'None' "$Tml/Libraries/Native/Windows/*.dll"
$native.SetAttribute('Link', '%(Filename)%(Extension)')
$native.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
$path = Join-Path $output 'Checks.csproj'
$project.Save($path)
$mode = if ($GeometryOnly) { 'geometry' } else { 'all' }
dotnet run --project $path --configuration Release --verbosity quiet -- $repo "$output/out" $mode
if ($LASTEXITCODE -ne 0) { throw 'Water laser checks failed.' }
