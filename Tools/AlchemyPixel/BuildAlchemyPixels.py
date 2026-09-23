"""Build the 22 notebook item sprites directly on integer pixel grids.

Requires Pillow. SVG/Pencil extraction is separate (ExportAlchemyArt.cjs).
These sprites are art assets only; this script changes no runtime item classes.
"""
import importlib.util
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
OUT = ROOT / 'ElainaModAlchemy/item/ExampleAssets'
O = '#332f43'
W = '#eaf5ff'
G = '#7c9dbc'
H = '#b1cde8'
S = '#57799a'
P = '#e69cbf'
R = '#9c1857'


def load(name, file):
    spec = importlib.util.spec_from_file_location(name, file)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


MANA = load('approved_mana', HERE.parent / 'PotionPixel/GenerateManaElixirPixel.py')
BREAD = load('approved_bread', HERE / 'GenerateHoneyStarBreadPixel.py')


def canvas(size):
    im = Image.new('RGBA', size)
    return im, ImageDraw.Draw(im)


def star(d, x, y, color=W):
    d.line([(x-1,y),(x+1,y)], fill=color)
    d.line([(x,y-1),(x,y+1)], fill=color)


# Five discrete liquid values, dark to highlight. Glass/bow stay shared.
LIQUIDS = {
    'bloodlust': ['#351c36','#5f203e','#913052','#cb526e','#f6a0ab'],
    'starpower': ['#202344','#28356e','#3d52a0','#6082ce','#a9c7fa'],
    'focus': ['#183238','#24524e','#337e6a','#54b396','#a0e1c4'],
    'resonance': ['#3a2346','#632e69','#944496','#c477c2','#ecb3e1'],
    'featherlight': ['#243b56','#3a6387','#639bbe','#9bcfe2','#d8f1f3'],
    'isolation': ['#233d3d','#396454','#60915e','#99c783','#d1e8a8'],
    'starspring': ['#193847','#286377','#4499b0','#7bcbd6','#cbf0ed'],
    'flameflask': ['#472c35','#783c35','#ad5935','#e88e45','#ffd187'],
    'ward': ['#45332e','#73562e','#a98735','#dbb850','#f7e89a'],
    'homeward': ['#4c2c48','#79425b','#af6a73','#dc9c8b','#f6d2b3'],
    'bottledsky': ['#1c1e3d','#292858','#383c84','#555cac','#9fa5e9'],
}


def potion(kind):
    if kind == 'mana':
        return MANA.build_sprite()
    colors = dict(MANA.PALETTE)
    colors.update(dict(zip('ndmvl', LIQUIDS[kind])))
    im, d = canvas((20,28))
    # Remove mana's star from the shared liquid before drawing each distinct mark.
    rows = [list(row) for row in MANA.PIXELS]
    for x,y,value in [(8,20,'v'),(7,21,'m'),(8,21,'v'),(9,21,'v'),(8,22,'v'),(14,22,'m')]:
        rows[y][x] = value
    for y,row in enumerate(rows):
        for x,symbol in enumerate(row):
            d.point((x,y), fill=colors[symbol])
    ink = '#fff1b9' if kind in ('starpower','bottledsky','flameflask','ward') else W
    if kind == 'bloodlust':
        d.polygon([(8,19),(6,22),(6,23),(7,24),(9,24),(10,23),(10,22)], fill='#ffb4ba')
        d.point((7,22), fill=W)
    elif kind == 'starpower':
        # Two four-point stars: a 5x5 large cross and a 3x3 small cross.
        d.line([(7,18),(7,22)], fill=ink)
        d.line([(5,20),(9,20)], fill=ink)
        star(d,13,23,ink)
    elif kind == 'focus':
        d.line([(7,19),(9,19),(10,20),(10,22),(9,23),(7,23),(6,22),(6,20),(7,19)], fill=ink)
        d.point((8,21), fill=ink)
    elif kind == 'resonance':
        d.line([(5,20),(4,21),(4,22),(5,23)], fill=ink)
        d.line([(11,20),(12,21),(12,22),(11,23)], fill=ink)
        d.rectangle((7,21,9,22), fill=ink)
    elif kind == 'featherlight':
        d.polygon([(10,19),(8,19),(6,21),(6,23),(8,23),(10,21)], fill=ink)
        d.line([(5,24),(9,20)], fill='#57799a')
    elif kind == 'isolation':
        d.polygon([(6,23),(6,21),(8,19),(11,19),(10,22),(8,23)], fill='#e5f5c2')
        d.line([(6,24),(9,21)], fill='#396454')
    elif kind == 'starspring':
        d.polygon([(5,20),(6,19),(7,19),(8,20),(9,19),(10,19),(11,20),(11,21),(8,24),(5,21)], fill=ink)
    elif kind == 'flameflask':
        d.polygon([(8,18),(10,21),(10,23),(9,24),(7,24),(6,23),(6,21),(7,22)], fill=ink)
        d.line([(8,22),(8,23)], fill='#e88e45')
    elif kind == 'ward':
        d.line([(8,18),(11,20),(11,23),(8,25),(5,23),(5,20),(8,18)], fill=ink)
        star(d,8,21,ink)
    elif kind == 'homeward':
        d.line([(5,21),(8,18),(11,21)], fill=ink)
        d.line([(6,21),(6,24),(10,24),(10,21)], fill=ink)
        d.line([(8,22),(8,24)], fill=ink)
    elif kind == 'bottledsky':
        star(d,7,20,ink)
        d.point((12,22), fill=W)
        d.point((6,24), fill=H)
        d.point((14,19), fill=ink)
        d.line([(8,22),(11,23)], fill='#555cac')
    return im


