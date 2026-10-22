"""Hand-authored Bottled Shimmer item sprite (18x22, study only until it has a catalog entry).

Not a potion but a sample: a small round glass bottle with a three-row neck and a puffy cork, holding
shimmer scooped up from the world. The glass is a one-pixel wall following the whole
round outline, lit on the left and shaded on the right and underneath, so the shimmer
inside fills the round bottom right down to the curve. The shimmer is coloured after the
world liquid: a lavender body with a round glow about a twinkle, pale sparkles and a deeper
rim where the glass curves away, under a wavy rainbow surface (aqua, periwinkle, violet,
pink, cream, mint) that climbs the glass at both walls and drips a pixel into the liquid.
A curved highlight and a separate highlight dot sit on the upper-left of the glass.

Nothing floats free of the jar; outer silhouette pure black; all pixels opaque, no
antialiasing.

python Tools/PotionPixel/GenerateBottledShimmerPixel.py   # previews in Tools/PotionPixel/output
Requires Pillow.
"""

import math
from pathlib import Path

from PIL import Image

from pixel_common import NOTEBOOK_PAPER, blacken_silhouette, compare, render

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "ElainaModAlchemy/item/ExampleAssets"
OUT = ROOT / "Tools/PotionPixel/output"
NAME = "BottledShimmer"
WIDTH, HEIGHT = 18, 22
PALETTE = {
    ".": "#00000000",
    "O": "#000000",  # Outer silhouette: pure black so it reads on the pale notebook card.
    "K": "#ecc994",  # Cork: lit, body, shade, deep.
    "k": "#c99a62",
    "j": "#9c6c3e",
    "J": "#6e482a",
    "G": "#f6f3fd",  # Glass: lit wall, clear air, shaded wall, deep wall.
    "e": "#d9d2ef",
    "h": "#aba1d4",
    "H": "#8279b6",
    "w": "#ffffff",  # Highlights and sparkles.
    "L": "#bcafe6",  # Shimmer body (sampled from the world liquid): glow, base, shade, deep.
    "b": "#9d90cb",
    "a": "#8a6dc3",
    "A": "#7257ad",
    "1": "#96d6f5",  # Rainbow surface: aqua, periwinkle, violet, pink, cream, mint.
    "2": "#a5a6fc",
    "3": "#c48bfa",
    "4": "#f497f9",
    "5": "#f1dcd0",
    "6": "#b0eedd",
    "s": "#f8d0f6",  # Pale sparkles: pink, cyan.
    "c": "#cff6ff",
}

# First outline x per row; every row is mirrored (last x = 17 - first).
# Puffy cork 0-2, glass rim 3, neck 4-6, round shoulders 7-9, belly 10-16, round bottom 17-21.
# Chosen proportion: an 8-wide, three-row neck on an 18-wide body. A one-row neck or a
# 10-wide neck reads as a squat jar; an 8-wide neck on a 20-wide body reads bottom-heavy.
NECK_LEFT = 5
LEFT = {0: NECK_LEFT + 1, 1: NECK_LEFT, 2: NECK_LEFT, 3: NECK_LEFT - 1, **{y: NECK_LEFT for y in range(4, 7)},
        7: NECK_LEFT - 2, 8: 1, 9: 1, **{y: 0 for y in range(10, 17)}, 17: 1, 18: 1, 19: 2, 20: 3, 21: 5}
SPANS = {y: (x0, WIDTH - 1 - x0) for y, x0 in LEFT.items()}
CX, CY = (WIDTH - 1) / 2, 14.0  # centre of the round body
CORK, RIM = range(0, 3), 3
SURFACE = 10
WAVE = {2: -1, 5: -1, 6: -1, 11: -1, 12: -1, 15: -1}  # surface lifts: walls and crests
DRIPS = {4: 1, 9: 1, 13: 1}
SPARKLES = ((4, 13, "s"), (12, 12, "c"), (5, 17, "c"), (13, 16, "w"), (10, 18, "s"))
TWINKLE = (8, 14)
HIGHLIGHT = ((NECK_LEFT + 2, 4), (NECK_LEFT + 2, 5), (4, 7), (3, 8), (2, 9), (2, 11), (2, 12), (2, 13))  # neck glint, then curved upper-left
HIGHLIGHT_DOT = (3, 15)
REFLECTION = (15, 13)  # soft reflected light on the right
CORK_GRAIN = (9, 1)


def neighbours(p):
    x, y = p
    return ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))


