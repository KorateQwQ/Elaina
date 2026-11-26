"""Compare native FNA frames before/after the Bloom optimization; no rescaling."""
import json
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageStat

root = Path(__file__).resolve().parents[2] / ".vissandbox/bloom-resolution"
before, after = root / "visual-before", root / "visual-after"
rows = []
for path in sorted(before.glob("*.png")):
    if path.stem.endswith("-crop"):
        continue
    a = Image.open(path).convert("RGB")
    b = Image.open(after / path.name).convert("RGB")
    delta = ImageChops.difference(a, b)
    # Ignore the flat background for the mean; otherwise a small bright feature
    # could change substantially while a full-screen average appears tiny.
    def mask(image):
        channels = ImageChops.difference(image, Image.new("RGB", image.size, image.getpixel((0, 0)))).split()
        return ImageChops.lighter(ImageChops.lighter(channels[0], channels[1]), channels[2]).point(lambda p: 255 if p else 0)
    active = ImageChops.lighter(mask(a), mask(b))
    row = dict(image=path.name, max_channel_delta=max(hi for lo, hi in delta.getextrema()),
               active_mean_delta=sum(ImageStat.Stat(delta, active).mean) / 3)
    rows.append(row)
    print(row)
    if row["max_channel_delta"] > 8 or row["active_mean_delta"] > 1:
        raise AssertionError(f"Bloom appearance changed beyond tolerance: {row}")
(root / "visual-comparison.json").write_text(json.dumps(rows, indent=2), encoding="utf-8")
font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 18)
sheet = Image.new("RGB", (980, 1020), "#20232c")
draw = ImageDraw.Draw(sheet)
draw.text((12, 8), "FNA 离线预览 · 原生像素局部 · 左优化前 / 右优化后", font=font, fill="white")
for row, (kind, label) in enumerate([("star", "十字星"), ("fire", "火焰粒子"), ("trail", "飞弹拖尾")]):
    y = 40 + row * 325
    draw.text((12, y), label, font=font, fill="white")
    for col, folder in enumerate([before, after]):
        with Image.open(folder / f"{kind}-1920x1080-point-crop.png") as image:
            sheet.paste(image.convert("RGB"), (col * 490, y + 25))
sheet.save(root / "performance-comparison.png")
