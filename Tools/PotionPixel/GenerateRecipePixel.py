"""Hand-authored sprite for the generic recipe item (34x34): a slanted sheet of parchment, after the user's pixel sketch.

The sheet is shown at a slant, leaning to the right. Its top edge rolls a little backwards:
the bend faces up into the light with a glint along it, and the roll's hollow end shows at
the upper right. Its bottom edge rolls a little forwards: the paper's edge is a dark line
across, the curl below it shaded as a tube, its hollow end hooked at the left, and past its
right end the flat sheet shows. The sheet darkens towards its right edge and carries a few
age spots and staggered words of ink. Light from the upper left, pure black outer
silhouette, all pixels opaque, nothing floating free.

python Tools/PotionPixel/GenerateRecipePixel.py           # previews in Tools/PotionPixel/output
python Tools/PotionPixel/GenerateRecipePixel.py --apply   # also write the live asset
Requires Pillow.
"""

import argparse
import json
from pathlib import Path

from PIL import Image

from pixel_common import blacken_silhouette, render, write_previews

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "ElainaModAlchemy/item/ExampleAssets"
OUT = ROOT / "Tools/PotionPixel/output"
NAME = "Recipe"

PALETTE = {
    ".": "#00000000", "O": "#000000",
    # Parchment, dark to light, a glint along the top bend, and the dark of the rolled edges and roll ends.
    "0": "#a8845a", "1": "#c9a874", "2": "#e2c897", "3": "#f2e3c0", "4": "#fbf0d6", "5": "#fffaee",
    "i": "#6e4e32",
    # Ink.
    "t": "#5e4e72", "u": "#9a8aae",
}

PIXELS = (
    "..................................",
    "..................................",
    "............OOOOOOOOOOOOOOOOOOOOO.",
    "...........O445555555555545443443O",
    "..........O4444444444444444332ii4O",
    ".........O4433333333333333333i002O",
    ".........O4433333333322333322i0i1O",
    ".........O443333333333333321O2ii1O",
    "........O443333333333333221O.O11O.",
    "........O44333tttu3ttttu22O...OO..",
    "........O443333333333333221O......",
    ".......O4433333333333333221O......",
    ".......O4ttttu3ttttttu33221O......",
    "......O4433333333333333221O.......",
    "......O4433333333333333221O.......",
    "......O443ttttu3tttu333221O.......",
    "......O4333333333333333221O.......",
    "......O433332333333333221O........",
    "......O433333333333333221O........",
    "......O43333333tttu3ttu21O........",
    ".....O433333333333333221O.........",
    ".....O433333333333333221O.........",
    ".....O43ttttu3tttttttu21O.........",
    "....O4333333333333333221O.........",
    "....O4333333333333323221O.........",
    ".O..O422222222222223221O..........",
    "OiOOiiiiiiiiiiiiiiii221O..........",
    "O00i4444444443333330221O..........",
    "Oi0i2222222222222220211O..........",
    ".OO1111111111111111O11O...........",
    "...OOOOOOOOOOOOOOOOOOOO...........",
    "..................................",
    "..................................",
    "..................................",
)


def build():
    assert all(len(row) == len(PIXELS[0]) for row in PIXELS)
    grid = blacken_silhouette([list(row) for row in PIXELS])
    return render(grid, PALETTE), grid


def apply(sprite, grid):
    sprite.save(ASSETS / f"{NAME}_Pixel.png")
    data = {
        "name": f"{NAME} - hand-authored generic recipe sprite",
        "source_note": "Hand-authored. Rebuild with Tools/PotionPixel/GenerateRecipePixel.py --apply.",
        "size": [sprite.width, sprite.height],
        "palette": dict(sorted(PALETTE.items())),
        "pixels": ["".join(row) for row in grid],
    }
    (ASSETS / f"{NAME}.pixel.json").write_text(json.dumps(data, indent=2), encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--apply", action="store_true", help="write the live sprite")
    args = parser.parse_args()
    sprite, grid = build()
    live = ASSETS / f"{NAME}_Pixel.png"
    old = Image.open(live).convert("RGBA") if live.exists() else Image.new("RGBA", sprite.size)
    write_previews(OUT, NAME, old, sprite)
    if args.apply:
        apply(sprite, grid)
    colors = sum(1 for _, c in sprite.getcolors() if c[3])
    print(f"{NAME}: {sprite.width}x{sprite.height}, {colors} opaque colors" + (", applied" if args.apply else ""))


if __name__ == "__main__":
    main()
