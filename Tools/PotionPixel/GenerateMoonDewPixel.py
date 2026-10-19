"""Hand-authored Moon Dew material sprite (20x28, matching its catalog size).

A small, round, mirror-symmetric flask of bottled water steeped with moonglow: a cool
blue that deepens downwards and glows around a pale-gold crescent reflection.
The dew climbs the glass walls in a meniscus, a moonglow blossom is knotted
into the twine at the neck, and a four-point glint and a few motes rise through
the dew. All pixels are opaque, no antialiasing.

python Tools/PotionPixel/GenerateMoonDewPixel.py           # previews in Tools/PotionPixel/output
python Tools/PotionPixel/GenerateMoonDewPixel.py --apply   # also write live assets
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
LIVE = ASSETS / "MoonDew_Pixel.png"
OUT = ROOT / "Tools/PotionPixel/output"
WIDTH, HEIGHT = 20, 28
PALETTE = {
    ".": "#00000000",
    "O": "#000000",  # Outer silhouette: pure black so it reads on the pale notebook card.
    "a": "#332f43",  # Material inner lines.
    "K": "#6e4f4a",  # Cork: shadow, body, lit top.
    "k": "#a57c62",
    "j": "#d6b07f",
    "E": "#6f86a6",  # Empty glass: edge, shade, body, glint.
    "G": "#a7bbd6",
    "g": "#cfdcec",
    "w": "#f4faff",
    "0": "#1d3a66",  # Dew: deepest, dark, middle, blue, light, surface.
    "1": "#28578e",
    "2": "#3778b8",
    "3": "#4f9fd8",
    "4": "#86caee",
    "5": "#c6eefb",
    "Y": "#fff6d6",  # Crescent reflection: lit and shaded.
    "y": "#e6c97e",
    "T": "#a98a63",  # Twine.
    "Q": "#f3f9ff",  # Moonglow blossom: petal, heart.
    "h": "#5d8fd6",
}

# First outline x per row; every row is mirrored about x = 9.5 (last x = 19 - first).
# A short 6-wide neck over a round belly, 18 wide at most, on a narrow flat foot.
# The art stands 26 rows tall; the top two rows of the catalog canvas stay clear.
LEFT = {7: 7, 8: 7, 9: 7, 10: 5, 11: 4, 12: 3, 13: 2, 14: 2, 15: 2,
        **{y: 1 for y in range(16, 22)}, 22: 2, 23: 2, 24: 3, 25: 3, 26: 4, 27: 6}
SPANS = {y: (x0, 19 - x0) for y, x0 in LEFT.items()}
BOTTOM = max(SPANS)
# The dew is level in the middle and climbs both glass walls.
SURFACE = {x: 15 if 3 <= x <= 16 else 14 for x in range(WIDTH)}
MOON = (8, 20)
CRESCENT = (
    "..yYY",
    ".yY..",
    "yY...",
    "yY...",
    "yY...",
    ".yY..",
    "..yYY",
)
TOP = (
    "......aaaaaaaa......",  # 2 cork, centred over the neck
    "......ajjjjjja......",
    "......akkjkkka......",
    "......aKkKkKKa......",
    ".....aaaaaaaaaa.....",  # 6 rim
)
# Twine round the neck, knotted at the front with a small moonglow blossom.
NECK = (
    ".......aaQQaa.......",
    "......TTQhhQTT......",
    ".......aaQQaa.......",
)
MOTES = ((16, 17, "4"), (12, 24, "5"), (6, 25, "4"), (10, 25, "3"))


def paint(grid, x0, y0, rows):
    for dy, row in enumerate(rows):
        for dx, symbol in enumerate(row):
            if symbol != "." and 0 <= x0 + dx < WIDTH and 0 <= y0 + dy < HEIGHT:
                grid[y0 + dy][x0 + dx] = symbol


def dew(x, y, x0, x1):
    """Light from the upper left, deepening downwards, glowing round the crescent."""
    top = SURFACE[x]
    depth = (y - top) / (BOTTOM - top)
    side = (x - 9.5) / ((x1 - x0) / 2)
    glow = max(0.0, 1 - math.hypot(x - MOON[0] - 1, (y - MOON[1]) * 0.9) / 5)
    value = 2.9 - 2.6 * depth - 1.1 * side + 0.9 * glow
    if x == x1 - 1 or y == BOTTOM - 1:
        value -= 1
    return str(max(0, min(4, round(value))))


def build_grid():
    grid = [["."] * WIDTH for _ in range(HEIGHT)]
    mask = {(x, y) for y, (x0, x1) in SPANS.items() for x in range(x0, x1 + 1)}
    for y, (x0, x1) in SPANS.items():
        for x in range(x0, x1 + 1):
            # Any pixel touching the outside is outline, closing every stair step.
            if any(n not in mask for n in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))):
                grid[y][x] = "a"
            elif y > SURFACE[x]:
                grid[y][x] = dew(x, y, x0, x1)
            elif y == SURFACE[x]:
                grid[y][x] = "5" if x < x1 - 3 else "4"
            elif x == x1 - 1:
                grid[y][x] = "E"
            elif x >= x1 - 2 and y > 9:
                grid[y][x] = "G"
            else:
                grid[y][x] = "g"
    paint(grid, MOON[0] - 2, MOON[1] - 3, CRESCENT)
    for x, y, c in MOTES:
        grid[y][x] = c
    # A small four-point glint beside the moon.
    for x, y in ((13, 18), (12, 19), (14, 19), (13, 20)):
        grid[y][x] = "4"
    grid[19][13] = "w"
    # Glint arcing down the left of the glass, carried on into the dew.
    for x, y in ((6, 11), (5, 12), (4, 13), (3, 14)):
        grid[y][x] = "w"
    for x, y in ((2, 17), (2, 18), (2, 19), (3, 23)):
        grid[y][x] = "5"
    # Reflected light along the lower-right belly.
    for x, y in ((17, 20), (17, 21), (16, 23)):
        grid[y][x] = "3"
    paint(grid, 0, 2, TOP)
    paint(grid, 0, 7, NECK)
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


def compare(sprites, scales=(1, 2, 8), backgrounds=((58, 52, 82), (236, 226, 206))):
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
    outline_ring(image).save(UI / "Outlines/MoonDew_Pixel.png", optimize=True)
    used = sorted({s for row in grid for s in row} - {"."})
    draft = {
        "name": "MoonDew - round flask of moonglow-steeped blue dew with a crescent reflection",
        "source_note": "Hand-authored. Rebuild with Tools/PotionPixel/GenerateMoonDewPixel.py --apply.",
        "size": [WIDTH, HEIGHT],
        "palette": {".": "#00000000", **{s: PALETTE[s] for s in used}},
        "pixels": ["".join(row) for row in grid],
    }
    (OUT / "MoonDew.pixel.json").write_text(json.dumps(draft, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--apply", action="store_true", help="write the live sprite, outline and previews")
    args = parser.parse_args()
    sprite, grid = build_sprite()
    alpha = sprite.getchannel("A")
    assert alpha.tobytes() == alpha.transpose(Image.Transpose.FLIP_LEFT_RIGHT).tobytes(), "silhouette must be symmetric"
    old = Image.open(LIVE).convert("RGBA")
    OUT.mkdir(parents=True, exist_ok=True)
    sprite.save(OUT / "MoonDew_Pixel_New.png")
    sprite.resize((WIDTH * 8, HEIGHT * 8), Image.Resampling.NEAREST).save(OUT / "MoonDew_Pixel_x8.png")
    compare([old, sprite]).save(OUT / "MoonDew_Compare.png")
    if args.apply:
        apply(sprite, grid)
    colors = sum(1 for _, c in sprite.getcolors() if c[3])
    print(f"MoonDew pixel: {sprite.width}x{sprite.height}, {colors} opaque colors, bounds {sprite.getbbox()}"
          + (", applied" if args.apply else ", study only"))


if __name__ == "__main__":
    main()
