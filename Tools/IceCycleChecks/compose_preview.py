"""Compose current FNA outputs after the ice/water behavior and localization previews."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

repo = Path(__file__).resolve().parents[2]
output = repo / '.vissandbox/ice-cycle-checks'
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 17)
panels = Image.new('RGB', (4 * 268, 2 * 500), (22, 24, 30))
draw = ImageDraw.Draw(panels)
for row, culture in enumerate(('zh-Hans', 'en-US')):
    for column, spell in enumerate(('stream', 'orb', 'cone', 'lock')):
        path = repo / f'.vissandbox/constellation-skill-panel/output/{culture}/ice-water-{spell}-1280x720-1.00.png'
        with Image.open(path) as frame:
            panels.paste(frame.crop((820, 150, 1088, 620)), (column * 268, row * 500 + 30))
        draw.text((column * 268 + 12, row * 500 + 5), f'{culture} / {spell}', font=font, fill='white')
panels.save(output / 'skill-text-contact-sheet.png')

states = [('water-1', '潮湿积累 6.7%'), ('water-7', '潮湿积累 46.7%'), ('water-14', '潮湿积累 93.3%'),
          ('wet-10s', '潮湿 10 秒'), ('wet-5s', '潮湿 5 秒'), ('wet-consumed', '被火消耗完毕')]
sheet = Image.new('RGB', (6 * 240, 275), (22, 24, 30))
draw = ImageDraw.Draw(sheet)
for column, (name, label) in enumerate(states):
    with Image.open(output / f'progress-preview/{name}-1.5.png') as frame:
        sheet.paste(frame, (column * 240, 0))
    draw.text((column * 240 + 120, 252), label, font=font, fill='white', anchor='mm')
sheet.save(output / 'progress-preview/contact-sheet.png')