def mask_depth():
    """Rings in from the silhouette by four-neighbour steps: 0 is the outline itself."""
    inside = {(x, y) for y, (x0, x1) in SPANS.items() for x in range(x0, x1 + 1)}
    depth, frontier, d = {}, [p for p in inside if any(n not in inside for n in neighbours(p))], 0
    while frontier:
        for p in frontier:
            depth[p] = d
        d += 1
        frontier = list({n for p in frontier for n in neighbours(p) if n in inside and n not in depth})
    return depth


def rainbow(x):
    return "1234561"[((x - 1) * 7 // (WIDTH - 2)) % 7]


def surface(x):
    return SURFACE + WAVE.get(x, 0)


def shimmer(x, y, d):
    """Lavender body: a round glow about the twinkle, shade bowing with the belly on the right,
    and a deeper rim where the liquid meets the curving glass on the right and underneath."""
    tx, ty = TWINKLE
    if math.hypot(x - tx, y - ty) <= 2.2:
        return "L"
    value = 1 if x + 0.45 * abs(y - CY) <= CX + 3 else 2
    if d == 2 and (x > CX or y > CY + 2):
        value += 1
    return "LbaA"[min(3, value)]


def build_grid():
    grid = [["."] * WIDTH for _ in range(HEIGHT)]
    for (x, y), d in mask_depth().items():
        x0, x1 = SPANS[y]
        if y in CORK:
            u = (x - x0) / (x1 - x0)
            grid[y][x] = "J" if u > 0.8 else ("K" if u < 0.4 else "k") if y == 1 else "j"
        elif y == RIM:
            grid[y][x] = "h" if x >= x1 - 2 else "G"
        elif d == 1:  # glass wall all the way round, lit on the upper left
            grid[y][x] = "G" if x < CX and y < CY + 3 else "H" if x > CX and y > CY else "h"
        elif y >= surface(x):
            # The rainbow runs unbroken along SURFACE; crests and the climb at the walls sit on top.
            grid[y][x] = rainbow(x) if y <= SURFACE else shimmer(x, y, d)
        else:
            grid[y][x] = "h" if x >= x1 - 2 else "e"  # clear air in the jar
    gx, gy = CORK_GRAIN
    grid[gy][gx] = "j"
    for x, length in DRIPS.items():
        for d in range(1, length + 1):
            grid[SURFACE + d][x] = rainbow(x)
    for x, y, c in SPARKLES:
        grid[y][x] = c
    tx, ty = TWINKLE
    for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
        grid[ty + dy][tx + dx] = "s"
    grid[ty][tx] = "w"
    # A curved highlight hugging the upper-left of the glass, and a separate dot below it.
    for x, y in HIGHLIGHT + (HIGHLIGHT_DOT,):
        grid[y][x] = "w"
    rx, ry = REFLECTION
    grid[ry][rx] = "c"
    return blacken_silhouette(grid)


def main():
    grid = build_grid()
    sprite = render(grid, PALETTE)
    alpha = sprite.getchannel("A")
    assert alpha.tobytes() == alpha.transpose(Image.Transpose.FLIP_LEFT_RIGHT).tobytes(), "silhouette must be symmetric"
    OUT.mkdir(parents=True, exist_ok=True)
    sprite.save(OUT / f"{NAME}_Pixel_New.png")
    sprite.resize((WIDTH * 8, HEIGHT * 8), Image.Resampling.NEAREST).save(OUT / f"{NAME}_Pixel_x8.png")
    card = Image.new("RGBA", (WIDTH + 16, HEIGHT + 16), NOTEBOOK_PAPER + (255,))
    card.alpha_composite(sprite, (8, 8))
    card.resize((card.width * 4, card.height * 4), Image.Resampling.NEAREST).save(OUT / f"{NAME}_OnCard_x4.png")
    # No live sprite yet: compare beside the sibling bottles and the shimmer droplet.
    siblings = [Image.open(ASSETS / f"{n}_Pixel.png").convert("RGBA") for n in ("BottledRain", "BottledSky", "ShimmerDroplet")]
    compare(siblings + [sprite]).save(OUT / f"{NAME}_Compare.png")
    colors = sum(1 for _, c in sprite.getcolors() if c[3])
    print(f"{NAME}: {sprite.width}x{sprite.height}, {colors} opaque colors, bounds {sprite.getbbox()}, study only")
    for row in grid:
        print("".join(row))


if __name__ == "__main__":
    main()
