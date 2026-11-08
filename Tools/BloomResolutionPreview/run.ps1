param(
    [string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader',
    [string]$Dotnet = 'C:/Users/PC/.dotnet/dotnet.exe',
    [string]$Label = 'after',
    [string]$BaselineRevision = '',
    [switch]$Check,
    [switch]$StarTips
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$kl = (Resolve-Path "$repo/../KL").Path
$work = Join-Path $repo '.vissandbox/bloom-resolution'
New-Item -ItemType Directory -Force $work | Out-Null
$pipelineRoot = $kl
if ($BaselineRevision) {
    $pipelineRoot = Join-Path $work 'baseline'
    New-Item -ItemType Directory -Force "$pipelineRoot/Drawing" | Out-Null
    foreach ($name in @('DrawSystem.cs','RenderHelper.cs','DrawHelper.Effects.cs','DrawHelper.DrawingMethod.cs','LayerDrawRequestSystem.cs')) {
        $lines = & git -C $kl show "${BaselineRevision}:Drawing/$name"
        if ($LASTEXITCODE -ne 0) { throw "Cannot read baseline $name" }
        [IO.File]::WriteAllText("$pipelineRoot/Drawing/$name", ($lines -join "`n"))
    }
}
$project = [IO.File]::ReadAllText("$PSScriptRoot/../BloomBackbufferPreview/Preview.csproj.txt").Replace('<ItemGroup>', '<ItemGroup><Compile Remove="baseline/**/*.cs" />')
[IO.File]::WriteAllText("$work/Preview.csproj", $project)
Copy-Item -LiteralPath "$PSScriptRoot/../BloomBackbufferPreview/Program.cs.txt" -Destination "$work/Pipeline.cs"
Copy-Item -LiteralPath "$PSScriptRoot/Preview.cs.txt" -Destination "$work/Preview.cs"
$previewArgs = @($repo, $kl, $pipelineRoot, "$work/$Label")
if ($Check) { $previewArgs += '--check' }
if ($StarTips) { $previewArgs += '--star-tips' }
& $Dotnet run --project "$work/Preview.csproj" '-p:StartupObject=BloomResolutionPreview' "-p:TmlPath=$TmlPath" -- @previewArgs
if ($LASTEXITCODE -ne 0) { throw 'Bloom resolution preview failed' }
