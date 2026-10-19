"""Hand-authored Moon Dew Elixir item sprite (33x40, matching its catalog size).

A dewdrop-shaped flask in the family's lavender glass. The violet elixir has a
curved surface seen slightly from above, glows outward from a floating pale-gold
crescent, and carries star specks, a four-point sparkle and bubbles. The family's
pink bow is tied at the neck with mismatched loops, a short left tail and a long
right tail rippling over the shoulder; a dew drop falls from the knot towards a
ringed surface. All pixels are opaque, no antialiasing.

python Tools/PotionPixel/GenerateMoonDewElixirPixel.py           # study preview only
python Tools/PotionPixel/GenerateMoonDewElixirPixel.py --apply   # also write live assets
Requires Pillow.
"""

import argparse
import json
import math
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "ElainaModAlchemy/item/ExampleAssets"
UI = ROOT / "ElainaModAlchemy/UI/Assets"
LIVE = ASSETS / "MoonDewElixir_Pixel.png"
STUDY = ROOT / "Tools/PotionPixel/output/MoonDewElixir_Pixel_New.png"
COMPARE = ROOT / "Tools/PotionPixel/output/MoonDewElixir_Compare.png"
PREVIEW = ROOT / "Tools/PotionPixel/preview"
ROUND1 = ROOT / "Tools/PotionBottleDesigns/output/round1/pixel"
WIDTH, HEIGHT = 33, 40
PALETTE = {
    ".": "#00000000",
    "O": "#000000",  # Outer silhouette: pure black so it reads on the pale notebook card.
    "a": "#51415f",  # Family inner lines.
    "K": "#7b5761",  # Cork: shadow, body, lit top.
    "k": "#b58b72",
    "j": "#e1bd8d",
    "J": "#f4dbb0",
    "E": "#88719b",  # Empty glass: edge, shade, body, glint.
    "G": "#a79ac5",
    "g": "#d4ccea",
    "w": "#fff5f1",
    "0": "#2c2560",  # Elixir: deepest, dark, middle, violet, light, surface.
    "1": "#3f3588",
    "2": "#5a49b3",
    "3": "#7e68d6",
    "4": "#a993f0",
    "5": "#d6c9ff",
    "Y": "#fff3c4",  # Crescent: lit and shaded.
    "y": "#efc979",
    "P": "#ab648f",  # Pink ribbon: shadow, body, light.
    "p": "#e697bd",
    "q": "#ffd1e3",
}

CX = 14
# Dewdrop flask: half-width per row (outline included); narrow neck flaring into a round belly.
HALF = {7: 3, 8: 3, 9: 3, 10: 3, 11: 3, 12: 4, 13: 5, 14: 6, 15: 7, 16: 8, 17: 9,
        18: 10, 19: 11, 20: 11, 21: 12, 22: 12, **{y: 13 for y in range(23, 30)},
        30: 12, 31: 12, 32: 12, 33: 11, 34: 10, 35: 9, 36: 8, 37: 7, 38: 5}
BOTTOM = max(HALF)
SURFACE = 19
MOON = (12, 28)

CRESCENT = (
    "...yYY.",
    ".yYY...",
    "yYY....",
    "yYY....",
    "yYY....",
    "yYY....",
    ".yYY...",
    "...yYY.",
)
SPARKLE = (
    "..w..",
    "..5..",
    "w5w5w",
    "..5..",
    "..w..",
)
BUBBLES = ((21, 33, "4"), (19, 22, "5"), (8, 34, "4"), (23, 27, "4"), (17, 31, "3"))
SPECKS = ((6, 25), (17, 26), (10, 36), (24, 31), (15, 21))
TOP = (
    ".aaaaaaa.",   # 0 cork
    "aJJJJJJJa",
    "ajjkjjjja",
    "akkkkkkKa",
    "aKkKkKKKa",
    "aaaaaaaaa",   # 5 rim
)
# Neck, bow and the long right tail, drawn whole from row 6; "." keeps the glass below.
# A small drooping left loop, a big lifted right loop, a knot on the neck, a short
# left tail, and a right tail that ripples down over the shoulder to a notched end.
UPPER = (
    "...........awgggGa.....aaa.......",
    "...........awgggGa...aaqqpa......",
    ".....aaa...awgggGa.aaqpppPa......",
    "...aaqqpa.aaaaaaaaaqpaaPPa.......",
    "..apqaaPaapqaqqpapqpaPPPa........",
    "..aPpPPPaaPpapppPaPPPPPa.........",
    "...aPPPPaaaaaPpPaaaaaaa..........",
    "....aaaa...apPaPpaaqpa...........",
    "..........aPPa.aPpaPppa..........",
    ".........aPPa...aPPapPPaa........",
    ".........aaa.....aaaaPPppa.......",
    "....................aaPqPPa......",
    ".......................aPpa......",
    "........................apPa.....",
    ".........................aa......",
)


