param(
    [ValidateSet('zh-Hans','en-US')][string]$Culture = 'zh-Hans',
    [string]$Tml = 'D:/Steam/steamapps/common/tModLoader',
    [switch]$CompileOnly,
    [switch]$BookOnly,
    [switch]$QuillOnly,
    [switch]$ScrollOnly,
    [switch]$StarPowerOnly,
    [switch]$ExportMaterials,
    [string]$Scene = '',
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug',
    [string]$Python = 'python'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
# Validate the actual pixel outline assets used by this renderer.
if (!$CompileOnly -and !$ExportMaterials) {
    & $Python (Join-Path $PSScriptRoot 'bake_redesign.py') --check
    if ($LASTEXITCODE -ne 0) { throw 'Notebook redesign assets are stale. Export materials and run bake_redesign.py.' }
}
$out = Join-Path $repo '.vissandbox/alchemy-preview'
$ui = Join-Path $repo 'ElainaModAlchemy/UI'
New-Item -ItemType Directory -Force -Path $out | Out-Null
# Read vanilla IDs from the installed game and the production resolver, without
# initializing Terraria or copying an independent item-ID table into the preview.
$gameAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $Tml 'tModLoader.dll'))
$itemIdType = $gameAssembly.GetType('Terraria.ID.ItemID')
$resolverText = [IO.File]::ReadAllText((Join-Path $repo 'ElainaModAlchemy/item/AlchemyCatalogItemTypes.cs'))
$vanillaIds = @{}
foreach ($match in [regex]::Matches($resolverText, '"([^"\r\n]+)"\s*=>\s*ItemID\.(\w+)')) {
    $vanillaIds[$match.Groups[1].Value] = $itemIdType.GetField($match.Groups[2].Value).GetRawConstantValue()
}
[IO.File]::WriteAllText((Join-Path $out 'vanilla-item-ids.json'), ($vanillaIds | ConvertTo-Json))
foreach ($name in @('Program','Stubs','BookPreview','QuillPreview','ScrollPreview','NotebookFixture')) {
    [IO.File]::WriteAllText((Join-Path $out "$name.cs"), [IO.File]::ReadAllText((Join-Path $PSScriptRoot "$name.cs.txt")))
}
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NoWarn>0436</NoWarn></PropertyGroup><ItemGroup /></Project>'
$items = $project.SelectSingleNode('/Project/ItemGroup')
function Add-PreviewItem([string]$kind, [string]$path) {
    $node = $project.CreateElement($kind)
    $node.SetAttribute('Include', $path)
    [void]$items.AppendChild($node)
    return $node
}
$null = Add-PreviewItem 'Compile' "$repo/Localization/UIText.cs"
$null = Add-PreviewItem 'Compile' "$repo/Tools/LocalizationChecks/PreviewLocalization.cs.txt"
$null = Add-PreviewItem 'Reference' "$Tml/Libraries/hjson/3.0.0/lib/netstandard1.3/Hjson.dll"
foreach ($name in @('FNA','ReLogic')) { $null = Add-PreviewItem 'Reference' "$Tml/Libraries/$name/1.0.0/$name.dll" }
foreach ($name in @('Program','Stubs','BookPreview','QuillPreview','ScrollPreview','NotebookFixture')) { $null = Add-PreviewItem 'Compile' "$out/$name.cs" }
foreach ($name in @('AlchemyDrawing','AlchemyNotebookSkin','AlchemyNotebookRedesign','AlchemyCatalog','AlchemyCatalogLayout','AlchemySelectionQuillMotion','AlchemyScrollMotion','AlchemyCardMotion','AlchemyNotebookSnapshot','AlchemyNotebookState','AlchemyNotebookPresentation','AlchemyBookArt')) {
    $path = Join-Path $ui "$name.cs"
    if (!(Test-Path -LiteralPath $path)) { throw "Missing production presentation source: $path" }
    $null = Add-PreviewItem 'Compile' $path
}
$null = Add-PreviewItem 'Compile' "$repo/ElainaModAlchemy/Gameplay/AlchemyProgressionRules.cs"
$bookUi = Join-Path $repo 'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel'
foreach ($name in @('ConstellationBookMotion','ConstellationBookRenderer','ConstellationUIClock')) {
    $null = Add-PreviewItem 'Compile' (Join-Path $bookUi "$name.cs")
}
$native = Add-PreviewItem 'None' "$Tml/Libraries/Native/Windows/*.dll"
$native.SetAttribute('Link', '%(Filename)%(Extension)')
$native.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
$projectPath = Join-Path $out 'Preview.csproj'
$project.Save($projectPath)
if ($CompileOnly) { dotnet build $projectPath --configuration $Configuration --nologo }
else {
    $previewArgs = @($repo, $Tml, $Culture)
    if ($BookOnly) { $previewArgs += '--book-only' }
    if ($QuillOnly) { $previewArgs += '--quill-only' }
    if ($ScrollOnly) { $previewArgs += '--scroll-only' }
    if ($StarPowerOnly) { $previewArgs += '--star-power-only' }
    if ($ExportMaterials) { $previewArgs += '--export-materials' }
    if ($Scene) { $previewArgs += "--scene=$Scene" }
    dotnet run --project $projectPath --configuration $Configuration -- @previewArgs
}
if ($LASTEXITCODE -ne 0) { throw 'Alchemy FNA preview failed.' }
