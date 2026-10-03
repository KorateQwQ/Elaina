"""Hand-designed 32x32 Revealing Dust, matching the Ashen pouch pixel style.

Integer pixel layers only. The powder mound is physical material; detached
particles, stars, magical trails, and the SVG's cast shadow are omitted.
Writes the existing item asset, an editable character grid, and offline proofs.
"""
import json
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
OUT = HERE / 'RevealingDust'
ASSETS = ROOT / 'ElainaModAlchemy/item/ExampleAssets'
TARGET = ASSETS / 'RevealingDust_Pixel.png'
PALETTE = {
    '.': '#00000000',
    'o': '#42364f', 'e': '#64536f',
    's': '#92705e', 'm': '#b38d63', 'g': '#d2ad74',
    'l': '#e7c98e', 'h': '#f5e1b3', 'w': '#fff3df',
    'r': '#70547f', 'p': '#a88ab8', 'b': '#d8b9df',
    'd': '#bda083', 'a': '#e2cc9e',
}
NEIGHBORS = ((-1, 0), (1, 0), (0, -1), (0, 1))


def build_sprite():
    im = Image.new('RGBA', (32, 32))
    draw = ImageDraw.Draw(im)

    def line(points, key):
        draw.line(points, fill=PALETTE[key])

    def poly(points, key):
        draw.polygon(points, fill=PALETTE[key])

    def dot(x, y, key):
        draw.point((x, y), fill=PALETTE[key])

    # Low, attached powder spill: no detached grains or decorative effects.
    powder = set()
    for y, left, right in [(25, 23, 25), (26, 22, 27), (27, 21, 28),
                           (28, 20, 30), (29, 21, 30), (30, 23, 28)]:
        powder.update((x, y) for x in range(left, right + 1))
        line([(left, y), (right, y)], 'e')
        if right - left > 2:
            line([(left + 1, y), (right - 1, y)], 'a' if y < 28 else 'd')
    line([(23, 26), (25, 26)], 'h')
    line([(22, 27), (26, 27)], 'a')
    line([(23, 27), (24, 27)], 'h')
    line([(22, 28), (25, 28)], 'a')
    line([(27, 28), (28, 28)], 'a')
    line([(24, 29), (27, 29)], 'd')

    # Broad warm cloth planes, lit from the upper left like the Ashen reference.
    spans = {12: (9, 19), 13: (8, 20), 14: (7, 20), 15: (7, 21),
             16: (6, 21), 17: (6, 22), 18: (5, 22), 19: (5, 23),
             20: (4, 23), 21: (4, 23), 22: (4, 23), 23: (4, 23),
             24: (4, 23), 25: (5, 23), 26: (5, 22), 27: (6, 21),
             28: (7, 20), 29: (9, 18), 30: (11, 17)}
    body = {(x, y) for y, (left, right) in spans.items()
            for x in range(left, right + 1)}
    for y, (left, right) in spans.items():
        line([(left, y), (right, y)], 'o')
        if y < 30:
            line([(left + 1, y), (right - 1, y)], 'g' if y < 27 else 'm')
    poly([(10, 14), (14, 15), (17, 18), (17, 23), (14, 26),
          (10, 27), (7, 25), (6, 23), (7, 19), (8, 16)], 'l')
    poly([(18, 15), (20, 17), (22, 21), (22, 24), (20, 27),
          (17, 28), (13, 28), (17, 25), (19, 22), (19, 18)], 'm')
    line([(20, 18), (21, 21), (21, 24), (19, 27), (16, 28)], 's')
    line([(9, 16), (8, 18), (7, 21), (7, 24), (8, 25)], 'h')
    line([(10, 16), (10, 18)], 'g')
    line([(17, 15), (18, 17)], 'm')
    line([(9, 26), (11, 27)], 'g')
    line([(11, 28), (15, 28)], 'g')

    # Scalloped open cloth mouth, deliberately different from the ash heap.
    poly([(8, 6), (10, 6), (11, 4), (13, 4), (15, 5),
          (16, 4), (18, 4), (20, 6), (21, 6), (21, 8),
          (19, 12), (10, 12), (9, 9)], 'o')
    poly([(9, 7), (11, 7), (12, 5), (13, 5), (15, 6),
          (16, 5), (18, 5), (19, 7), (20, 7), (20, 8),
          (18, 11), (11, 11)], 'l')
    line([(10, 7), (11, 10)], 'h')
    line([(12, 6), (13, 9), (13, 11)], 'h')
    line([(14, 6), (14, 10)], 'm')
    line([(17, 6), (16, 10)], 'h')
    line([(19, 7), (18, 10)], 'm')
    dot(20, 11, 'm')  # Cloth beneath the bow; close its one-pixel attachment gap.

    # Rolled rim and continuous lilac cinch, with no transparent mouth seam.
    poly([(9, 11), (18, 11), (21, 12), (20, 14),
          (17, 15), (11, 15), (8, 14), (7, 12)], 'o')
    line([(9, 12), (18, 12)], 'h')
    line([(9, 13), (19, 13)], 'g')
    line([(8, 13), (11, 14), (17, 14), (20, 13)], 'r')
    line([(9, 13), (11, 14), (17, 14), (19, 13)], 'p')
    line([(10, 13), (12, 13)], 'b')

    # Filled ribbon loops and two short tails. Dark fills are never alpha holes.
    poly([(20, 13), (21, 11), (23, 10), (24, 10),
          (25, 11), (24, 13), (22, 14)], 'r')
    line([(22, 12), (23, 11), (24, 11)], 'b')
    line([(22, 13), (24, 12)], 'p')
    poly([(22, 13), (24, 13), (26, 14), (26, 15),
          (24, 16), (22, 15), (20, 14)], 'r')
    line([(23, 14), (25, 14)], 'p')
    poly([(21, 14), (23, 15), (23, 17), (25, 20),
          (23, 21), (21, 18), (21, 16)], 'r')
    line([(22, 15), (22, 17), (23, 19), (24, 20)], 'p')
    line([(20, 15), (20, 17), (19, 19)], 'r')
    line([(20, 15), (20, 16)], 'b')
    draw.rectangle((20, 13, 22, 14), fill=PALETTE['p'])
    dot(21, 13, 'b')

    # Woven revealing emblem: lilac diamond around a small ivory four-point star.
    line([(13, 18), (17, 22), (13, 26), (9, 22), (13, 18)], 'p')
    line([(13, 20), (13, 24)], 'w')
    line([(11, 22), (15, 22)], 'w')
    dot(13, 19, 'h')
    dot(13, 25, 'h')

    # Restore all exterior stair steps after layering so highlights cannot break
    # the perimeter. The physical spill uses the Ashen secondary outline.
    visible = {(x, y) for y in range(32) for x in range(32)
               if im.getpixel((x, y))[3]}
    for x, y in visible:
        if any((x + dx, y + dy) not in visible for dx, dy in NEIGHBORS):
            dot(x, y, 'e' if (x, y) in powder and (x, y) not in body else 'o')
    return im


