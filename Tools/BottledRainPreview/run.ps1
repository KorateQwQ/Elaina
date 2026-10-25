param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/bottled-rain-preview'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$entries = @(
    @('Reference', "$Tml/Libraries/FNA/1.0.0/FNA.dll"),
    @('Compile', "$repo/ElainaModAlchemy/Rain/RainCloudDrawing.cs"),
    @('Compile', "$PSScriptRoot/Preview.cs.txt"),
    @('None', "$Tml/Libraries/Native/Windows/*.dll")
)
foreach ($entry in $entries) {
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
dotnet run --project $projectPath --configuration Release --verbosity quiet -- $output $content
if ($LASTEXITCODE -ne 0) { throw 'Bottled Rain FNA preview failed.' }
python (Join-Path $PSScriptRoot 'compose.py') $output
if ($LASTEXITCODE -ne 0) { throw 'Rain animation composition failed.' }
