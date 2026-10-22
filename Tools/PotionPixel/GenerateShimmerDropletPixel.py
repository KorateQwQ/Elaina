"""Hand-authored Shimmer Droplet material sprite (20x28, matching its catalog size).

An upright, mirror-symmetric teardrop of shimmer. Like a clear drop it is lit twice:
a glossy streak and spark on the upper left where light strikes the surface, and a
cyan caustic pooling in the lower right where light refracted through the drop comes
out. In between the liquid shifts through shimmer's pink, violet and aqua along
gentle diagonal bands, and a four-point twinkle sits inside the drop. Nothing floats
free of the drop. All pixels are opaque, no antialiasing.

python Tools/PotionPixel/GenerateShimmerDropletPixel.py           # previews in Tools/PotionPixel/output
python Tools/PotionPixel/GenerateShimmerDropletPixel.py --apply   # also write live assets
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
NAME = "ShimmerDroplet"
WIDTH, HEIGHT = 20, 28
PALETTE = {
    ".": "#00000000",
    "O": "#000000",  # Outer silhouette: pure black so it reads on the pale notebook card.
    "0": "#3d2f72",  # Violet: deepest, dark, middle, light.
    "1": "#5a4596",
    "2": "#7d67c0",
    "3": "#a592e0",
    "p": "#b46bc4",  # Pink: dark, middle, light, pale.
    "q": "#d98ed8",
    "r": "#f2b8ec",
    "s": "#ffe0fa",
    "g": "#8a5cb6",  # Lilac, between pink and violet: dark, middle, light, pale.
    "h": "#aa7fd2",
    "i": "#cba6ec",
    "j": "#ead4fb",
    "k": "#4762a6",  # Periwinkle, between violet and aqua: dark, middle, light, pale.
    "l": "#5f8ccc",
    "m": "#86b8e6",
    "n": "#c4e6fb",
    "c": "#4f9fc0",  # Aqua: dark, middle, light, pale.
    "d": "#72cfd6",
    "e": "#a6eee6",
    "f": "#dcfff6",
    "W": "#ffffff",  # Specular.
}

# First outline x per row; every row is mirrored about x = 9.5 (last x = 19 - first).
# A two-pixel tip widening in even stairs to an 18-wide belly that rounds off at the base.
LEFT = {1: 9, 2: 9, 3: 8, 4: 8, 5: 7, 6: 7, 7: 6, 8: 6, 9: 5, 10: 5, 11: 4, 12: 4, 13: 3, 14: 3,
        15: 2, 16: 2, 17: 2, **{y: 1 for y in range(18, 23)}, 23: 2, 24: 2, 25: 3, 26: 4, 27: 6}
SPANS = {y: (x0, 19 - x0) for y, x0 in LEFT.items()}
CAUSTIC = (12.0, 22.6, 4.3, 2.8)  # centre and radii of the refracted glow in the lower right


def family(x, y):
    """Shimmer's hue drifts diagonally: pink at the tip, violet across the middle, aqua below right."""
    t = 0.72 * (y - 1) / 26 + 0.38 * x / 19
    for limit, ramp in ((0.41, "pqrs"), (0.49, "ghij"), (0.68, "0123"), (0.76, "klmn")):
        if t < limit:
            return ramp
    return "cdef"


def build_grid():
    grid = [["."] * WIDTH for _ in range(HEIGHT)]
    mask = {(x, y) for y, (x0, x1) in SPANS.items() for x in range(x0, x1 + 1)}

    def depth(x, y):
        """Rings in from the silhouette: 0 is the outline itself."""
        for d in range(4):
            if any((x + dx, y + dy) not in mask for dx in range(-d - 1, d + 2) for dy in range(-d - 1, d + 2)
                   if abs(dx) + abs(dy) == d + 1):
                return d
        return 4

    cx, cy, rx, ry = CAUSTIC
    for x, y in mask:
        x0, x1 = SPANS[y]
        u = (x + 0.5 - 10) / ((x1 - x0 + 1) / 2)
        d = depth(x, y)
        value = 2
        if u > 0.35:
            value = 1  # shadow side
        if d == 1:
            value -= 1  # darker inner rim gives the drop its roundness
        if math.hypot((x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry) <= 1:
            value = 3 if d > 1 else 2  # light refracted through the drop pools in the lower right
        grid[y][x] = family(x, y)[max(0, min(3, value))]
    # Glossy streak curving down the upper-left flank, just inside the rim.
    for x, y in ((9, 4), (8, 6), (7, 8), (6, 10), (5, 12)):
        grid[y][x] = "W"
    for x, y in ((8, 5), (7, 7), (6, 9), (5, 11), (4, 13), (4, 14)):
        grid[y][x] = "s"
    # Faint secondary reflection on the lower-left belly.
    for x, y in ((3, 19), (3, 20), (4, 22)):
        grid[y][x] = family(x, y)[3]
    # A four-point twinkle inside the drop.
    sx, sy = 9, 16
    for dx, dy in ((0, -2), (0, 2), (-2, 0), (2, 0)):
        grid[sy + dy][sx + dx] = family(sx + dx, sy + dy)[3]
    for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0)):
        grid[sy + dy][sx + dx] = "s"
    grid[sy][sx] = "W"
    # Tiny bubbles caught in the glow.
    grid[21][13] = "W"
    grid[23][15] = "f"
    return blacken_silhouette(grid)


def build_sprite():
    grid = build_grid()
    return render(grid, PALETTE), grid


def apply(image, grid):
    image.save(ASSETS / f"{NAME}_Pixel.png")
    outline_ring(image).save(UI / f"Outlines/{NAME}_Pixel.png", optimize=True)
    used = sorted({s for row in grid for s in row} - {"."})
    draft = {
        "name": f"{NAME} - upright iridescent teardrop of shimmer",
        "source_note": "Hand-authored. Rebuild with Tools/PotionPixel/GenerateShimmerDropletPixel.py --apply.",
        "size": [WIDTH, HEIGHT],
        "palette": {".": PALETTE["."], **{s: PALETTE[s] for s in used}},
        "pixels": ["".join(row) for row in grid],
    }
    (OUT / f"{NAME}.pixel.json").write_text(json.dumps(draft, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--apply", action="store_true", help="write the live sprite, outline and pixel draft")
    args = parser.parse_args()
    sprite, grid = build_sprite()
    alpha = sprite.getchannel("A")
    assert alpha.tobytes() == alpha.transpose(Image.Transpose.FLIP_LEFT_RIGHT).tobytes(), "silhouette must be symmetric"
    old = Image.open(ASSETS / f"{NAME}_Pixel.png").convert("RGBA")
    assert sprite.size == old.size, f"{sprite.size} does not match the catalog size {old.size}"
    write_previews(OUT, NAME, old, sprite)
    if args.apply:
        apply(sprite, grid)
    colors = sum(1 for _, c in sprite.getcolors() if c[3])
    print(f"{NAME} pixel: {sprite.width}x{sprite.height}, {colors} opaque colors, bounds {sprite.getbbox()}"
          + (", applied" if args.apply else ", study only"))
    for row in grid:
        print("".join(row))


if __name__ == "__main__":
    main()
