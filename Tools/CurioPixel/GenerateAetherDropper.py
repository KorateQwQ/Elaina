"""Upright Aether Dropper item sprite, drawn on a native 19x34 grid.

Rebuilds the runtime PNG, editable pixel grid and nearest-neighbor review.
The bulb, level gold collar and glass tip form one connected item; no drip.
"""
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
OUTPUT = HERE / 'AetherDropper'
RUNTIME = ROOT / 'ElainaModAlchemy/item/ExampleAssets/AetherDropper_Pixel.png'
REFERENCE = ROOT / 'ElainaModAlchemy/item/ExampleAssets/PurificationDew_Pixel.png'
SIZE = (19, 34)
PALETTE = {
    '.': '#00000000',
    'O': '#554665', 'E': '#88739b',
    'S': '#8966a4', 'P': '#ac84c8', 'L': '#c8a4e0', 'H': '#efdaff',
    'D': '#8393b3', 'G': '#bfd0e4', 'I': '#edf6fa',
    'B': '#97d4db', 'C': '#76acbf', 'V': '#b7a0e0', 'R': '#e3aed1',
    'Q': '#997a59', 'K': '#c3a475', 'A': '#e4c996', 'F': '#fff0c8',
}

# Each row is (left x, palette symbols). The silhouette shares x=9 with
# Purification Dew; material lighting remains on the upper left.
# The glass encloses the cyan/lilac/pink shimmer all the way to its tip.
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
    colors = {key: Image.new('RGBA', (1, 1), color).getpixel((0, 0))
              for key, color in PALETTE.items()}
    sprite = Image.new('RGBA', SIZE)
    for y, (left, row) in enumerate(ROWS):
        assert 0 <= left <= left + len(row) <= SIZE[0]
        assert not row or 2 * left + len(row) == SIZE[0], ('off-center', y)
        for x, symbol in enumerate(row, left):
            sprite.putpixel((x, y), colors[symbol])
    return sprite


def review(sprite, before):
    with Image.open(REFERENCE) as source:
        reference = source.convert('RGBA')
    sheet = Image.new('RGB', (960, 710), '#241f32')
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default(size=17)
    for top, bg, ink in ((0, '#241f32', '#e0d4ea'), (355, '#e9dfd0', '#53465d')):
        draw.rectangle((0, top, 959, top + 354), fill=bg)
        for col, (label, im) in enumerate((('BEFORE', before),
                                         ('AETHER / UPRIGHT', sprite),
                                         ('PURIFICATION / REFERENCE', reference))):
            left = col * 320
            draw.text((left + 16, top + 12), label, font=font, fill=ink)
            for scale, x, y in ((8, left + 22, top + 45),
                                (1, left + 241, top + 72),
                                (2, left + 227, top + 169)):
                enlarged = im.resize((im.width * scale, im.height * scale), Image.Resampling.NEAREST)
                sheet.paste(enlarged, (x, y), enlarged)
                draw.text((x, y + enlarged.height + 9), f'{scale}x', font=font, fill=ink)
    sheet.save(OUTPUT / 'AetherDropper_Review.png')


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    before_path = OUTPUT / 'AetherDropper_Before.png'
    if not before_path.exists():
        before_path.write_bytes(RUNTIME.read_bytes())
    sprite = build_sprite()
    visible = {(x, y) for y in range(sprite.height) for x in range(sprite.width)
               if sprite.getpixel((x, y))[3]}
    pending = [next(iter(visible))]
    seen = set(pending)
    while pending:
        x, y = pending.pop()
        for neighbor in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if neighbor in visible and neighbor not in seen:
                seen.add(neighbor)
                pending.append(neighbor)
    assert seen == visible, 'Detached drop or stray pixels'
    pixels = list(sprite.get_flattened_data())
    assert {p[3] for p in pixels} == {0, 255}
    sprite.save(RUNTIME)
    sprite.save(OUTPUT / 'AetherDropper_Pixel.png')
    sprite.resize((SIZE[0] * 8, SIZE[1] * 8), Image.Resampling.NEAREST).save(
        OUTPUT / 'AetherDropper_Upright_x8.png')
    grid = ['.' * left + row + '.' * (SIZE[0] - left - len(row)) for left, row in ROWS]
    draft = {'name': 'Aether Dropper - upright, no falling drop',
             'source_note': 'Based on the upright Purification Dew silhouette; retains the Aether Dropper purple bulb, gold collar and shimmer inside the glass. Rebuild with Tools/CurioPixel/GenerateAetherDropper.py.',
             'size': list(SIZE), 'palette': PALETTE, 'pixels': grid}
    (OUTPUT / 'AetherDropper_Pixel.json').write_text(json.dumps(draft, indent=2) + '\n', encoding='utf-8')
    with Image.open(before_path) as source:
        review(sprite, source.convert('RGBA'))
    metrics = {'size': list(SIZE), 'visibleColors': len({p for p in pixels if p[3]}),
               'alpha': sorted({p[3] for p in pixels}), 'bounds': list(sprite.getbbox()),
               'opaqueComponents': 1, 'runtime': RUNTIME.relative_to(ROOT).as_posix()}
    (OUTPUT / 'metrics.json').write_text(json.dumps(metrics, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(metrics))


if __name__ == '__main__':
    main()
