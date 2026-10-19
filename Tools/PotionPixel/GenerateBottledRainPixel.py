"""Hand-authored Bottled Rain item sprite (31x38, matching its catalog size).

A test tube holding a little rain, stoppered with a faceted violet crystal on a
gold collar and bound with a gold lip. Inside, a soft lilac-lit cloud hangs at
the top, glowing droplets fall from it through twilight air past a gold twinkle,
and they gather in a luminous pool in the rounded foot. The test-tube shape was
chosen over round, conical and diamond flasks; Concentration Potion is also a
tube, so the crystal, gold and night colours keep the two apart.

Outer silhouette pure black; all pixels are opaque, no antialiasing.

python Tools/PotionPixel/GenerateBottledRainPixel.py           # previews in Tools/PotionPixel/output
python Tools/PotionPixel/GenerateBottledRainPixel.py --apply   # also write live assets
Requires Pillow.
"""

import argparse
import json
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "ElainaModAlchemy/item/ExampleAssets"
UI = ROOT / "ElainaModAlchemy/UI/Assets"
LIVE = ASSETS / "BottledRain_Pixel.png"
OUT = ROOT / "Tools/PotionPixel/output"
WIDTH, HEIGHT = 31, 38
PALETTE = {
    ".": "#00000000",
    "O": "#000000",  # Outer silhouette: pure black so it reads on the pale notebook card.
    "o": "#42364f",  # Inner lines.
    "X": "#eee3ff",  # Crystal stopper: lit facet, body, shade, deep.
    "x": "#b796ef",
    "Z": "#7c5cc8",
    "z": "#4e3891",
    "Y": "#fff0b8",  # Gold: light, body, shade.
    "y": "#e6b955",
    "u": "#a5762c",
    "w": "#ffffff",  # Glass glint.
    "0": "#45488a",  # Twilight air: deep, dusk, body, glow.
    "1": "#5664a6",
    "2": "#6b82c0",
    "3": "#88a5da",
    "C": "#7f74b8",  # Cloud: underside, lilac shade, body, lit crest.
    "c": "#ab9fd8",
    "d": "#dcd5f3",
    "e": "#f8f5ff",
    "r": "#8fdcf5",  # Glowing rain droplet: trail, head.
    "R": "#e8fbff",
    "P": "#2b5c9c",  # Pool: deep, body, light, surface.
    "p": "#3a8ac6",
    "q": "#5fc0e8",
    "s": "#aef0ff",
}

CX = 15
# Half-width per row, outline included: crystal 0-4, collar 5, gold lip 6-7, tube, rounded foot.
HALF = {0: 1, 1: 2, 2: 3, 3: 3, 4: 2, 5: 3, 6: 6, 7: 6,
        **{y: 5 for y in range(8, 33)}, 33: 4, 34: 3, 35: 2}
BOTTOM = max(HALF)
TUBE = 8   # First row inside the glass.
POOL = 30  # Pool surface row.
# Crystal, gold collar and gold lip, centred on CX.
TOP = (
    "OOO",
    "OXxZO",
    "OXXxxZO",
    "OXxxZzO",
    "OxZzO",
    "OyYyyuO",
    "OyYYyyyyyyyuO",  # 6 lip, lit
    "OuuyuuuuuuuuO",  # 7 lip, underside
)
# Cloud from x = 11 (the full inner width): lobed lit crest, lilac shade, wisps trailing below.
CLOUD = (
    "..ee..e..",  # 9
    ".eeeedde.",
    "eeeddddcc",
    "ddddcccCC",
    "cccCCCCCC",
    ".C.CC..C.",  # 14
)
DROPS = ((13, 16), (17, 17), (15, 20), (13, 23), (17, 24), (15, 26))
TWINKLE = (18, 21)
MOTES = ((12, 18, "3"), (16, 28, "3"), (13, 30, "w"), (18, 30, "w"))


def air(x, y, x0, x1):
    """Bright glass above the cloud, dusky under it, lifting again towards the glowing pool."""
    value = 3 if y < 9 else 1 if y <= 14 else 0 if y <= 16 else 1 if y <= 26 else 2
    value += -1 if x >= x1 - 1 else 1 if x <= x0 + 1 else 0
    return str(max(0, min(3, value)))