def pills():
    im,d = canvas((26,22))
    # Rear violet/cream capsule; front pink/cream capsule crosses it.
    d.polygon([(9,1),(12,1),(21,7),(23,9),(23,12),(21,14),(18,14),(8,7),(7,5),(7,3)],fill=O)
    d.polygon([(10,2),(12,2),(16,5),(12,10),(9,7),(8,5),(8,3)],fill='#a18bc9')
    d.polygon([(16,6),(20,8),(22,10),(22,12),(20,13),(18,13),(13,10)],fill='#e4dcf3')
    d.line([(10,3),(11,3),(14,5)],fill='#d3c6f0')
    d.line([(15,6),(12,9)],fill='#64507f')
    d.polygon([(2,13),(10,7),(13,7),(16,10),(16,13),(7,20),(4,20),(1,17),(1,15)],fill=O)
    d.polygon([(3,14),(7,11),(11,15),(6,19),(4,19),(2,17),(2,15)],fill=P)
    d.polygon([(8,11),(11,8),(13,8),(15,10),(15,12),(11,15)],fill='#f4eaf4')
    d.line([(7,11),(10,14)],fill='#9b547e')
    d.line([(3,15),(6,13)],fill='#ffdce9')
    d.polygon([(21,15),(23,15),(25,17),(25,19),(23,21),(20,21),(18,19),(18,17)],fill=O)
    d.polygon([(21,16),(23,16),(24,17),(24,19),(23,20),(20,20),(19,19),(19,17)],fill='#e4dcf3')
    d.line([(19,18),(24,18)],fill='#a18bc9')
    d.line([(21,16),(22,16)],fill=W)
    return im


def extractor():
    im,d=canvas((24,30))
    d.line([(17,10),(20,10),(22,12),(22,16),(20,18),(17,18)],fill=O,width=3)
    d.line([(18,10),(20,11),(21,12),(21,16),(20,17),(18,17)],fill='#ba914d')
    d.rectangle((8,1,13,4),fill=O);d.rectangle((9,2,12,4),fill='#dcb66f')
    d.rectangle((4,5,17,8),fill=O);d.rectangle((5,6,16,7),fill='#bd9357')
    d.line([(6,6),(15,6)],fill='#efd396')
    d.rectangle((5,8,16,25),fill=O);d.rectangle((6,9,15,24),fill=S)
    d.rectangle((7,9,14,16),fill=G);d.line([(7,10),(7,15)],fill=W)
    d.rectangle((6,18,15,22),fill='#ae8cce');d.rectangle((6,23,15,24),fill='#745a9e')
    d.line([(6,18),(9,17),(12,18),(15,17)],fill='#edd1ef')
    d.rectangle((12,19,15,21),fill='#8fcbd2');star(d,9,20)
    d.point((13,23),fill=H)
    for y in (11,14,17):d.point((15,y),fill=O)
    d.rectangle((4,25,17,28),fill=O);d.rectangle((5,26,16,27),fill='#bd9357')
    d.point((6,26),fill='#efd396');d.point((15,27),fill='#74513d')
    d.line([(4,7),(2,8),(3,10)],fill=R,width=2);d.point((2,8),fill=P)
    return im


