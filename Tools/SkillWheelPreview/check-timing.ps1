param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/skillwheel-timing'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(
    @('Reference', "$Tml/Libraries/FNA/1.0.0/FNA.dll"),
    @('Reference', "$Tml/Libraries/ReLogic/1.0.0/ReLogic.dll"),
    @('Compile', "$PSScriptRoot/TimingChecks.cs.txt"),
    @('Compile', "$PSScriptRoot/DollTimingChecks.cs.txt"),
    @('Compile', "$PSScriptRoot/TimingStubs.cs.txt"),
    @('Compile', "$repo/ElainaModSkills/ElainaSkillUI/ElainaSkillWheel.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollCommandWheel.cs"),
    @('Compile', "$repo/ElainaModSkills/Skills/DollControl/DollCommand.cs"),
    @('Compile', "$repo/ElainaModSkills/ElainaSkillUI/SkillWheelDrawing.cs"),
    @('Compile', "$repo/ElainaModSkills/ElainaSkillUI/SkillBarDrawing.cs"),
    @('Compile', "$repo/ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/ConstellationUIClock.cs"))) {
    $node = $project.CreateElement($entry[0])
    $node.SetAttribute('Include', $entry[1])
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$path = Join-Path $output 'Checks.csproj'
$project.Save($path)
dotnet run --project $path --configuration Release --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Skill wheel timing checks failed.' }
