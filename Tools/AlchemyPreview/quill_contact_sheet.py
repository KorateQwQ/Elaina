from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
folder = root / '.vissandbox/alchemy-preview/output/quill-frames'
frames = [0, 12, 20, 26, 80, 96]
labels = ['Initial selection', 'Selection flight', 'Scroll during flight',
          'Retarget during flight', 'Settled on selected card', 'Scroll after arrival']
sheet = Image.new('RGB', (1200, 1494), '#18151f')
draw = ImageDraw.Draw(sheet)
for i, (frame, label) in enumerate(zip(frames, labels)):
    image = Image.open(folder / f'quill-1280-{frame:03}.png').convert('RGB')
    crop = image.crop((199, 146, 799, 612))
    x, y = i % 2 * 600, i // 2 * 498
    draw.text((x + 12, y + 10), f'{frame / 60:.2f}s - {label}', fill='#ede2f4')
    sheet.paste(crop, (x, y + 32))
sheet.save(folder / 'quill-contact-sheet.png')
animation = [Image.open(path).convert('RGB').crop((199, 146, 799, 612))
             for path in sorted(folder.glob('quill-1280-*.png'))]
animation[0].save(folder / 'quill-motion.webp', save_all=True, append_images=animation[1:],
                  duration=17, loop=0, quality=90, method=4)
print(folder / 'quill-contact-sheet.png')
