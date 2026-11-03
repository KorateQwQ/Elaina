$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
# Fixed atlas: rows for 3..24 screen pixels, filled/outline columns, 28px cells.
# Average 32x32 subpixel samples and store premultiplied white RGBA.
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
public static class CollectionMaskBake {
    public static void Save(string path) {
        using (var atlas = new Bitmap(56, 616)) {
            for (int size = 3; size <= 24; size++)
            for (int state = 0; state < 2; state++) {
                double radius = size / 2.0;
                double inner = radius - Math.Sqrt(2) * Math.Max(.7, size * .16);
                for (int y = 0; y < size + 2; y++)
                for (int x = 0; x < size + 2; x++) {
                    int hits = 0;
                    for (int sy = 0; sy < 32; sy++)
                    for (int sx = 0; sx < 32; sx++) {
                        double d = Math.Abs(x + (sx + .5) / 32 - 1 - radius)
                                 + Math.Abs(y + (sy + .5) / 32 - 1 - radius);
                        if (d < radius && (state == 0 || d >= inner)) hits++;
                    }
                    int alpha = (int)Math.Round(hits * 255.0 / 1024);
                    atlas.SetPixel(state * 28 + x, (size - 3) * 28 + y, Color.FromArgb(alpha, alpha, alpha, alpha));
                }
            }
            atlas.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
'@
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
[CollectionMaskBake]::Save((Join-Path $repo 'ElainaModAlchemy/UI/Assets/CollectionDiamonds.png'))
Write-Output 'Baked collection diamonds: 3..24px, matching filled/outline boundaries.'
