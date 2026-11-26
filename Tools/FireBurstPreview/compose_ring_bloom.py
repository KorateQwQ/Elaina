"""原生尺寸局部对照：python compose_ring_bloom.py <out> [修复前目录]。"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

out = Path(sys.argv[1]) / "ring-bloom"
crop = (720, 300, 1200, 780)

def pair(left, right, name, labels):
    sheet = Image.new("RGB", (960, 504), (8, 12, 16))
    draw = ImageDraw.Draw(sheet)
    for index, path in enumerate([left, right]):
        with Image.open(path) as source:
            sheet.paste(source.crop(crop), (index * 480, 24))
        draw.text((index * 480 + 12, 6), labels[index], fill="white")
    sheet.save(out / name)

pair(out / "night-z1-t002-off.png", out / "night-z1-t002-on.png", "bloom-off-on.png", ["Bloom off", "Bloom on"])
if len(sys.argv) > 2:
    before = Path(sys.argv[2])
    pair(before / "night-z1-t002-on.png", out / "night-z1-t002-on.png", "before-after.png", ["Before", "After"])
    for background in ["night", "day"]:
        for zoom in ["1", "1.25"]:
            for age in [12, 20, 24, 40, 300]:
                for state in ["on", "off"]:
                    name = f"{background}-z{zoom}-t{age:03}-{state}.png"
                    with Image.open(before / name) as old, Image.open(out / name) as new:
                        if old.tobytes() != new.tobytes():
                            raise SystemExit(f"Steady-stage visual changed: {name}")
    print("PASS: all post-ignition frames match the baseline pixel for pixel")
