param([string]$Tml = 'D:/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo '.vissandbox/doll-selection'
New-Item -ItemType Directory -Force -Path $output | Out-Null
# Refresh the real world-selection renderer for a furniture-only FNA scene.
$drawing = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'ElainaModSkills/Skills/DollControl/DollMarkSystem.cs')
$drawStart = $drawing.IndexOf('    private static void DrawMarks(')
$drawEnd = $drawing.IndexOf('    private readonly Dictionary', $drawStart)
$cellStart = $drawing.IndexOf('    private static Rectangle Cell(')
$cellEnd = $drawing.IndexOf('    public override void ModifyInterfaceLayers', $cellStart)
"using Terraria.GameContent;`nnamespace 伊蕾娜.ElainaModSkills.Skills.DollControl;`ninternal static class FurnitureMarkPreview {`n" +
    $drawing.Substring($drawStart, $drawEnd - $drawStart).Replace('private static void DrawMarks', 'internal static void DrawMarks') +
    $drawing.Substring($cellStart, $cellEnd - $cellStart) + "`n}" |
    Set-Content -LiteralPath (Join-Path $output 'FurnitureMarkPreview.cs') -Encoding utf8
# Refresh actual input/selection and character persistence every run. WorldGen/network behavior is outside this harness.
$source = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repo 'ElainaModSkills/Skills/DollControl/DollCommandPlayer.cs')
$end = $source.IndexOf('    #region 挖掘')
if ($end -lt 0) { throw 'Could not locate the end of the production selection code.' }
$saveStart = $source.IndexOf('    public override void SaveData(')
$saveEnd = $source.IndexOf('    #endregion', $saveStart)
if ($saveStart -lt 0 -or $saveEnd -lt 0) { throw 'Could not locate production persistence methods.' }
$source.Substring(0, $end) + $source.Substring($saveStart, $saveEnd - $saveStart) + "`n    public Item BestHammer() => Player.Hammer;`n    private bool InRange(Point p) => Vector2.DistanceSquared(Player.Center, p.ToWorldCoordinates()) <= WorkRange * WorkRange;`n}" |
    Set-Content -LiteralPath (Join-Path $output 'SelectionPlayer.cs') -Encoding utf8
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup /></Project>'
foreach ($entry in @(
    @('Reference', (Join-Path $Tml 'Libraries/FNA/1.0.0/FNA.dll')),
    @('Reference', (Join-Path $Tml 'Libraries/ReLogic/1.0.0/ReLogic.dll')),
    @('Reference', (Join-Path $Tml 'Libraries/hjson/3.0.0/lib/netstandard1.3/Hjson.dll')),
    @('Compile', (Join-Path $output 'SelectionPlayer.cs')),
    @('Compile', (Join-Path $output 'FurnitureMarkPreview.cs')),
    @('Compile', (Join-Path $repo 'ElainaModSkills/Skills/DollControl/DollCommand.cs')),
    @('Compile', (Join-Path $repo 'ElainaModSkills/Skills/DollControl/DollMiningRules.cs')),
    @('Compile', (Join-Path $repo 'ElainaModSkills/Skills/DollControl/DollTileSelection.cs')),
    @('Compile', (Join-Path $repo 'ElainaModSkills/Skills/DollControl/DollSelectionBarDrawing.cs')),
    @('Compile', (Join-Path $repo 'ElainaModSkills/Skills/DollControl/DollWorkAssignments.cs')),
    @('Compile', (Join-Path $PSScriptRoot 'SelectionStubs.cs.txt')),
    @('Compile', (Join-Path $PSScriptRoot 'SelectionChecks.cs.txt')),
    @('Compile', (Join-Path $PSScriptRoot 'SelectionPreview.cs.txt')),
    @('Compile', (Join-Path $repo 'Tools/LocalizationChecks/PreviewLocalization.cs.txt')),
    @('None', (Join-Path $Tml 'Libraries/Native/Windows/*.dll')))) {
    $node = $project.CreateElement($entry[0]); $node.SetAttribute('Include', $entry[1])
    if ($entry[0] -eq 'None') {
        $node.SetAttribute('Link', '%(Filename)%(Extension)')
        $node.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
    }
    [void]$project.Project.SelectSingleNode('ItemGroup').AppendChild($node)
}
$projectPath = Join-Path $output 'Preview.csproj'
$project.Save($projectPath)
dotnet run --project $projectPath --verbosity quiet -- $repo $output
if ($LASTEXITCODE -ne 0) { throw 'Doll selection checks/preview failed.' }
