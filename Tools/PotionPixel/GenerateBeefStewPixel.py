"""Hand-authored Beef Stew item sprite (30x27, matching its catalog size).

The same lavender enamel pot with two ring handles, redrawn with depth: an
elliptical rim seen from above with the shadowed inner wall at the back, a
glossy broth holding potato cubes, beef chunks, carrot and herbs, a rounded
body shaded from the upper left. No detached steam: the item is also drawn
dropped in the world, where loose decoration would spin with the sprite.
Outer silhouette pure black; all pixels are opaque, no antialiasing.

python Tools/PotionPixel/GenerateBeefStewPixel.py           # previews in Tools/PotionPixel/output
python Tools/PotionPixel/GenerateBeefStewPixel.py --apply   # also write live assets
Requires Pillow.
"""

import argparse
import json
from pathlib import Path

from PIL import Image

from pixel_common import blacken_silhouette, outline_ring, render, write_previews

ROOT = Path(__file__).resolve().parents[2]
NAME = "BeefStew"
LIVE = ROOT / f"ElainaModAlchemy/item/ExampleAssets/{NAME}_Pixel.png"
UI = ROOT / "ElainaModAlchemy/UI/Assets"
OUT = ROOT / "Tools/PotionPixel/output"
WIDTH, HEIGHT = 30, 27
CX = 14.5
PALETTE = {
    ".": "#00000000",
    "O": "#000000",  # Outer silhouette.
    "o": "#3d2f4d",  # Inner lines: rim lip, handle joints.
    "4": "#efe6f6",  # Lavender enamel: highlight, light, body, shade, deep.
    "3": "#cdb8de",
    "2": "#a98fc4",
    "1": "#8569a3",
    "0": "#5f4a7e",
    "W": "#6b5784",  # Inner wall at the back of the pot, in shadow.
    "b": "#a35a36",  # Broth: body, lit gloss, shade.
    "B": "#c97c4a",
    "n": "#7c4029",
    "T": "#f6dc9c",  # Potato: lit, body, shade.
    "t": "#e2b764",
    "u": "#b98a45",
    "M": "#b8645c",  # Beef: lit, body, shade.
    "m": "#8b423f",
    "C": "#ef9a4a",  # Carrot.
    "g": "#9cc06a",  # Herb.
}

# Pot body half-width per row; the rim ellipse sits over its top.
BODY = {**{y: 11.0 for y in range(10, 21)}, 21: 10.6, 22: 10.2, 23: 9.6, 24: 8.8, 25: 7.6, 26: 5.5}
RIM = (CX, 10.0, 12.6, 4.4)      # centre x, centre y, rx, ry of the outer lip
OPENING = (CX, 10.9, 10.3, 2.9)  # the mouth, filled with stew
# Left handle, mirrored to the right: a ring with a 1x2 opening, joined to the wall.
HANDLE = (
    ".ooo",
    "o43o",
    "o2.o",
    "o2.o",
    "o10o",
    ".ooo",
)
HANDLE_TOP = 13
# Stew contents over the broth from x = 6, y = 8: potato cubes, beef, carrot and herbs.
CONTENTS = (
    "....................",  # 8
    "....................",
    "..TTu...mMm...TTu...",  # 10
    "..Ttu.g.mmm.CC.Tu.g.",
    "........B......mMm..",
    "...BB..TTu..g..mm...",  # 13
)


def inside(x, y, ellipse):
    cx, cy, rx, ry = ellipse
    return ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1


def build_grid():
    grid = [["."] * WIDTH for _ in range(HEIGHT)]
    # Body: lit from the upper left, darkening right and towards the foot.
    for y, half in BODY.items():
        for x in range(WIDTH):
            t = (x - CX) / half
            if abs(t) > 1:
                continue
            level = 3 if t < -0.62 else 2 if t < 0.3 else 1 if t < 0.72 else 0
            if y >= 24:
                level = max(0, level - 1)
            grid[y][x] = str(level)
    for y in range(15, 22):
        grid[y][6] = "4"
    grid[22][7] = "4"
    # Rim: a lit band round the mouth; the lip's lower edge is drawn where it overhangs the body.
    for y in range(HEIGHT):
        for x in range(WIDTH):
            if inside(x, y, RIM) and not inside(x, y, OPENING):
                front = y >= OPENING[1]
                t = (x - CX) / RIM[2]
                grid[y][x] = "4" if not front and t < 0.4 else "3" if not front or t < 0.1 else "2"
    for y in range(HEIGHT - 1):
        for x in range(WIDTH):
            if inside(x, y, RIM) and not inside(x, y + 1, RIM) and grid[y + 1][x] != ".":
                grid[y + 1][x] = "o"
    # Mouth: the shadowed back wall, then broth that catches light at the front left.
    for y in range(HEIGHT):
        for x in range(WIDTH):
            if inside(x, y, OPENING):
                if not inside(x, y - 1, OPENING):
                    grid[y][x] = "W"
                else:
                    grid[y][x] = "B" if x < CX - 4 and y >= 11 else "n" if x > CX + 6 else "b"
    for dy, row in enumerate(CONTENTS):
        for dx, symbol in enumerate(row):
            if symbol != ".":
                grid[8 + dy][6 + dx] = symbol
    for dy, row in enumerate(HANDLE):
        for dx, symbol in enumerate(row):
            if symbol != ".":
                for x in (dx, WIDTH - 1 - dx):
                    if grid[HANDLE_TOP + dy][x] == "." or dx == len(row) - 1:
                        grid[HANDLE_TOP + dy][x] = symbol
    blacken_silhouette(grid)
    return grid


def build_sprite():
    grid = build_grid()
    return render(grid, PALETTE), grid


def apply(image, grid):
    image.save(LIVE)
    outline_ring(image).save(UI / f"Outlines/{NAME}_Pixel.png", optimize=True)
    used = sorted({s for row in grid for s in row} - {"."})
    draft = {
        "name": f"{NAME} - lavender enamel pot of beef and potato stew",
        "source_note": "Hand-authored. Rebuild with Tools/PotionPixel/GenerateBeefStewPixel.py --apply.",
        "size": [WIDTH, HEIGHT],
        "palette": {".": "#00000000", **{s: PALETTE[s] for s in used}},
        "pixels": ["".join(row) for row in grid],
    }
    (OUT / f"{NAME}.pixel.json").write_text(json.dumps(draft, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--apply", action="store_true", help="write the live sprite and outline")
    args = parser.parse_args()
    sprite, grid = build_sprite()
    write_previews(OUT, NAME, Image.open(LIVE).convert("RGBA"), sprite)
    if args.apply:
        apply(sprite, grid)
    colors = sum(1 for _, c in sprite.getcolors() if c[3])
    print(f"{NAME} pixel: {sprite.width}x{sprite.height}, {colors} opaque colors, bounds {sprite.getbbox()}"
          + (", applied" if args.apply else ", study only"))


if __name__ == "__main__":
    main()
