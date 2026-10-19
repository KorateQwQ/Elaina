"""Hand-authored Painkiller item sprite (24x34, matching its catalog size).

A round-shouldered potion bottle in the family's lavender glass, two-thirds full
of a warm orange tonic with a cream cross floating in it, the way the star and
moon potions carry their own mark in the liquid. The potion family's rose bow
is tied round the neck. All pixels are opaque with no antialiasing.

python Tools/PotionPixel/GeneratePainkillerPixel.py           # study preview only
python Tools/PotionPixel/GeneratePainkillerPixel.py --apply   # also write live assets
Requires Pillow.
"""

import argparse
import json
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "ElainaModAlchemy/item/ExampleAssets"
UI = ROOT / "ElainaModAlchemy/UI/Assets"
LIVE = ASSETS / "Painkiller_Pixel.png"
STUDY = ROOT / "Tools/PotionPixel/output/Painkiller_Pixel_New.png"
COMPARE = ROOT / "Tools/PotionPixel/output/Painkiller_Compare.png"
PREVIEW = ROOT / "Tools/PotionPixel/preview"
WIDTH, HEIGHT = 24, 34
PALETTE = {
    ".": "#00000000",
    "O": "#000000",  # Outer silhouette: pure black so it reads on the pale notebook card.
    "a": "#51415f",  # Family inner lines.
    "K": "#7b5761",  # Cork: shadow, body, lit top.
    "k": "#b58b72",
    "j": "#e1bd8d",
    "J": "#f4dbb0",
    "G": "#8b7a9d",  # Empty glass: shade, body, light, glint.
    "g": "#aaa0c7",
    "l": "#d9d3ed",
    "w": "#f6faf3",
    "1": "#7e2f2c",  # Orange tonic: deepest, shade, body, light, surface.
    "2": "#b9483a",
    "3": "#e2733f",
    "4": "#f5a052",
    "5": "#ffd08a",
    "C": "#fff4dc",  # Floating cross: lit and shaded.
    "c": "#f6c48c",
    "P": "#8a2c48",  # Rose ribbon: shadow, body, light.
    "p": "#cf4d68",
    "q": "#f4909a",
}

# Bottle silhouette: outline-inclusive spans (first x, last x) per row.
BODY = {10: (7, 16), 11: (5, 18), 12: (4, 19), 13: (3, 20),
        **{y: (2, 21) for y in range(14, 30)},
        30: (3, 20), 31: (4, 19), 32: (5, 18), 33: (7, 16)}
NECK = {8: (8, 15), 9: (8, 15)}
SURFACE = 17
CROSS = (
    "..CC..",
    "..CCc.",
    "CCCCCC",
    "CCCCCc",
    "..CCc.",
    "..cc..",
)
BUBBLES = ((6, 27), (16, 20), (17, 25), (15, 28))
TOP = (
    "..aaaaaaaa..",   # 0 cork
    ".aJJJJJJJJa.",
    ".ajjkjjjkja.",
    ".akkkkkkkKa.",
    ".aKkKkKKKKa.",
    "aaaaaaaaaaaa",   # 5 rim
    "awllllllgGGa",
    "aaggggggGGaa",
)
# Bow tied over the neck; its short tails fall onto the shoulder glass.
BOW = (
    "aaa.aa.aaa",
    "aqpaqqapPa",
    "apPapPaPPa",
    ".aaapPaaa.",
    "..apaaPa..",
    "..aa..aa..",
)


def paint(grid, x0, y0, rows):
    for dy, row in enumerate(rows):
        for dx, symbol in enumerate(row):
            if symbol != "." and 0 <= x0 + dx < WIDTH and 0 <= y0 + dy < HEIGHT:
                grid[y0 + dy][x0 + dx] = symbol


def build_grid():
    grid = [["."] * WIDTH for _ in range(HEIGHT)]
    for y, (x0, x1) in {**NECK, **BODY}.items():
        for x in range(x0, x1 + 1):
            if x in (x0, x1) or y == max(BODY) or (y == 10 and not x0 < x < x1):
                grid[y][x] = "a"
            # Light falls from the upper left: the tonic darkens to the right and the floor.
            elif y == SURFACE:
                grid[y][x] = "5" if x < x1 - 3 else "4"
            elif y > SURFACE:
                if x >= x1 - 2 or y >= max(BODY) - 1:
                    grid[y][x] = "1"
                elif x >= x1 - 5 or y >= max(BODY) - 3:
                    grid[y][x] = "2"
                elif x <= x0 + 2:
                    grid[y][x] = "4"
                else:
                    grid[y][x] = "3"
            elif x >= x1 - 2:
                grid[y][x] = "G"
            elif x <= x0 + 1:
                grid[y][x] = "l"
            else:
                grid[y][x] = "g" if y <= 12 or x >= x1 - 4 else "l"
    paint(grid, 9, 20, CROSS)
    for x, y in BUBBLES:
        grid[y][x] = "5"
    # Glint down the left of the glass, carried on into the tonic, and a shoulder spark.
    for x, y in ((6, 11), (5, 12), (4, 13), (3, 14), (3, 15), (3, 16), (12, 10)):
        grid[y][x] = "w"
    for x, y in ((3, 19), (3, 20), (3, 21), (3, 23)):
        grid[y][x] = "5"
    paint(grid, 6, 0, TOP)
    paint(grid, 7, 7, BOW)
    return grid


