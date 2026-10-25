param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader', [string]$CompilerDirectory = '', [switch]$Benchmark)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/bottled-rain-shader'
# Keep gameplay and the existing sprite comparison preview untouched while rebuilding the visual.
$protected = @("$repo/ElainaModAlchemy/Rain/RainPlantGrowth.cs", "$repo/Tools/BottledRainPreview/Preview.cs.txt")
$before = $protected | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash }
& (Join-Path $PSScriptRoot 'compile.ps1') -CompilerDirectory $CompilerDirectory
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
$entries = @(
    @('Reference', "$Tml/Libraries/FNA/1.0.0/FNA.dll"),
    @('Compile', "$repo/ElainaModAlchemy/Rain/RainCloudDrawing.cs"),
    @('Compile', "$repo/ElainaModAlchemy/Rain/RainPlantIndicatorDrawing.cs"),
    @('Compile', "$repo/ElainaModAlchemy/Rain/RainCloudShaderRenderer.cs"),
    @('Compile', "$PSScriptRoot/Preview.cs.txt"),
    @('Compile', "$PSScriptRoot/Benchmark.cs.txt"),
    @('Compile', "$repo/ElainaModAlchemy/Rain/RainCloudBakedData.cs"),
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
$arguments = @($output, $content, "$repo/Effects/Content")
if ($Benchmark) { $arguments += '--benchmark' }
dotnet run --project $projectPath --configuration Release --verbosity quiet -- @arguments
if ($LASTEXITCODE -ne 0) { throw 'Cloud shader FNA preview failed.' }
if (!$Benchmark) {
    python (Join-Path $PSScriptRoot 'compose.py') $output
    if ($LASTEXITCODE -ne 0) { throw 'Cloud shader animation composition failed.' }
}
$after = $protected | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash }
if (Compare-Object $before $after) { throw 'Rain gameplay or original sprite preview changed during rendering.' }
Write-Output 'PASS: rain gameplay and original sprite preview are unchanged (SHA-256).'
