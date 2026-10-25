"""Deterministic pixel edits of supplied sprites; python Tools/DollSpriteBake/bake.py [--check]."""
from pathlib import Path
import hashlib
import json
import sys
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / 'ElainaModSkills/Skills/DollControl'
SOURCE = BASE / 'NewDoll'
OUT = SOURCE / 'Ready'
CLEAR = (0, 0, 0, 0)
OUTLINE = (52, 38, 14, 255)
TOOL = {(104, 77, 46, 255), (64, 44, 22, 255), (128, 128, 128, 255),
        (174, 174, 174, 255), (252, 252, 252, 255)}
SPECS = {
    'WhiteCat': ('1baigou', (8, 3), [(9, 4), (8, 4), (7, 2), (7, 2)], (216, 222, 226, 255), (145, 165, 177, 255), 6, 21),
    'YellowDog': ('1huanggou', (7, 0), [(8, 1), (7, 1), (6, -1), (6, -1)], (191, 172, 118, 255), (151, 131, 75, 255), 5, 19),
    'Rabbit': ('1tu', (6, 3), [(7, 4), (6, 4), (6, 3), (6, 3)], (243, 217, 230, 255), (191, 145, 167, 255), 6, 21),
}

def read(path):
    im = Image.open(path).convert('RGBA')
    for y in range(im.height):
        for x in range(im.width):
            if not im.getpixel((x, y))[3]: im.putpixel((x, y), CLEAR)
    return im

def canvas(im, offset=(0, 0)):
    result = Image.new('RGBA', (64, 64))
    result.paste(im, (8 - offset[0], 8 - offset[1]))
    assert sum(p[3] > 0 for p in result.get_flattened_data()) == sum(p[3] > 0 for p in im.get_flattened_data()), 'Clipped sprite'
    return result

old_mine = [read(BASE / 'Kuma' / f'Kuma_Mine_{i}.png') for i in range(1, 5)]
old_use = [read(BASE / 'Kuma' / f'Kuma_Use_{i}.png') for i in range(1, 5)]
hammer = [read(SOURCE / f'2xiong{i}.png') for i in range(1, 5)]

def empty_hand(im, index, spec):
    prefix, _, offsets, body, shade, donor, head_bottom = spec
    dx, dy = offsets[index]
    result = im.copy()
    if index == 1:
        # Restore the exact same head pose from the supplied idle frame, including ears hidden by the pick.
        head = read(SOURCE / f'{prefix}{donor}.png')
        result.paste(head.crop((0, 0, 48, head_bottom + 1)), (0, 0))
        # Remaining exposed shaft immediately beside the face, below the restored head.
        for y in range(head_bottom + 1, 48):
            for x in range(48):
                if result.getpixel((x, y)) in TOOL: result.putpixel((x, y), CLEAR)
    else:
        colors = {(163, 147, 103, 255): body, (121, 104, 62, 255): shade}
        for y in range(48):
            for x in range(48):
                a, b = old_mine[index].getpixel((x, y)), old_use[index].getpixel((x, y))
                if a == b: continue
                xx, yy = x + dx, y + dy
                if 0 <= xx < 48 and 0 <= yy < 48:
                    result.putpixel((xx, yy), colors.get(b, b))
    assert not any(p in TOOL for p in result.get_flattened_data()), f'Tool residue: {prefix}/{index}'
    return result

def hammer_layer(index):
    # Bear's supplied hammer is offset (+7,+2) relative to the existing unarmed pose.
    layer = Image.new('RGBA', (48, 48))
    for y in range(48):
        for x in range(48):
            pixel = hammer[index].getpixel((x, y))
            u = old_use[index].getpixel((x - 7, y - 2)) if 0 <= x - 7 < 48 and 0 <= y - 2 < 48 else CLEAR
            if pixel[3] and (not u[3] or pixel in TOOL): layer.putpixel((x, y), pixel)
    return layer

