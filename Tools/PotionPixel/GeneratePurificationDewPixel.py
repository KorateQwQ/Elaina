"""Extract the approved lower-left Purification Dew without resampling.

Retain both disconnected components of the blue dropper. Rebuild the notebook
mystery icon and outlines afterwards (see PurificationDew.md).
"""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'ElainaModAlchemy/item/Untitled 10152026 050309.png'
TARGET = ROOT / 'ElainaModAlchemy/item/ExampleAssets/PurificationDew_Pixel.png'


def build_sprite():
    with Image.open(SOURCE) as original:
        region = original.convert('RGBA').crop((0, 32, 36, 64))
        sprite = region.crop(region.getchannel('A').getbbox())
    if sprite.size != (27, 28):
        raise ValueError('The approved lower-left dropper must be 27x28')
    return sprite


def main():
    sprite = build_sprite()
    sprite.save(TARGET)
    preview = ROOT / 'Tools/PotionPixel/preview/PurificationDew_Pixel_x8.png'
    sprite.resize((216, 224), Image.Resampling.NEAREST).save(preview)
    print('Promoted lower-left Purification Dew (27x28), without resampling.')


if __name__ == '__main__':
    main()
