"""Prepare KL particle masks with the sprite and ReColor RGB multipliers, before gain."""
from pathlib import Path
from PIL import Image, ImageChops

HERE = Path(__file__).resolve().parent
KL = HERE.parents[2] / 'KL'
OUT = HERE / 'particles'
OUT.mkdir(exist_ok=True)

for source, cols, rows, tint in [
    ('Dusts/Lightning/LightningDust.PNG', 3, 3, tuple(round(c*c/255) for c in (151, 206, 255))),
    ('Dusts/Lightning/LightningDust3.png', 2, 2, tuple(round(c*c/255) for c in (151, 206, 255))),
    ('Effects/Tex/Lightning/LightningDustTex.png', 1, 10, (71, 176, 255)),
]:
    image = Image.open(KL / source).convert('RGBA')
    w, h = image.width // cols, image.height // rows
    atlas = Image.new('RGBA', (cols*w, rows*h))
    for j in range(rows):
        for i in range(cols):
            frame = image.crop((i*w, j*h, (i+1)*w, (j+1)*h))
            mask = ImageChops.multiply(frame.getchannel('A'), frame.convert('L'))
            ink = Image.new('RGBA', frame.size, tint)
            ink.putalpha(mask)
            atlas.paste(ink, (i*w, j*h))
    atlas.save(OUT / (Path(source).stem + '.png'), optimize=True)
