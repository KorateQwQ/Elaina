"""Build the Painkiller notebook pencil drawing and hand-authored pixel jar."""
import argparse
import json
from pathlib import Path
import subprocess
import sys
from urllib.parse import quote, unquote

from PIL import Image, ImageDraw

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / 'ElainaModAlchemy/item/ExampleAssets'
UI = ROOT / 'ElainaModAlchemy/UI/Assets'
GENERATED = ROOT / 'Tools/AlchemyPlan/generated'
PREVIEW = ROOT / 'Tools/PotionPixel/preview'
sys.path.insert(0, str(ROOT / 'Tools/AlchemyPlan'))
sys.path.insert(0, str(ROOT / 'Tools/AlchemyPreview'))
from pencil_style import convert
from prepare_pencil import premultiply

SIZE = (32, 34)
COLORS = {
    'outline': '#403749', 'glass_edge': '#748d86',
    'glass': '#b4c6b2', 'glass_light': '#e7e8cc',
    'glass_shadow': '#8da598', 'reflection': '#fcf7dd',
    'cork_shadow': '#75534c', 'cork': '#bc9169',
    'cork_light': '#e7c99a', 'wax_shadow': '#603b50',
    'wax': '#913f5a', 'wax_light': '#d27b83',
    'ribbon_shadow': '#70455c', 'ribbon': '#b96f82',
    'ribbon_light': '#e4a1a0', 'herb': '#758d68',
    'tablet_shadow': '#705045', 'tablet_dark': '#a56b4a',
    'tablet': '#d99857', 'tablet_light': '#f7cf83',
}


def build_sprite():
    image = Image.new('RGBA', SIZE)
    draw = ImageDraw.Draw(image)
    c = COLORS
    spans = {
        8: (12, 19), 9: (11, 20), 10: (10, 21),
        11: (9, 22), 12: (8, 23), 13: (7, 24),
        14: (6, 25), 15: (5, 26), 16: (4, 27),
        17: (3, 28), 18: (2, 29), 19: (2, 29),
        20: (2, 29), 21: (2, 29), 22: (2, 29),
        23: (2, 29), 24: (2, 29), 25: (2, 29),
        26: (3, 28), 27: (3, 28), 28: (4, 27),
        29: (5, 26), 30: (6, 25), 31: (7, 24),
        32: (8, 23),
    }
    for y, (left, right) in spans.items():
        draw.line((left, y, right, y), fill=c['outline'])
        if y == 32:
            continue
        draw.line((left + 1, y, right - 1, y), fill=c['glass'])
        draw.point((left + 1, y), fill=c['glass_light'])
        draw.point((right - 1, y), fill=c['glass_edge'])
        if y >= 17:
            draw.point((right - 2, y), fill=c['glass_shadow'])

    draw.polygon([(5, 25), (8, 23), (12, 23), (15, 25), (14, 28),
                  (11, 30), (7, 29)], fill=c['tablet_shadow'])
    draw.polygon([(6, 25), (9, 24), (12, 24), (14, 26), (12, 28),
                  (8, 28), (6, 27)], fill=c['tablet'])
    draw.line([(7, 25), (10, 24), (12, 25)], fill=c['tablet_light'])
    draw.line([(8, 27), (11, 26)], fill=c['tablet_dark'])

    draw.polygon([(18, 25), (21, 23), (25, 23), (28, 26), (26, 29),
                  (21, 30), (18, 28)], fill=c['tablet_shadow'])
    draw.polygon([(19, 25), (22, 24), (25, 24), (27, 26), (25, 28),
                  (21, 28), (19, 27)], fill=c['tablet'])
    draw.line([(21, 25), (23, 24), (25, 25)], fill=c['tablet_light'])
    draw.line([(21, 27), (24, 26)], fill=c['tablet_dark'])

    draw.polygon([(12, 19), (15, 17), (19, 17), (22, 19), (23, 22),
                  (20, 24), (14, 24), (11, 22)], fill=c['tablet_shadow'])
    draw.polygon([(13, 19), (16, 18), (19, 18), (21, 19), (22, 21),
                  (19, 23), (15, 23), (12, 21)], fill=c['tablet'])
    draw.line([(15, 19), (17, 18), (20, 19)], fill=c['tablet_light'])
    draw.line([(15, 22), (18, 21)], fill=c['tablet_dark'])
    for x, y in ((9, 26), (23, 26), (17, 20)):
        draw.point((x, y), fill=c['tablet_light'])

    draw.line([(7, 17), (5, 19), (4, 23)], fill=c['reflection'])
    draw.line([(8, 17), (6, 19)], fill=c['glass_light'])
    draw.point((5, 27), fill=c['reflection'])
    draw.line([(10, 30), (14, 31), (20, 31), (23, 30)], fill=c['glass_light'])

    draw.rectangle((12, 1, 19, 7), fill=c['outline'])
    draw.rectangle((13, 2, 18, 6), fill=c['cork'])
    draw.line((13, 2, 18, 2), fill=c['cork_light'])
    draw.point((14, 5), fill=c['cork_shadow'])
    draw.point((17, 4), fill=c['cork_light'])
    draw.rectangle((11, 7, 20, 9), fill=c['wax_shadow'])
    draw.line((12, 7, 19, 7), fill=c['wax_light'])
    draw.line((12, 8, 19, 8), fill=c['wax'])
    draw.point((14, 10), fill=c['wax'])
    draw.point((18, 10), fill=c['wax'])

    draw.polygon([(11, 9), (8, 9), (4, 9), (1, 12), (7, 13),
                  (5, 17), (11, 14), (14, 10)], fill=c['ribbon_shadow'])
    draw.polygon([(10, 9), (7, 9), (3, 11), (7, 12),
                  (9, 11), (11, 10)], fill=c['ribbon'])
    draw.line([(4, 10), (7, 9), (9, 10)], fill=c['ribbon_light'])
    draw.polygon([(21, 9), (25, 9), (29, 10), (31, 12),
                  (25, 14), (27, 17), (21, 14), (18, 10)],
                 fill=c['ribbon_shadow'])
    draw.polygon([(22, 9), (25, 9), (29, 11), (25, 12),
                  (23, 11), (20, 10)], fill=c['ribbon'])
    draw.line([(23, 9), (26, 9), (28, 10)], fill=c['ribbon_light'])
    draw.line([(7, 13), (6, 15), (9, 14)], fill=c['ribbon'])
    draw.line([(25, 14), (26, 15)], fill=c['ribbon'])
    draw.rectangle((12, 9, 19, 11), fill=c['wax_shadow'])
    draw.line((13, 9, 18, 9), fill=c['wax_light'])
    draw.ellipse((14, 10, 17, 13), fill=c['ribbon_shadow'])
    draw.rectangle((15, 10, 16, 12), fill=c['ribbon_light'])
    draw.line((15, 14, 15, 16), fill=c['herb'])
    draw.point((14, 15), fill=c['herb'])
    draw.point((16, 15), fill=c['herb'])
    outline = tuple(bytes.fromhex(c['outline'][1:])) + (255,)
    for y in range(18, 33):
        left, right = spans[y]
        draw.point((left, y), fill=c['outline'])
        draw.point((right, y), fill=c['outline'])
        assert all(image.getpixel((x, y))[3] == 255 for x in range(left, right + 1))
        assert image.getpixel((left, y)) == image.getpixel((right, y)) == outline
    assert all(abs(spans[y][0] - spans[y - 1][0]) <= 1 and
               abs(spans[y][1] - spans[y - 1][1]) <= 1 for y in range(9, 33))
    return image


