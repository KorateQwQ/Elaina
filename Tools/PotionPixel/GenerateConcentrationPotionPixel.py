"""Approved upright Concentration Potion from the round1 SVG.

Run with Python + Pillow. Rebuilds the approved runtime item, editable pixel
grid and nearest-neighbor previews.
"""
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Tools/PotionBottleDesigns/output/round1/pixel'
PREVIEW = ROOT / 'Tools/PotionPixel/preview'
SIZE = (19, 38)
PALETTE = {
    'O': '#51415f',  # violet contour
    'E': '#8b7a9d',  # lit glass edge / lip shadow
    'D': '#aaa0c7',  # shaded glass
    'G': '#d9d3ed',  # empty glass
    'H': '#f6faf3',  # focused reflection
    'S': '#7b5761',  # cork shade
    'K': '#b58b72',  # cork middle
    'A': '#e1bd8d',  # cork highlight
    'F': '#f4dbb0',  # cork top
    'T': '#a6ebce',  # liquid meniscus
    'B': '#54b997',  # lit liquid
    'M': '#2b967b',  # green liquid
    'V': '#226b60',  # shaded liquid
    'P': '#e8dfc9',  # parchment label
    'Q': '#b7af9b',  # label shadow
    'R': '#627d79',  # reticle / label edge
}

# Upright silhouette, symmetric about x=9. Cork, lip and tube share one axis.
# The straight walls turn into a rounded bottom; shading comes from upper left.
ROWS = [
    (0, ''),
    (5, 'SSSSSSSSS'),
    (4, 'SFFFFFAAAKS'),
    (4, 'SAAAAAKKKKS'),
    (4, 'SAKAAKKKKSS'),
    (4, 'SSKKKKSSSSS'),
    (3, 'EEEEEEEEEEEEE'),
    (2, 'EHGGGGGGGGGGDEO'),
    (3, 'OEEEEEEEEEEDO'),
    (3, 'OEGGGGGGGGDDO'),
    (3, 'OEHGGGGGGGDDO'),
    (3, 'OEHGGGGGGGDDO'),
    (3, 'OEHGGGGGHHDDO'),
    (3, 'OEHGGGGGGGDDO'),
    (3, 'OEHGGGGGGGDDO'),
    (3, 'OEHGGGGGGHDDO'),
    (3, 'OEHGGGGGGGDDO'),
    (3, 'OEGGGGGGGGDDO'),
    (3, 'OETTTTTTTTTDO'),
    (3, 'OEHBBBBMMMVDO'),
    (3, 'OEHBBMMMMMVDO'),
    (3, 'OEHBBMMMMMVDO'),
    (3, 'OEHBBMMMMMVDO'),
    (3, 'OEHBBMMMMMVDO'),
    (3, 'OEHBBMMMMMVDO'),
    (3, 'OEHBBMMMMMVDO'),
    (3, 'OEHBBMMMMMVDO'),
    (3, 'OEHBBMMMMMVDO'),
    (3, 'OEHBBMMMMMVDO'),
    (3, 'OEBBBMMMMMVDO'),
    (3, 'OEBBMMMMMMVDO'),
    (3, 'OEBMMMMMMVVDO'),
    (4, 'OEBMMMMVVDO'),
    (4, 'OEMMMMMVVDO'),
    (5, 'OEEMMVDDO'),
    (7, 'OOOOO'),
    (0, ''),
    (0, ''),
]

# Centered parchment and a five-pixel ring with four short crosshair arms.
LABEL = [
    (6, 21, 'QQQQQQQ'),
    (5, 22, 'QPPPRPPPQ'),
    (5, 23, 'PPPRRRPPQ'),
    (5, 24, 'PPRPPPRPQ'),
    (5, 25, 'PRRPPPRRQ'),
    (5, 26, 'PPRPPPRPQ'),
    (5, 27, 'PPPRRRPPQ'),
    (5, 28, 'QPPPRPPPQ'),
    (6, 29, 'QQQQQQQ'),
]


def build_sprite():
    assert len(ROWS) == SIZE[1]
    im = Image.new('RGBA', SIZE)
    colors = {key: tuple(bytes.fromhex(value[1:])) + (255,) for key, value in PALETTE.items()}
    for y, (left, row) in enumerate(ROWS):
        assert 0 <= left <= left + len(row) <= SIZE[0]
        for x, key in enumerate(row, left):
            im.putpixel((x, y), colors[key])
    for left, y, row in LABEL:
        for x, key in enumerate(row, left):
            # A label cannot enlarge the tube silhouette or cover its contour.
            assert im.getpixel((x,y))[3] and x > ROWS[y][0] and x < ROWS[y][0]+len(ROWS[y][1])-1, (x,y)
            im.putpixel((x,y), colors[key])
    return im


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    im = build_sprite()
    im.save(OUTPUT / 'ConcentrationPotion.png')
    im.save(ROOT / 'ElainaModAlchemy/item/ExampleAssets/ConcentrationPotion_Pixel.png')
    im.resize((SIZE[0]*8, SIZE[1]*8), Image.Resampling.NEAREST).save(PREVIEW / 'ConcentrationPotion_Study_x8.png')
    im.resize((SIZE[0]*8, SIZE[1]*8), Image.Resampling.NEAREST).save(PREVIEW / 'ConcentrationPotion_Pixel_x8.png')
    lookup = {tuple(bytes.fromhex(value[1:])) + (255,): key for key, value in PALETTE.items()}
    draft = {
        'name': 'Concentration Potion - upright green laboratory tube',
        'source_note': 'Hand-drawn upright adaptation from Tools/PotionBottleDesigns/output/round1/svg/ConcentrationPotion.svg. Rebuild with Tools/PotionPixel/GenerateConcentrationPotionPixel.py.',
        'size': list(SIZE), 'palette': {'.': '#00000000', **PALETTE},
        'pixels': [''.join(lookup.get(im.getpixel((x,y)), '.') for x in range(SIZE[0])) for y in range(SIZE[1])],
    }
    (OUTPUT / 'ConcentrationPotion.pixel.json').write_text(json.dumps(draft, indent=2)+'\n', encoding='utf-8')
    colors = {p for p in im.get_flattened_data() if p[3]}
    print(f'ConcentrationPotion: {SIZE}, {len(colors)} opaque colors, bounds {im.getbbox()}')


if __name__ == '__main__':
    main()
