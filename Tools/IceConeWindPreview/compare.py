"""Compare opaque FNA captures; never apply alpha a second time to render output."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[2]
work = root / ".vissandbox/ice-cone-wind-preview"
font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 20)
small = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 16)
canvas = Image.new("RGB", (840, 350), (33, 49, 64))
draw = ImageDraw.Draw(canvas)
draw.text((20, 10), "FNA 离线对照 · 第 8 tick · 像素放大 2 倍", font=font, fill="white")
for column, (folder, label) in enumerate((("legacy-alpha", "修复前"), ("out", "修复后"))):
    source = Image.open(work / folder / "overlap-stages.png").convert("RGB")
    draw.text((column * 420 + 170, 45), label, font=font, fill="white")
    for row, copies in enumerate((1, 6)):
        source_y = (0 if copies == 1 else 2) * 110
        crop = source.crop((420, source_y + 15, 630, source_y + 95))
        canvas.paste(crop.resize((420, 160), Image.Resampling.NEAREST), (column * 420, 70 + row * 130))
        draw.text((column * 420 + 12, 80 + row * 130), f"{copies} 层", font=small, fill=(180, 194, 207))
canvas.save(work / "comparison.png")
print(work / "comparison.png")
