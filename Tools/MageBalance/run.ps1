param([int]$Port = 8768, [switch]$NoBrowser)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$config = Join-Path $root 'ElainaModSkills/Data/SkillBalance.json'
$url = "http://127.0.0.1:$Port/"
$ready = $false
try {
    $info = Invoke-RestMethod -Uri "${url}api/info" -TimeoutSec 1
    $ready = $info.tool -eq 'elaina-mage-balance' -and $info.path -eq $config -and $info.schema -eq 'elaina-mage-balance-v2'
} catch {}
if (-not $ready) {
    $python = (Get-Command python -ErrorAction Stop).Source
    $out = Join-Path $PSScriptRoot 'test-output'
    New-Item -ItemType Directory -Path $out -Force | Out-Null
    $process = Start-Process -FilePath $python -ArgumentList @('-u', ('"' + (Join-Path $PSScriptRoot 'server.py') + '"'), '--port', $Port) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $out 'server.log') -RedirectStandardError (Join-Path $out 'server-error.log')
    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Milliseconds 100
        try {
            $info = Invoke-RestMethod -Uri "${url}api/info" -TimeoutSec 1
            $ready = $info.tool -eq 'elaina-mage-balance' -and $info.path -eq $config -and $info.schema -eq 'elaina-mage-balance-v2'
            if ($ready) { break }
        } catch {}
        if ($process.HasExited) { break }
    }
}
if (-not $ready) { throw "无法启动编辑器，请检查 test-output/server-error.log，或用 -Port 指定空闲端口。" }
Write-Host "编辑器：$url"
Write-Host "固定 JSON：$config"
if (-not $NoBrowser) { Start-Process $url }
