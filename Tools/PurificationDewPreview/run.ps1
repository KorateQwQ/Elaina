param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [switch]$Benchmark)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/purification-dew'
& "$PSScriptRoot/compile.ps1"
# Extract the existing native KL bloom replay compiler, which reads current KL methods via Roslyn.
$probe = [IO.File]::ReadAllText("$repo/Tools/BloomBackbufferPreview/Program.cs.txt")
$start = $probe.IndexOf('    internal static Assembly CompilePipeline')
$end = $probe.IndexOf('internal sealed class Probe')
if ($start -lt 0 -or $end -le $start) { throw 'KL bloom compiler source boundary not found.' }
$preamble = @'
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
internal static class DewBloomCompiler {
'@
[IO.File]::WriteAllText("$output/BloomCompiler.cs", $preamble + "`n" + $probe.Substring($start, $end - $start))
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(
    @('Reference', "$Tml/Libraries/FNA/1.0.0/FNA.dll"),
    @('Compile', "$repo/ElainaModAlchemy/Purification/PurificationDewRenderer.cs"),
    @('Compile', "$repo/ElainaModAlchemy/Purification/PurificationDewVisualSettings.cs"),
    @('Compile', "$PSScriptRoot/Preview.cs.txt"),
    @('Compile', "$PSScriptRoot/Benchmark.cs.txt"),
    @('Compile', "$PSScriptRoot/BloomReplay.cs.txt"),
    @('Compile', "$output/BloomCompiler.cs"),
    @('None', "$Tml/Libraries/Native/Windows/*.dll")
)) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') {
        $node.SetAttribute('Link', '%(Filename)%(Extension)')
        $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
# Use the same installed SDK Roslyn compiler as the existing bloom replay tool.
foreach ($name in @('Microsoft.CodeAnalysis', 'Microsoft.CodeAnalysis.CSharp')) {
    $node = $project.CreateElement('Reference'); $node.SetAttribute('Include', $name)
    $node.SetAttribute('HintPath', ('$(MSBuildSDKsPath)/../Roslyn/bincore/' + $name + '.dll'))
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$project.Save("$output/Preview.csproj")
$content = Join-Path (Split-Path $Tml -Parent) 'Terraria/Content'
$arguments = @($output, $content, "$repo/Effects/Content")
if ($Benchmark) { $arguments += '--benchmark' }
dotnet run --project "$output/Preview.csproj" -c Release --verbosity quiet -- @arguments
if ($LASTEXITCODE -ne 0) { throw 'Purification Dew FNA preview failed.' }
if ($Benchmark) { return }
python "$PSScriptRoot/compose.py" $output
if ($LASTEXITCODE -ne 0) { throw 'Purification Dew frame composition failed.' }