def metrics(im):
    visible = {(x, y) for y in range(im.height) for x in range(im.width)
               if im.getpixel((x, y))[3]}
    remaining = set(visible)
    components = []
    while remaining:
        queue = [remaining.pop()]
        size = 0
        while queue:
            x, y = queue.pop()
            size += 1
            for dx, dy in NEIGHBORS:
                point = (x + dx, y + dy)
                if point in remaining:
                    remaining.remove(point)
                    queue.append(point)
        components.append(size)
    # Flood the outside: any unreachable transparent cell is an unwanted hole.
    exterior = {(-1, -1)}
    queue = deque(exterior)
    while queue:
        x, y = queue.popleft()
        for dx, dy in NEIGHBORS:
            point = (x + dx, y + dy)
            if (-1 <= point[0] <= im.width and -1 <= point[1] <= im.height
                    and point not in visible and point not in exterior):
                exterior.add(point)
                queue.append(point)
    holes = [(x, y) for y in range(im.height) for x in range(im.width)
             if (x, y) not in visible and (x, y) not in exterior]
    rgba = list(im.get_flattened_data())
    outline = {Image.new('RGBA', (1, 1), PALETTE[c]).getpixel((0, 0))
               for c in ('o', 'e')}
    broken = [(x, y) for x, y in visible
              if any((x + dx, y + dy) not in visible for dx, dy in NEIGHBORS)
              and im.getpixel((x, y)) not in outline]
    result = {
        'size': list(im.size), 'visibleColors': len({p for p in rgba if p[3]}),
        'alpha': sorted({p[3] for p in rgba}), 'bounds': list(im.getbbox()),
        'opaquePixels': len(visible), 'fourConnectedComponents': sorted(components),
        'transparentHolePixels': len(holes), 'unoutlinedBoundaryPixels': len(broken),
        'source': 'ElainaModAlchemy/item/ExampleAssets/RevealingDust.svg',
        'styleReference': 'ElainaModAlchemy/item/ExampleAssets/AshenFacsimileDust_Pixel.png',
        'runtime': TARGET.relative_to(ROOT).as_posix(),
        'effects': 'No floating particles, sparkles, trails, glow, or cast shadow.',
    }
    assert result['alpha'] == [0, 255], result
    assert len(components) == 1, result
    assert not holes and not broken, result
    return result


