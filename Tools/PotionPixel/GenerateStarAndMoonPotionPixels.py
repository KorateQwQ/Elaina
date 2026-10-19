"""Hand-authored triangular Star Power and round Moon Dew Elixir sprites.

Python + Pillow. Rebuilds runtime PNGs, editable character grids and previews.
The SVG references guide shape and marks; no rasterized SVG sampling is used.
"""
import json
from pathlib import Path
import sys
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Tools/PotionBottleDesigns/output/round1/pixel'
PREVIEW = ROOT / 'Tools/PotionPixel/preview'
ASSETS = ROOT / 'ElainaModAlchemy/item/ExampleAssets'
sys.path.insert(0, str(Path(__file__).resolve().parent))

COMMON = {
    'outline':'#51415f', 'edge':'#88719b', 'glass':'#d4ccea',
    'glass_dark':'#a79ac5', 'white':'#fff5f1',
    'cork_dark':'#7b5761', 'cork':'#b58b72', 'cork_light':'#e1bd8d',
    'gold':'#eed5a6',
}


def stopper(d, c):
    # Centered soft cork and a separate glass lip, same light as the body.
    d.line((12,1,18,1),fill=c['outline'])
    d.rectangle((11,2,19,5),fill=c['outline'])
    d.line((12,2,18,2),fill=c['gold'])
    d.rectangle((12,3,18,4),fill=c['cork'])
    d.point((12,4),fill=c['cork_dark'])
    d.line((12,5,18,5),fill=c['cork_dark'])
    d.rectangle((10,6,20,7),fill=c['outline'])
    d.line((11,6,19,6),fill=c['glass'])
    d.line((11,7,19,7),fill=c['edge'])


def four_point(d,x,y,radius,color):
    d.line((x-radius,y,x+radius,y),fill=color)
    d.line((x,y-radius,x,y+radius),fill=color)
    if radius>=3:
        d.rectangle((x-1,y-1,x+1,y+1),fill=color)


def starpower():
    im=Image.new('RGBA',(31,36));d=ImageDraw.Draw(im)
    c={**COMMON,'light':'#9bbaf0','blue':'#5979d0','middle':'#3b55b0',
       'dark':'#303878','ribbon':'#c7a1d0','ribbon_dark':'#906895'}
    # Trapezoidal neck transitions into two matched 2:1 triangular walls.
    # The bottom corners turn inward; the broad base is level.
    lefts={8:12,9:12,10:12,11:12,12:11,13:11,14:10,15:10,
           16:9,17:8,18:8,19:7,20:7,21:6,22:5,23:5,24:4,25:4,
           26:3,27:3,28:2,29:2,30:1,31:1,32:1,33:2,34:4}
    for y,left in lefts.items():
        right=30-left
        d.line((left,y,right,y),fill=c['outline'])
        if y==34:continue
        fill='glass' if y<20 else 'light' if y==20 else 'blue' if y<24 else 'middle' if y<31 else 'dark'
        d.line((left+1,y,right-1,y),fill=c[fill])
        if y>11:
            d.point((left+1,y),fill=c['edge'] if y<20 else c['light'])
            d.point((right-1,y),fill=c['glass_dark'] if y<20 else c['dark'])
    stopper(d,c)
    d.point((13,8),fill=c['white'])
    # A slim reflection on the left wall, and a blue facet on the right.
    d.line([(10,17),(8,21),(6,25),(5,28)],fill=c['white'])
    d.line([(19,17),(23,27),(24,31)],fill=c['light'])
    d.line((5,32,25,32),fill=c['blue'])
    # Two gold stars, spaced so both survive native inventory size.
    four_point(d,13,26,3,c['gold'])
    d.point((13,26),fill=c['white'])
    four_point(d,20,29,1,c['gold'])
    # Small side ribbon and hanging charm follow the triangular SVG.
    d.rectangle((11,8,19,9),fill=c['ribbon_dark'])
    d.line((12,8,18,8),fill=c['ribbon'])
    d.polygon([(19,8),(23,7),(26,9),(25,11),(21,11),(19,10)],fill=c['ribbon_dark'])
    d.line([(20,9),(23,8),(24,9),(23,10),(21,10)],fill=c['ribbon'])
    d.polygon([(20,10),(25,12),(25,13),(22,13),(19,11)],fill=c['ribbon_dark'])
    d.line([(20,10),(23,12),(24,12)],fill=c['ribbon'])
    d.line((14,10,13,13),fill=c['cork_light'])
    d.ellipse((11,13,14,16),fill=c['cork_dark'])
    d.rectangle((12,14,13,15),fill=c['gold'])
    return im


def moon_dew():
    # The round bow bottle was replaced by the dewdrop flask in GenerateMoonDewElixirPixel.py.
    import GenerateMoonDewElixirPixel
    return GenerateMoonDewElixirPixel.build_sprite()[0]


def write_sprite(name,im):
    OUTPUT.mkdir(parents=True,exist_ok=True);PREVIEW.mkdir(parents=True,exist_ok=True)
    im.save(OUTPUT/f'{name}.png')
    im.save(ASSETS/f'{name}_Pixel.png')
    im.resize((im.width*8,im.height*8),Image.Resampling.NEAREST).save(PREVIEW/f'{name}_Pixel_x8.png')
    pixels=list(im.get_flattened_data())
    assert {p[3] for p in pixels}=={0,255}
    colors=sorted({p for p in pixels if p[3]})
    symbols='abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'
    lookup={color:symbols[i] for i,color in enumerate(colors)}
    draft={'name':name+' - hand-authored pixel sprite',
           'source_note':'Rebuild with Tools/PotionPixel/GenerateStarAndMoonPotionPixels.py. References: round1 triangle StarPowerPotion.svg and ExampleAssets/MoonDewElixir.svg.',
           'size':list(im.size),'palette':{'.':'#00000000',**{lookup[c]:'#'+bytes(c[:3]).hex() for c in colors}},
           'pixels':[''.join(lookup.get(im.getpixel((x,y)),'.') for x in range(im.width)) for y in range(im.height)]}
    (OUTPUT/f'{name}.pixel.json').write_text(json.dumps(draft,indent=2)+'\n',encoding='utf-8')
    print(name,im.size,'colors:',len(colors),'bounds:',im.getbbox())


if __name__=='__main__':
    write_sprite('StarPowerPotion',starpower())
    write_sprite('MoonDewElixir',moon_dew())
