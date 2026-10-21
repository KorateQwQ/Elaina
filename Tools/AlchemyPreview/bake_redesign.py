"""Rebuild the notebook's premultiplied, text-free redesign assets.

First export vanilla sources with run.ps1 -ExportMaterials. Item_75 has eight
frames with two padding rows; dilate frames separately so animation cannot bleed.
The outline is a one-source-pixel four-neighbour mask, matching HTML olDark.
"""
from pathlib import Path
import json
import sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / 'ElainaModAlchemy/UI/Assets'
OUT = ASSETS / 'Outlines'
OUT.mkdir(exist_ok=True)
CHECK = '--check' in sys.argv
def save(image, path):
    if CHECK:
        if not path.exists() or Image.open(path).convert('RGBA').tobytes() != image.convert('RGBA').tobytes():
            raise SystemExit(f'Stale or missing baked texture: {path}')
    else:
        image.save(path, optimize=True)
sources = list((ROOT / 'ElainaModAlchemy/item/ExampleAssets').glob('*_Pixel.png'))
vanilla = list((ROOT / '.vissandbox/alchemy-preview/material-sources').glob('Item_*.png'))
if not vanilla:
    raise SystemExit('Run Tools/AlchemyPreview/run.ps1 -ExportMaterials first.')
manifest = []
for source in sources + vanilla:
    original = Image.open(source).convert('RGBA')
    count = 8 if source.stem == 'Item_75' else 1
    stride = original.height // count
    h = stride - 2 if count > 1 else stride
    w = original.width
    atlas = Image.new('RGBA', (w + 2, (h + 2) * count))
    for frame in range(count):
        mask = Image.new('L', (w + 2, h + 2))
        mask.paste(original.getchannel('A').crop((0, frame * stride, w, frame * stride + h)), (1, 1))
        dilated = mask.copy()
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            shifted = Image.new('L', mask.size)
            shifted.paste(mask, (dx, dy))
            dilated = ImageChops.lighter(dilated, shifted)
        ring = ImageChops.multiply(dilated, ImageChops.invert(mask))
        atlas.paste(Image.merge('RGBA', (ring, ring, ring, ring)), (0, frame * (h + 2)))
    save(atlas, OUT / source.name)
    manifest.append({'name': source.stem, 'width': w, 'height': h, 'frames': count})
manifest_path = OUT / 'manifest.json'
if CHECK:
    if not manifest_path.exists() or json.loads(manifest_path.read_text(encoding='utf-8')) != manifest:
        raise SystemExit(f'Stale or missing outline manifest: {manifest_path}')
else:
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding='utf-8')

# Reuse the skill atlas (Tools/ConstellationSkillPanelPreview/bake_stars.py, HTML crossStar
# geometry), preserving premultiplied pixels. One 128x64 row per shape the notebook draws; the row
# order must match AlchemyDrawing.StarRows. Levels 64/32/16/8/4 sit at x 0/64/96/112/120.
skill = ROOT / 'ElainaModSkills/ElainaSkillUI/ConstellationPreview/Assets'
regions = json.loads((skill / 'Stars.json').read_text())
sheet = Image.open(skill / 'Stars.png')
STAR_ROWS = ((1000, 200), (2833, 200), (778, 250), (976, 280))
star_atlas = Image.new('RGBA', (128, 64 * len(STAR_ROWS)))
for row, (aspect, waist) in enumerate(STAR_ROWS):
    star = next(r for r in regions if r['Aspect'] == aspect and r['Waist'] == waist and r['Filled'])
    offset = 0
    for x, y, w, h in star['Levels']:
        star_atlas.paste(sheet.crop((x, y, x + w, y + h)), (offset, row * 64))
        offset += w
save(star_atlas, ASSETS / 'NotebookStars.png')

# Prefiltered small round dot; the large glow disc loses thin dots on downsampling.
dot = Image.new('L', (64, 64))
ImageDraw.Draw(dot).ellipse((0, 0, 63, 63), fill=255)
dot = dot.resize((8, 8), Image.Resampling.LANCZOS)
save(Image.merge('RGBA', (dot,) * 4), ASSETS / 'NotebookDot.png')

# Page-colored edge fades. Draw a whole prefiltered strip rather than independent
# one-pixel rows, which can leave seams at fractional UI scales.
surface = Image.open(ASSETS / 'NotebookSurface.png').convert('RGB')
for name, box, reverse in (
    ('CatalogFadeTop', (26, 150, 745, 166), True),
    ('CatalogFadeBottom', (26, 674, 745, 700), False),
    ('DetailFadeBottom', (790, 571, 1060, 593), False),
):
    crop = surface.crop(box)
    fade = Image.new('RGBA', crop.size)
    for y in range(crop.height):
        alpha = (1-y/(crop.height-1)) if reverse else y/(crop.height-1)
        for x in range(crop.width):
            rgb = crop.getpixel((x,y))
            fade.putpixel((x,y), tuple(round(c*alpha) for c in rgb)+(round(alpha*255),))
    save(fade, ASSETS / (name+'.png'))

# Clean moon paper, with a slightly darker lower band; same cell geometry as the
# old atlas so pickup shadows and the selected silhouette retain their alignment.
W, H, P, R = 118, 132, 12, 2
cw, ch = (W + P * 2) * R, (H + P * 2) * R
paper = Image.new('RGBA', (cw, ch * 3))
mask_hi = Image.new('L', (cw * 4, ch * 4))
ImageDraw.Draw(mask_hi).rounded_rectangle((P*R*4, P*R*4, (P+W)*R*4-1, (P+H)*R*4-1), radius=16, fill=255)
mask = mask_hi.resize((cw, ch), Image.Resampling.LANCZOS)
face = Image.new('RGBA', (cw, ch))
for y in range(ch):
    tone = (218, 211, 227) if y >= (P+H-30)*R else (226, 220, 234)
    for x in range(cw):
        a = mask.getpixel((x,y))
        face.putpixel((x,y), tuple(round(c*a/255) for c in tone)+(a,))
for col in range(1):
    paper.paste(face, (col*cw, 0))
    for row, alpha in ((1, mask.filter(ImageFilter.MaxFilter(5))), (2, mask.filter(ImageFilter.GaussianBlur(6)))):
        paper.paste(Image.merge('RGBA', (alpha,)*4), (col*cw, row*ch))
save(paper, ASSETS / 'NotebookCards.png')
print(f'Baked {len(manifest)} outline masks, notebook stars and moon paper.')
