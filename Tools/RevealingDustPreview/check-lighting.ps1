param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/revealing-light-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(
    @('Reference', "$Tml/tModLoader.dll"),
    @('Reference', "$Tml/Libraries/**/*.dll"),
    @('Compile', "$repo/ElainaModAlchemy/Revealing/*.cs"),
    @('Compile', "$repo/Tools/AlchemyItemChecks/RevealingDustChecks.cs.txt"),
    @('Compile', "$output/Program.cs")
)) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[1] -like '*Libraries/**') {
        $node.SetAttribute('Exclude', "$Tml/Libraries/Native/**;$Tml/Libraries/**/runtime*/**;$Tml/Libraries/**/*.resources.dll;$Tml/Libraries/tModCodeAssist/**")
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
@'
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Revealing;
Terraria.Program.SavePath = Path.GetTempPath();
var mod = new CheckMod();
var area = new RevealingDustArea();
((ILoadable)area).Load(mod);
ContentInstance.Register(area);
RevealingDustChecks.Run(mod);
sealed class CheckMod : Mod { public override string Name => "RevealingLightChecks"; }
'@ | Set-Content -LiteralPath (Join-Path $output 'Program.cs') -Encoding utf8
$projectPath = Join-Path $output 'Checks.csproj'
$project.Save($projectPath)
dotnet run --project $projectPath --configuration Release --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Revealing Dust lighting checks failed.' }
