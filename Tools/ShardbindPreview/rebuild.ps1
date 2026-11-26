param([switch]$Full, [switch]$NoOpen, [switch]$Open, [switch]$Interactive, [string]$Frames)
$ErrorActionPreference = 'Stop'
if ($Open -and $NoOpen) { throw '-Open 与 -NoOpen 不能同时使用。' }
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$selectionFile = Join-Path $root '.vissandbox/shardbind-preview/last-selection.json'
if ($Interactive -and !$Full -and !$PSBoundParameters.ContainsKey('Frames')) {
    $previousFrames = ''
    if (Test-Path -LiteralPath $selectionFile) {
        $selection = Get-Content -Raw -LiteralPath $selectionFile | ConvertFrom-Json
        if ($selection.frames -isnot [string]) { throw "无效的帧选择记录：$selectionFile" }
        $previousFrames = $selection.frames
    }
    $previousLabel = if ($previousFrames) { $previousFrames } else { '完整动画' }
    Write-Host "上次选择：$previousLabel。回车沿用；输入新帧号可更改；输入 all 渲染完整动画。" -ForegroundColor Cyan
    $answer = (Read-Host '渲染帧（0–277，例如 137,142,209 或 140-151）').Trim()
    $Frames = if ($answer) { $answer } else { $previousFrames }
}
$Frames = $Frames.Trim()
if ($Frames -ieq 'all') { $Frames = '' }
if ($Full -and $Frames) { throw '-Full 与 -Frames 不能同时使用。' }
$output = Join-Path $root '.vissandbox/shardbind-preview/html-v2'
$port = 8873
$url = "http://127.0.0.1:$port/index.html"

# 已有对照帧时只重建优化版；第一次运行或显式 -Full 时生成全部版本。
$hasBaseline = $true
foreach ($npc in @('none', 'fishron', 'golem')) {
    $scene = if ($npc -eq 'none') { $output } else { Join-Path $output "targets/$npc" }
    foreach ($mode in @('normal', 'dark')) {
        if (!(Test-Path -LiteralPath (Join-Path $scene "before/$mode/180.png"))) { $hasBaseline = $false }
    }
}
Write-Host '正在编译冰凌锁预览并渲染，请等待完成。' -ForegroundColor Cyan
$buildWatch = [System.Diagnostics.Stopwatch]::StartNew()
& (Join-Path $PSScriptRoot 'run.ps1') -Html -FastPreview -AfterOnly:($hasBaseline -and !$Full) -Frames $Frames
# 仅在渲染及发布成功后记忆；失败输入不会覆盖上一次选择。
$selectionPending = "$selectionFile.pending"
@{ frames = $Frames } | ConvertTo-Json | Set-Content -LiteralPath $selectionPending -Encoding utf8
Move-Item -LiteralPath $selectionPending -Destination $selectionFile -Force
Write-Host $(if ($Frames) { "原动画页面已更新指定帧：$Frames。" } else { '完整动画预览已更新。' }) -ForegroundColor Cyan
Write-Host "预览地址：$url"

if (!$NoOpen) {
    $startedServer = $false
    $listeners = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
    if (!($listeners | Where-Object { $_.Port -eq $port })) {
        & (Join-Path $PSScriptRoot 'serve.ps1') -Port $port
        $startedServer = $true
    }
    # 错误直接上抛；不把其他服务或旧版网页误报为打开成功。
    $response = Invoke-WebRequest -Uri $url -UseBasicParsing
    if ($response.StatusCode -ne 200 -or !$response.Content.Contains('冰凌锁')) {
        throw "端口 $port 返回的不是冰凌锁预览，请检查占用该端口的服务。"
    }
    if ($startedServer -or $Open) {
        Start-Process ($url + '?build=' + [DateTimeOffset]::UtcNow.ToUnixTimeSeconds())
    } else {
        Write-Host '原预览页面会自动刷新；需要重新打开页面时使用 -Open。'
    }
}
Write-Host ('冰凌锁预览编译完成，用时 {0:N1} 秒。' -f $buildWatch.Elapsed.TotalSeconds) -ForegroundColor Green
