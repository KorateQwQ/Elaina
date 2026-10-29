"""Generate the shared premultiplied glow for tML's rawimg pipeline."""
from pathlib import Path
from PIL import Image
import math

output = Path(__file__).resolve().parents[2] / 'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets'

# ImageIO.ToRaw preserves nonzero-alpha RGB. Store RGB=alpha for a white glow,
# otherwise AlphaBlend treats every nontransparent pixel as equally bright.
glow = Image.new('RGBA', (256, 256))
pixels = glow.load()
for y in range(glow.height):
    for x in range(glow.width):
        alpha = round(255 * max(0, 1 - math.hypot(x / 255 - .5, y / 255 - .5) * 2) ** 2.4)
        pixels[x, y] = (alpha, alpha, alpha, alpha)
glow.save(output / 'Glow.png')
