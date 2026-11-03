from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont

root = Path(sys.argv[1])
states = [
    ('buildup-1', '积累 10%'), ('buildup-5', '积累 50%'), ('buildup-9', '积累 90%'),
    ('burning-10s', '燃烧 10 秒'), ('burning-5s', '燃烧 5 秒'), ('interrupted', '中断后清空'),
]
canvas = Image.new('RGB', (6 * 240, 285), (22, 24, 30))
draw = ImageDraw.Draw(canvas)
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 20)
for i, (name, label) in enumerate(states):
    with Image.open(root / f'{name}-1.5.png') as frame:
        canvas.paste(frame, (240 * i, 0))
    draw.text((240 * i + 120, 252), label, font=font, fill=(225, 229, 235), anchor='mm')
canvas.save(root / 'contact-sheet.png')
