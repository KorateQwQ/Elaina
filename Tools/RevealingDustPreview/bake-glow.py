"""Bake the shared 64x64 white falloff. RGB carries falloff for alpha=0 additive tint."""
from pathlib import Path
from math import exp, sqrt
import sys
from PIL import Image

target = Path(__file__).resolve().parents[2] / 'ElainaModAlchemy/Revealing/Assets/MarkGlow.png'
size = 64
im = Image.new('RGBA', (size, size))
for y in range(size):
    for x in range(size):
        r = sqrt(((x + .5 - size / 2) / (size / 2)) ** 2 + ((y + .5 - size / 2) / (size / 2)) ** 2)
        value = round(255 * max(0, (exp(-4.5 * r * r) - exp(-4.5)) / (1 - exp(-4.5)))) if r < 1 else 0
        # Opaque nonzero pixels prevent FromStream's premultiply path from attenuating RGB twice.
        im.putpixel((x, y), (value, value, value, 255 if value else 0))
if '--check' in sys.argv:
    assert target.exists() and Image.open(target).convert('RGBA').tobytes() == im.tobytes(), 'Stale MarkGlow.png'
    print('MarkGlow: 64x64 RGB falloff matches generator')
else:
    target.parent.mkdir(parents=True, exist_ok=True)
    im.save(target)
    print(target)