def build_grid():
    grid = [["."] * WIDTH for _ in range(HEIGHT)]
    for y, h in HALF.items():
        x0, x1 = CX - h, CX + h
        for x in range(x0, x1 + 1):
            if x in (x0, x1) or y == BOTTOM:
                grid[y][x] = "o"
            elif y >= POOL:
                depth = y - POOL
                grid[y][x] = "s" if depth == 0 else "q" if depth == 1 else "p" if depth < 4 else "P"
                if depth and x >= x1 - 1:
                    grid[y][x] = "P"
            elif y >= TUBE:
                grid[y][x] = air(x, y, x0, x1)
    for dy, row in enumerate(CLOUD):
        for dx, symbol in enumerate(row):
            if symbol != ".":
                grid[9 + dy][11 + dx] = symbol
    for x, y in DROPS:
        grid[y][x], grid[y + 1][x] = "r", "R"
    x, y = TWINKLE
    for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
        grid[y + dy][x + dx] = "Y"
    grid[y][x] = "w"
    for x, y, c in MOTES:
        grid[y][x] = c
    # A long glint down the left of the glass, broken where the cloud passes.
    for y in list(range(16, 27)) + [8]:
        grid[y][11] = "w"
    for y, row in enumerate(TOP):
        x0 = CX - len(row) // 2
        for dx, symbol in enumerate(row):
            grid[y][x0 + dx] = symbol
    return grid


def blacken_silhouette(grid):
    """Every opaque pixel touching transparency or the canvas edge becomes pure black."""
    def clear(x, y):
        return not (0 <= x < WIDTH and 0 <= y < HEIGHT) or grid[y][x] == "."
    edge = [(x, y) for y in range(HEIGHT) for x in range(WIDTH) if grid[y][x] != "."
            and any(clear(nx, ny) for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))]
    for x, y in edge:
        grid[y][x] = "O"
    return grid


def to_rgba(symbol):
    value = PALETTE[symbol].lstrip("#")
    value += "ff" * (len(value) == 6)
    return tuple(int(value[i:i + 2], 16) for i in range(0, 8, 2))


def build_sprite():
    grid = blacken_silhouette(build_grid())
    image = Image.new("RGBA", (WIDTH, HEIGHT))
    for y, row in enumerate(grid):
        for x, symbol in enumerate(row):
            image.putpixel((x, y), to_rgba(symbol))
    alpha = image.getchannel("A")
    assert alpha.tobytes() == alpha.transpose(Image.Transpose.FLIP_LEFT_RIGHT).tobytes(), "silhouette must be symmetric"
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


def compare(sprites, scales=(1, 2, 8), backgrounds=((58, 52, 82), (226, 220, 238))):
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
    outline_ring(image).save(UI / "Outlines/BottledRain_Pixel.png", optimize=True)
    used = sorted({s for row in grid for s in row} - {"."})
    draft = {
        "name": "BottledRain - crystal-stoppered test tube with a cloud and glowing rain",
        "source_note": "Hand-authored. Rebuild with Tools/PotionPixel/GenerateBottledRainPixel.py --apply.",
        "size": [WIDTH, HEIGHT],
        "palette": {".": "#00000000", **{s: PALETTE[s] for s in used}},
        "pixels": ["".join(row) for row in grid],
    }
    (OUT / "BottledRain.pixel.json").write_text(json.dumps(draft, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--apply", action="store_true", help="write the live sprite and outline")
    args = parser.parse_args()
    sprite, grid = build_sprite()
    old = Image.open(LIVE).convert("RGBA")
    OUT.mkdir(parents=True, exist_ok=True)
    sprite.save(OUT / "BottledRain_Pixel_New.png")
    sprite.resize((WIDTH * 8, HEIGHT * 8), Image.Resampling.NEAREST).save(OUT / "BottledRain_Pixel_x8.png")
    card = Image.new("RGBA", (WIDTH + 16, HEIGHT + 16), (226, 220, 238, 255))
    card.alpha_composite(sprite, (8, 8))
    card.resize((card.width * 4, card.height * 4), Image.Resampling.NEAREST).save(OUT / "BottledRain_OnCard_x4.png")
    compare([old, sprite]).save(OUT / "BottledRain_Compare.png")
    if args.apply:
        apply(sprite, grid)
    colors = sum(1 for _, c in sprite.getcolors() if c[3])
    print(f"BottledRain pixel: {sprite.width}x{sprite.height}, {colors} opaque colors, bounds {sprite.getbbox()}"
          + (", applied" if args.apply else ", study only"))


if __name__ == "__main__":
    main()
