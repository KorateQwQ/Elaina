"""Build pixel-art mystery icons from the active alchemy item sprites."""

from pathlib import Path
import re

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / 'ElainaModAlchemy/UI/AlchemyCatalog.cs'
SOURCE = ROOT / 'ElainaModAlchemy/item/ExampleAssets'
TARGET = ROOT / 'ElainaModAlchemy/UI/Assets/MysteryPixel'
QUESTION = (
    ' ##### ',
    '##   ##',
    '     ##',
    '    ## ',
    '   ##  ',
    '   #   ',
    '       ',
    '   #   ',
    '   #   ',
)


def build(name):
    with Image.open(SOURCE / f'{name}_Pixel.png') as original:
        alpha = original.convert('RGBA').getchannel('A')
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError(f'{name} has no visible pixels')
    width, height = alpha.size
    image = Image.new('RGBA', alpha.size)
    pixels = image.load()
    for y in range(height):
        for x in range(width):
            if not alpha.getpixel((x, y)):
                continue
            edge = any(
                not 0 <= nx < width or not 0 <= ny < height or not alpha.getpixel((nx, ny))
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))
            )
            pixels[x, y] = (113, 95, 129, 255) if edge else (70, 59, 84, 255)

    left = (bounds[0] + bounds[2] - len(QUESTION[0])) // 2
    top = (bounds[1] + bounds[3] - len(QUESTION)) // 2
    for y, row in enumerate(QUESTION):
        for x, char in enumerate(row):
            if char != ' ':
                pixels[left + x, top + y] = (230, 195, 237, 255)
    return image


def main():
    catalog = CATALOG.read_text(encoding='utf-8')
    names = re.findall(r'Category = "[^"]+", Art = "([^"]+)"', catalog)
    if not names or len(names) != len(set(names)):
        raise ValueError('Expected distinct artwork names in the alchemy catalog')
    TARGET.mkdir(parents=True, exist_ok=True)
    for name in names:
        build(name).save(TARGET / f'{name}_Mystery_Pixel.png')
    print(f'Prepared {len(names)} pixel mystery icons from active alchemy item sprites.')


if __name__ == '__main__':
    main()