def dust():
    im,d=canvas((28,26))
    d.polygon([(7,9),(15,9),(18,13),(19,18),(18,22),(15,24),(6,24),(3,22),(2,18),(3,14)],fill=O)
    d.polygon([(7,10),(14,10),(17,14),(18,18),(17,21),(14,23),(6,23),(4,21),(3,18),(4,14)],fill='#83738e')
    d.polygon([(7,11),(12,11),(15,14),(15,19),(12,21),(5,20),(4,17),(5,14)],fill='#b7a9bd')
    d.line([(6,12),(5,14),(5,18)],fill='#e2d8e4')
    d.line([(6,10),(14,10),(17,12)],fill=R,width=2)
    d.line([(6,10),(13,10),(16,11)],fill=P)
    d.line([(15,11),(19,13),(20,15)],fill=P)
    d.rectangle((5,8,15,9),fill=O);d.line([(6,8),(14,8)],fill='#d7cbdc')
    d.polygon([(8,6),(7,5),(8,3),(10,3),(11,1),(13,2),(14,4),(16,4),(16,6)],fill='#b7a9bd')
    d.line([(9,3),(10,4),(13,4)],fill='#e2d8e4')
    star(d,10,17,'#ece2ef')
    d.polygon([(18,24),(20,21),(23,20),(25,21),(27,24)],fill=O)
    d.polygon([(19,23),(21,21),(23,21),(25,23)],fill='#b7a9bd')
    d.point((23,18),fill=P);d.point((26,20),fill=H)
    return im


def star_ink():
    im,d=canvas((26,28))
    # Quill remains a distinct tall silhouette above the low ink pot.
    d.polygon([(14,15),(17,8),(22,1),(24,0),(25,1),(25,3),(19,11),(17,16)],fill=O)
    d.polygon([(15,14),(18,8),(23,1),(24,1),(24,3),(18,12)],fill='#d7cce9')
    d.line([(15,15),(23,3)],fill='#8d79ad')
    for x,y in [(20,5),(18,8),(17,11)]:d.point((x+1,y),fill=W)
    d.rectangle((7,12,15,16),fill=O);d.rectangle((8,13,14,15),fill='#b68757')
    d.line([(9,13),(13,13)],fill='#e4c187')
    d.polygon([(5,16),(18,16),(20,19),(20,25),(18,27),(4,27),(2,25),(2,19)],fill=O)
    d.polygon([(5,17),(17,17),(19,20),(19,25),(17,26),(4,26),(3,25),(3,20)],fill=S)
    d.rectangle((4,21,18,24),fill='#4858a2');d.rectangle((5,25,17,25),fill='#30345f')
    d.line([(4,21),(18,21)],fill='#8898d4')
    d.line([(5,18),(4,20),(4,23)],fill=W)
    d.line([(7,22),(10,24),(14,22),(17,24)],fill='#b1c9ed')
    for xy in [(7,22),(10,24),(14,22),(17,24)]:d.point(xy,fill=W)
    return im


