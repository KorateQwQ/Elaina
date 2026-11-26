param(
    [string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader',
    [string]$Dotnet = 'C:/Users/PC/.dotnet/dotnet.exe',
    [string]$TerrariaContent = 'D:/Steam/steamapps/common/Terraria/Content',
    [ValidateSet('all', 'bosses', 'none', 'fishron', 'golem')][string]$Target = 'all',
    [switch]$Probe,
    [switch]$Html,
    [switch]$AfterOnly,
    [switch]$FastPreview,
    [string]$Frames,
    [switch]$VerifyVisual,
    [switch]$SwitchDemo,
    [switch]$CameraProbe,
    [switch]$ImpactProbe
)
$ErrorActionPreference = 'Stop'
if (($CameraProbe -or $ImpactProbe) -and ($Html -or $Probe -or $AfterOnly -or $Frames -or $VerifyVisual -or $SwitchDemo -or ($CameraProbe -and $ImpactProbe))) {
    throw '镜头和终爆回归入口各自独立，不能与动画导出选项混用。'
}
if ($VerifyVisual -and !$Html) { throw '-VerifyVisual 需要 -Html。' }
. (Join-Path $PSScriptRoot 'parse_frames.ps1')
$selectedFrames = @(ConvertTo-ShardbindFrames $Frames)
if ($selectedFrames.Count -gt 0 -and (!$Html -or $Probe -or $VerifyVisual)) {
    throw '-Frames 需要 -Html，且不能与 -Probe 或 -VerifyVisual 同用。'
}
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$work = Join-Path $root '.vissandbox/shardbind-preview'
if ($SwitchDemo -and (!$Html -or $Frames -or $Probe -or !(Test-Path -LiteralPath (Join-Path $work 'html-v2/viewer/timing.json')))) {
    throw '-SwitchDemo 需要 -Html 和已有完整预览，不可与 -Frames / -Probe 同用。'
}
if ($selectedFrames.Count -gt 0 -and !(Test-Path -LiteralPath (Join-Path $work 'html-v2/viewer/timing.json'))) {
    throw '指定帧更新需要已有完整动画，请先不带 -Frames 运行 rebuild。'
}
New-Item -ItemType Directory -Path $work -Force | Out-Null
if (!$CameraProbe -and !$ImpactProbe) { & (Join-Path $PSScriptRoot 'compile-sheet.ps1') }
[xml]$project = Get-Content -Raw (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Preview.csproj.txt')
foreach ($source in @('Preview.cs.txt', 'Replay.cs.txt', 'NpcTargets.cs.txt', 'PngExport.cs.txt', 'Timeline.cs.txt', 'SwitchPreview.cs.txt', 'ReticleTrackingChecks.cs.txt', 'CameraPreview.cs.txt', 'ImpactPreview.cs.txt')) {
    $node = $project.CreateElement('Compile')
    $node.SetAttribute('Include', (Join-Path $PSScriptRoot $source))
    $null = $project.Project.ItemGroup.AppendChild($node)
}
foreach ($source in @('ShardbindSequence', 'ReticleTracking')) {
    $node = $project.CreateElement('Compile')
    $node.SetAttribute('Include', (Join-Path $root "ElainaModSkills/Skills/Ice/$source.cs"))
    $null = $project.Project.ItemGroup.AppendChild($node)
}
$project.Save((Join-Path $work 'Preview.csproj'))
$output = Join-Path $work $(if ($Html) { 'html-v2' } else { 'out' })
if ($Probe) { $output = Join-Path $work 'npc-probe' }
if ($CameraProbe) { $output = Join-Path $work 'camera-probe' }
if ($ImpactProbe) { $output = Join-Path $work 'impact-probe' }
if ($selectedFrames.Count -gt 0) { $output = Join-Path $work 'selected-frames' }
$mode = if ($Html) { 'html' } else { 'standard' }
if ($AfterOnly -and $selectedFrames.Count -eq 0) {
    if (!$Html -or $Probe) { throw '-AfterOnly 需要 -Html，且不能与 -Probe 同用。' }
    $targets = switch ($Target) { 'all' { @('none', 'fishron', 'golem') } 'bosses' { @('fishron', 'golem') } default { @($Target) } }
    foreach ($npc in $targets) {
        $sceneOutput = if ($npc -eq 'none') { $output } else { Join-Path $output "targets/$npc" }
        foreach ($background in @('normal', 'dark')) {
            if (!(Test-Path (Join-Path $sceneOutput "before/$background/180.png"))) {
                throw "-AfterOnly 缺少 $npc/$background 的修改前帧；先导出该目标的两个版本。"
            }
        }
    }
}
$version = if ($AfterOnly -or $selectedFrames.Count -gt 0) { 'after' } else { 'all' }
$sampling = if ($ImpactProbe) { 'impact' } elseif ($CameraProbe) { 'camera' } elseif ($SwitchDemo) { 'switch' } elseif ($Probe) { 'probe' } else { 'full' }
Write-Host '[1/2] Compiling and rendering FNA frames...'
$renderArguments = @($root, $output, $mode, $version, $TerrariaContent, $Target, $sampling)
if ($selectedFrames.Count -gt 0) { $renderArguments += ($selectedFrames -join ',') }
& $Dotnet run --project (Join-Path $work 'Preview.csproj') "-p:TmlPath=$TmlPath" -- @renderArguments
if ($LASTEXITCODE -ne 0) { throw "Shardbind preview failed: $LASTEXITCODE" }
if ($Probe -or $CameraProbe -or $ImpactProbe) { return }
if ($selectedFrames.Count -gt 0) {
    python -I (Join-Path $PSScriptRoot 'compose_selected.py') $output (Join-Path $work 'html-v2') ($selectedFrames -join ',') $Target
    if ($LASTEXITCODE -ne 0) { throw "Selected frame composition failed: $LASTEXITCODE" }
    return
}
$composer = if ($Html) { 'compose_html.py' } else { 'compose.py' }
Write-Host '[2/2] Preparing browser frames and checking output...'
$composeArguments = @('-I', (Join-Path $PSScriptRoot $composer), $output)
if ($Html -and $FastPreview) { $composeArguments += '--png' }
if ($VerifyVisual) {
    $composeArguments += '--verify-visual'
}
python @composeArguments
if ($LASTEXITCODE -ne 0) { throw "Shardbind composition failed: $LASTEXITCODE" }