def paint(grid, x0, y0, rows):
    for dy, row in enumerate(rows):
        for dx, symbol in enumerate(row):
            if symbol != "." and 0 <= x0 + dx < WIDTH and 0 <= y0 + dy < HEIGHT:
                grid[y0 + dy][x0 + dx] = symbol


def elixir(x, y, x0, x1):
    """Light from the upper left, deepening downwards, glowing round the crescent."""
    depth = (y - SURFACE) / (BOTTOM - SURFACE)
    side = (x - CX) / max(1, (x1 - x0) / 2)
    glow = max(0.0, 1 - math.hypot(x - MOON[0] - 1, (y - MOON[1]) * 0.9) / 8)
    value = 2.9 - 2.6 * depth - 1.1 * side + 1.6 * glow
    if x == x1 - 1 or y == BOTTOM - 1:
        value -= 1
    return str(max(0, min(4, round(value))))


def build_grid():
    grid = [["."] * WIDTH for _ in range(HEIGHT)]
    for y, half in HALF.items():
        x0, x1 = CX - half, CX + half
        for x in range(x0, x1 + 1):
            if x in (x0, x1) or y == BOTTOM:
                grid[y][x] = "a"
            elif y > SURFACE:
                grid[y][x] = elixir(x, y, x0, x1)
            elif y == SURFACE:
                grid[y][x] = "5" if x < x1 - 4 else "4"
            elif x >= x1 - 2:
                grid[y][x] = "G" if x == x1 - 1 else "g"
            else:
                grid[y][x] = "g"
        # Outline steps get a soft edge pixel so the glass curve reads round.
        if y < SURFACE and y > 10 and half > 4:
            grid[y][x1 - 1] = "E"
    # The far rim of the surface ellipse shows through the glass.
    for x in range(CX - 7, CX + 7):
        grid[SURFACE - 1][x] = "4"
    for x in range(CX - 4, CX + 4):
        grid[SURFACE - 1][x] = "5"
    # Reflected light along the lower-right belly.
    for x, y in ((24, 29), (24, 30), (23, 32), (22, 33), (21, 34)):
        grid[y][x] = "3"
    paint(grid, MOON[0] - 3, MOON[1] - 4, CRESCENT)
    paint(grid, 18, 24, SPARKLE)
    for x, y, c in BUBBLES:
        grid[y][x] = c
    for x, y in SPECKS:
        grid[y][x] = "Y"
    # Window glint on the shoulder kept off the outline, then a long arc down the belly.
    for x, y in ((10, 13), (9, 14), (9, 15), (8, 16), (7, 17)):
        grid[y][x] = "w"
    for x, y in ((4, 22), (3, 23), (3, 24), (3, 25), (3, 26), (4, 27), (5, 30), (6, 32)):
        grid[y][x] = "5"
    grid[21][5] = "w"
    paint(grid, CX - 4, 0, TOP)
    paint(grid, 0, 6, UPPER)
    # A dew drop falls from the knot; the surface rings where it will land.
    grid[14][14] = "4"
    grid[15][14] = "5"
    for x in (12, 16):
        grid[SURFACE - 2][x] = "5"
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
    image.save(ROOT / "ElainaModAlchemy/item/MoonDewElixir.png")
    image.save(ROUND1 / "MoonDewElixir.png")
    outline_ring(image).save(UI / "Outlines/MoonDewElixir_Pixel.png", optimize=True)
    image.resize((WIDTH * 8, HEIGHT * 8), Image.Resampling.NEAREST).save(PREVIEW / "MoonDewElixir_Pixel_x8.png")
    used = sorted({s for row in grid for s in row} - {"."})
    draft = {
        "name": "MoonDewElixir - dewdrop flask of glowing violet elixir with a floating crescent",
        "source_note": "Hand-authored. Rebuild with Tools/PotionPixel/GenerateMoonDewElixirPixel.py --apply.",
        "size": [WIDTH, HEIGHT],
        "palette": {".": "#00000000", **{s: PALETTE[s] for s in used}},
        "pixels": ["".join(row) for row in grid],
    }
    (ROUND1 / "MoonDewElixir.pixel.json").write_text(json.dumps(draft, indent=2) + "\n", encoding="utf-8")


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
    print(f"MoonDewElixir pixel: {sprite.width}x{sprite.height}, {colors} opaque colors, bounds {sprite.getbbox()}"
          + (", applied" if args.apply else ", study only"))


if __name__ == "__main__":
    main()
