"""Aged deckled paper for the alchemy catalog cards.

One premultiplied RGBA atlas, 2x the 118x132 card with a 12 px margin per side.
Columns are paper variants; rows are the near-white paper (tinted per category at
runtime), a 1 px dilated silhouette (selected outline) and a soft silhouette
(drop shadow and selected glow). Deterministic; Pillow is the only dependency.
"""
from pathlib import Path
import math
import random
from PIL import Image, ImageDraw, ImageFilter

repo = Path(__file__).resolve().parents[2]
CARD_W, CARD_H, PAD, RES, VARIANTS = 118, 132, 12, 2, 6
CELL_W, CELL_H = (CARD_W + PAD * 2) * RES, (CARD_H + PAD * 2) * RES
SS = 4  # polygon supersampling on top of RES
BROWN = (.63, .50, .34)  # rgba(150,118,80) relative to the mist paper colors


def deckle(rng):
    """Card outline in 1x units: a coarse 12-point wobble plus fine fiber jitter."""
    def edge(length, coarse):
        points = []
        for i in range(int(length / 2)):  # the next edge supplies the corner
            t = i * 2 / length
            k = t * 12
            a, b = coarse[int(k)], coarse[min(12, int(k) + 1)]
            points.append((t * length, a + (b - a) * (k - int(k)) + rng.random() * .5))
        return points
    top, right, bottom, left = ([rng.random() * 1.8 for _ in range(13)] for _ in range(4))
    poly = [(x, d) for x, d in edge(CARD_W, top)]
    poly += [(CARD_W - d, y) for y, d in edge(CARD_H, right)]
    poly += [(CARD_W - x, CARD_H - d) for x, d in edge(CARD_W, bottom)]
    poly += [(d, CARD_H - y) for y, d in edge(CARD_H, left)]
    return poly


def smooth_noise(rng, cells_x, cells_y, width, height):
    grid = Image.new("L", (cells_x, cells_y))
    grid.putdata([rng.randrange(256) for _ in range(cells_x * cells_y)])
    return grid.resize((width, height), Image.BICUBIC)


def bake(seed):
    rng = random.Random(seed * 7919 + 17)
    w, h = CARD_W * RES, CARD_H * RES
    big = Image.new("L", ((CARD_W + PAD * 2) * RES * SS, (CARD_H + PAD * 2) * RES * SS))
    ImageDraw.Draw(big).polygon([((x + PAD) * RES * SS, (y + PAD) * RES * SS) for x, y in deckle(rng)], fill=255)
    mask = big.resize((CELL_W, CELL_H), Image.BOX)
    rim = mask.filter(ImageFilter.GaussianBlur(1.6))
    fiber = smooth_noise(rng, 16, 110, w, h)
    cloud = smooth_noise(rng, 5, 6, w, h)
    grain = [rng.random() for _ in range(w * h)]
    # Faint foxing only near the bottom edge, never under the seal.
    spots = []
    if rng.random() > .4:
        spots.append(((15 + rng.random() * 70) / 100 * CARD_W, CARD_H * (.86 + rng.random() * .06), 4 + rng.random() * 2))
    if rng.random() > .6:
        spots.append((8 + rng.random() * (CARD_W - 16), CARD_H * (.78 + rng.random() * .14), 1.1))
    fp, cp, mp, rp = fiber.load(), cloud.load(), mask.load(), rim.load()
    paper = Image.new("RGBA", (CELL_W, CELL_H))
    out = paper.load()
    for py in range(h):
        y = (py + .5) / RES
        for px in range(w):
            alpha = mp[px + PAD * RES, py + PAD * RES] / 255
            if alpha <= 0: continue
            x = (px + .5) / RES
            # Top light fades into the slightly darker body of the sheet.
            value = 1 - .035 * min(1, y / (CARD_H * .4))
            value *= 1 - .028 * (fp[px, py] / 255 - .5) - .035 * (grain[py * w + px] - .5) - .03 * (cp[px, py] / 255 - .5)
            age = .30 * max(0, min(1, (math.hypot((x / CARD_W - .5) / 1.2, (y / CARD_H - .42) / 1.15) - .40) / .5))
            age += .22 * math.exp(-min(x, y, CARD_W - x, CARD_H - y) / 5)
            age += .30 * max(0, 1 - rp[px + PAD * RES, py + PAD * RES] / 255) * 2
            for sx, sy, radius in spots:
                d = math.hypot(x - sx, y - sy)
                age += .08 * max(0, min(1, (radius * 1.8 - d) / (radius * 1.2)))
            age = min(.6, age)
            rgb = [value * (1 - age) + b * age for b in BROWN]
            out[px + PAD * RES, py + PAD * RES] = tuple(round(255 * max(0, min(1, c)) * alpha) for c in rgb) + (round(255 * alpha),)
    outline = mask.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.GaussianBlur(.8))
    soft = mask.filter(ImageFilter.GaussianBlur(6))
    white = lambda m: Image.merge("RGBA", (m, m, m, m))  # premultiplied white
    return paper, white(outline), white(soft)


atlas = Image.new("RGBA", (CELL_W * VARIANTS, CELL_H * 3))
for v in range(VARIANTS):
    for row, image in enumerate(bake(v)):
        atlas.paste(image, (v * CELL_W, row * CELL_H))
target = repo / "ElainaModAlchemy/UI/Assets/CatalogPaper.png"
atlas.save(target, optimize=True)
print(target, atlas.size)
