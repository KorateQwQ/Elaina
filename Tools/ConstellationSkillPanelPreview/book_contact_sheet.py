"""Create a labelled FNA contact sheet and animation from the generated production-code frames."""
from pathlib import Path
from PIL import Image, ImageDraw

repo = Path(__file__).resolve().parents[2]
out = repo/'.vissandbox/constellation-skill-panel/output'
frames = out/'book-frames'
indices = [4, 12, 16, 22, 28, 34, 48, 52, 56, 60, 66, 73]
sheet = Image.new('RGB', (1600, 4*360), '#14151c')
draw = ImageDraw.Draw(sheet)
for i, frame in enumerate(indices):
    pic = Image.open(frames/f'book-1600x1000-1.00-{frame:03}.png').convert('RGB')
    pic.thumbnail((520, 325), Image.Resampling.LANCZOS)
    x, y = (i % 3)*533, (i//3)*360
    sheet.paste(pic, (x, y+28))
    draw.text((x+12, y+8), f'{"OPEN" if frame < 46 else "CLOSE"} | frame {frame:03} | {frame/60:.3f}s', fill='#e8d8f6')
sheet.save(out/'book-contact-sheet.jpg', quality=94)
images = []
for path in sorted(frames.glob('book-1600x1000-1.00-*.png')):
    pic = Image.open(path).convert('RGB'); pic.thumbnail((960, 600), Image.Resampling.LANCZOS)
    images.append(pic)
images[0].save(out/'book-animation.webp', save_all=True, append_images=images[1:], duration=17, loop=0, quality=85)
print(out/'book-contact-sheet.jpg')
print(out/'book-animation.webp')