assets = {}
for i in range(1, 4): assets[f'Bear_Idle_{i}'] = canvas(read(SOURCE / f'2xiong{i+4}.png'), (7, 2))
for i in range(1, 5):
    assets[f'Bear_Mine_{i}'] = canvas(old_mine[i-1])
    assets[f'Bear_Use_{i}'] = canvas(old_use[i-1])
    assets[f'Bear_Hammer_{i}'] = canvas(hammer[i-1], (7, 2))
    # Legacy bear movement uses a centered 32px canvas.
    move = read(BASE / 'Kuma' / f'Kuma_Move_{i}.png')
    assets[f'Bear_Move_{i}'] = canvas(move, (-8, -8))

for name, spec in SPECS.items():
    prefix, origin, offsets, *_ = spec
    for i in range(1, 4): assets[f'{name}_Idle_{i}'] = canvas(read(SOURCE / f'{prefix}{i+4}.png'), origin)
    for i, idle in enumerate([1, 2, 3, 2], 1): assets[f'{name}_Move_{i}'] = assets[f'{name}_Idle_{idle}'].copy()
    for index in range(4):
        raw = read(SOURCE / f'{prefix}{index+1}.png')
        bare = empty_hand(raw, index, spec)
        layer = hammer_layer(index)
        dx, dy = offsets[index]
        combined = Image.new('RGBA', (64, 64))
        combined.alpha_composite(layer, (8 + dx - 7, 8 + dy - 2))
        # Head/ears cover the rear part of the raised tool; hands cover its grip.
        combined.alpha_composite(bare, (8, 8))
        if index == 1:
            # Raised hammer head passes in front of the ears, as in the supplied bear frame.
            combined.alpha_composite(layer, (8 + dx - 7, 8 + dy - 2))
        result = Image.new('RGBA', (64, 64)); result.paste(combined, (-origin[0], -origin[1]))
        assets[f'{name}_Mine_{index+1}'] = canvas(raw, origin)
        assets[f'{name}_Use_{index+1}'] = canvas(bare, origin)
        assets[f'{name}_Hammer_{index+1}'] = result

check = '--check' in sys.argv
OUT.mkdir(exist_ok=True)
for name, im in assets.items():
    path = OUT / f'{name}.png'
    assert set(im.getchannel('A').get_flattened_data()) <= {0, 255}
    if check:
        assert path.exists() and read(path).tobytes() == im.tobytes(), f'Stale asset: {path}'
    else: im.save(path)
manifest = {'generator': 'Tools/DollSpriteBake/bake.py', 'canvas': [64, 64],
            'sources': {str(p.relative_to(ROOT)).replace('\\', '/'): hashlib.sha256(p.read_bytes()).hexdigest()
                        for p in sorted(SOURCE.glob('*.png')) + sorted((BASE / 'Kuma').glob('*.png'))},
            'assets': {name + '.png': hashlib.sha256(im.tobytes()).hexdigest() for name, im in assets.items()}}
if check: assert json.loads((OUT / 'manifest.json').read_text()) == manifest
else: (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding='utf-8')
preview = ROOT / '.vissandbox/doll-assets'; preview.mkdir(parents=True, exist_ok=True)
sheet = Image.new('RGB', (1024, 4 * 420), (40, 44, 53)); draw = ImageDraw.Draw(sheet)
for row, name in enumerate(['Bear', *SPECS]):
    for group, action in enumerate(['Mine', 'Use', 'Hammer']):
        draw.text((4, row * 420 + group * 140), f'{name} {action}', fill='white')
        for i in range(1, 5):
            im = assets[f'{name}_{action}_{i}'].resize((128, 128), Image.Resampling.NEAREST)
            sheet.paste(im, (128 + (i-1)*200, row*420+group*140+12), im)
sheet.save(preview / 'actions.png')
print(f'{"Verified" if check else "Baked"} {len(assets)} RGBA sprites; source PNGs are read-only.')
