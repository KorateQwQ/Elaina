param([string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader', [string]$Dotnet = 'C:/Users/PC/.dotnet/dotnet.exe', [switch]$SkipShader, [switch]$GlowComparison, [switch]$Domain, [switch]$Cast, [switch]$RingBloom, [switch]$ImpactOnly, [switch]$LaunchTicks, [switch]$WandTrail)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$kl = (Resolve-Path (Join-Path $root '../KL')).Path
$work = Join-Path $root '.vissandbox/fire-burst-preview'
if (!$SkipShader) { & (Join-Path $PSScriptRoot 'compile.ps1') }
New-Item -ItemType Directory -Path $work -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Preview.csproj.txt') -Destination (Join-Path $work 'Preview.csproj')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Program.cs.txt') -Destination (Join-Path $work 'Pipeline.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Preview.cs.txt') -Destination (Join-Path $work 'Preview.cs')
$previewArguments = @($kl, $root, (Join-Path $work 'out'))
if ($GlowComparison) { $previewArguments += '--glow-comparison' }
if ($Domain) { $previewArguments += '--domain' }
if ($ImpactOnly) { $previewArguments += '--impact-only' }
if ($Cast) { $previewArguments += '--cast' }
if ($LaunchTicks) { $previewArguments += '--cast'; $previewArguments += '--launch-ticks' }
if ($RingBloom) { $previewArguments += '--ring-bloom' }
if ($WandTrail) { $previewArguments += '--cast'; $previewArguments += '--wand-trail' }
# Copy-Item 保留源时间戳；强制刷新，避免运行此前本地修改过的旧预览程序集。
(Get-Item -LiteralPath (Join-Path $work 'Preview.cs')).LastWriteTime = Get-Date
& $Dotnet run --project (Join-Path $work 'Preview.csproj') '-p:StartupObject=FireBurstPreview' "-p:TmlPath=$TmlPath" -- @previewArguments
if ($LASTEXITCODE -ne 0) { throw "Fire burst preview failed: $LASTEXITCODE" }
if ($Domain) { python (Join-Path $PSScriptRoot 'compose_domain.py') (Join-Path $work 'out'); if ($LASTEXITCODE -ne 0) { throw 'Domain GIF composition failed' } }
if ($Cast) { python (Join-Path $PSScriptRoot 'compose_cast.py') (Join-Path $work 'out'); if ($LASTEXITCODE -ne 0) { throw 'Cast GIF composition failed' } }
if ($RingBloom) { python (Join-Path $PSScriptRoot 'compose_ring_bloom.py') (Join-Path $work 'out'); if ($LASTEXITCODE -ne 0) { throw 'Ring Bloom comparison failed' } }
