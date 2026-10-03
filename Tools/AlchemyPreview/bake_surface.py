"""Alchemy-only continuous purple paper, without the skill chart's clipped layers.

1100x800 opaque RGB PNG; one cached draw at runtime, ~3.36 MiB as RGBA8.
Pillow is the only dependency. Does not modify the skill notebook surface.
"""
from pathlib import Path
import math
from PIL import Image

repo = Path(__file__).resolve().parents[2]
width, height = 1100, 800
surface = Image.new("RGB", (width, height))
pixels = surface.load()
for y in range(height):
    for x in range(width):
        t = (x / width * .8 + y / height * .2)
        base = [34 - 7 * t, 31 - 5 * t, 48 - 7 * t]
        # Broad diffuse light across the catalog, continuous through former header.
        light = math.exp(-2 * (((x - 300) / 630) ** 2 + ((y - 340) / 580) ** 2))
        # A soft horizontal transition gives the detail column depth, without a box seam.
        detail = 1 / (1 + math.exp(-(x - 751) / 23))
        edge = math.exp(-min(x, width - 1 - x, y, height - 1 - y) / 28)
        pixels[x, y] = tuple(round(max(0, min(255, channel + glow * light - 2.5 * detail - 3 * edge)))
                            for channel, glow in zip(base, (8, 6, 12)))
target = repo / "ElainaModAlchemy/UI/Assets/NotebookSurface.png"
surface.save(target)
print(target)
