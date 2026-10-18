"""Rebuild the small additive muzzle glow (requires Pillow)."""
import math
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[2]
image = Image.new("RGBA", (128, 128))
for y in range(128):
    for x in range(128):
        radius_squared = ((x - 63.5) / 63.5) ** 2 + ((y - 63.5) / 63.5) ** 2
        falloff = max(0, (math.exp(-radius_squared * 4) - math.exp(-4)) / (1 - math.exp(-4))) if radius_squared < 1 else 0
        value = round(255 * falloff)
        image.putpixel((x, y), (value, value, value, 255))
image.save(root / "ElainaModSkills/Skills/Water/WaterLaserGlow.png")
