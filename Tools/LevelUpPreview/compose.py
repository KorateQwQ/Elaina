from pathlib import Path
import sys
from PIL import Image, ImageDraw

root = Path(sys.argv[1])
pick = [6, 18, 30, 40, 50, 62, 76, 96]
cell = (360, 240)
sheet = Image.new('RGB', (4 * cell[0], 4 * (cell[1] + 20)), '#101520')
draw = ImageDraw.Draw(sheet)
for row, scene in enumerate(['night', 'day']):
    frames = [Image.open(p).convert('RGB') for p in sorted(root.glob(f'frame-{scene}-*.png'))]
    frames[0].save(root / f'level-up-{scene}.gif', save_all=True, append_images=frames[1:], duration=33, loop=0)
    for i, tick in enumerate(pick):
        frame = Image.open(root / f'frame-{scene}-{tick:03}.png').convert('RGB')
        x, y = (i % 4) * cell[0], (row * 2 + i // 4) * (cell[1] + 20)
        sheet.paste(frame.resize(cell, Image.LANCZOS), (x, y + 20))
        draw.text((x + 8, y + 4), f'{scene} / tick {tick}', fill='#f0d9a8')
sheet.save(root / 'level-up-sequence.png')
