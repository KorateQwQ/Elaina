"""Compose the production smooth-follow trail frames; no alternative renderer."""
from pathlib import Path
import sys
from PIL import Image, ImageDraw

out = Path(sys.argv[1])
frames = []
for tick in range(1, 21):
    frame = Image.new('RGB', (1040, 380), '#15161d')
    draw = ImageDraw.Draw(frame)
    draw.text((10, 6), f'FNA smooth follow - tick {tick - 1} - 30 px/tick - straight / curved', fill='white')
    for row, bg in enumerate(('day', 'night')):
        source = Image.open(out / f'smooth-{bg}-{tick:02}.png')
        frame.paste(source.crop((0, 220, 1040, 390)), (0, 30 + row * 170))
    frames.append(frame)
frames[0].save(out / 'smooth-follow.gif', save_all=True, append_images=frames[1:],
               duration=[[20, 20, 10][i % 3] for i in range(19)] + [600], loop=0)
ticks = [1, 3, 5, 8, 11, 14]
sheet = Image.new('RGB', (1040, 190 * len(ticks)), '#15161d')
draw = ImageDraw.Draw(sheet)
for row, tick in enumerate(ticks):
    source = Image.open(out / f'smooth-night-{tick:02}.png')
    sheet.paste(source.crop((0, 220, 1040, 390)), (0, row * 190 + 20))
    draw.text((10, row * 190 + 4), f'tick {tick - 1}', fill='white')
sheet.save(out / 'smooth-follow-stages.png')
print(out / 'smooth-follow.gif')
