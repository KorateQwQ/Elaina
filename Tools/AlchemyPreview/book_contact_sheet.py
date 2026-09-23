"""Create labelled contact sheets and a looping animation from actual FNA book frames."""
from pathlib import Path
from PIL import Image, ImageDraw

repo = Path(__file__).resolve().parents[2]
out = repo/'.vissandbox/alchemy-preview/output'
frames = out/'book-frames'
indices = [4, 12, 18, 24, 34, 45, 48, 54, 60, 66, 73, 95, 97, 105, 107, 121, 123, 143]
sheet = Image.new('RGB', (1600, 6*330), '#14151c')
draw = ImageDraw.Draw(sheet)
for i, frame in enumerate(indices):
    pic = Image.open(frames/f'alchemy-book-1280x720-1.00-{frame:03}.png').convert('RGB')
    pic.thumbnail((520, 292), Image.Resampling.LANCZOS)
    x, y = i % 3*533, i//3*330
    sheet.paste(pic, (x, y+30))
    phase = 'OPEN' if frame < 46 else 'CLOSE' if frame < 80 else 'REVERSE'
    draw.text((x+12, y+9), f'{phase} | frame {frame:03} | {frame/60:.3f}s', fill='#e8d8f6')
sheet.save(out/'alchemy-book-contact-sheet.jpg', quality=94)

sheet = Image.new('RGB', (1600, 3*380), '#14151c')
draw = ImageDraw.Draw(sheet)
for row, (width, height, scale) in enumerate([(1600,1000,'1.00'),(1280,720,'1.00'),(1600,1000,'1.25')]):
    for column, frame in enumerate([12, 24, 34]):
        pic = Image.open(frames/f'alchemy-book-{width}x{height}-{scale}-{frame:03}.png').convert('RGB')
        pic.thumbnail((520, 342), Image.Resampling.LANCZOS)
        x, y = column*533, row*380
        sheet.paste(pic, (x, y+30))
        draw.text((x+12, y+9), f'{width}x{height} | UI {scale} | frame {frame:03}', fill='#e8d8f6')
sheet.save(out/'alchemy-book-scale-sheet.jpg', quality=94)

images = []
for path in sorted(frames.glob('alchemy-book-1280x720-1.00-*.png')):
    pic = Image.open(path).convert('RGB')
    pic.thumbnail((960, 540), Image.Resampling.LANCZOS)
    images.append(pic)
images[0].save(out/'alchemy-book-animation.webp', save_all=True, append_images=images[1:], duration=17, loop=0, quality=85)
for name in ['alchemy-book-contact-sheet.jpg', 'alchemy-book-scale-sheet.jpg', 'alchemy-book-animation.webp']:
    print(out/name)
