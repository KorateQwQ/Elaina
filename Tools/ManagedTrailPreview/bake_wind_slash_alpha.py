"""Feather the transverse alpha of the two scrolling WindSlash materials."""
from pathlib import Path

from PIL import Image, ImageChops


root = Path(__file__).resolve().parents[2]
source = root.parent / 'KL/Effects/Tex/Wind'
destination = root / 'ElainaModSkills/Skills/Wind/Assets'

for base, name in [('wind3', 'WindSlashStreaks'), ('windNoi', 'WindSlashNoise')]:
    with Image.open(source / f'{base}.png') as original:
        image = original.convert('RGBA')
    width, height = image.size
    # Guard the LinearWrap seam on both sides. Horizontal scrolling uses the original RGB and phase.
    feather = round(height * .16)
    mask = Image.new('L', (1, height))
    for y in range(height):
        t = min(1, min(y, height - 1 - y) / feather)
        mask.putpixel((0, y), round(255 * t * t * (3 - 2 * t)))
    image.putalpha(ImageChops.multiply(image.getchannel('A'), mask.resize((width, height))))
    image.save(destination / f'{name}.png')
