param(
    [string]$Tml = 'D:/Steam/steamapps/common/tModLoader',
    [string]$Sources = 'D:/泰拉原版贴图与代码与其他工具/tModLoader',
    [switch]$IconsOnly,
    [switch]$HeadersOnly,
    [switch]$RequirementsOnly,
    [switch]$ToggleOnly,
    [switch]$PolishOnly,
    [switch]$PrimaryOnly,
    [switch]$BookOnly,
    [switch]$BookChecksOnly,
    [switch]$CameraOnly,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (!(Test-Path -LiteralPath "$Sources/Terraria/UI/Chat/TextSnippet.cs")) {
    $candidate = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'Downloads/tML 2025.06源码与ExampleMod/tModLoader'
    if (Test-Path -LiteralPath "$candidate/Terraria/UI/Chat/TextSnippet.cs") { $Sources = $candidate }
    else { throw 'Terraria sources not found. Pass -Sources with the tModLoader source directory.' }
}
if (!(Test-Path -LiteralPath "$repo/../SilkyUIFramework/Components/SnippetModule.cs")) {
    throw 'Place matching SilkyUIFramework source beside this mod before running the preview.'
}
$out = Join-Path $repo '.vissandbox/constellation-skill-panel'
$ui = Join-Path $repo 'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$source = [IO.File]::ReadAllText((Join-Path $ui 'ConstellationSkillPanel.cs'))
$source += "`n" + [IO.File]::ReadAllText((Join-Path $ui 'ConstellationLayoutUI.cs'))
$scene = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Scene.cs.txt'))
foreach ($name in @('CenterOn','AdvanceCamera','MapPoint','RequestTooltip','DrawChrome','DrawFilter','DrawCloseButton','CameraControl','DrawMap','DrawEdge','DrawNode','DrawDetails','DrawActionButton','DrawSlot','DrawLoadoutCaption','DrawClearSlot','DrawLayoutHints','DrawLayoutButton','DrawDebugButton')) {
    $match = [regex]::Match($source, '(?m)^    private [^\r\n]*\b' + $name + '\(')
    if (!$match.Success) { throw "Missing drawing method: $name" }
    $start = $match.Index
    $brace = $source.IndexOf('{', $start)
    $semi = $source.IndexOf(';', $start)
    if ($source.Substring($start, $brace - $start).Contains('=>') -and $semi -lt $brace) { $end = $semi + 1 }
    else {
        $depth = 1; $end = $brace + 1
        while ($depth -gt 0) {
            if ($source[$end] -eq '{') { $depth++ }
            if ($source[$end] -eq '}') { $depth-- }
            $end++
        }
    }
    $scene += "`n" + $source.Substring($start, $end - $start)
}
$scene += "`n}"
[IO.File]::WriteAllText((Join-Path $out 'Scene.cs'), $scene)
foreach ($name in @('Program','Stubs','Checks','IconChecks','TogglePreview','PolishPreview','PrimaryButtonPreview','BookPreview')) {
    [IO.File]::WriteAllText((Join-Path $out "$name.cs"), [IO.File]::ReadAllText((Join-Path $PSScriptRoot "$name.cs.txt")))
}
# Exercise KL's actual default, upgrade guard and Skill accessor instead of duplicating their rules in a stub.
$modSkillSource = [IO.File]::ReadAllText((Join-Path $repo '../KL/SkillSystem/ModSkill.cs'))
$skillSource = [IO.File]::ReadAllText((Join-Path $repo '../KL/SkillSystem/Skill.cs'))
$members = @{
    '// LIVE_MAX_LEVEL' = [regex]::Match($modSkillSource, '(?m)^    public virtual int MaxLevel =>[^\r\n]+').Value
    '// LIVE_TRY_LEVEL_UP' = [regex]::Match($modSkillSource, '(?s)    public virtual void TryLevelUp\(\)\s*\{.*?\n    \}').Value
    '// LIVE_SKILL_MAX_LEVEL' = [regex]::Match($skillSource, '(?m)^    public int MaxLevel =>[^\r\n]+').Value
}
$stubs = [IO.File]::ReadAllText((Join-Path $out 'Stubs.cs'))
foreach ($marker in $members.Keys) {
    if (!$members[$marker]) { throw "Missing KL member: $marker" }
    $stubs = $stubs.Replace($marker, $members[$marker])
}
[IO.File]::WriteAllText((Join-Path $out 'Stubs.cs'), $stubs)
$project = [xml]'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NoWarn>0436</NoWarn></PropertyGroup><ItemGroup /></Project>'
$items = $project.SelectSingleNode('/Project/ItemGroup')
function Add-Item([string]$kind, [string]$path) {
    $node = $project.CreateElement($kind)
    $node.SetAttribute('Include', $path)
    [void]$items.AppendChild($node)
    return $node
}
foreach ($name in @('FNA','ReLogic')) { $null = Add-Item 'Reference' "$Tml/Libraries/$name/1.0.0/$name.dll" }
foreach ($name in @('Program','Stubs','Checks','IconChecks','TogglePreview','PolishPreview','PrimaryButtonPreview','BookPreview','Scene')) { $null = Add-Item 'Compile' "$out/$name.cs" }
foreach ($name in @('ConstellationUIClock','ConstellationBookMotion','ConstellationBookRenderer')) { $null = Add-Item 'Compile' "$ui/$name.cs" }
foreach ($name in @('ConstellationState','ConstellationDrawing','ConstellationDetail','ConstellationRichText','ConstellationRequirements','ConstellationToggleAnimation','ConstellationPolishMotion','ElainaSkill.Constellation','ConstellationLayout','SkillIconVariants')) { $null = Add-Item 'Compile' "$ui/$name.cs" }
foreach ($name in @('PreviewDrawing','PreviewData')) { $null = Add-Item 'Compile' "$ui/../ConstellationPreview/$name.cs" }
foreach ($name in @('SnippetModule','SnippetLine','SnippetToken')) { $null = Add-Item 'Compile' "$repo/../SilkyUIFramework/Components/$name.cs" }
$null = Add-Item 'Compile' "$repo/../SilkyUIFramework/Helper/TextSnippetHelper.cs"
foreach ($name in @('KLTextureSnippet','KLTextureTagHandler')) { $null = Add-Item 'Compile' "$repo/../KL/Drawing/Snippets/$name.cs" }
$null = Add-Item 'Compile' "$repo/../KL/SkillSystem/SkillUnlockCondition.cs"
$null = Add-Item 'Compile' "$repo/../KL/SkillSystem/SilkyUI/SkillUIInfoAttribute.cs"
foreach ($name in @('TextSnippet','ITagHandler')) { $null = Add-Item 'Compile' "$Sources/Terraria/UI/Chat/$name.cs" }
foreach ($name in @('PlainTagHandler','ColorTagHandler')) { $null = Add-Item 'Compile' "$Sources/Terraria/GameContent/UI/Chat/$name.cs" }
$native = Add-Item 'None' "$Tml/Libraries/Native/Windows/*.dll"
$native.SetAttribute('Link', '%(Filename)%(Extension)')
$native.SetAttribute('CopyToOutputDirectory', 'PreserveNewest')
$project.Save((Join-Path $out 'Preview.csproj'))
$previewArgs = @($repo, (Join-Path $Tml '../Terraria/Content'))
if ($IconsOnly) { $previewArgs += '--icons-only' }
if ($HeadersOnly) { $previewArgs += '--headers-only' }
if ($RequirementsOnly) { $previewArgs += '--requirements-only' }
if ($ToggleOnly) { $previewArgs += '--toggle-only' }
if ($PolishOnly) { $previewArgs += '--polish-only' }
if ($PrimaryOnly) { $previewArgs += '--primary-only' }
if ($BookOnly) { $previewArgs += '--book-only' }
if ($BookChecksOnly) { $previewArgs += '--book-checks-only' }
if ($CameraOnly) { $previewArgs += '--camera-only' }
dotnet run --project (Join-Path $out 'Preview.csproj') --configuration $Configuration -- @previewArgs
if ($LASTEXITCODE -ne 0) { throw 'Constellation checks or FNA rendering failed.' }
