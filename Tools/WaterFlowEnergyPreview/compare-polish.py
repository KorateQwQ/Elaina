from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont, ImageChops

folder = Path(sys.argv[1])
font = ImageFont.truetype('msyh.ttc', 14)
frames = []
for i in range(120):
    with Image.open(folder / f'polish-125-{i:03}.png') as image:
        img = image.convert('RGB')
    card = Image.new('RGB', (336, 298), '#19242c')
    draw = ImageDraw.Draw(card)
    for col, label in enumerate(('上一版', '精修水弧')):
        x = 5 + col * 166
        draw.text((x, 6), label, font=font, fill='#deecf3')
        for row in range(2):
            region = (160*col, 128*row, 160*(col+1), 128*(row+1))
            card.paste(img.crop(region), (x, 32+row*133))
    diff = ImageChops.difference(img.crop((0,0,160,256)), img.crop((160,0,320,256)))
    if bool(diff.getbbox()) != (0 < i < 115):
        # During fade, 8-bit coverage can already be zero at the final sample.
        if i != 114:
            raise ValueError(f'Unexpected material difference at frame {i}')
    frames.append(card)
samples = Image.new('RGB',(336,298*12))
for i in range(12):
    samples.paste(frames[10*i],(0,298*i))
palette = samples.quantize(256)
indexed = [f.quantize(palette=palette,dither=Image.Dither.NONE) for f in frames]
indexed[0].save(folder/'comparison.gif',save_all=True,append_images=indexed[1:],duration=[30,30,40]*40,loop=0,optimize=False)
