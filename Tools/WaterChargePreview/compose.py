"""Assemble actual FNA frames; requires Pillow. Run after run.ps1."""
from pathlib import Path
from PIL import Image, ImageDraw

output = Path(__file__).resolve().parents[2] / '.vissandbox/water-charge-preview/out'
names = ['frame-006', 'frame-010', 'frame-014', 'frame-018', 'frame-022',
         'frame-026', 'day-zoom', 'alternate-small', 'ball-only']
sheet = Image.new('RGB', (1080, 960), (13, 21, 31))
draw = ImageDraw.Draw(sheet)
for i, name in enumerate(names):
    frame = Image.open(output / (name + '.png')).convert('RGB').resize((360, 300))
    x, y = i % 3 * 360, i // 3 * 320
    sheet.paste(frame, (x, y))
    draw.text((x + 10, y + 302), name, fill='white')
sheet.save(output / 'contact-sheet.png')
frames = [Image.open(output / f'frame-{i:03}.png').convert('RGB').resize((600, 500)) for i in range(90)]
frames[0].save(output / 'water-charge.gif', save_all=True, append_images=frames[1:], duration=33, loop=0)
