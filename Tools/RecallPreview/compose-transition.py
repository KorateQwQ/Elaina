"""Compose production FNA transition frames; backgrounds are illustrative, not game captures."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2] / '.vissandbox/recall-preview/transition'
samples = [5, 6, 12, 18, 24, 32]
sheet = Image.new('RGB', (640 * 3, 390 * 6), '#191820')
draw = ImageDraw.Draw(sheet)
for col, name in enumerate(['near', 'medium', 'far']):
    frames = [Image.open(root / f'{name}-{i:02}.png').convert('RGB') for i in range(37)]
    for row, index in enumerate(samples):
        x, y = col * 640, row * 390
        sheet.paste(frames[index], (x, y + 30))
        label = 'before travel' if index < 6 else f'{(index - 6) / 60:.3f}s after travel'
        draw.text((x + 12, y + 8), f'{name}: {label}', fill='white')
    animation = [frames[0]] * 24 + frames + [frames[-1]] * 30
    animation[0].save(root / f'{name}.webp', save_all=True, append_images=animation[1:], duration=17, loop=0, lossless=True)
sheet.save(root / 'contact-sheet.jpg', quality=92)
print(root / 'contact-sheet.jpg')
