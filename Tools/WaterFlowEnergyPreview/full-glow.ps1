param([string]$TmlPath='D:/Steam/steamapps/common/tModLoader',[string]$TerrariaContent='D:/Steam/steamapps/common/Terraria/Content',[string]$Dotnet='C:/Users/PC/.dotnet/dotnet.exe',[switch]$Swell,[switch]$Polish)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$out=Join-Path $root '.vissandbox/water-full-glow'
$swellDir=Join-Path $root '.vissandbox/water-full-swell'
if ($Swell -or $Polish) { $out=$swellDir }
if ($Polish) { $out=Join-Path $root '.vissandbox/water-ring-polish' }
New-Item -Force -ItemType Directory $out | Out-Null
& "$PSScriptRoot/compile.ps1"
if ($Polish) {
    $source=Get-Content -Raw -Encoding utf8 "$root/Effects/Content/WaterEnergyRing.fx"
        $start=$source.IndexOf('    float trackEdge = ArcDistance')
        $end=$source.IndexOf('}', $source.IndexOf('    return extent > 0'))
        if ($start -lt 0 -or $end -le $start) { throw 'Ring material boundaries changed.' }
        $source=$source.Substring(0,$start)+(Get-Content -Raw -Encoding utf8 "$PSScriptRoot/polished-material.hlsl")+"`n"+$source.Substring($end)
    [IO.File]::WriteAllText("$out/WaterEnergyRing.fx",$source)
    & "$PSScriptRoot/compile.ps1" -PreviewSource "$out/WaterEnergyRing.fx"
}
[xml]$project=Get-Content -Raw "$PSScriptRoot/../BloomBackbufferPreview/Preview.csproj.txt"
foreach($file in @("$PSScriptRoot/FullGlow.cs.txt", "$root/ElainaModSkills/Skills/Water/WaterEnergyRingDrawing.cs")) {
    $entry=$project.CreateElement('Compile')
    $entry.SetAttribute('Include',$file)
    $null=$project.Project.ItemGroup.AppendChild($entry)
}
$project.Save("$out/FullGlow.csproj")
& $Dotnet run --project "$out/FullGlow.csproj" "-p:TmlPath=$TmlPath" -- $root $out $TerrariaContent $Swell.IsPresent $Polish.IsPresent
if($LASTEXITCODE -ne 0){throw 'Full glow rendering failed.'}
if ($Polish) { & python "$PSScriptRoot/compare-polish.py" $out }
elseif ($Swell) { & python "$PSScriptRoot/compare-swell.py" $out }
else { & python "$PSScriptRoot/compare-glow.py" $out }
if($LASTEXITCODE -ne 0){throw 'Full glow comparison failed.'}
