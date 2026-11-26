"""把施法演出预览的动画帧合成为 GIF：python compose_cast.py <out 目录>"""
import sys
from pathlib import Path
from PIL import Image

out = Path(sys.argv[1]) / "cast"
frames = [Image.open(p).convert("RGB") for p in sorted((out / "frames").glob("*.png"))]
if not frames:
    raise SystemExit("no cast frames")
small = [f.resize((f.width * 2 // 3, f.height * 2 // 3), Image.LANCZOS) for f in frames]
small[0].save(out / "cast.gif", save_all=True, append_images=small[1:], duration=17, loop=0)
print(f"cast: {len(frames)} frames -> cast.gif")

# 原生像素裁切：左为起手，右为悬停后的下挥；不对截图二次缩放。
detail = Image.new("RGB", (600, 260))
for index, name in enumerate(["tail-start-day-z2", "tail-strike-night-z2"]):
    with Image.open(out / f"{name}.png") as source:
        detail.paste(source.crop((820, 390, 1120, 650)), (index * 300, 0))
detail.save(out / "wand-tail-detail.png")

scales = Image.new("RGB", (900, 520))
for row, background in enumerate(["day", "night"]):
    for column, zoom in enumerate(["1", "1.25", "2"]):
        with Image.open(out / f"tail-start-{background}-z{zoom}.png") as source:
            scales.paste(source.crop((820, 390, 1120, 650)), (column * 300, row * 260))
scales.save(out / "wand-tail-scales.png")

with Image.open(out / "impact-stars-night.png") as source:
    source.crop((660, 280, 1260, 800)).save(out / "impact-stars-detail.png")

# 左昼右夜，保留原生像素；同时覆盖天空火种与杖尖。
exposure = Image.new("RGB", (1040, 740))
for index, background in enumerate(["day", "night"]):
    with Image.open(out / f"near-{background}-f30.png") as source:
        exposure.paste(source.crop((700, 80, 1220, 820)), (index * 520, 0))
exposure.save(out / "exposure-day-night.png")
