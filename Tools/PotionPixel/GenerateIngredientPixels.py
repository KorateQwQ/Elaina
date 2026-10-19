"""Hand-authored sprites for the merchant ingredients: Flour, Sugar, Egg, Beef and Potato.

Each keeps its earlier idea, redrawn with real volume: light from the upper
left, shaded ramps instead of flat fills, inner lines where parts overlap and a
pure black outer silhouette. Nothing floats free of the item, because dropped
items are drawn and spun as one sprite. All pixels are opaque, no antialiasing.
Canvas sizes match the catalog entries.

python Tools/PotionPixel/GenerateIngredientPixels.py                    # previews in Tools/PotionPixel/output
python Tools/PotionPixel/GenerateIngredientPixels.py --apply Egg Sugar  # also write those live assets
Requires Pillow.
"""

import argparse
import json
import math
from pathlib import Path

from PIL import Image

from pixel_common import blacken_silhouette, outline_ring, render, write_previews

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "ElainaModAlchemy/item/ExampleAssets"
UI = ROOT / "ElainaModAlchemy/UI/Assets"
OUT = ROOT / "Tools/PotionPixel/output"
LIGHT = (-0.55, -0.62, 0.56)  # from the upper left and towards the viewer


def blank(w, h):
    return [["."] * w for _ in range(h)]


def shade(u, v, ramp, bias=0.0):
    """Pick from a dark-to-light ramp by lighting a sphere normal at (u, v), each in -1..1."""
    nz = math.sqrt(max(0.0, 1 - u * u - v * v))
    d = u * LIGHT[0] + v * LIGHT[1] + nz * LIGHT[2]
    d = max(0.0, min(0.999, d * 0.95 + 0.12 + bias))
    return ramp[int(d * len(ramp))]


def ring(grid, mask, symbol):
    """Inner line round a part: its pixels that touch anything outside the part."""
    for x, y in mask:
        if any(n not in mask for n in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))):
            grid[y][x] = symbol


def put(grid, x, y, symbol):
    if 0 <= y < len(grid) and 0 <= x < len(grid[0]):
        grid[y][x] = symbol


