param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$kl = (Resolve-Path -LiteralPath (Join-Path $repo '../KL')).Path
$output = Join-Path $repo '.vissandbox/tornado-layer-merge'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><Nullable>disable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$entries = @(
    @('Reference', (Join-Path $Tml 'Libraries/FNA/1.0.0/FNA.dll')),
    @('Compile', (Join-Path $kl 'Drawing/TornadoLayers.cs')),
    @('Compile', (Join-Path $PSScriptRoot 'Program.cs.txt')),
    @('None', (Join-Path $Tml 'Libraries/Native/Windows/*.dll'))
)
foreach ($entry in $entries) {
    $node = $project.CreateElement($entry[0])
    $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') {
        $node.SetAttribute('Link', '%(Filename)%(Extension)')
        $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$path = Join-Path $output 'Preview.csproj'
$project.Save($path)
dotnet run --project $path --verbosity quiet -- $kl $output
if ($LASTEXITCODE -ne 0) { throw 'Tornado layer merge preview failed.' }
