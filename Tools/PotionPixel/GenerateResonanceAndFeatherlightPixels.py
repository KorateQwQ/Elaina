"""Approved pixel sprites for the remaining round1 potion SVGs.

Python + Pillow. Integer silhouettes and authored marks, never SVG resampling.
Rebuilds runtime assets, editable design grids and nearest-neighbor previews.
"""
import importlib.util
import json
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Tools/PotionBottleDesigns/output/round1/pixel'
PREVIEW = ROOT / 'Tools/PotionPixel/preview'


def load_concentration():
    spec = importlib.util.spec_from_file_location('approved_upright_tube', Path(__file__).with_name('GenerateConcentrationPotionPixel.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def featherlight():
    """Approved upright tube geometry; blue liquid and a separate feather."""
    tube = load_concentration()
    colors = dict(tube.PALETTE)
    colors.update(T='#e8faff', B='#a2ddee', M='#70b8da', V='#447fad')
    im = Image.new('RGBA', tube.SIZE)
    d = ImageDraw.Draw(im)
    for y, (left, row) in enumerate(tube.ROWS):
        for x, key in enumerate(row, left):
            d.point((x,y), fill=colors[key])
    feather_colors = {'p':'#f2ecdb', 'q':'#c9c8b7', 'h':'#fffbed', 'r':'#688996'}
    # Feather barbs taper to the top and have a stepped lower-left quill.
    feather_rows = [
        (11,20,'qp'),
        (9,21,'qppp'),
        (8,22,'qhhpp'),
        (7,23,'qhhprp'),
        (7,24,'qhprpq'),
        (6,25,'qhprppq'),
        (6,26,'qprppq'),
        (6,27,'qrppq'),
        (6,28,'rppq'),
        (5,29,'rrq'),
        (5,30,'r'),
    ]
    for left,y,row in feather_rows:
        for x,key in enumerate(row,left):
            assert im.getpixel((x,y))[3] == 255
            d.point((x,y),fill=feather_colors[key])
    # Two short veins branch off the quill instead of filling the feather.
    d.point((8,24),fill=feather_colors['r'])
    d.point((9,27),fill=feather_colors['r'])
    return im


def resonance():
    """Round purple bottle with a side ribbon, charm and symmetric sound waves."""
    im = Image.new('RGBA',(31,39))
    d = ImageDraw.Draw(im)
    c = {
        'outline':'#51415f', 'edge':'#88719b', 'glass':'#d4ccea',
        'glass_dark':'#a79ac5', 'white':'#fff5f1',
        'cork_dark':'#7b5761', 'cork':'#b58b72', 'cork_light':'#e1bd8d',
        'gold':'#eed5a6', 'pink_light':'#ef9cdd', 'pink':'#c46abb',
        'purple':'#a14a9e', 'purple_dark':'#793584',
        'ribbon':'#c7a1d0', 'ribbon_dark':'#906895', 'wave':'#f6d2ed',
    }
    # Bottle crown, small straight neck and round body. Boundaries are authored
    # as symmetric row spans; the directional lighting is wholly inside them.
    spans = {
        1:(12,18),2:(11,19),3:(11,19),4:(11,19),5:(11,19),
        6:(10,20),7:(10,20),8:(12,18),9:(12,18),10:(12,18),
        11:(10,20),12:(8,22),13:(6,24),14:(5,25),15:(4,26),
        16:(3,27),17:(3,27),18:(2,28),19:(2,28),20:(2,28),
        21:(1,29),22:(1,29),23:(1,29),24:(1,29),25:(1,29),
        26:(1,29),27:(2,28),28:(2,28),29:(2,28),30:(3,27),
        31:(3,27),32:(4,26),33:(5,25),34:(6,24),35:(8,22),
        36:(10,20),37:(12,18),
    }
    for y,(left,right) in spans.items():
        d.line((left,y,right,y),fill=c['outline'])
        if y in (1,37): continue
        if y <= 5:
            fill='cork_light' if y==2 else 'cork' if y<5 else 'cork_dark'
        elif y<=7: fill='glass' if y==6 else 'edge'
        elif y<=10: fill='glass'
        elif y<21: fill='glass'
        elif y<24: fill='pink_light'
        elif y<28: fill='pink'
        elif y<33: fill='purple'
        else: fill='purple_dark'
        d.line((left+1,y,right-1,y),fill=c[fill])
        if y>=11:
            d.point((right-1,y),fill=c['glass_dark'] if y<21 else c['purple_dark'])
            if right-left>10: d.point((right-2,y),fill=c['glass_dark'] if y<21 else c['purple'])
    # Thin horizontal liquid meniscus, no broad wavy band.
    d.line((3,21,27,21),fill=c['pink_light'])
    # Broad glass reflection follows only the left shoulder, then gets thinner.
    d.line([(8,14),(6,16),(5,18),(4,21),(4,24)],fill=c['white'])
    d.line([(9,14),(7,16),(6,18)],fill=c['white'])
    d.line((12,2,17,2),fill=c['gold'])
    d.point((12,4),fill=c['cork_dark'])
    d.point((13,8),fill=c['white'])
    # Ribbon wraps the neck, one loop and two folded tails on the right.
    d.rectangle((11,8,19,9),fill=c['ribbon_dark'])
    d.line((12,8,18,8),fill=c['ribbon'])
    d.polygon([(19,8),(23,7),(26,8),(27,10),(25,12),(20,10)],fill=c['ribbon_dark'])
    d.polygon([(20,8),(23,8),(25,9),(25,10),(23,10),(20,9)],fill=c['ribbon'])
    d.polygon([(20,10),(25,12),(24,15),(22,13),(19,11)],fill=c['ribbon_dark'])
    d.line([(21,11),(23,12),(23,13)],fill=c['ribbon'])
    # Gold charm hangs under the neck, staying separate from the sound glyph.
    d.line((14,10,13,13),fill=c['cork_light'])
    d.ellipse((11,13,14,16),fill=c['cork_dark'])
    d.rectangle((12,14,13,15),fill=c['gold'])
    # Readable symmetric inner and outer waves, with a small central light.
    for sign in (-1,1):
        def pts(values): return [(15+sign*x,y) for x,y in values]
        d.line(pts([(4,25),(3,26),(3,28),(4,29)]),fill=c['white'])
        d.line(pts([(9,23),(8,24),(7,26),(7,28),(8,30),(9,31)]),fill=c['wave'])
    d.line((14,27,16,27),fill=c['white'])
    d.line((15,26,15,28),fill=c['white'])
    return im


def write_design(name, sprite):
    OUTPUT.mkdir(parents=True,exist_ok=True)
    PREVIEW.mkdir(parents=True,exist_ok=True)
    sprite.save(OUTPUT/f'{name}.png')
    sprite.save(ROOT/f'ElainaModAlchemy/item/ExampleAssets/{name}_Pixel.png')
    sprite.resize((sprite.width*8,sprite.height*8),Image.Resampling.NEAREST).save(PREVIEW/f'{name}_Study_x8.png')
    sprite.resize((sprite.width*8,sprite.height*8),Image.Resampling.NEAREST).save(PREVIEW/f'{name}_Pixel_x8.png')
    visible=sorted({p for p in sprite.get_flattened_data() if p[3]})
    assert all(p[3] in (0,255) for p in sprite.get_flattened_data())
    keys='abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'
    lookup={color:keys[i] for i,color in enumerate(visible)}
    palette={'.':'#00000000', **{lookup[color]:'#'+bytes(color[:3]).hex() for color in visible}}
    draft={
        'name':name+' - approved pixel sprite',
        'source_note':f'Hand-authored from Tools/PotionBottleDesigns/output/round1/svg/{name}.svg. Featherlight uses the approved upright tube. Rebuild with Tools/PotionPixel/GenerateResonanceAndFeatherlightPixels.py, which updates runtime assets.',
        'size':list(sprite.size),'palette':palette,
        'pixels':[''.join(lookup.get(sprite.getpixel((x,y)),'.') for x in range(sprite.width)) for y in range(sprite.height)],
    }
    (OUTPUT/f'{name}.pixel.json').write_text(json.dumps(draft,indent=2)+'\n',encoding='utf-8')
    print(f'{name}: {sprite.size}, {len(visible)} colors, bounds {sprite.getbbox()}')


if __name__=='__main__':
    write_design('ResonancePotion',resonance())
    write_design('FeatherlightPotion',featherlight())
