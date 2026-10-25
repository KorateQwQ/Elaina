from pathlib import Path
import sys
from PIL import Image, ImageDraw

root = Path(sys.argv[1])
pick = [2, 6, 10, 24, 110, 124, 132, 144]
sheet = Image.new('RGB', (4 * 320, 4 * 200), '#101520')
draw = ImageDraw.Draw(sheet)
for row, scene in enumerate(['night', 'day']):
    frames = [Image.open(p).convert('RGB') for p in sorted(root.glob(f'frame-{scene}-*.png'))]
    frames[0].save(root / f'doll-summon-{scene}.gif', save_all=True, append_images=frames[1:], duration=33, loop=0)
    for i, tick in enumerate(pick):
        frame = Image.open(root / f'frame-{scene}-{tick:03}.png').convert('RGB')
        x, y = (i % 4) * 320, (row * 2 + i // 4) * 200
        sheet.paste(frame.resize((320, 180), Image.LANCZOS), (x, y + 20))
        draw.text((x + 8, y + 4), f'{scene} / {"summon" if tick < 100 else "dismiss"} / tick {tick}', fill='#e9b8f5')
sheet.save(root / 'doll-summon-sequence.png')
