param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$Check)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/fixed-texture-bake'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(@('Reference', "$Tml/Libraries/FNA/1.0.0/FNA.dll"), @('Compile', "$PSScriptRoot/Bake.cs.txt"), @('Compile', "$PSScriptRoot/OriginalFixedTextures.cs.txt"))) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$project.Save("$output/Bake.csproj")
dotnet run --project "$output/Bake.csproj" -c Release --verbosity quiet -- $output
if ($LASTEXITCODE -ne 0) { throw 'Fixed texture pixel export failed.' }
$arguments = @($repo, $output)
if ($Check) { $arguments += '--check' }
python "$PSScriptRoot/write_png.py" @arguments
if ($LASTEXITCODE -ne 0) { throw 'Fixed texture PNG generation/verification failed.' }