def ellipse(cx, cy, rx, ry, w=26, h=22):
    """Pixels inside an ellipse, each with its normalised offset (u, v) from the centre."""
    return {(x, y): ((x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry) for y in range(h) for x in range(w)
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1}


# ---------------------------------------------------------------- Egg (18x24)
EGG_PALETTE = {"O": "#000000", "0": "#c9a77e", "1": "#dcc09a", "2": "#ead6b6", "3": "#f5ead6",
               "4": "#fffaf0", "s": "#c79f78"}


def egg():
    w, h = 18, 24
    grid = blank(w, h)
    cx, cy, a, b = 8.5, 12.6, 7.4, 10.9
    for y in range(h):
        s = (y + 0.5 - cy) / b
        if abs(s) >= 1:
            continue
        half = a * math.sqrt(1 - s * s) * (1 + 0.16 * s)  # narrower at the top, full at the base
        for x in range(w):
            u = (x + 0.5 - cx) / half if half else 2
            if abs(u) <= 1:
                grid[y][x] = shade(u * 0.95, s * 0.95, "01234")
    # A little reflected light from the ground along the lower right rim, and faint speckles.
    for x, y in ((13, 18), (12, 20), (14, 16)):
        grid[y][x] = "1"
    for x, y in ((10, 9), (12, 13), (7, 17), (11, 18)):
        if grid[y][x] in "12":
            grid[y][x] = "s"
    return grid, EGG_PALETTE


# ---------------------------------------------------------------- Potato (26x22)
POTATO_PALETTE = {"O": "#000000", "o": "#4f3420", "0": "#7a5230", "1": "#9a6c3c", "2": "#b88a4e",
                  "3": "#d4aa68", "4": "#ecca8a", "e": "#5e3d22", "E": "#e8c27e", "d": "#8a6a48"}


def potato_mask(cx, cy, rx, ry, phase):
    mask = {}
    for y in range(22):
        for x in range(26):
            dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
            theta = math.atan2(dy, dx)
            r = 1 + 0.03 * math.sin(2 * theta + phase) + 0.015 * math.sin(3 * theta + 2 * phase)
            # A slightly squared ellipse keeps the ends blunt, so no tip pokes out of the outline.
            if abs(dx) ** 2.5 + abs(dy) ** 2.5 <= r ** 2.5:
                mask[(x, y)] = (dx / r, dy / r)
    return mask


def potato():
    grid = blank(26, 22)
    back = potato_mask(17.6, 7.8, 7.0, 6.0, 1.3)
    front = potato_mask(10.8, 13.4, 9.4, 7.3, 2.2)
    for mask, eyes in ((back, ((16, 5), (21, 7))), (front, ((6, 12), (12, 16), (15, 11), (9, 18)))):
        for (x, y), (u, v) in mask.items():
            grid[y][x] = shade(u, v, "01234")
        for x, y in eyes:
            grid[y][x] = "e"
            put(grid, x + 1, y + 1, "E")
        ring(grid, set(mask), "o")
    for x, y in ((4, 14), (14, 18), (16, 13)):
        if grid[y][x] in "123":
            grid[y][x] = "d"
    return grid, POTATO_PALETTE


# ---------------------------------------------------------------- Sugar (24x22)
SUGAR_PALETTE = {"O": "#000000", "o": "#6d6288", "T": "#ffffff", "t": "#ebe6f6", "L": "#d3cae6",
                 "l": "#e6e0f2", "R": "#a99dc6", "r": "#c1b7d8"}


def cube(grid, cx, ty, w=5.5, depth=6.0):
    """Isometric sugar cube: bright top face, pale left face, shaded right face, sparkling grain."""
    h = w / 2
    mask = set()
    for y in range(len(grid)):
        for x in range(len(grid[0])):
            px, py = x + 0.5, y + 0.5
            if abs(px - cx) / w + abs(py - (ty + h)) / h <= 1:
                face = "T"
            elif cx - w <= px <= cx and ty + h + (px - cx + w) * h / w <= py <= ty + h + (px - cx + w) * h / w + depth:
                face = "L"
            elif cx <= px <= cx + w and ty + 2 * h - (px - cx) * h / w <= py <= ty + 2 * h - (px - cx) * h / w + depth:
                face = "R"
            else:
                continue
            grain = (x * 7 + y * 13) % 9 == 0
            grid[y][x] = {"T": "t" if grain else "T", "L": "l" if grain else "L", "R": "r" if grain else "R"}[face]
            mask.add((x, y))
    ring(grid, mask, "o")


def sugar():
    grid = blank(24, 22)
    cube(grid, 6.5, 8.5)
    cube(grid, 17.5, 8.5)
    cube(grid, 12.0, 2.5)
    return grid, SUGAR_PALETTE


# ---------------------------------------------------------------- Beef (26x22)
BEEF_PALETTE = {"O": "#000000", "o": "#5a2328", "0": "#8a3340", "1": "#a8485a", "2": "#c2606f",
                "3": "#d67f88", "4": "#e9a3a6", "f": "#f6e1d8", "F": "#e8c3bb", "g": "#cf9a98",
                "s": "#8e4a52", "S": "#6e3540", "b": "#f3e7cf", "B": "#d6c4a2", "k": "#b0846e"}


BONE = (
    "..ooo..",
    ".obbbo.",
    "obbkkBo",
    "obkkkBo",
    ".oBBBo.",
    "..ooo..",
)


def beef():
    """A thick round cut of beef seen from above, as before: a fat rim round marbled red meat and a
    round bone, now with the slab's side showing below and the top lit from the upper left."""
    grid = blank(26, 22)
    cx, cy, rx, ry = 12.6, 10.0, 11.8, 8.6
    slab, top = set(), {}
    for y in range(22):
        for x in range(26):
            px, py = x + 0.5, y + 0.5
            theta = math.atan2(py - cy, px - cx)
            r = 1 + 0.035 * math.sin(3 * theta + 0.8)
            if ((px - cx) / rx) ** 2 + ((py - cy - 1.6) / ry) ** 2 <= r * r:
                slab.add((x, y))  # the whole cut, its side showing below the top face
            u, v = (px - cx) / rx, (py - cy) / ry
            if u * u + v * v <= r * r:
                top[(x, y)] = (u / r, v / r)
    for x, y in slab:
        grid[y][x] = "S" if (x, y - 1) in slab and (x, y - 2) not in top else "s"
    for (x, y), (u, v) in top.items():
        if u * u + v * v > 0.74:
            grid[y][x] = "f" if u + v < 0.3 else "F" if u + v < 1.0 else "g"  # fat rim
        else:
            grid[y][x] = shade(u, v, "01234", -0.12)
    # Marbling: three curved seams of fat, as in the earlier design.
    for path in (((8, 4), (6, 8), (6, 11)), ((12, 3), (10, 8), (11, 14)), ((16, 4), (14, 8), (15, 12))):
        for (x0, y0), (x1, y1) in zip(path, path[1:]):
            for i in range(max(abs(x1 - x0), abs(y1 - y0)) + 1):
                t = i / max(abs(x1 - x0), abs(y1 - y0))
                x, y = round(x0 + (x1 - x0) * t), round(y0 + (y1 - y0) * t)
                if (x, y) in top and grid[y][x] in "01234":
                    grid[y][x] = "f" if y < 9 else "F"
    # Round bone at the lower right: a cream ring round the marrow, drawn from x = 15, y = 10.
    for dy, row in enumerate(BONE):
        for dx, symbol in enumerate(row):
            if symbol != ".":
                grid[10 + dy][15 + dx] = symbol
    return grid, BEEF_PALETTE


# ---------------------------------------------------------------- Flour (20x26)
FLOUR_PALETTE = {"O": "#000000", "o": "#6b5338", "0": "#a6875f", "1": "#c2a477", "2": "#d8bf93",
                 "3": "#e9d6b0", "4": "#f6ead0", "W": "#ffffff", "w": "#ece8f2", "t": "#b98a4e",
                 "T": "#8a5f30", "i": "#9b7146"}
# Wheat ear printed on the sack, drawn from x = 8, y = 14.
WHEAT = (
    "..i..",
    ".iii.",
    "i.i.i",
    ".iii.",
    "i.i.i",
    "..i..",
    "..i..",
)


def flour():
    """A paper flour sack, gathered and tied with twine at the neck, its open top heaped with flour."""
    grid = blank(20, 26)
    body = set()
    for y in range(9, 26):
        t = (y - 9) / 16
        half = 6.2 + 2.6 * math.sin(min(1.0, t * 1.4) * math.pi / 2) - (1.6 if y == 25 else 0.6 if y == 24 else 0)
        for x in range(20):
            u = (x + 0.5 - 10) / half
            if abs(u) <= 1:
                body.add((x, y))
                grid[y][x] = shade(u * 0.9, (t - 0.45) * 1.2, "01234", 0.1)
    # Paper folds running down from the tie.
    for x, y0 in ((6, 10), (13, 10)):
        for y in range(y0, y0 + 4):
            if (x, y) in body:
                grid[y][x] = "0" if x > 10 else "1"
    ring(grid, body, "o")
    # Gathered neck and ruffled mouth above the tie, with the flour heaped inside.
    top = {}
    for y, (a, b) in {3: (6, 13), 4: (5, 14), 5: (6, 13), 6: (7, 12), 7: (8, 11), 8: (7, 12)}.items():
        for x in range(a, b + 1):
            top[(x, y)] = "2" if x < 10 else "1"
    for (x, y), c in top.items():
        grid[y][x] = c
    ring(grid, set(top), "o")
    for x, y, c in ((7, 3, "w"), (8, 2, "W"), (9, 2, "W"), (10, 2, "W"), (11, 3, "w"), (8, 3, "W"), (9, 3, "W"),
                    (10, 3, "W"), (9, 1, "W"), (10, 1, "w"), (12, 3, "w")):
        grid[y][x] = c
    # Twine wound round the neck and knotted at the front.
    for x in range(6, 14):
        grid[8][x] = "t"
    grid[8][9], grid[8][10] = "T", "T"
    grid[9][9] = "t"
    grid[10][10] = "t"
    for dy, row in enumerate(WHEAT):
        for dx, symbol in enumerate(row):
            if symbol != ".":
                grid[14 + dy][8 + dx] = symbol
    return grid, FLOUR_PALETTE


ITEMS = {"Flour": flour, "Sugar": sugar, "Egg": egg, "Beef": beef, "Potato": potato}


def build_sprite(name):
    grid, palette = ITEMS[name]()
    blacken_silhouette(grid)
    return render(grid, {".": "#00000000", **palette}), grid, palette


def apply(name, image, grid, palette):
    image.save(ASSETS / f"{name}_Pixel.png")
    outline_ring(image).save(UI / f"Outlines/{name}_Pixel.png", optimize=True)
    used = sorted({s for row in grid for s in row} - {"."})
    draft = {
        "name": f"{name} - hand-authored ingredient sprite",
        "source_note": f"Hand-authored. Rebuild with Tools/PotionPixel/GenerateIngredientPixels.py --apply {name}.",
        "size": [image.width, image.height],
        "palette": {".": "#00000000", **{s: palette[s] for s in used}},
        "pixels": ["".join(row) for row in grid],
    }
    (OUT / f"{name}.pixel.json").write_text(json.dumps(draft, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--apply", nargs="+", choices=ITEMS, metavar="NAME", help="write these live sprites")
    args = parser.parse_args()
    for name in ITEMS:
        sprite, grid, palette = build_sprite(name)
        old = Image.open(ASSETS / f"{name}_Pixel.png").convert("RGBA")
        assert sprite.size == old.size, f"{name}: {sprite.size} does not match the catalog size {old.size}"
        write_previews(OUT, name, old, sprite)
        applied = args.apply and name in args.apply
        if applied:
            apply(name, sprite, grid, palette)
        colors = sum(1 for _, c in sprite.getcolors() if c[3])
        print(f"{name}: {sprite.width}x{sprite.height}, {colors} opaque colors" + (", applied" if applied else ""))


if __name__ == "__main__":
    main()