def hair_dye():
    im,d=canvas((20,30))
    # Narrow capped vial plus a curling lock of silver hair.
    d.line([(15,6),(18,8),(18,11),(15,14),(15,17),(18,19),(18,22),(16,25)],fill=O,width=3)
    d.line([(15,6),(17,8),(17,11),(14,14),(14,17),(17,19),(17,22),(15,25)],fill='#c2b6d2')
    d.rectangle((4,7,11,25),fill=O);d.rectangle((5,8,10,25),fill=S)
    d.polygon([(4,24),(11,24),(11,27),(9,29),(6,29),(4,27)],fill=O)
    d.rectangle((5,14,10,25),fill='#a79ebd');d.rectangle((7,15,8,26),fill='#ddd6e6')
    d.line([(6,27),(9,27)],fill='#a79ebd');d.line([(5,14),(10,14)],fill='#eee6f3')
    d.line([(5,9),(5,12)],fill=W);d.point((9,24),fill=W)
    d.rectangle((3,6,12,8),fill=O);d.line([(4,7),(11,7)],fill='#c9bada')
    d.polygon([(6,0),(9,0),(11,2),(11,3),(9,5),(6,5),(4,3),(4,2)],fill=O)
    d.polygon([(6,1),(9,1),(10,2),(10,3),(8,4),(6,4),(5,3),(5,2)],fill='#b69ad6')
    d.line([(6,1),(8,1)],fill='#e5d9f2')
    d.rectangle((4,11,11,12),fill='#9f79c1')
    d.line([(4,12),(2,13),(1,16)],fill='#9f79c1',width=2)
    star(d,8,19,'#f0e8f6')
    return im


def mimic_lure():
    im,d=canvas((28,24))
    d.polygon([(2,10),(3,7),(6,4),(10,3),(18,3),(22,4),(25,7),(26,10)],fill=O)
    d.polygon([(3,9),(4,7),(7,5),(10,4),(18,4),(21,5),(24,7),(25,9)],fill='#a66c3b')
    d.line([(6,6),(9,5),(12,5)],fill='#d29a58')
    d.rectangle((2,13,25,22),fill=O);d.rectangle((3,14,24,20),fill='#945631')
    d.line([(4,14),(23,14)],fill='#bb7b40');d.line([(4,21),(23,21)],fill='#633d32')
    d.rectangle((3,10,24,13),fill='#4c253c')
    for x in (4,8,12,16,20):
        d.polygon([(x,10),(x+2,10),(x+1,12)],fill='#f5dfc4')
    d.line([(12,12),(15,12)],fill=P)
    d.rectangle((12,4,15,9),fill='#e1b858');d.rectangle((12,14,15,21),fill='#c79b48')
    d.rectangle((11,15,16,18),fill=O);d.rectangle((12,15,15,17),fill='#efce7c');d.point((14,16),fill='#684832')
    d.line([(13,2),(10,0),(8,0),(8,2),(12,3)],fill=R,width=2)
    d.line([(14,2),(17,0),(19,0),(19,2),(15,3)],fill=R,width=2)
    d.line([(10,1),(12,2)],fill=P);d.line([(17,1),(15,2)],fill=P);d.point((14,2),fill=P)
    return im


def wish_slip():
    im,d=canvas((18,32))
    d.line([(8,0),(8,6)],fill=R)
    d.line([(7,3),(4,1),(3,2),(4,3),(7,3)],fill=P)
    d.line([(9,3),(12,1),(13,2),(12,3),(9,3)],fill=P)
    d.polygon([(2,6),(14,6),(14,27),(8,24),(2,27)],fill=O)
    d.polygon([(3,7),(13,7),(13,25),(8,23),(3,25)],fill='#e7d8bd')
    d.rectangle((3,7,13,9),fill='#a78bbf');d.line([(4,7),(12,7)],fill='#d8c5e7')
    d.line([(3,10),(3,23)],fill='#faf0d7')
    d.polygon([(8,11),(10,14),(13,14),(10,17),(11,20),(8,18),(5,20),(6,17),(3,14),(6,14)],fill='#a57335')
    d.polygon([(8,12),(9,15),(11,15),(9,16),(10,18),(8,17),(6,18),(7,16),(5,15),(7,15)],fill='#edc665')
    d.point((8,14),fill='#fff0ab')
    d.line([(5,21),(11,21)],fill='#a08a77');d.line([(5,23),(8,23)],fill='#a08a77')
    d.line([(8,24),(8,28)],fill='#8e6fae')
    d.polygon([(8,27),(10,29),(8,31),(6,29)],fill=O)
    d.point((8,28),fill='#d5b6ef');d.line([(7,29),(8,30)],fill='#ae85d3')
    return im


