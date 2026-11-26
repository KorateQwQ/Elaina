from pathlib import Path
import sys
from PIL import Image, ImageChops, ImageDraw, ImageFont

folder = Path(sys.argv[1])
font = ImageFont.truetype('msyh.ttc', 15)
frames = []
for i in range(90):
    with Image.open(folder / f'swell-{i:02}.png') as image:
        img = image.convert('RGB')
    left = img.crop((0, 0, 176, 144))
    right = img.crop((176, 0, 352, 144))
    delta = ImageChops.difference(left, right).getbbox()
    if i < 24 and delta or i > 25 and not delta:
        raise ValueError(f'Unexpected silhouette difference at frame {i}')
    card = Image.new('RGB', (368, 186), '#19242c')
    draw = ImageDraw.Draw(card)
    draw.text((5, 8), '当前水弧', font=font, fill='#deecf3')
    draw.text((187, 8), '满溢时鼓起', font=font, fill='#deecf3')
    card.paste(left, (5, 37))
    card.paste(right, (187, 37))
    frames.append(card)
samples = Image.new('RGB', (368, 186 * 9))
for i in range(9):
    samples.paste(frames[10*i], (0, 186*i))
palette = samples.quantize(256)
indexed = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
indexed[0].save(folder / 'comparison.gif', save_all=True, append_images=indexed[1:],
                duration=[30, 30, 40] * 30, optimize=False, loop=0)
