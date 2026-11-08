param([string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader', [string]$Dotnet = 'C:/Users/PC/.dotnet/dotnet.exe')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$work = Join-Path $root '.vissandbox/ice-cone-wind-preview'
New-Item -ItemType Directory -Path $work -Force | Out-Null
[xml]$project = Get-Content -Raw (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Preview.csproj.txt')
foreach ($path in @(
    "$root/ElainaModSkills/Skills/Ice/IceConeWindDrawing.cs",
    "$root/ElainaModSkills/Skills/Ice/IceConeWindDust.cs",
    "$root/../KL/Drawing/LayerDrawRequestSystem.cs",
    "$PSScriptRoot/Preview.cs.txt", "$PSScriptRoot/Stubs.cs.txt")) {
    $node = $project.CreateElement('Compile')
    $node.SetAttribute('Include', $path)
    $null = $project.Project.ItemGroup.AppendChild($node)
}
$project.Save((Join-Path $work 'Preview.csproj'))
& $Dotnet run --project (Join-Path $work 'Preview.csproj') "-p:TmlPath=$TmlPath" -- $root (Join-Path $work 'out')
if ($LASTEXITCODE -ne 0) { throw "Ice wind preview failed: $LASTEXITCODE" }