def preview(im):
    reference = Image.open(ASSETS / 'AshenFacsimileDust_Pixel.png').convert('RGBA')
    sheet = Image.new('RGB', (960, 540))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default(size=17)
    small = ImageFont.load_default(size=14)
    for left, bg, ink, title in [(0, '#241f32', '#e0d4ea', 'DARK'),
                                  (480, '#e9dfd0', '#53465d', 'LIGHT')]:
        draw.rectangle((left, 0, left + 479, 539), fill=bg)
        draw.text((left + 24, 18), 'REVEALING DUST / ' + title, fill=ink, font=font)
        large = im.resize((256, 256), Image.Resampling.NEAREST)
        sheet.paste(large, (left + 20, 60), large)
        ash = reference.resize((160, 160), Image.Resampling.NEAREST)
        sheet.paste(ash, (left + 298, 124), ash)
        draw.text((left + 302, 99), 'Ashen reference', fill=ink, font=small)
        draw.text((left + 29, 325), '8x nearest-neighbor / solid outline', fill=ink, font=small)
        draw.text((left + 25, 382), '1x', fill=ink, font=font)
        sheet.paste(im, (left + 67, 376), im)
        sheet.paste(reference, (left + 108, 376), reference)
        draw.text((left + 186, 382), '2x', fill=ink, font=font)
        for offset, sprite in [(229, im), (306, reference)]:
            doubled = sprite.resize((64, 64), Image.Resampling.NEAREST)
            sheet.paste(doubled, (left + offset, 362), doubled)
        draw.text((left + 25, 459), '32 x 32 / no particles / no alpha holes', fill=ink, font=small)
        draw.text((left + 25, 487), 'Offline asset comparison; not an in-game screenshot.', fill=ink, font=small)
    sheet.save(OUT / 'RevealingDust_Review.png')


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    im = build_sprite()
    report = metrics(im)
    lookup = {Image.new('RGBA', (1, 1), color).getpixel((0, 0)): key
              for key, color in PALETTE.items()}
    grid = [''.join(lookup[im.getpixel((x, y))] for x in range(im.width))
            for y in range(im.height)]
    data = {'name': 'RevealingDust', 'size': list(im.size), 'palette': PALETTE,
            'source_note': 'Hand-designed to match AshenFacsimileDust_Pixel; no SVG downsampling.',
            'pixels': grid}
    (OUT / 'RevealingDust_Pixel.json').write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8')
    (OUT / 'metrics.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    im.save(OUT / 'RevealingDust_Pixel.png')
    im.save(TARGET)
    preview(im)
    print(json.dumps(report))


if __name__ == '__main__':
    main()