def incense():
    im,d=canvas((28,30))
    # Open brazier, visible burning stick, a thin curling smoke trail.
    d.line([(8,25),(6,28)],fill=O,width=2);d.line([(19,25),(21,28)],fill=O,width=2)
    d.polygon([(1,18),(26,18),(25,22),(22,25),(18,27),(9,27),(5,25),(2,22)],fill=O)
    d.polygon([(3,20),(24,20),(23,23),(20,25),(17,26),(10,26),(6,24),(4,22)],fill='#746184')
    d.line([(6,23),(9,24),(18,24),(22,22)],fill='#c0a8c6')
    d.polygon([(3,17),(24,17),(27,19),(24,21),(3,21),(0,19)],fill=O)
    d.polygon([(3,18),(24,18),(25,19),(23,20),(4,20),(2,19)],fill='#b9a6c8')
    d.line([(5,19),(22,19)],fill='#62556e')
    d.line([(13,18),(13,10)],fill='#a37648');d.point((13,9),fill='#efab67')
    d.line([(13,7),(11,6),(11,4),(14,2),(14,0)],fill='#cab9dd')
    d.point((15,4),fill='#9a89af')
    d.line([(23,1),(21,2),(21,5),(23,7),(25,7)],fill='#dfc991')
    d.line([(22,2),(22,5),(24,6),(25,6)],fill='#f1deb1')
    return im


def pudding():
    im,d=canvas((28,25))
    # A low plate and a translucent-looking (opaque palette) blue dessert.
    d.polygon([(4,18),(23,18),(27,20),(27,22),(23,24),(4,24),(0,22),(0,20)],fill=O)
    d.polygon([(4,19),(23,19),(26,20),(26,22),(23,23),(4,23),(1,22),(1,20)],fill='#dcd8e8')
    d.line([(4,22),(23,22)],fill='#a89cbc')
    d.polygon([(8,7),(19,7),(21,9),(24,20),(21,22),(6,22),(3,20),(6,9)],fill=O)
    d.polygon([(8,8),(18,8),(20,10),(23,20),(20,21),(6,21),(4,20),(7,10)],fill='#6da5df')
    d.polygon([(8,8),(18,8),(19,9),(18,10),(8,10),(7,9)],fill='#466fbb')
    d.line([(8,10),(8,12),(9,13)],fill='#466fbb');d.line([(18,10),(19,12)],fill='#466fbb')
    d.line([(7,13),(6,17),(6,19)],fill='#b9e1f3')
    d.rectangle((9,15,10,16),fill=O);d.rectangle((17,15,18,16),fill=O)
    d.line([(12,18),(13,19),(14,19),(15,18)],fill=O)
    d.point((8,18),fill=P);d.point((20,18),fill=P)
    d.polygon([(12,3),(15,3),(17,5),(15,7),(12,7),(10,5)],fill=R)
    d.rectangle((12,4,15,5),fill=P);d.point((12,4),fill='#f6cadc')
    d.line([(14,3),(15,1),(17,0)],fill='#688b62')
    return im


DRAW = {'painkiller':pills,'extractor':extractor,'mimic':dust,'bread':BREAD.build_sprite,
        'starink':star_ink,'hairdye':hair_dye,'mimiclure':mimic_lure,'wishslip':wish_slip,
        'incense':incense,'pudding':pudding}


def main():
    manifest = json.loads((HERE/'manifest.json').read_text(encoding='utf-8'))
    OUT.mkdir(parents=True,exist_ok=True)
    for item in manifest['items']:
        key=item['id']
        image=potion(key) if key=='mana' or key in LIQUIDS else DRAW[key]()
        data=list(image.get_flattened_data())
        assert set(p[3] for p in data)<= {0,255}, key
        assert image.getbbox(), key
        image.save(OUT/item['pixel'])
        item['pixelSize']=list(image.size)
        item['pixelColors']=len({p for p in data if p[3]})
    (HERE/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('Built '+str(len(manifest['items']))+' opaque-pixel, transparent-background sprites.')


if __name__=='__main__':main()
