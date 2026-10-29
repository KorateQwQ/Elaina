param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/bottled-rain-aim'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$entries = @(
    @('Reference', "$Tml/tModLoader.dll"),
    @('Reference', "$repo/../KL/bin/Debug/net8.0/KL.dll"),
    @('Reference', "$Tml/Libraries/**/*.dll"),
    @('Compile', "$repo/ElainaModAlchemy/Rain/*.cs"),
    @('Compile', "$repo/ElainaModAlchemy/item/Curios/BottledRain.cs"),
    @('Compile', "$repo/Tools/AlchemyItemChecks/BottledRainChecks.cs.txt"),
    @('Compile', "$PSScriptRoot/CheckHost.cs.txt")
)
foreach ($entry in $entries) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[1] -like '*Libraries/**/*.dll') {
        $node.SetAttribute('Exclude', "$Tml/Libraries/Native/**;$Tml/Libraries/**/runtime*/**;$Tml/Libraries/**/*.resources.dll;$Tml/Libraries/tModCodeAssist/**")
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$projectPath = Join-Path $output 'Checks.csproj'
$project.Save($projectPath)
dotnet run --project $projectPath --configuration Release --verbosity quiet -- $output
if ($LASTEXITCODE -ne 0) { throw 'Bottled Rain behavior checks failed.' }
