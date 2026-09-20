param([string]$Tml = 'D:\Steam\steamapps\common\tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$preview = Join-Path $repo '.vissandbox\font-integration\preview'
New-Item -ItemType Directory -Path $preview -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Preview.cs.txt') -Destination (Join-Path $preview 'Program.cs')
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup/></Project>'
foreach ($entry in @(
    @('Reference', (Join-Path $Tml 'Libraries\FNA\1.0.0\FNA.dll')),
    @('Reference', (Join-Path $Tml 'Libraries\ReLogic\1.0.0\ReLogic.dll')),
    @('None', (Join-Path $Tml 'Libraries\Native\Windows\*.dll')))) {
    $node = $project.CreateElement($entry[0])
    $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') {
        $node.SetAttribute('Link', '%(Filename)%(Extension)')
        $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$project.Save((Join-Path $preview 'Preview.csproj'))
dotnet run --project (Join-Path $preview 'Preview.csproj') -- (Join-Path $repo '..\KL\Fonts') (Join-Path $preview 'output')
if ($LASTEXITCODE -ne 0) { throw 'Font FNA preview failed.' }
