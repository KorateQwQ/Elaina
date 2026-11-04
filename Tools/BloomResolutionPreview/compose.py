"""Native-pixel before/after crops from the 1920x1080 FNA captures."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[2] / '.vissandbox/bloom-resolution'
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 18)
small = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 14)
canvas = Image.new('RGB', (980, 720), '#20232c')
draw = ImageDraw.Draw(canvas)
draw.text((16, 10), '1920 × 1080 · FNA 离线预览 · 局部原生像素', font=font, fill='#e8e6f0')
draw.text((16, 42), '修改前', font=font, fill='#e8e6f0')
draw.text((506, 42), '修改后', font=font, fill='#e8e6f0')
for row, (kind, label) in enumerate([('star', '多重射击十字星'), ('fire', '火焰粒子'), ('trail', '魔法飞弹拖尾')]):
    top = 76 + row * 210
    draw.text((16, top), label, font=small, fill='#c8c5d3')
    for col, version in enumerate(['before', 'after']):
        with Image.open(root / version / f'{kind}-1920x1080-point-crop.png') as image:
            canvas.paste(image.crop((0, 60, 480, 240)).convert('RGB'), (5 + col * 490, top + 24))
path = root / 'comparison-1080p.png'
canvas.save(path)
print(path)
