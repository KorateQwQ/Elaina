param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$modName = Split-Path $repo -Leaf
$output = Join-Path $repo '.vissandbox/broom-collision-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
dotnet build "$repo/$modName.csproj" -c Debug '-t:Compile;CopyFilesToOutputDirectory' '-p:BuildProjectReferences=false' -v:q *> "$output/build.log"
if ($LASTEXITCODE -ne 0) { Get-Content "$output/build.log" -Tail 30; throw 'Mod compilation failed.' }
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(
    @('Reference', "$Tml/tModLoader.dll"),
    @('Reference', "$Tml/Libraries/**/*.dll"),
    @('Reference', "$repo/bin/Debug/net8.0/$modName.dll"),
    @('Reference', "$repo/../KL/bin/Debug/net8.0/KL.dll"),
    @('Reference', "$repo/../SilkyUIFramework/bin/Debug/net8.0/SilkyUIFramework.dll"),
    @('Reference', "$repo/../Homura/bin/Debug/net8.0/Homura.dll"),
    @('Compile', "$PSScriptRoot/FlightStateChecks.cs.txt"),
    @('Compile', "$PSScriptRoot/Checks.cs.txt"))) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[1].EndsWith('**/*.dll')) {
        $node.SetAttribute('Exclude', "$Tml/Libraries/Native/**;$Tml/Libraries/**/runtime*/**;$Tml/Libraries/**/*.resources.dll;$Tml/Libraries/tModCodeAssist/**")
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$path = Join-Path $output 'Checks.csproj'
$project.Save($path)
dotnet run --project $path --configuration Release --verbosity quiet -- $repo
if ($LASTEXITCODE -ne 0) { throw 'Broom collision checks failed.' }