def write_pixel():
    image = build_sprite()
    assert image.getbbox() == (1, 1, 32, 33)
    colors = sorted({pixel for pixel in image.get_flattened_data() if pixel[3]})
    assert all(pixel[3] in (0, 255) for pixel in image.get_flattened_data())
    image.save(ASSETS / 'Painkiller_Pixel.png')
    image.save(ROOT / 'ElainaModAlchemy/item/Painkiller.png')
    PREVIEW.mkdir(parents=True, exist_ok=True)
    image.resize((SIZE[0] * 8, SIZE[1] * 8), Image.Resampling.NEAREST).save(
        PREVIEW / 'Painkiller_Pixel_x8.png')
    symbols = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'
    lookup = {color: symbols[index] for index, color in enumerate(colors)}
    draft = {
        'name': 'Painkiller - corked jar of three herbal lozenges',
        'source_note': 'Hand-authored from ElainaModAlchemy/item/ExampleAssets/Painkiller.svg. Rebuild with Tools/PotionPixel/GeneratePainkillerArt.py.',
        'size': list(SIZE),
        'palette': {'.': '#00000000', **{lookup[color]: '#' + bytes(color[:3]).hex() for color in colors}},
        'pixels': [''.join(lookup.get(image.getpixel((x, y)), '.') for x in range(SIZE[0]))
                   for y in range(SIZE[1])],
    }
    (PREVIEW / 'Painkiller.pixel.json').write_text(
        json.dumps(draft, indent=2) + '\n', encoding='utf-8')
    print(f'Painkiller pixel: {SIZE}, {len(colors)} opaque colors, bounds {image.getbbox()}')


def write_pencil(converter):
    original = (ASSETS / 'Painkiller.svg').read_text(encoding='utf-8')
    pencil = unquote(convert('data:image/svg+xml,' + quote(original),
                             profile='hatched', preserve_colors=True).split(',', 1)[1])
    source = GENERATED / 'Painkiller_Pencil.svg'
    source.write_text(pencil.rstrip() + '\n', encoding='utf-8')
    png = ASSETS / 'Painkiller_Pencil.png'
    subprocess.run([sys.executable, str(converter), str(source), str(png),
                    '--width', '100', '--height', '100', '--overwrite'], check=True)
    with Image.open(png) as image:
        ui = premultiply(image)
        ui.save(UI / 'Pencil/Painkiller_Pencil.png')
        alpha = ui.getchannel('A')
        Image.merge('RGBA', (alpha, alpha, alpha, alpha)).save(
            UI / 'Silhouettes/Painkiller_Silhouette.png')
    print('Painkiller pencil: 100x100 SVG, PNG, notebook copy and silhouette')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--converter', type=Path,
                        default=Path.home() / '.codex/skills/svg-to-png/scripts/convert_svg.py')
    args = parser.parse_args()
    write_pixel()
    write_pencil(args.converter)


if __name__ == '__main__':
    main()