def to_rgba(symbol):
    value = PALETTE[symbol].lstrip("#")
    value += "ff" * (len(value) == 6)
    return tuple(int(value[i:i + 2], 16) for i in range(0, 8, 2))


def blacken_silhouette(grid):
    """Every opaque pixel touching transparency or the canvas edge becomes pure black."""
    def clear(x, y):
        return not (0 <= x < WIDTH and 0 <= y < HEIGHT) or grid[y][x] == "."
    edge = [(x, y) for y in range(HEIGHT) for x in range(WIDTH) if grid[y][x] != "."
            and any(clear(nx, ny) for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))]
    for x, y in edge:
        grid[y][x] = "O"
    return grid


def build_sprite():
    grid = blacken_silhouette(build_grid())
    image = Image.new("RGBA", (WIDTH, HEIGHT))
    for y, row in enumerate(grid):
        for x, symbol in enumerate(row):
            image.putpixel((x, y), to_rgba(symbol))
    return image, grid


def outline_ring(image):
    """One-pixel four-neighbour ring on a +1 padded canvas, as bake_redesign.py bakes it."""
    w, h = image.size
    alpha = image.getchannel("A").load()
    ring = Image.new("RGBA", (w + 2, h + 2))
    for y in range(h + 2):
        for x in range(w + 2):
            sx, sy = x - 1, y - 1
            if 0 <= sx < w and 0 <= sy < h and alpha[sx, sy]:
                continue
            if any(0 <= nx < w and 0 <= ny < h and alpha[nx, ny]
                   for nx, ny in ((sx - 1, sy), (sx + 1, sy), (sx, sy - 1), (sx, sy + 1))):
                ring.putpixel((x, y), (255, 255, 255, 255))
    return ring


def compare(sprites, scales=(1, 2, 6), backgrounds=((58, 52, 82), (236, 226, 206))):
    pad = 4
    cell_w = max(s.width for s in sprites)
    cell_h = max(s.height for s in sprites)
    blocks = []
    for bg in backgrounds:
        for scale in scales:
            w = (cell_w * scale + pad * scale) * len(sprites) + pad * scale
            h = cell_h * scale + pad * 2 * scale
            block = Image.new("RGBA", (w, h), bg + (255,))
            x = pad * scale
            for sprite in sprites:
                big = sprite.resize((sprite.width * scale, sprite.height * scale), Image.Resampling.NEAREST)
                block.alpha_composite(big, (x, h - big.height - pad * scale))
                x += cell_w * scale + pad * scale
            blocks.append(block)
    out = Image.new("RGBA", (max(b.width for b in blocks), sum(b.height for b in blocks)))
    y = 0
    for block in blocks:
        out.alpha_composite(block, (0, y))
        y += block.height
    return out


def apply(image, grid):
    image.save(LIVE)
    image.save(ROOT / "ElainaModAlchemy/item/Painkiller.png")
    outline_ring(image).save(UI / "Outlines/Painkiller_Pixel.png", optimize=True)
    image.resize((WIDTH * 8, HEIGHT * 8), Image.Resampling.NEAREST).save(PREVIEW / "Painkiller_Pixel_x8.png")
    used = sorted({s for row in grid for s in row} - {"."})
    draft = {
        "name": "Painkiller - lavender potion bottle of orange tonic with a floating cream cross",
        "source_note": "Hand-authored. Rebuild with Tools/PotionPixel/GeneratePainkillerPixel.py --apply.",
        "size": [WIDTH, HEIGHT],
        "palette": {".": "#00000000", **{s: PALETTE[s] for s in used}},
        "pixels": ["".join(row) for row in grid],
    }
    (PREVIEW / "Painkiller.pixel.json").write_text(json.dumps(draft, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--apply", action="store_true", help="write the live sprite, outline and previews")
    args = parser.parse_args()
    sprite, grid = build_sprite()
    old = Image.open(LIVE).convert("RGBA")
    STUDY.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(STUDY)
    compare([old, sprite]).save(COMPARE)
    if args.apply:
        apply(sprite, grid)
    colors = sum(1 for _, c in sprite.getcolors() if c[3])
    print(f"Painkiller pixel: {sprite.width}x{sprite.height}, {colors} opaque colors, bounds {sprite.getbbox()}"
          + (", applied" if args.apply else ", study only"))


if __name__ == "__main__":
    main()
