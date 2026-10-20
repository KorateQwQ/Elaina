param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [string]$TerrariaContent)
$ErrorActionPreference = 'Stop'
if (!$TerrariaContent) { $TerrariaContent = Join-Path (Split-Path $Tml -Parent) 'Terraria/Content' }
if (!(Test-Path -LiteralPath (Join-Path $TerrariaContent 'Images/MagicPixel.xnb'))) {
    throw 'Specify -TerrariaContent containing Images/MagicPixel.xnb.'
}
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/frost-veil-preview'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$entries = @(
    @('Reference', (Join-Path $Tml 'Libraries/FNA/1.0.0/FNA.dll')),
    @('Compile', (Join-Path $repo 'ElainaModSkills/Skills/Ice/FrostVeilDrawing.cs')),
    @('Compile', (Join-Path $PSScriptRoot 'Preview.cs.txt')),
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
dotnet run --project $path --verbosity quiet -- $output $TerrariaContent
if ($LASTEXITCODE -ne 0) { throw 'Frost veil preview failed.' }
