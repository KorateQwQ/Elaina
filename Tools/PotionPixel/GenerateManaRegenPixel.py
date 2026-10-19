"""Build the continuous mana regeneration potion's item sprite.

An hourglass flask: the upper bulb drains a thin violet stream into the lower
bulb, which reads as recovery over time rather than the Moon Dew Elixir's
stored one-shot charges. Palette is shared with the notebook potion family
(outline, glass, cork, pink bow); all pixels are opaque with no antialiasing.

Run from any directory: python Tools/PotionPixel/GenerateManaRegenPixel.py
Requires Pillow.
"""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "ElainaModAlchemy/item/ExampleAssets/ManaRegenPotion_Pixel.png"
PREVIEW = ROOT / "Tools/PotionPixel/output/ManaRegenPotion_Preview.png"
PALETTE = {
    ".": (0, 0, 0, 0),
    "a": "#51415f",  # Shared outline.
    "b": "#eed5a6",  # Cork: top, light, body, shadow.
    "i": "#e1bd8d",
    "c": "#b58b72",
    "d": "#7b5761",
    "k": "#fff5f1",  # Glass: highlight, body, inner shadow, dark.
    "e": "#d4ccea",
    "j": "#a79ac5",
    "f": "#88719b",
    "l": "#b4a4f1",  # Violet mana liquid, light to dark.
    "m": "#8879d7",
    "o": "#6753b7",
    "n": "#494080",
    "r": "#ffd1e3",  # Bow: light, body, shadow.
    "p": "#e697bd",
    "q": "#ab648f",
}
WIDTH = 25
# The bow carries its own pink edge, like the Moon Dew Elixir's.
UNOUTLINED = ".arpq"
# Interior runs as (first x, symbols); the outline is added around them.
ROWS = (
    (),
    (),
    ((9, "bbbbbbb"),),
    ((9, "icccccc"),),
    ((9, "icccccd"),),
    ((9, "ddddddd"),),
    ((8, "keeeeeeej"),),
    ((5, "qqq"), (10, "keeej"), (17, "qqq")),
    ((4, "qrppq"), (10, "keeej"), (16, "qpprq")),
    ((4, "qprpp" + "qq" + "rpr" + "qq" + "pprpq"),),
    ((5, "qppq"), (10, "kqpqj"), (16, "qppq")),
    ((6, "qq"), (8, "keqeeeqej"), (17, "qq")),
    ((6, "kkeeeeeeeeejj"),),
    ((5, "kkeeeeeeeeeeeej"),),
    ((5, "keekeeeeeeeeeej"),),
    ((5, "k" + "lllll" + "eee" + "lllll" + "j"),),
    ((6, "k" + "mmmmm" + "l" + "mmmmo" + "o"),),
    ((7, "kmmmmlmmmoo"),),
    ((9, "lmmlmon"),),
    ((10, "omlmn"),),
    ((11, "klj"),),
    ((11, "klj"),),
    ((10, "kelej"),),
    ((8, "keeeeeeej"),),
    ((6, "keeeeeleeeeej"),),
    ((5, "keeeeeeeeeeeeej"),),
    ((4, "k" + "eeeeee" + "kek" + "eeeeee" + "j"),),
    ((4, "k" + "llllll" + "lkl" + "llllll" + "m"),),
    ((3, "k" + "m" * 17 + "o"),),
    ((3, "k" + "mmmmm" + "k" + "mmmmmmmm" + "l" + "mm" + "o"),),
    ((3, "k" + "mmmm" + "kkk" + "m" * 9 + "oo"),),
    ((3, "k" + "mmmmm" + "k" + "m" * 10 + "oo"),),
    ((3, "l" + "o" * 11 + "l" + "o" * 5 + "n"),),
    ((4, "l" + "o" * 9 + "klk" + "o" * 3 + "n"),),
    ((4, "l" + "o" * 5 + "l" + "o" * 4 + "l" + "o" * 4 + "n"),),
    ((5, "l" + "n" * 14),),
    ((7, "n" * 11),),
    (),
)


def to_rgba(symbol):
    color = PALETTE[symbol]
    if isinstance(color, str):
        color = tuple(int(color[i:i + 2], 16) for i in (1, 3, 5)) + (255,)
    return color


def build_sprite():
    grid = [["."] * WIDTH for _ in ROWS]
    for y, runs in enumerate(ROWS):
        for start, symbols in runs:
            assert start + len(symbols) <= WIDTH, f"Row {y} overflows"
            grid[y][start:start + len(symbols)] = symbols

    height = len(grid)
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


if __name__ == "__main__":
    sprite = build_sprite()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(OUTPUT)
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview = Image.new("RGBA", (sprite.width * 10, sprite.height * 10), (40, 36, 58, 255))
    preview.alpha_composite(sprite.resize(preview.size, Image.Resampling.NEAREST))
    preview.save(PREVIEW)
    colors = sum(1 for _, c in sprite.getcolors() if c[3])
    print(f"Saved {OUTPUT.name}: {sprite.width}x{sprite.height}, RGBA, {colors} opaque colors")
