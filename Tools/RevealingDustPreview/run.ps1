param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
python (Join-Path $PSScriptRoot 'bake-glow.py') --check
if ($LASTEXITCODE -ne 0) { throw 'Revealing Dust glow asset is stale.' }
$output = Join-Path $repo '.vissandbox/revealing-dust-preview'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(
    @('Reference', "$Tml/Libraries/FNA/1.0.0/FNA.dll"),
    @('Compile', "$repo/ElainaModAlchemy/Revealing/RevealingDustDrawing.cs"),
    @('Compile', "$PSScriptRoot/Preview.cs.txt"),
    @('None', "$Tml/Libraries/Native/Windows/*.dll")
)) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') {
        $node.SetAttribute('Link', '%(Filename)%(Extension)')
        $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$projectPath = Join-Path $output 'Preview.csproj'
$project.Save($projectPath)
$content = Join-Path (Split-Path $Tml -Parent) 'Terraria/Content'
dotnet run --project $projectPath --configuration Release --verbosity quiet -- $output $content $repo
if ($LASTEXITCODE -ne 0) { throw 'Revealing Dust FNA preview failed.' }
python (Join-Path $PSScriptRoot 'compose.py') $output
if ($LASTEXITCODE -ne 0) { throw 'Revealing Dust animation composition failed.' }
