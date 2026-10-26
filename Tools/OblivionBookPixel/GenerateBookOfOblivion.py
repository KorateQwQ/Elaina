"""32x32 Book of Oblivion item sprite, drawn from integer pixel layers.

`python Tools/OblivionBookPixel/GenerateBookOfOblivion.py` writes the preview,
editable palette/character grid and metrics into this folder. Add `--apply` to
also write the production texture `Items/消耗品/BookOfOblivion.png`.
"""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
RUNTIME = ROOT / 'Items/消耗品/BookOfOblivion.png'
NAME = 'BookOfOblivion'
SIZE = 32
PALETTE = {
    '.': '#00000000',
    'o': '#2a2338',                                   # outline
    'b': '#3a3155', 'c': '#4b4070', 'C': '#62578a',   # cover shades
    'L': '#8175a8', 'H': '#a89bcb',                   # cover light
    'p': '#b3a58d', 'P': '#e3d8c1',                   # page block
    'g': '#7f8ca6', 'G': '#cbd6e6',                   # pale silver metal
    'e': '#9d90c9', 'E': '#e9e1ff',                   # oblivion sigil
    'r': '#9c537f', 'R': '#db91b6',                   # ribbon
}

# Front cover including its outline; the book block is swept down-right.
FRONT = (5, 2, 22, 25)
DEPTH = 3


def in_front(x, y):
    x0, y0, x1, y1 = FRONT
    if (x, y) in ((x0, y0), (x0, y1)):   # soften the spine corners
        return False
    return x0 <= x <= x1 and y0 <= y <= y1


def build_grid():
    g = [['.'] * SIZE for _ in range(SIZE)]

    def put(x, y, ch):
        if 0 <= x < SIZE and 0 <= y < SIZE:
            g[y][x] = ch

    # Book block: pages at depth 1-2, back cover at depth 3.
    for y in range(SIZE):
        for x in range(SIZE):
            if in_front(x, y):
                continue
            depth = next((t for t in range(1, DEPTH + 1) if in_front(x - t, y - t)), None)
            if depth is None:
                continue
            put(x, y, 'b' if depth == DEPTH else ('P' if depth == 1 else 'p'))
    # Outline the book block; the front cover draws its own edge.
    block = {(x, y) for y in range(SIZE) for x in range(SIZE) if g[y][x] != '.'}
    for x, y in list(block):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if n not in block and not in_front(*n):
                put(*n, 'o')
    # Fine page lines on the right and bottom faces of the block.
    for y in range(6, 25, 3):
        put(23, y, 'p')
    for x in range(9, 22, 3):
        put(x, 26, 'p')

    # Front cover.
    x0, y0, x1, y1 = FRONT
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if not in_front(x, y):
                continue
            edge = x in (x0, x1) or y in (y0, y1)
            put(x, y, 'o' if edge else 'C')
    put(x0 + 1, y0 + 1, 'o'); put(x0 + 1, y1 - 1, 'o')
    # Spine with a lit ridge and two silver bands, then the hinge groove.
    for y in range(y0 + 1, y1):
        put(6, y, 'c'); put(7, y, 'L'); put(8, y, 'c'); put(9, y, 'b')
    put(6, y0 + 1, 'o'); put(6, y1 - 1, 'o')
    for y in (5, 22):
        put(6, y, 'g'); put(7, y, 'G'); put(8, y, 'g')
    # Cover bevel: light top/left, shaded bottom/right.
    for x in range(10, 22):
        put(x, 3, 'L'); put(x, 24, 'c')
    for y in range(3, 25):
        put(10, y, 'L'); put(21, y, 'c')
    put(10, 3, 'H'); put(11, 3, 'H'); put(10, 4, 'H')
    put(10, 24, 'C'); put(21, 3, 'C')
    # Silver corner caps on the fore edge.
    for x, y, ch in ((19, 3, 'G'), (20, 3, 'G'), (21, 3, 'G'), (21, 4, 'G'), (21, 5, 'g'),
                     (20, 4, 'g'),
                     (21, 22, 'G'), (21, 23, 'g'), (21, 24, 'g'), (20, 24, 'g'), (19, 24, 'g'),
                     (20, 23, 'G')):
        put(x, y, ch)

    # Tooled inner frame, worn away at its lower right corner.
    for x in range(11, 21):
        put(x, 5, 'c')
    for y in range(5, 23):
        put(11, y, 'c')
    for y in range(5, 18):
        put(20, y, 'c')
    for x in range(11, 16):
        put(x, 22, 'c')
    put(20, 19, 'c'); put(17, 22, 'c'); put(19, 21, 'b')    # last crumbs of the frame
    # Closed eye: a lowered lid with lashes, the brightest mark on the cover.
    for x, y in ((12, 11), (13, 12), (14, 13), (15, 13), (16, 13), (17, 13), (18, 12), (19, 11)):
        put(x, y, 'E')
    for x, y in ((12, 13), (14, 14), (17, 14), (19, 13)):
        put(x, y, 'e')
    # Faded title plate.
    for x in range(13, 19):
        put(x, 18, 'L')
    for x in range(14, 18):
        put(x, 19, 'c')

    # Ribbon bookmark slipping out of the bottom of the pages.
    for y, row in ((26, 'oRro'), (27, 'oRro'), (28, 'oRro'), (29, 'oRro'), (30, 'oRro'), (31, 'oooo')):
        for i, ch in enumerate(row):
            put(16 + i, y, ch)
    return [''.join(row) for row in g]


