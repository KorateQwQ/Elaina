"""Hand-authored Caramel Brulee item sprite (28x21, matching its catalog size).

The same ramekin on a saucer, redrawn with depth and seen from a little above:
a fluted cream ceramic dish whose lip and saucer read as ellipses, a torched
caramel crust that darkens to the rim with a glossy highlight and a crack
showing the custard beneath, an outlined foot standing on a lavender saucer
with a soft shadow. No detached glints: the item is also drawn dropped in the
world, where loose decoration would spin with the sprite.
Outer silhouette pure black; all pixels are opaque, no antialiasing.

python Tools/PotionPixel/GenerateCaramelBruleePixel.py           # previews in Tools/PotionPixel/output
python Tools/PotionPixel/GenerateCaramelBruleePixel.py --apply   # also write live assets
Requires Pillow.
"""

import argparse
import json
import math
from pathlib import Path

from PIL import Image

from pixel_common import blacken_silhouette, outline_ring, render, write_previews

ROOT = Path(__file__).resolve().parents[2]
NAME = "CaramelBrulee"
LIVE = ROOT / f"ElainaModAlchemy/item/ExampleAssets/{NAME}_Pixel.png"
UI = ROOT / "ElainaModAlchemy/UI/Assets"
OUT = ROOT / "Tools/PotionPixel/output"
WIDTH, HEIGHT = 28, 21
CX = 13.5
PALETTE = {
    ".": "#00000000",
    "O": "#000000",  # Outer silhouette.
    "4": "#fffaf0",  # Cream ceramic: highlight, light, body, shade, deep.
    "3": "#f0e6d2",
    "2": "#dccdb1",
    "1": "#bba98b",
    "0": "#8f7d66",
    "K": "#6f3a1b",  # Caramel crust: scorched, dark, body, golden, gloss.
    "k": "#9a5626",
    "c": "#c97f34",
    "G": "#e6a84a",
    "H": "#fbe0a0",
    "F": "#f6e6b8",  # Custard showing through the crack.
    "o": "#4a3a33",  # Inner line: the dish's foot on the saucer.
    "L": "#ece6f4",  # Lavender saucer: top, rim, underside, shadow.
    "l": "#cfc4e0",
    "v": "#a898c2",
    "V": "#8a7aa6",
}

# Ellipses as (centre x, centre y, rx, ry), seen from a little above.
RAMEKIN = (CX, 6.0, 10.6, 5.0)   # outer lip of the dish
CRUST = (CX, 6.4, 9.0, 3.3)      # caramel top inside the lip
WALL_HALF = 9.5                  # fluted wall half-width
FOOT_Y, FOOT_DROP = 13, 3.2      # the foot curves from y = 13 at the sides to 16 in front
SAUCER = (CX, 15.2, 13.6, 5.3)
WELL = (CX, 15.0, 10.4, 3.6)     # the saucer's shallow centre
# Torch marks, gloss and the crack showing custard, drawn over the crust from x = 5, y = 3.
CRUST_MARKS = (
    "......k....k......",  # 3
    "...HH......kK.....",
    "..HHGG...KK.......",
    "..HGG...kFFk...c..",
    "...G.....kF..kK...",
    "........k.....k...",  # 8
)


def inside(x, y, ellipse):
    cx, cy, rx, ry = ellipse
    return ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1


def build_grid():
    grid = [["."] * WIDTH for _ in range(HEIGHT)]
    # Saucer: a lit top face round a slightly shaded well, its thickness showing at the front.
    for y in range(HEIGHT):
        for x in range(WIDTH):
            if inside(x, y, SAUCER):
                if not inside(x, y + 1, SAUCER):
                    grid[y][x] = "V"
                elif not inside(x, y + 2, SAUCER):
                    grid[y][x] = "v"
                elif inside(x, y, WELL):
                    grid[y][x] = "l"
                else:
                    grid[y][x] = "L" if x < CX + 4 else "l"
    # Fluted wall, straight-sided under the lip, its foot curving with the saucer's perspective;
    # lit from the left with a groove every third pixel. Mirrored, so the silhouette stays even.
    foot = {}
    for x in range(WIDTH):
        t = (x - CX) / WALL_HALF
        if abs(t) > 1:
            continue
        foot[x] = round(FOOT_Y + FOOT_DROP * math.sqrt(1 - t * t))
        base = 3 if t < -0.55 else 2 if t < 0.35 else 1 if t < 0.75 else 0
        for y in range(int(RAMEKIN[1]), foot[x] + 1):
            grid[y][x] = str(max(0, base - 1)) if (x - 3) % 3 == 0 else str(base)
    # The foot is outlined where it stands on the saucer, and casts a shadow to the front right.
    for x, y in foot.items():
        if grid[y + 1][x] in "Ll":
            grid[y + 1][x] = "v" if x > CX - 4 else grid[y + 1][x]
    for x, y in foot.items():
        grid[y][x] = "o"
    for x in (min(foot), max(foot)):
        for y in range(int(RAMEKIN[1]), foot[x]):
            if any(grid[y][n] in "LlvV" for n in (x - 1, x + 1)):
                grid[y][x] = "o"
    # Lip: bright at the back, cream at the front; a soft shade where it turns down into the wall.
    for y in range(HEIGHT):
        for x in range(WIDTH):
            if inside(x, y, RAMEKIN) and not inside(x, y, CRUST):
                grid[y][x] = "4" if y < CRUST[1] or x < CX - 4 else "3"
    for y in range(HEIGHT - 1):
        for x in range(WIDTH):
            if inside(x, y, RAMEKIN) and not inside(x, y + 1, RAMEKIN) and grid[y + 1][x] in "01234":
                grid[y + 1][x] = "0"
    # Crust: scorched at the edge, golden towards the middle.
    cx, cy, rx, ry = CRUST
    for y in range(HEIGHT):
        for x in range(WIDTH):
            if inside(x, y, CRUST):
                r = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                grid[y][x] = "k" if r > 0.7 else "c" if r > 0.28 else "G"
    for dy, row in enumerate(CRUST_MARKS):
        for dx, symbol in enumerate(row):
            if symbol != "." and inside(5 + dx, 3 + dy, CRUST):
                grid[3 + dy][5 + dx] = symbol
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
        "name": f"{NAME} - fluted ramekin of torched caramel custard on a lavender saucer",
        "source_note": "Hand-authored. Rebuild with Tools/PotionPixel/GenerateCaramelBruleePixel.py --apply.",
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
