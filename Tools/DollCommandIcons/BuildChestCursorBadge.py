"""Export the command-wheel bear head without its dismiss badge.

python Tools/DollCommandIcons/BuildChestCursorBadge.py
python Tools/DollCommandIcons/BuildChestCursorBadge.py --check
"""
import argparse
from pathlib import Path

from PIL import Image
from BuildDollCommandIcons import ROOT, kuma_head


def build():
    head = kuma_head(scale=1)
    image = Image.new("RGBA", (head.width + 2, head.height + 2))
    # A one-pixel dark silhouette stays legible on both bright chest rims and terrain.
    for y in range(head.height):
        for x in range(head.width):
            if head.getpixel((x, y))[3]:
                for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                    image.putpixel((x + 1 + dx, y + 1 + dy), (27, 18, 34, 255))
    image.alpha_composite(head, (1, 1))
    return image


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    path = Path(ROOT) / "ElainaModSkills/Skills/DollControl/Assets/ChestCursorBear.png"
    image = build()
    assert image.size == (19, 16)
    assert set(image.getchannel("A").tobytes()) == {0, 255}
    if args.check:
        with Image.open(path) as saved:
            assert saved.mode == "RGBA" and saved.size == image.size and saved.tobytes() == image.tobytes()
        print("PASS: packaged 19x16 bear badge matches its reproducible source; binary alpha.")
    else:
        path.parent.mkdir(parents=True, exist_ok=True)
        image.save(path)
        print(path)


if __name__ == "__main__":
    main()
