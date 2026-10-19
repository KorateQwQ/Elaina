import csv
import sys
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(sys.argv[1])
rows = list(csv.DictReader((root / 'frames.csv').open(encoding='utf-8')))
sheet = Image.new('RGB', (9 * 192, 3 * 244), '#101b2a')
draw = ImageDraw.Draw(sheet)
for i, row in enumerate(rows):
    x, y = (i % 9) * 192, (i // 9) * 244
    sheet.paste(Image.open(root / row['file']).convert('RGB'), (x, y + 22))
    draw.text((x + 5, y + 4), row['scene'], fill='white')
    draw.text((x + 5, y + 217), f"tick {row['tick']} | fade {float(row['fade']):.2f}", fill='#bad8ff')
sheet.save(root / 'contact-sheet.png')
print(root / 'contact-sheet.png')
