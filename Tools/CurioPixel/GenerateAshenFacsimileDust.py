"""Approved 32x32 Ashen Facsimile Dust sprite, using integer pixel layers.

Writes the production texture, review PNG and editable palette/character grid.
Does not rasterize SVGs or modify the separate pencil UI artwork.
"""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
RUNTIME = ROOT / 'ElainaModAlchemy/item/ExampleAssets/AshenFacsimileDust_Pixel.png'
PALETTE = {
    '.': '#00000000',
    'o': '#42364f', 'e': '#64536f', 's': '#7f6d8b',
    'm': '#a18dae', 'l': '#bba9c8', 'h': '#ddcfdf', 'w': '#fff3ee',
    'r': '#9c537f', 'p': '#db91b6', 'b': '#f5c1d6',
    'd': '#8e86a3', 'a': '#c4bbd5',
}


def build_sprite():
    im = Image.new('RGBA', (32, 32))
    d = ImageDraw.Draw(im)
    def poly(points, color): d.polygon(points, fill=PALETTE[color])
    def line(points, color): d.line(points, fill=PALETTE[color])
    def dot(x, y, color): d.point((x, y), fill=PALETTE[color])

    # Low, soft ash mound with a rounded crest and a clean silhouette.
    # Avoid the solid black zigzags that made the old pile resemble rocks.
    for y,left,right in [(25,23,25),(26,22,26),(27,21,28),
                         (28,20,29),(29,20,30),(30,22,28)]:
        line([(left,y),(right,y)], 'e')
    line([(23,26),(25,26)], 'a'); dot(23,26,'h')
    line([(22,27),(26,27)], 'a'); line([(23,27),(24,27)], 'h')
    line([(21,28),(28,28)], 'd'); line([(22,28),(24,28)], 'a')
    line([(26,28),(27,28)], 'a')
    line([(21,29),(29,29)], 'd'); dot(25,29,'a')
    line([(23,30),(27,30)], 's')

    # Stepped cloth outline; the shaded right plane gives the pouch volume.
    spans = {12:(8,18),13:(7,19),14:(7,20),15:(6,20),16:(6,21),
             17:(5,21),18:(5,22),19:(4,22),20:(4,22),21:(4,22),
             22:(4,22),23:(4,22),24:(4,22),25:(5,22),26:(5,21),
             27:(6,20),28:(7,19),29:(9,18),30:(11,16)}
    for y,(left,right) in spans.items():
        line([(left,y),(right,y)], 'o')
        if y<30:
            line([(left+1,y),(right-1,y)], 's' if y>26 else 'm')
            dot(right-1,y,'e')
            if y<26: dot(left,y,'e')
    poly([(9,14),(13,14),(16,17),(16,22),(14,25),(9,26),
          (6,23),(6,19),(7,16)], 'l')
    poly([(16,14),(18,16),(20,19),(20,23),(19,26),(17,28),
          (14,28),(17,25),(18,22),(18,18)], 's')
    line([(18,16),(20,20),(20,23),(19,25)], 'e')
    line([(8,15),(7,17),(7,19)], 'h')
    line([(6,21),(6,23),(7,24)], 'h')
    # Short folds follow the cloth's volume instead of filling it with noise.
    line([(9,16),(10,18)], 'm')
    line([(8,20),(10,21)], 'm')
    line([(8,24),(10,25)], 'm')
    line([(9,27),(12,28),(15,28)], 'm')
    dot(10,15,'m')  # Retain the existing shoulder crease from the item asset.

    # Gathered cloth mouth: the approved Revealing Dust pleat geometry, shifted
    # one pixel left to center on this pouch, in the existing lavender palette.
    # Alternating light folds taper inward toward the pink drawstring.
    poly([(7,6),(9,6),(10,4),(12,4),(14,5),(15,4),(17,4),
          (19,6),(20,6),(20,8),(18,12),(9,12),(8,9)], 'o')
    poly([(8,7),(10,7),(11,5),(12,5),(14,6),(15,5),(17,5),
          (18,7),(19,7),(19,8),(17,11),(10,11)], 'l')
    line([(9,7),(10,10)], 'h')
    line([(11,6),(12,9),(12,11)], 'h')
    line([(13,6),(13,10)], 's')
    line([(16,6),(15,10)], 'h')
    line([(18,7),(17,10)], 's')

    # Rolled mouth and the pink binding are separate two-tone pixel clusters.
    poly([(9,10),(18,10),(20,11),(19,13),(17,14),
          (10,14),(7,12),(7,11)], 'o')
    line([(9,10),(17,10)], 'e')
    line([(8,11),(18,11)], 'h'); dot(19,11,'l')
    line([(8,12),(18,12)], 'l')
    line([(8,13),(18,13)], 'r')
    line([(8,12),(10,13),(16,13),(19,12)], 'p')
    line([(9,12),(11,12)], 'b')
    # Keep the seam closed with cloth values instead of a black horizontal slit.
    line([(10,14),(17,14)], 's')
    # Loose short cord ends curve down the shoulders, staying legible at 1x.
    line([(9,13),(8,15),(9,17)], 'r')
    line([(9,13),(9,15),(10,16)], 'p')
    line([(18,13),(19,15),(19,16)], 'r')
    dot(18,14,'b')

    # Finish the bag contour after the highlights and cord layers. A four-neighbor
    # boundary covers horizontal stair steps as well as the left/right endpoints;
    # shading can no longer overpaint a boundary pixel into an apparent break.
    body = {(x,y) for y,(left,right) in spans.items() for x in range(left,right+1)}
    for x,y in body:
        if y<15:
            continue  # the closed mouth and its cord supply the upper contour
        if any((x+dx,y+dy) not in body for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
            dot(x,y,'o')

    # Four-point white cloth emblem; no detached decorative motes.
    line([(12,19),(12,25)], 'h')
    line([(10,22),(14,22)], 'h')
    line([(12,20),(12,24)], 'w')
    line([(11,22),(13,22)], 'w')
    dot(16,22,'b')

    # Close the new mouth's full stair-step contour after the highlights and
    # binding. Work only above the shoulders; the original bag stays intact.
    visible = {(x,y) for y in range(32) for x in range(32)
               if im.getpixel((x,y))[3]}
    for x,y in visible:
        if y < 15 and any((x+dx,y+dy) not in visible
                          for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
            dot(x,y,'o')
    return im


def preview(im, output):
    sheet = Image.new('RGB', (760, 404), '#241f32')
    d = ImageDraw.Draw(sheet)
    # Default Pillow font keeps generation independent of installed fonts.
    font = ImageFont.load_default(size=17)
    for left,bg,ink,title in [(0,'#241f32','#e0d4ea','DARK'),
                               (380,'#e9dfd0','#53465d','LIGHT')]:
        d.rectangle((left,0,left+379,403), fill=bg)
        d.text((left+22,16), 'ASHEN FACSIMILE DUST / '+title, font=font, fill=ink)
        sheet.paste(im.resize((256,256),Image.Resampling.NEAREST),(left+62,51),
                    im.resize((256,256),Image.Resampling.NEAREST))
        d.text((left+22,330),'1x',font=font,fill=ink)
        sheet.paste(im,(left+61,324),im)
        d.text((left+126,330),'2x',font=font,fill=ink)
        doubled=im.resize((64,64),Image.Resampling.NEAREST)
        sheet.paste(doubled,(left+169,311),doubled)
        d.text((left+260,330),'8x above',font=font,fill=ink)
    sheet.save(output)


def main():
    out = HERE / 'AshenFacsimileDust'
    out.mkdir(parents=True, exist_ok=True)
    im = build_sprite()
    im.save(out / 'AshenFacsimileDust_Pixel.png')
    im.save(RUNTIME)
    lookup = {Image.new('RGBA',(1,1),color).getpixel((0,0)):key for key,color in PALETTE.items()}
    pixels = list(im.get_flattened_data())
    assert set(p[3] for p in pixels) == {0,255}
    grid=[''.join(lookup[im.getpixel((x,y))] for x in range(im.width)) for y in range(im.height)]
    data={'name':'AshenFacsimileDust','size':list(im.size),'palette':PALETTE,'pixels':grid}
    (out/'AshenFacsimileDust_Pixel.json').write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
    preview(im,out/'AshenFacsimileDust_Review.png')
    metrics={'size':list(im.size),'visibleColors':len({p for p in pixels if p[3]}),
             'alpha':sorted({p[3] for p in pixels}),'bounds':list(im.getbbox()),
             'source':'ElainaModAlchemy/item/ExampleAssets/AshenFacsimileDust.svg',
             'status':'integrated', 'runtime':RUNTIME.relative_to(ROOT).as_posix()}
    (out/'metrics.json').write_text(json.dumps(metrics,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(metrics))


if __name__ == '__main__':
    main()
