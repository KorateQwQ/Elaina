param([int]$Port = 8873)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$viewer = Join-Path $root '.vissandbox/shardbind-preview/html-v2/viewer'
if (!(Test-Path (Join-Path $viewer 'index.html')) -and !(Test-Path (Join-Path $viewer 'selected/index.html'))) { throw '先运行 Tools/ShardbindPreview/run.ps1 -Html。' }
$python = (Get-Command python).Source
$serverArguments = @('-I', '-m', 'http.server', "$Port", '--bind', '127.0.0.1', '--directory', ('"' + $viewer + '"'))
$server = Start-Process -FilePath $python -ArgumentList $serverArguments -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput (Join-Path $viewer 'server.stdout.log') -RedirectStandardError (Join-Path $viewer 'server.stderr.log')
$server.Id | Set-Content (Join-Path $viewer 'server.pid')
Write-Output "http://127.0.0.1:$Port/index.html"
Write-Output "本次预览服务 PID: $($server.Id)"
