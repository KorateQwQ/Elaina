"""Compose current FNA transition frames and a real-time GIF; no alternative rendering."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2] / '.vissandbox/fire-laser-preview/out'
for name, ticks in [('entry', [0, 5, 7, 10, 14, 18, 24]), ('exit', [0, 12, 18, 24, 28, 32, 36, 42])]:
    sheet = Image.new('RGB', (1100, 205 * len(ticks)), '#10141e')
    draw = ImageDraw.Draw(sheet)
    for row, tick in enumerate(ticks):
        frame = Image.open(root / f'{name}-{tick:02}.png').crop((100, 245, 1200, 430))
        sheet.paste(frame, (0, row * 205 + 20))
        draw.text((8, row * 205 + 3), f'{name}  {tick}/60 s', fill='white')
    sheet.save(root / f'{name}-sheet.png')
files = sorted((root / 'transitions').glob('frame-*.png'))
if files:
    frames = [Image.open(path).crop((75, 200, 1200, 475)).convert('P', palette=Image.Palette.ADAPTIVE) for path in files]
    frames[0].save(root / 'fire-laser-transitions.gif', save_all=True, append_images=frames[1:], duration=17, loop=0)
print(root)
