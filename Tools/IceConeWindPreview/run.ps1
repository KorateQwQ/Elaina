param([string]$TmlPath = 'D:/Steam/steamapps/common/tModLoader', [string]$Dotnet = 'C:/Users/PC/.dotnet/dotnet.exe', [switch]$LegacyAlpha, [switch]$ColdStart, [switch]$ShaderState)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$work = Join-Path $root '.vissandbox/ice-cone-wind-preview'
New-Item -ItemType Directory -Path $work -Force | Out-Null
$drawing = "$root/ElainaModSkills/Skills/Ice/IceConeWindDrawing.cs"
if ($LegacyAlpha) {
    $source = Get-Content -LiteralPath $drawing -Raw
    $premultiplied = 'new Vector4(new Vector3(.62f, .82f, 1f) * fade, fade)'
    if (!$source.Contains($premultiplied)) { throw 'Cannot reconstruct legacy alpha: production expression changed.' }
    $drawing = Join-Path $work 'LegacyWindDrawing.cs.txt'
    [IO.File]::WriteAllText($drawing, $source.Replace($premultiplied, 'new Vector4(new Vector3(.62f, .82f, 1f), fade)'))
}
[xml]$project = Get-Content -Raw (Join-Path $PSScriptRoot '../BloomBackbufferPreview/Preview.csproj.txt')
foreach ($path in @(
    $drawing,
    "$root/ElainaModSkills/Skills/Fire/FireBurstWindDrawing.cs",
    "$root/ElainaModSkills/Skills/Ice/IceConeWindDust.cs",
    "$root/../KL/Drawing/LayerDrawRequestSystem.cs",
    "$PSScriptRoot/Preview.cs.txt", "$PSScriptRoot/ColdStartChecks.cs.txt", "$PSScriptRoot/Stubs.cs.txt")) {
    $node = $project.CreateElement('Compile')
    $node.SetAttribute('Include', $path)
    $null = $project.Project.ItemGroup.AppendChild($node)
}
$project.Save((Join-Path $work 'Preview.csproj'))
$output = Join-Path $work $(if ($LegacyAlpha) { 'legacy-alpha' } else { 'out' })
$arguments = @($root, $output)
if ($ColdStart) { $arguments += '--cold-start' }
if ($ShaderState) { $arguments += '--shader-state' }
& $Dotnet run --project (Join-Path $work 'Preview.csproj') "-p:TmlPath=$TmlPath" -- @arguments
if ($LASTEXITCODE -ne 0) { throw "Ice wind preview failed: $LASTEXITCODE" }
