"""Generate non-text backgrounds and a premultiplied glow for tML's rawimg pipeline."""
from pathlib import Path
from PIL import Image
import math

image = Image.new('RGB', (1100, 800))
pixels = image.load()
for y in range(image.height):
    for x in range(image.width):
        t = x / (image.width - 1)
        base = tuple(a + (b - a) * t for a, b in zip((34, 32, 50), (27, 27, 42)))
        glow = max(0, 1 - math.hypot((t - .24) / .65, (y / 800 - .35) / .8)) * .16
        pixels[x, y] = tuple(round(a * (1 - glow) + b * glow) for a, b in zip(base, (104, 85, 126)))
output = Path(__file__).with_name('Assets')
image.save(output / 'Page.png')

# Old HTML .map-panel: radial-gradient(ellipse at 46% 56%,
# #322c4b66 0%, #201d3240 44%, #151421 80%), composited over --bg: #10101d.
# Bake the opaque result so the gradual shading does not depend on additive blending.
sky = Image.new('RGB', (1454, 1100))
pixels = sky.load()
stops = [(0, (50, 44, 75, 102)), (.44, (32, 29, 50, 64)), (.8, (21, 20, 33, 255))]
base = (16, 16, 29)
for y in range(sky.height):
    for x in range(sky.width):
        radius = math.hypot((x / (sky.width - 1) - .46) / (.54 * math.sqrt(2)),
                            (y / (sky.height - 1) - .56) / (.56 * math.sqrt(2)))
        lo, hi = (stops[0], stops[1]) if radius < .44 else (stops[1], stops[2])
        t = max(0, min(1, (radius - lo[0]) / (hi[0] - lo[0])))
        alpha = ((1 - t) * lo[1][3] + t * hi[1][3]) / 255
        # CSS gradients interpolate colors in premultiplied alpha space.
        pixels[x, y] = tuple(round(((1 - t) * lo[1][i] * lo[1][3] + t * hi[1][i] * hi[1][3]) / 255
                                   + base[i] * (1 - alpha)) for i in range(3))
sky.save(output / 'StarfieldBase.png')

# ImageIO.ToRaw preserves nonzero-alpha RGB. Store RGB=alpha for a white glow,
# otherwise AlphaBlend treats every nontransparent pixel as equally bright.
glow = Image.new('RGBA', (256, 256))
pixels = glow.load()
for y in range(glow.height):
    for x in range(glow.width):
        alpha = round(255 * max(0, 1 - math.hypot(x / 255 - .5, y / 255 - .5) * 2) ** 2.4)
        pixels[x, y] = (alpha, alpha, alpha, alpha)
glow.save(output / 'Glow.png')
