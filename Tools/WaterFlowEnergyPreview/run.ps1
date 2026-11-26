param([string]$TmlPath='D:/Steam/steamapps/common/tModLoader',[string]$TerrariaContent='D:/Steam/steamapps/common/Terraria/Content',[string]$Dotnet='C:/Users/PC/.dotnet/dotnet.exe')
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'compile.ps1')
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$out=Join-Path $root '.vissandbox/water-energy-ring'
[xml]$project=Get-Content -Raw "$PSScriptRoot/../BloomBackbufferPreview/Preview.csproj.txt"
foreach($file in @("$PSScriptRoot/Preview.cs.txt", "$root/ElainaModSkills/Skills/Water/WaterEnergyRingDrawing.cs", "$root/ElainaModSkills/Skills/Water/WaterEnergyRingMotion.cs", "$root/ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/ConstellationUIClock.cs")) {
    $entry=$project.CreateElement('Compile'); $entry.SetAttribute('Include',$file); $null=$project.Project.ItemGroup.AppendChild($entry)
}
$project.Save("$out/Ring.csproj")
& $Dotnet run --project "$out/Ring.csproj" "-p:TmlPath=$TmlPath" -- $root $out $TerrariaContent
if($LASTEXITCODE -ne 0){throw 'Water energy preview failed.'}
& python "$PSScriptRoot/check-flow.py" $out
if($LASTEXITCODE -ne 0){throw 'Water material contrast or motion check failed.'}
