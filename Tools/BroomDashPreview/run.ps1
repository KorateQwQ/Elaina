param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/broom-dash-preview'
New-Item -ItemType Directory -Force -Path $output | Out-Null
python "$PSScriptRoot/prepare.py" $repo $output
if ($LASTEXITCODE -ne 0) { throw 'Texture preparation failed.' }
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(
    @('Reference', "$Tml/Libraries/FNA/1.0.0/FNA.dll"),
    @('Compile', "$repo/Mounts/BroomDashWindRenderer.cs"),
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
$projectPath = Join-Path $output 'Preview.csproj'; $project.Save($projectPath)
dotnet run --project $projectPath --configuration Release --verbosity quiet -- $output $repo
if ($LASTEXITCODE -ne 0) { throw 'Broom dash FNA preview failed.' }
python "$PSScriptRoot/compose.py" $output
if ($LASTEXITCODE -ne 0) { throw 'Preview composition failed.' }
