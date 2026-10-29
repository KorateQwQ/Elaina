"""Rebake ConstellationSkillPanel/Assets/Stars.png with the HTML crossStar geometry.

Each region is a square tile per prefiltered level; the star's longer axis spans 112/128 of
the tile (CrossStar draws extent = max(size) * 128 / 112). The outline follows the HTML path:
four cubic Béziers whose two control points both sit at (waist·hx, waist·hy) of each quadrant,
so edges are smooth concave curves rather than KL's straight spikes. Filled stars are rasterised
with 16× supersampling and an exact box downsample; outlines use a ~1 px stroke at every level,
matching the HTML's non-scaling .9 px stroke at the size the level is drawn.
RGB = alpha (premultiplied white), as in the other baked UI masks.

Run: python Tools/ConstellationSkillPanelPreview/bake_stars.py [--check]
Then rerun Tools/AlchemyPreview/bake_redesign.py, which copies the notebook's stars from here.
"""
from pathlib import Path
import json
import sys
from PIL import Image, ImageDraw

ASSETS = Path(__file__).resolve().parents[2] / 'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets'
CHECK = '--check' in sys.argv
SS = 16
# The tab marker's slim flash (34 × 12, waist .2) gets its own aspect instead of a squashed square star.
EXTRA = [{'Aspect': 2833, 'Waist': 200, 'Filled': True,
          'Levels': [[78, 206, 64, 64], [146, 206, 32, 32], [182, 206, 16, 16], [202, 206, 8, 8], [214, 206, 4, 4]]}]


def outline(side, aspect, waist):
    major = side * 56 / 128
    hx, hy = (major, major * 1000 / aspect) if aspect >= 1000 else (major * aspect / 1000, major)
    c = min(.49, max(.05, waist / 1000))
    points = []
    for (ax, ay), (bx, by) in (((0, -1), (1, 0)), ((1, 0), (0, 1)), ((0, 1), (-1, 0)), ((-1, 0), (0, -1))):
        kx, ky = c * (ax + bx), c * (ay + by)
        for i in range(96):
            t = i / 96
            u = 1 - t
            x = u ** 3 * ax + 3 * u * u * t * kx + 3 * u * t * t * kx + t ** 3 * bx
            y = u ** 3 * ay + 3 * u * u * t * ky + 3 * u * t * t * ky + t ** 3 * by
            points.append(((side / 2 + x * hx) * SS, (side / 2 + y * hy) * SS))
    return points


def tile(side, aspect, waist, filled):
    mask = Image.new('L', (side * SS, side * SS))
    draw = ImageDraw.Draw(mask)
    path = outline(side, aspect, waist)
    if filled:
        draw.polygon(path, fill=255)
    else:
        draw.line(path + path[:2], fill=255, width=SS, joint='curve')
    mask = mask.resize((side, side), Image.Resampling.BOX)
    return Image.merge('RGBA', (mask,) * 4)


regions = json.loads((ASSETS / 'Stars.json').read_text())
for extra in EXTRA:
    if not any((r['Aspect'], r['Waist'], r['Filled']) == (extra['Aspect'], extra['Waist'], extra['Filled']) for r in regions):
        regions.append(extra)
height = max(y + h for r in regions for _, y, _, h in r['Levels']) + 2
sheet = Image.new('RGBA', (512, height))
for r in regions:
    for x, y, w, h in r['Levels']:
        sheet.paste(tile(w, r['Aspect'], r['Waist'], r['Filled']), (x, y))

if CHECK:
    old = Image.open(ASSETS / 'Stars.png').convert('RGBA')
    if old.size != sheet.size or old.tobytes() != sheet.tobytes():
        raise SystemExit('Stars.png is stale; run bake_stars.py.')
else:
    sheet.save(ASSETS / 'Stars.png', optimize=True)
    (ASSETS / 'Stars.json').write_text(json.dumps(regions, indent=2), encoding='utf-8')
print(f'Stars: {len(regions)} shapes, {sheet.size[0]}x{sheet.size[1]}, HTML crossStar geometry.')
