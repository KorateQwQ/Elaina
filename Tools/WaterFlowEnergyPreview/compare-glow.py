from pathlib import Path
import sys
from PIL import Image, ImageChops, ImageDraw, ImageFont

folder = Path(sys.argv[1])
for old in folder.glob('*-old.png'):
    new = old.with_name(old.name.replace('-old', '-new'))
    delta = ImageChops.difference(Image.open(old).convert('RGB'), Image.open(new).convert('RGB'))
    if bool(delta.getbbox()) != ('-full-' in old.name):
        raise ValueError(f'Unexpected glow difference: {old.name}')

font = ImageFont.truetype('msyh.ttc', 16)
sheet = Image.new('RGB', (3 * 208 + 4 * 12, 2 * 156 + 68), '#162029')
draw = ImageDraw.Draw(sheet)
for col, (state, label) in enumerate([('near-new', '接近满能量'), ('full-old', '满能量 · 原效果'), ('full-new', '满能量 · 辉光预览')]):
    x = 12 + col * 220
    draw.text((x, 9), label, font=font, fill='#dbeaf1')
    for row, scene in enumerate(('Day', 'Night')):
        with Image.open(folder / f'{scene}-125-{state}.png') as crop:
            sheet.paste(crop, (x, 40 + row * 168))
sheet.save(folder / 'comparison.png')
