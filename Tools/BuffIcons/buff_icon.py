"""buff 图标公共流程：内容层透明底绘制，预览与正式写入都合成到统一 buff 背景上。

游戏里不另叠背景，正式 PNG 必须是合成后的成图。
"""
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
OUTPUT = HERE / 'output'
BACKGROUND = ROOT / 'Buffs/NullBuffBackGround.png'
SIZE = 32


def outline(cells, key='O'):
    """四邻域外扩一像素描边。"""
    out = dict(cells)
    for (x, y) in cells:
        for ddx, ddy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            p = (x + ddx, y + ddy)
            if p not in cells and 0 <= p[0] < SIZE and 0 <= p[1] < SIZE:
                out[p] = key
    return out


def to_image(cells, palette):
    im = Image.new('RGBA', (SIZE, SIZE))
    for (x, y), k in cells.items():
        im.putpixel((x, y), Image.new('RGB', (1, 1), palette[k]).getpixel((0, 0)) + (255,))
    return im


def composite(im):
    comp = Image.open(BACKGROUND).convert('RGBA')
    comp.alpha_composite(im)
    return comp


def review(images, scale=8):
    pad = 12
    w = len(images) * (SIZE * scale + pad) + pad
    h = SIZE * scale + SIZE * 2 + pad * 3
    sheet = Image.new('RGBA', (w, h), (58, 58, 70, 255))
    for i, im in enumerate(images.values()):
        comp = composite(im)
        x = pad + i * (SIZE * scale + pad)
        sheet.paste(comp.resize((SIZE * scale,) * 2, Image.NEAREST), (x, pad))
        sheet.paste(comp, (x, SIZE * scale + pad * 2))
        sheet.paste(comp.resize((SIZE * 2,) * 2, Image.NEAREST), (x + SIZE + pad, SIZE * scale + pad * 2))
    return sheet


def run(name, images, target):
    """默认只输出预览；--apply <方案> 把该方案合成背景后写入 target。"""
    import argparse
    ap = argparse.ArgumentParser()
    ap.add_argument('--apply', metavar='VARIANT', help='把选定方案合成背景后写入正式贴图')
    args = ap.parse_args()
    OUTPUT.mkdir(exist_ok=True)
    for k, im in images.items():
        im.save(OUTPUT / f'{name}_{k}.png')
    review(images).save(OUTPUT / f'{name}-review.png')
    if args.apply:
        composite(images[args.apply]).save(target)
        print('wrote', target)
