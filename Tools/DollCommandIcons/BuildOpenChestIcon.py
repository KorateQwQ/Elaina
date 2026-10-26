"""Draw only the open-chest command icon on the existing 20 px grid.

python Tools/DollCommandIcons/BuildOpenChestIcon.py
python Tools/DollCommandIcons/BuildOpenChestIcon.py --apply

The preview stays in output/pixel; --apply writes the new PNG asset only.
No command registration or game logic is changed.
"""
import argparse
from pathlib import Path

from PIL import Image, ImageDraw

from BuildPixelDollCommandIcons import Layer, compose, APPLY_DIR, PIXEL_OUT


NAME = "DollCommand_OpenChest"


def icon_open_chest():
    lid, well, front = Layer(), Layer(), Layer()

    # Raised lid: stepped silhouette, warm wood and two brass straps.
    lid.rect(5, 2, 14, 2, "y")
    lid.rect(3, 3, 16, 3, "Y")
    lid.rect(3, 4, 16, 8, "d")
    lid.rect(4, 4, 15, 4, "w")
    lid.rect(5, 5, 14, 7, "W")
    lid.rect(6, 5, 13, 5, "w")
    lid.rect(6, 7, 13, 7, "d")
    lid.rect(3, 8, 16, 8, "o")
    for x in (4, 14):
        lid.rect(x, 3, x, 8, "Y")
        lid.set(x, 3, "r")
        lid.set(x, 5, "y")
    lid.rect(16, 4, 16, 8, "o")

    # A clearly visible dark cavity separates the raised lid from the box.
    well.rect(3, 9, 16, 11, "x")
    well.rect(5, 9, 14, 10, "K")
    well.rect(3, 8, 3, 11, "d")
    well.rect(16, 8, 16, 11, "d")
    well.rect(2, 10, 2, 12, "Y")
    well.set(3, 9, "y")
    well.rect(17, 10, 17, 12, "o")
    well.set(16, 9, "Y")

    # Front face: restrained plank shading, brass bands and a central catch.
    front.rect(2, 12, 17, 16, "W")
    front.rect(3, 17, 16, 17, "d")
    front.rect(2, 12, 16, 12, "y")
    front.rect(3, 13, 16, 13, "w")
    front.rect(3, 15, 16, 15, "d")
    front.rect(3, 16, 16, 16, "W")
    front.rect(16, 13, 17, 16, "d")
    for x in (4, 14):
        front.rect(x, 13, x, 17, "Y")
        front.set(x, 13, "r")
        front.set(x, 16, "y")
        front.set(x, 17, "o")
    front.rect(9, 12, 10, 14, "Y")
    front.rect(9, 12, 10, 12, "r")
    front.set(9, 13, "y")
    front.set(10, 14, "o")
    return compose(lid, well, front)


def preview(icon):
    sheet = Image.new("RGBA", (460, 260), (22, 18, 31, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((20, 16), "OPEN CHEST / 40 x 40 RGBA", fill="#e6d9ff")
    sheet.alpha_composite(icon.resize((200, 200), Image.Resampling.NEAREST), (16, 46))
    draw.text((250, 52), "1x", fill="#a3a9bd")
    sheet.alpha_composite(icon, (250, 76))
    draw.text((250, 140), "EXISTING ICONS", fill="#a3a9bd")
    for i, name in enumerate(("Cancel", "Mine", "BreakWall")):
        with Image.open(Path(APPLY_DIR) / f"DollCommand_{name}.png") as existing:
            sheet.alpha_composite(existing.convert("RGBA"), (250 + i * 60, 166))
    return sheet


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    output = Path(PIXEL_OUT)
    output.mkdir(parents=True, exist_ok=True)
    icon = icon_open_chest()
    icon.save(output / f"{NAME}.png")
    preview(icon).save(output / f"_preview_{NAME}.png")
    # Binary alpha means straight and premultiplied RGBA are identical.
    assert icon.size == (40, 40)
    assert set(icon.getchannel("A").tobytes()) == {0, 255}
    pixels = icon.tobytes()
    assert all(pixels[i + 3] or pixels[i:i + 3] == b"\x00\x00\x00"
               for i in range(0, len(pixels), 4))
    if args.apply:
        target = Path(APPLY_DIR) / f"{NAME}.png"
        icon.save(target)
        print(target)
    print(output / f"_preview_{NAME}.png")


if __name__ == "__main__":
    main()
