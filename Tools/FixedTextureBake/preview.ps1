param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/fixed-texture-bake'
& "$PSScriptRoot/run.ps1" -Tml $Tml -Check
python "$PSScriptRoot/prepare_reticle.py" $repo $output
if ($LASTEXITCODE -ne 0) { throw 'Reticle replay extraction failed.' }
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(@('Reference', "$Tml/Libraries/FNA/1.0.0/FNA.dll"), @('Compile', "$PSScriptRoot/Preview.cs.txt"), @('Compile', "$output/ReticleReplay.cs"), @('None', "$Tml/Libraries/Native/Windows/*.dll"))) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') { $node.SetAttribute('Link', '%(Filename)%(Extension)'); $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest') }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$project.Save("$output/Preview.csproj")
dotnet run --project "$output/Preview.csproj" -c Release --verbosity quiet -- $repo $output
if ($LASTEXITCODE -ne 0) { throw 'Fixed texture FNA preview failed.' }
