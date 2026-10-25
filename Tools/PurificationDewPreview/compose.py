from pathlib import Path
import sys
from PIL import Image, ImageDraw

root = Path(sys.argv[1])
sheet = Image.new('RGB', (1024, 944), '#0f121d')
draw = ImageDraw.Draw(sheet)
for i, age in enumerate((6, 20, 38, 80)):
    image = Image.open(root / f'dew-{age:03}.png').convert('RGB')
    x, y = (i % 2) * 512, (i // 2) * 472
    sheet.paste(image, (x, y + 24))
    draw.text((x + 12, y + 6), f'FNA + KL Bloom | age {age} ticks | tapered trail', fill='#c3d0e3')
sheet.save(root / 'dew-sequence.png')
frames = [Image.open(path).convert('RGB') for path in sorted(root.glob('motion-*.png'))]
frames[0].save(root / 'purification-dew.gif', save_all=True, append_images=frames[1:], duration=33, loop=0, disposal=2)
print(root / 'dew-sequence.png')

comparison = Image.new('RGB', (1536, 480), '#0f121d')
labels = [('Before', root / 'before-taper/dew-038.png'),
          ('Tapered texture + shared Bloom', root / 'dew-038.png'),
          ('Dark background', root / 'dew-dark.png')]
for i, (label, path) in enumerate(labels):
    if path.exists(): comparison.paste(Image.open(path).convert('RGB'), (512 * i, 32))
    ImageDraw.Draw(comparison).text((512 * i + 12, 10), label, fill='#c3d0e3')
comparison.save(root / 'taper-comparison.png')

detail = Image.new('RGB', (576, 580), '#0f121d')
for i, (name, label) in enumerate([('dew-038.png', 'Terrain'), ('dew-dark.png', 'Dark / KL Bloom')]):
    crop = Image.open(root / name).convert('RGB').crop((216, 120, 312, 400)).resize((192, 560), Image.Resampling.NEAREST)
    detail.paste(crop, (i * 288 + 48, 20))
    ImageDraw.Draw(detail).text((i * 288 + 12, 4), label + ' | 2x', fill='#c3d0e3')
detail.save(root / 'taper-detail.png')
