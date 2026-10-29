from pathlib import Path
import sys
from PIL import Image, ImageDraw

root = Path(sys.argv[1])
chests = [Image.open(p).convert('RGB') for p in sorted(root.glob('chests-*.png'))]
chests[0].save(root / 'recipe-chests.gif', save_all=True, append_images=chests[1:], duration=67, loop=0)
bolts = [Image.open(p).convert('RGB') for p in sorted(root.glob('waterbolt-*.png'))]
bolts[0].save(root / 'water-bolts.gif', save_all=True, append_images=bolts[1:], duration=67, loop=0)
frames = [Image.open(p).convert('RGB') for p in sorted(root.glob('motion-*.png'))]
frames[0].save(root / 'revealing-dust.gif', save_all=True, append_images=frames[1:], duration=33, loop=0)
wick = [Image.open(p).convert('RGB') for p in sorted(root.glob('wick-*.png'))]
wick[0].save(root / 'center-wick.gif', save_all=True, append_images=wick[1:], duration=66, loop=0)
names = ['dust-002.png', 'dust-012.png', 'dust-032.png', 'dust-065.png', 'dust-095.png', 'dust-120.png']
sheet = Image.new('RGB', (1440, 588), '#101520')
draw = ImageDraw.Draw(sheet)
for i, name in enumerate(names):
    frame = Image.open(root / name).convert('RGB').resize((480, 270))
    x, y = (i % 3) * 480, (i // 3) * 294
    sheet.paste(frame, (x, y + 24))
    draw.text((x + 12, y + 6), f'FNA / {int(name[5:8])} ticks', fill='#e4bf74')
sheet.save(root / 'dust-sequence.png')

comparison = Image.open(root / 'mark-comparison.png').convert('RGB')
labeled = Image.new('RGB', (960, 528), '#171923')
labels = ImageDraw.Draw(labeled)
for row in range(2):
    y = row * 264
    labeled.paste(comparison.crop((0, row * 240, 960, row * 240 + 240)), (0, y + 24))
    labels.text((12, y + 6), ('DARK' if row == 0 else 'LIGHT') + ' / Before', fill='#dfbf79')
    labels.text((492, y + 6), 'After / Gold mark + early tile vision', fill='#dfbf79')
labeled.save(root / 'mark-comparison-labeled.png')
