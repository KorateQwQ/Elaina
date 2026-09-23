param(
    [string]$Tml = 'D:/Steam/steamapps/common/tModLoader',
    [switch]$CompileOnly,
    [switch]$BookOnly,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$out = Join-Path $repo '.vissandbox/alchemy-preview'
$ui = Join-Path $repo 'ElainaModAlchemy/UI'
New-Item -ItemType Directory -Force -Path $out | Out-Null
foreach ($name in @('Program','Stubs','BookPreview','NotebookFixture')) {
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
foreach ($name in @('FNA','ReLogic')) { $null = Add-PreviewItem 'Reference' "$Tml/Libraries/$name/1.0.0/$name.dll" }
foreach ($name in @('Program','Stubs','BookPreview','NotebookFixture')) { $null = Add-PreviewItem 'Compile' "$out/$name.cs" }
foreach ($name in @('AlchemyDrawing','AlchemyCatalog','AlchemyNotebookSnapshot','AlchemyNotebookState','AlchemyNotebookPresentation','AlchemyBookArt')) {
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
    $previewArgs = @($repo)
    if ($BookOnly) { $previewArgs += '--book-only' }
    dotnet run --project $projectPath --configuration $Configuration -- @previewArgs
}
if ($LASTEXITCODE -ne 0) { throw 'Alchemy FNA preview failed.' }
