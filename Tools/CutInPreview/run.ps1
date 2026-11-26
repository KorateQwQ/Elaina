param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
# Keep this file ASCII (PowerShell 5.1 reads it as ANSI).
# Links the production cut-in timeline and drawing into an FNA host and writes native-size PNGs.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/cut-in-preview'
New-Item -ItemType Directory -Force -Path $output | Out-Null

$globalUsings = @'
global using System.Collections.Generic;
global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
'@
Set-Content -LiteralPath (Join-Path $output 'GlobalUsings.cs') -Value $globalUsings -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Preview.cs.txt') -Destination (Join-Path $output 'Preview.cs')

$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><Nullable>disable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$cutIn = Get-ChildItem -LiteralPath (Join-Path $repo 'ElainaModSkills') -Directory | Where-Object Name -eq 'CutIn' | Select-Object -First 1
$entries = ,@('Reference', (Join-Path $Tml 'Libraries/FNA/1.0.0/FNA.dll'))
foreach ($source in @('ElainaCutIn.cs', 'ElainaCutInDrawing.cs')) { $entries += ,@('Compile', (Join-Path $cutIn.FullName $source)) }
$entries += ,@('Compile', (Join-Path $repo 'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/ConstellationUIClock.cs'))
$entries += ,@('Compile', (Join-Path $output 'GlobalUsings.cs'))
$entries += ,@('Compile', (Join-Path $output 'Preview.cs'))
$entries += ,@('None', (Join-Path $Tml 'Libraries/Native/Windows/*.dll'))
foreach ($entry in $entries) {
    $node = $project.CreateElement($entry[0])
    $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') {
        $node.SetAttribute('Link', '%(Filename)%(Extension)')
        $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$projectPath = Join-Path $output 'CutInPreview.csproj'
$project.Save($projectPath)
dotnet run --project $projectPath --verbosity quiet -- $repo $output
if ($LASTEXITCODE -ne 0) { throw 'Cut-in preview failed.' }
