"""Approved upright Purification Dew sprite; Python + Pillow.

Uses the Iridescent SVG's materials on a new native grid for a held pipette.
Rebuilds the approved runtime item, editable pixel grid and design previews.
"""
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Tools/PotionBottleDesigns/output/round1/pixel'
PREVIEW = ROOT / 'Tools/PotionPixel/preview'
SIZE = (19, 34)
PALETTE = {
    'O': '#65566e', 'E': '#92839e',
    'S': '#b4a6c7', 'P': '#d6ccdf', 'L': '#eee5ef', 'H': '#fff8f0',
    'D': '#9b9cc2', 'G': '#d2d7ec', 'I': '#eff9fa',
    'B': '#a1dbe9', 'C': '#87bdd6', 'V': '#bbaae4', 'R': '#e5b5d4',
    'Q': '#997a59', 'K': '#c3a475', 'A': '#e4c996', 'F': '#fff0c8',
}

# (left x, colors) for each native y. Every part is centered at x=9.
# The bulb has a domed crown, the collar is level, the barrel tapers in pairs
# of rows. No detached drop: the sprite is intended as a held tool.
# Pearl and upper glass keep upper-left lighting. Liquid bands are mirrored
# around x=9, with a level meniscus and balanced blue, violet and pink widths.
ROWS = [
    (0, ''),
    (7, 'EEEEE'),
    (5, 'EELLLPSEO'),
    (4, 'ELHHLPPPSEO'),
    (4, 'EHLLPPPPSEO'),
    (4, 'EHLLPPPPSEO'),
    (4, 'ELLLPPPSSEO'),
    (4, 'ELLPPPPSSEO'),
    (4, 'ELPPPPPSSEO'),
    (4, 'ELPPPPPSSEO'),
    (4, 'EPPPPPPSSEO'),
    (4, 'ESPPPPPSSEO'),
    (4, 'QQQQQQQQQQQ'),
    (3, 'QHFFFFFAAAAKQ'),
    (3, 'QFAAAAAAAAKKQ'),
    (4, 'QKKKKKKKKKQ'),
    (5, 'OEIGGGGDO'),
    (5, 'OEIGGGGDO'),
    (5, 'OEIGGGGDO'),
    (5, 'OEIGGGGDO'),
    (5, 'OEIGGGGDO'),
    (5, 'ODIIIIIDO'),
    (5, 'ODCBBBCDO'),
    (5, 'ODCBBBCDO'),
    (6, 'OCBBBCO'),
    (6, 'OCVVVCO'),
    (6, 'ODVVVDO'),
    (7, 'OVRVO'),
    (7, 'OVRVO'),
    (8, 'ERE'),
    (8, 'ERE'),
    (8, 'EIE'),
    (8, 'EOE'),
    (0, ''),
]


def build_sprite():
    assert len(ROWS) == SIZE[1]
    colors = {key: tuple(bytes.fromhex(value[1:])) + (255,) for key, value in PALETTE.items()}
    im = Image.new('RGBA', SIZE)
    for y, (left, row) in enumerate(ROWS):
        assert 0 <= left <= left + len(row) <= SIZE[0]
        assert not row or left * 2 + len(row) == SIZE[0], ('off-center silhouette', y)
        for x, key in enumerate(row, left):
            im.putpixel((x, y), colors[key])
    return im


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    im = build_sprite()
    im.save(OUTPUT / 'PurificationDew_Upright.png')
    im.save(OUTPUT / 'PurificationDew.png')
    im.save(ROOT / 'ElainaModAlchemy/item/ExampleAssets/PurificationDew_Pixel.png')
    im.resize((SIZE[0]*8, SIZE[1]*8), Image.Resampling.NEAREST).save(PREVIEW / 'PurificationDew_Upright_x8.png')
    im.resize((SIZE[0]*8, SIZE[1]*8), Image.Resampling.NEAREST).save(PREVIEW / 'PurificationDew_Pixel_x8.png')
    lookup = {tuple(bytes.fromhex(value[1:])) + (255,): key for key, value in PALETTE.items()}
    draft = {
        'name': 'Purification Dew - upright held pipette, symmetric liquid, no falling drop',
        'source_note': 'Approved upright adaptation of PurificationDew_Iridescent.svg for handheld use, with balanced mirrored liquid bands. Rebuild with Tools/PotionPixel/GeneratePurificationDewUprightPixel.py, which updates the runtime item.',
        'size': list(SIZE), 'palette': {'.': '#00000000', **PALETTE},
        'pixels': [''.join(lookup.get(im.getpixel((x,y)), '.') for x in range(SIZE[0])) for y in range(SIZE[1])],
    }
    (OUTPUT / 'PurificationDew_Upright.pixel.json').write_text(json.dumps(draft, indent=2)+'\n', encoding='utf-8')
    (OUTPUT / 'PurificationDew.pixel.json').write_text(json.dumps(draft, indent=2)+'\n', encoding='utf-8')
    colors = {p for p in im.get_flattened_data() if p[3]}
    print(f'Purification Dew upright: {SIZE}, {len(colors)} colors, bounds {im.getbbox()}')


if __name__ == '__main__':
    main()
