"""Rebuild the mana elixir's 20x28 item sprite from an editable pixel grid.

The supplied reference bottles define the stepped glass silhouette and pink bow.
The elixir adds its own violet liquid ramp and compact star mark. All visible
pixels are opaque; no antialiasing, resampling, or procedural gradients are used.
This produces a separate art study and does not replace the live item texture.

Run from any directory: python Tools/PotionPixel/GenerateManaElixirPixel.py
Requires Pillow.
"""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "ElainaModAlchemy/item/RuriPotionPixel.png"
PALETTE = {
    ".": (0, 0, 0, 0),
    "o": "#332f43",  # Shared dark outline.
    "s": "#57799a",  # Glass: shadow, body, rim, highlight.
    "g": "#7c9dbc",
    "h": "#b1cde8",
    "w": "#eaf5ff",
    "n": "#2a244b",  # Violet liquid: five discrete value steps.
    "d": "#3c306f",
    "m": "#53469c",
    "v": "#7469cc",
    "l": "#b5a0ec",
    "r": "#9c1857",  # Bow: shadow, midtone, light.
    "q": "#b9658d",
    "p": "#e69cbf",
}
PIXELS = (
    ".....oooooooooo.....",
    "....ohhwwwwwwhho....",
    "....ohhwwwwwwhho....",
    "....ohhwwwwwwhho....",
    "....orrrggggrrro....",
    "....orpprrrrppro....",
    ".....rppqppqppr.....",
    ".....rppprrpppr.....",
    ".....rrrrrgrrrrr....",
    "......rqqrgssoqr....",
    ".....orpqrgsssoqr...",
    "....orqprggssssor...",
    "...owrpprggssssgo...",
    "..owrppprwwwwsssgo..",
    ".owgrpprgwwwwssssgo.",
    "owggrpprgggsssssssgo",
    "owggrrrggggsssssssgo",
    "owvvvvvvvvvllvvvvvgo",
    "owvvvvvvvvvllvvvvvgo",
    "ohmmllvvvmmvvvvvmmgo",
    "ohmmllvvwmmvvvvvmmgo",
    "ohddmmmwwwvvvvmmddgo",
    "ohddmmmmwvvvvvlmddgo",
    "ogddddddddddddddddgo",
    "ognnddddddddddddnngo",
    ".osnnnnnnnnnnnnnnso.",
    "..ogggghhghhhggggo..",
    "...oooooooooooooo...",
)


def build_sprite():
    image = Image.new("RGBA", (20, 28))
    assert len(PIXELS) == image.height
    for y, row in enumerate(PIXELS):
        assert len(row) == image.width, f"Invalid pixel row {y}"
        for x, symbol in enumerate(row):
            color = PALETTE[symbol]
            if isinstance(color, str):
                color = tuple(int(color[i:i + 2], 16) for i in (1, 3, 5)) + (255,)
            image.putpixel((x, y), color)
    return image


if __name__ == "__main__":
    sprite = build_sprite()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(OUTPUT)
    print(f"Saved {OUTPUT.name}: {sprite.width}x{sprite.height}, RGBA, 13 opaque colors")