def to_image(grid):
    im = Image.new('RGBA', (SIZE, SIZE))
    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            h = PALETTE[ch].lstrip('#')
            im.putpixel((x, y), tuple(int(h[i:i + 2], 16) for i in range(0, len(h), 2)) if len(h) == 8
                        else tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (255,))
    return im


def review(im):
    out = Image.new('RGBA', (760, 404))
    d = ImageDraw.Draw(out)
    try:
        font = ImageFont.truetype('arial.ttf', 15)
    except OSError:
        font = ImageFont.load_default()
    for i, (bg, fg, label) in enumerate((((38, 33, 52), (230, 225, 240), 'DARK'),
                                         ((232, 223, 208), (70, 60, 80), 'LIGHT'))):
        ox = i * 380
        d.rectangle((ox, 0, ox + 379, 403), fill=bg + (255,))
        d.text((ox + 22, 18), f'BOOK OF OBLIVION / {label}', fill=fg, font=font)
        out.alpha_composite(im.resize((256, 256), Image.NEAREST), (ox + 62, 52))
        d.text((ox + 22, 334), '1x', fill=fg, font=font)
        out.alpha_composite(im, (ox + 58, 326))
        d.text((ox + 122, 334), '2x', fill=fg, font=font)
        out.alpha_composite(im.resize((64, 64), Image.NEAREST), (ox + 152, 310))
        d.text((ox + 250, 334), '8x above', fill=fg, font=font)
    return out


def main():
    grid = build_grid()
    im = to_image(grid)
    used = sorted({ch for row in grid for ch in row} - {'.'})
    alphas = sorted({im.getpixel((x, y))[3] for y in range(SIZE) for x in range(SIZE)})
    im.save(HERE / f'{NAME}_Pixel.png')
    review(im).save(HERE / f'{NAME}_Review.png')
    (HERE / f'{NAME}_Pixel.json').write_text(json.dumps(
        {'size': [SIZE, SIZE], 'palette': {k: PALETTE[k] for k in ['.'] + used}, 'rows': grid},
        ensure_ascii=False, indent=2), encoding='utf-8')
    metrics = {'size': [SIZE, SIZE], 'visibleColors': len(used), 'alphaValues': alphas,
               'bbox': list(im.getbbox())}
    (HERE / 'metrics.json').write_text(json.dumps(metrics, indent=2), encoding='utf-8')
    if '--apply' in sys.argv:
        RUNTIME.parent.mkdir(parents=True, exist_ok=True)
        im.save(RUNTIME)
        print('wrote', RUNTIME.relative_to(ROOT))
    print(json.dumps(metrics))


if __name__ == '__main__':
    main()
