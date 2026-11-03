$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition ([IO.File]::ReadAllText("$PSScriptRoot/Bake.cs.txt")) -ReferencedAssemblies System.Drawing
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$assets = Join-Path $repo 'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets'
$regions = Get-Content -Raw -Encoding UTF8 "$assets/Stars.json" | ConvertFrom-Json
# Keep the original star atlas for its other consumers. The skill panel uses the denser atlas.
$atlas = New-Object Drawing.Bitmap 1024,2048
[int]$x=2; [int]$y=2; [int]$row=0
foreach ($region in $regions) {
    $levels = @()
    foreach ($size in (@(128,96,64,48,40,36,32,28,24) + (23..4))) {
        $tile = [RasterBake]::Star($size,$region.Aspect,$region.Waist,$region.Filled)
        $levels += ,([RasterBake]::Put($atlas,$tile,[ref]$x,[ref]$y,[ref]$row))
        $tile.Dispose()
    }
    $region.Levels = $levels
}
$crop = $atlas.Clone([Drawing.Rectangle]::new(0,0,1024,$y+$row+2),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$crop.Save("$assets/StarsRaster.png"); $crop.Dispose(); $atlas.Dispose()
[IO.File]::WriteAllText("$assets/StarsRaster.json", ($regions | ConvertTo-Json -Depth 5))
$atlas = New-Object Drawing.Bitmap 1024,1024
[int]$x=2; [int]$y=2; [int]$row=0
$masks = [ordered]@{}
$hat = [Drawing.Bitmap]::FromFile("$assets/HeaderHat.png")
$book = [Drawing.Bitmap]::FromFile("$assets/StudyBook.png")
$crest = [Drawing.Bitmap]::FromFile("$repo/ElainaModAlchemy/UI/Assets/AlchemyCrest.png")
foreach ($name in @('HeaderHat','Disc','StudyBook','AlchemyCrest')) {
    $levels = @()
    $sizes = switch ($name) {
        'HeaderHat' { @(128,112,96,88,80,72,64,60,56,52,48) + (47..24) }
        'AlchemyCrest' { @(128,112,96,88,80,72,64,60,56,52,48) + (47..24) }
        'StudyBook' { @(64,60,56,52,48,44,40,36,32,30,28,26,24) + (23..10) }
        default { @(64,48,32,24,16,12,10,8,7,6,5,4,3) }
    }
    foreach ($size in $sizes) {
        $tile = switch ($name) {
            'HeaderHat' { [RasterBake]::Reduce($hat,$size,$true) }
            'AlchemyCrest' { [RasterBake]::Reduce($crest,$size,$true) }
            'StudyBook' { if ($size -lt 24) { [RasterBake]::SmallBook($size) } else { [RasterBake]::Reduce($book,$size,$true) } }
            default { [RasterBake]::Disc($size) }
        }
        $levels += ,([RasterBake]::Put($atlas,$tile,[ref]$x,[ref]$y,[ref]$row))
        $tile.Dispose()
    }
    $masks[$name] = $levels
}
$hat.Dispose()
$book.Dispose()
$crest.Dispose()
$crop = $atlas.Clone([Drawing.Rectangle]::new(0,0,1024,$y+$row+2),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$crop.Save("$assets/RasterMasks.png"); $crop.Dispose(); $atlas.Dispose()
[IO.File]::WriteAllText("$assets/RasterMasks.json", ($masks | ConvertTo-Json -Depth 5))
# Transparent rows beside the opaque core give thin rotated strokes coverage at their edges.
$line = New-Object Drawing.Bitmap 3,3
for ($i=0; $i -lt 3; $i++) { $line.SetPixel($i,1,[Drawing.Color]::White) }
$line.Save("$assets/LineAA.png"); $line.Dispose()
Write-Output 'Baked skill-panel raster masks and dense star levels.'
