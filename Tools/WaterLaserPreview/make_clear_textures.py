"""Create optional clear-water atlases without modifying source PNGs (Pillow)."""
import argparse
from functools import lru_cache
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "ElainaModSkills/Skills/Water"
OUT = Path(__file__).resolve().parent / "output/clear-textures"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


@lru_cache(maxsize=65536)
def recolor(pixel, foam):
    r, g, b, a = pixel
    if a == 0:
        return (196, 237, 251, 0)
    luminance = .2126 * r + .7152 * g + .0722 * b
    if foam:
        t = max(0., min(1., (luminance - 85.) / 105.))
        low, high = (166, 222, 245), (244, 253, 255)
        opacity = .78 + .12 * t
    else:
        t = max(0., min(1., (luminance - 170.) / 85.))
        low, high = (166, 222, 243), (255, 255, 255)
        opacity = .62 + .38 * t
    rgb = tuple(round(lo + (hi - lo) * t) for lo, hi in zip(low, high))
    return (*rgb, max(1, round(a * opacity)))


def compose_fna():
    preview = ROOT / ".vissandbox/water-laser-preview"
    sheet = Image.new("RGB", (840, 980), (24, 33, 43))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 22)
    small = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 17)
    draw.text((22, 14), "水射线终点 · FNA 离线预览", font=font, fill="white")
    draw.text((22, 48), "同一帧、同一随机种子；仅替换预览器中的贴图", font=small, fill=(192, 214, 229))
    for row, (name, label) in enumerate((("day-60", "白天"), ("night", "夜晚"))):
        y = 88 + row * 444
        for col, (folder, variant) in enumerate((("out", "原贴图"), ("out-clear", "清透版"))):
            x = 16 + col * 412
            draw.text((x, y), f"{label} · {variant}", font=small, fill="white")
            with Image.open(preview / folder / f"{name}.png") as frame:
                crop = frame.crop((850, 285, 1180, 620)).resize((396, 402), Image.Resampling.NEAREST)
                sheet.paste(crop, (x, y + 30))
    sheet.save(OUT / "fna-comparison.png")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fna-comparison", action="store_true", help="Compose the two completed FNA preview runs.")
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    atlases, report = {}, []
    for number in (4001, 4002):
        source = ASSETS / f"Eff_Splash_{number}.png"
        destination = ASSETS / f"Eff_Splash_{number}_Clear.png"
        original_hash = digest(source)
        original = Image.open(source).convert("RGBA")
        original_pixels = list(original.get_flattened_data())
        pixels = [recolor(p, number == 4002) for p in original_pixels]
        edited = Image.new("RGBA", original.size)
        edited.putdata(pixels)
        edited.save(destination, optimize=True)
        saved = Image.open(destination).convert("RGBA")
        assert saved.size == original.size
        assert all((a[3] == 0) == (b[3] == 0) and b[3] <= a[3]
                   for a, b in zip(original_pixels, saved.get_flattened_data()))
        assert digest(source) == original_hash
        atlases[number] = (original, edited)
        report.append({"source": str(source.relative_to(ROOT)), "source_sha256": original_hash,
                       "variant": str(destination.relative_to(ROOT)), "variant_sha256": digest(destination),
                       "size": list(saved.size), "transparent_silhouette_preserved": True,
                       "source_unchanged": True})

    # Selected actual animation frames on both backgrounds at a readable size.
    sheet = Image.new("RGB", (1040, 650), (24, 33, 43))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 20)
    small = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 16)
    draw.text((24, 14), "水射线粒子 · 原贴图 / 清透版（独立新文件）", font=font, fill="white")
    for column, label in enumerate(("原贴图 · 白天", "清透版 · 白天", "原贴图 · 夜晚", "清透版 · 夜晚")):
        x = 16 + column * 256
        draw.text((x + 8, 52), label, font=small, fill=(214, 232, 242))
        bg = (67, 134, 141) if column < 2 else (12, 23, 34)
        draw.rectangle((x, 84, x + 247, 620), fill=bg)
        splash, foam = atlases[4001][column % 2], atlases[4002][column % 2]
        for row, frame in enumerate((0, 2, 4)):
            crop = splash.crop((0, frame * 1024 // 6, 512, (frame + 1) * 1024 // 6))
            crop.thumbnail((228, 86), Image.Resampling.LANCZOS)
            sheet.paste(crop, (x + 10, 108 + row * 100), crop)
        for row, frame in enumerate((2, 6, 10)):
            crop = foam.crop((frame % 4 * 128, frame // 4 * 128,
                              (frame % 4 + 1) * 128, (frame // 4 + 1) * 128))
            crop = crop.resize((96, 96), Image.Resampling.LANCZOS)
            sheet.paste(crop, (x + 24 + row % 2 * 96, 400 + row // 2 * 100), crop)
    sheet.save(OUT / "atlas-comparison.png")
    (OUT / "validation.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(report, indent=2, ensure_ascii=False))
    if args.fna_comparison:
        compose_fna()


if __name__ == "__main__":
    main()
