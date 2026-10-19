"""Build a redesigned Warm Resin item sprite (25x24, matching its catalog size).

A glossy amber glob rests on a tree-ring wood slice and drips over its edge:
wood + gel brewed into resin. The glob stays rounded so it does not read as a
flame on a log. All pixels are opaque with no antialiasing. The output is a
study and does not replace the live WarmResin_Pixel.png.

Run from any directory: python Tools/PotionPixel/GenerateWarmResinPixel.py
Requires Pillow.
"""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
LIVE = ROOT / "ElainaModAlchemy/item/ExampleAssets/WarmResin_Pixel.png"
OUTPUT = ROOT / "Tools/PotionPixel/output/WarmResin_Pixel_New.png"
PREVIEW = ROOT / "Tools/PotionPixel/output/WarmResin_Compare.png"
WIDTH = 25
PALETTE = {
    ".": (0, 0, 0, 0),
    "a": "#000000",  # Outer outline: pure black so it reads on the pale notebook card.
    "1": "#9a4a1c",  # Amber resin, deep edge to inner glow.
    "2": "#c96b22",
    "3": "#eb9a32",
    "4": "#ffc85a",
    "h": "#fff2c8",
    "c": "#9a6a4c",  # Bark: lit, body, shadow.
    "b": "#7a5040",
    "d": "#5a3a33",
    "e": "#e6c08e",  # End grain: wood, rim, rings.
    "f": "#c4925f",
    "r": "#a06a43",
}
UNOUTLINED = ".a"
HEIGHT = 24
# Wood slice: top face spans per row, seen from slightly above.
FACE = {12: (7, 17), 13: (4, 20), 14: (2, 22), 15: (1, 23),
        16: (1, 23), 17: (2, 22), 18: (4, 20), 19: (7, 17)}
SIDE_DEPTH = 3
SIDE_GROOVES = {5, 9, 14, 19}
# Resin glob and drip as (first x, symbols) runs painted over the wood.
RESIN = {
    7: ((9, "1222221"),),
    8: ((7, "1hh22222221"),),
    9: ((6, "1h" + "2" * 10 + "1"),),
    10: ((6, "1h22333332221"),),
    11: ((6, "1223333333321"),),
    12: ((6, "1233444443321"),),
    13: ((6, "123444444h321"),),
    14: ((5, "112344444443211"),),
    15: ((4, "11" + "2" * 13 + "11"),),
    16: ((6, "111"), (15, "21")),
    17: ((15, "21"),),
    18: ((15, "31"),),
    19: ((15, "21"),),
    20: ((15, "21"),),
    21: ((14, "2331"),),
    22: ((14, "2h31"),),
}


def wood_grid():
    grid = [["."] * WIDTH for _ in range(HEIGHT)]
    face = {(x, y) for y, (x0, x1) in FACE.items() for x in range(x0, x1 + 1)}
    for x, y in face:
        q = ((x - 12) / 11.5) ** 2 + ((y - 15.5) / 4.2) ** 2
        grid[y][x] = "f" if q > 0.84 else "r" if 0.6 < q < 0.7 or 0.3 < q < 0.38 else "e"
    for x in range(WIDTH):
        column = [y for fx, y in face if fx == x]
        if not column:
            continue
        bottom = max(column)
        for depth in range(1, SIDE_DEPTH + 1):
            shadow = depth == SIDE_DEPTH or x >= 20 or x in SIDE_GROOVES
            grid[bottom + depth][x] = "d" if shadow else "c" if depth == 1 else "b"
    return grid


def to_rgba(symbol):
    color = PALETTE[symbol]
    if isinstance(color, str):
        color = tuple(int(color[i:i + 2], 16) for i in (1, 3, 5)) + (255,)
    return color


def build_sprite():
    grid = wood_grid()
    for layer in (RESIN,):
        for y, runs in layer.items():
            for start, symbols in runs:
                assert start + len(symbols) <= WIDTH, f"Row {y} overflows"
                grid[y][start:start + len(symbols)] = symbols

    height = HEIGHT
    for y in range(height):
        for x in range(WIDTH):
            if grid[y][x] != ".":
                continue
            neighbors = ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))
            if any(0 <= nx < WIDTH and 0 <= ny < height and grid[ny][nx] not in UNOUTLINED
                   for nx, ny in neighbors):
                grid[y][x] = "a"

    image = Image.new("RGBA", (WIDTH, height))
    for y, row in enumerate(grid):
        for x, symbol in enumerate(row):
            image.putpixel((x, y), to_rgba(symbol))
    return image


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


if __name__ == "__main__":
    sprite = build_sprite()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(OUTPUT)
    compare([Image.open(LIVE).convert("RGBA"), sprite]).save(PREVIEW)
    colors = sum(1 for _, c in sprite.getcolors() if c[3])
    print(f"Saved {OUTPUT.name}: {sprite.width}x{sprite.height}, RGBA, {colors} opaque colors")
