"""Approved Bottled Rain sprite on a 31x38 integer pixel grid.

Uses the approved SVG silhouette and the readable pencil version's three drops.
Rebuilds the production texture alongside review assets and an editable grid.
"""
import json
from pathlib import Path
from collections import deque
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
RUNTIME = ROOT / 'ElainaModAlchemy/item/ExampleAssets/BottledRain_Pixel.png'
PALETTE = {
    '.': '#00000000', 'o': '#42364f', 'e': '#73668d',
    'v': '#8d7aac', 'l': '#b99dce', 'h': '#dfcdea',
    'g': '#b9d5e3', 'b': '#8ab7cf', 's': '#577d9f',
    't': '#609cb8', 'a': '#8fd3dc', 'c': '#b3badd',
    'w': '#f8f1f7', 'p': '#e9a8c8', 'r': '#b573a5',
    'y': '#efdbad', 'd': '#496888',
}


def build_sprite():
    im = Image.new('RGBA', (31,38))
    d = ImageDraw.Draw(im)
    def line(points, color): d.line(points, fill=PALETTE[color])
    def poly(points, color): d.polygon(points, fill=PALETTE[color])
    def dot(x,y,color): d.point((x,y), fill=PALETTE[color])

    # Long neck flows into a pear-shaped bulb. All outline pixels are opaque.
    spans={6:(10,20),7:(10,20),8:(10,20),9:(10,20),10:(10,20),
           11:(10,20),12:(10,20),13:(9,21),14:(8,22),15:(7,23),
           16:(6,24),17:(5,25),18:(4,26),19:(4,26),20:(3,27),
           21:(3,27),22:(2,28),23:(2,28),24:(2,28),25:(2,28),
           26:(2,28),27:(2,28),28:(2,28),29:(2,28),30:(2,28),
           31:(2,28),32:(3,27),33:(3,27),34:(4,26),35:(6,24),36:(9,21)}
    for y,(left,right) in spans.items():
        line([(left,y),(right,y)],'o')
        if y==36: continue
        fill='l' if y<13 else 'c' if y<19 else 'b' if y<32 else 't'
        line([(left+1,y),(right-1,y)],fill)
        dot(left+1,y,'h' if y<18 else 'g')
        dot(right-1,y,'v' if y<18 else 's')

    # Broad lavender and blue facets evoke glass without smooth gradients.
    poly([(11,8),(15,8),(15,12),(10,16),(7,20),(5,25),
          (4,28),(4,23),(6,18),(9,14),(11,12)],'g')
    poly([(17,13),(21,16),(24,20),(26,25),(26,31),(24,33),
          (23,27),(23,20),(20,16)],'b')
    line([(24,20),(26,24),(26,30)],'s')

    # Shallow collected water leaves a clear band for distinct suspended drops.
    poly([(4,32),(8,31),(13,32),(19,32),(23,31),(26,32),
          (25,34),(22,35),(9,35),(6,34)],'t')
    line([(5,32),(8,32),(11,33),(18,33),(22,32),(25,32)],'a')
    line([(8,34),(12,35),(21,35),(24,34)],'s')
    line([(11,35),(20,35)],'t')

    # Three rounded cloud lobes: warm upper light, cool lilac underside.
    # One-pixel saddles at x=12 and x=19 keep the side lobes distinct.
    cloud_rows={16:((14,17),),17:((13,18),),18:((9,11),(13,18),(20,21)),
                19:((8,22),),20:((7,23),),21:((7,23),),22:((7,23),),
                23:((8,22),),24:((10,20),)}
    cloud={(x,y) for y,segments in cloud_rows.items() for left,right in segments for x in range(left,right+1)}
    for x,y in cloud:
        edge=any((x+dx,y+dy) not in cloud for dx,dy in ((-1,0),(1,0),(0,-1),(0,1)))
        dot(x,y,'e' if edge else 'w' if y<21 else 'h' if y<23 else 'c')
    line([(9,21),(10,22)],'h')
    # Small gold glint remains INSIDE the bottle, on the crest of the cloud.
    line([(16,18),(16,20)],'y'); line([(15,19),(17,19)],'y')
    dot(16,19,'w')

    # Broad-bottomed 5x5 teardrops retain their pointed silhouette at 1x.
    for x,y in ((10,26),(16,27),(22,26)):
        dot(x,y,'d')
        line([(x-1,y+1),(x+1,y+1)],'d')
        line([(x-2,y+2),(x+2,y+2)],'d')
        line([(x-2,y+3),(x+2,y+3)],'d')
        line([(x-1,y+4),(x+1,y+4)],'d')
        dot(x,y+1,'g')
        line([(x-1,y+2),(x+1,y+2)],'g');dot(x-1,y+2,'w')
        line([(x-1,y+3),(x+1,y+3)],'a')

    # Curved side reflections do not cross the cloud or rain silhouettes.
    line([(7,17),(6,18),(5,20),(5,24)],'w')
    line([(4,25),(4,27)],'g')
    line([(25,22),(25,25)],'g')
    dot(25,28,'a')

    # Lilac closure, softly arched at the top. A pair of pink sealing bands
    # echoes the reviewed illustration, with no redundant neck cloud emblem.
    poly([(11,1),(19,1),(21,2),(21,5),(20,6),(10,6),(9,5),(9,2)],'o')
    line([(11,2),(19,2)],'h')
    line([(10,3),(20,3)],'l'); line([(11,3),(17,3)],'h')
    line([(10,4),(20,4)],'v'); line([(11,4),(19,4)],'l')
    line([(11,5),(19,5)],'v')
    line([(10,6),(20,6)],'r'); line([(11,6),(19,6)],'p')
    line([(10,12),(20,12)],'r'); line([(11,12),(19,12)],'p')
    dot(11,8,'h'); dot(11,9,'h')

    # Repair all body stair transitions last. Opaque contents must never reach
    # outside the sealed glass silhouette or erase its one-pixel outer edge.
    mask={(x,y) for y,(left,right) in spans.items() for x in range(left,right+1)}
    for x,y in mask:
        if y<13: continue
        if any((x+dx,y+dy) not in mask for dx,dy in ((-1,0),(1,0),(0,-1),(0,1))):
            dot(x,y,'o')
    return im


def preview(im,path):
    sheet=Image.new('RGB',(760,458),'#241f32');d=ImageDraw.Draw(sheet)
    font=ImageFont.load_default(size=18)
    for left,bg,ink,title in ((0,'#241f32','#e0d4ea','DARK'),(380,'#e9dfd0','#53465d','LIGHT')):
        d.rectangle((left,0,left+379,457),fill=bg)
        d.text((left+24,15),'BOTTLED RAIN / '+title,font=font,fill=ink)
        large=im.resize((im.width*8,im.height*8),Image.Resampling.NEAREST)
        sheet.paste(large,(left+66,48),large)
        d.text((left+22,388),'1x',font=font,fill=ink)
        sheet.paste(im,(left+64,380),im)
        d.text((left+121,388),'2x',font=font,fill=ink)
        two=im.resize((im.width*2,im.height*2),Image.Resampling.NEAREST)
        sheet.paste(two,(left+164,362),two)
        d.text((left+258,388),'8x above',font=font,fill=ink)
    sheet.save(path)


def main():
    out=HERE/'BottledRain';out.mkdir(parents=True,exist_ok=True)
    im=build_sprite()
    pixels=list(im.get_flattened_data())
    assert set(p[3] for p in pixels)=={0,255}
    # The bottle must be one opaque component: no stray decorative motes.
    visible={(x,y) for y in range(im.height) for x in range(im.width) if im.getpixel((x,y))[3]}
    queue=deque([next(iter(visible))]);seen=set(queue)
    while queue:
        x,y=queue.popleft()
        for point in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            if point in visible and point not in seen: seen.add(point);queue.append(point)
    assert seen==visible, 'Detached pixels outside bottle'
    im.save(out/'BottledRain_Pixel.png')
    im.save(RUNTIME)
    lookup={Image.new('RGBA',(1,1),color).getpixel((0,0)):key for key,color in PALETTE.items()}
    grid=[''.join(lookup[im.getpixel((x,y))] for x in range(im.width)) for y in range(im.height)]
    data={'name':'BottledRain','size':list(im.size),'palette':PALETTE,'pixels':grid}
    (out/'BottledRain_Pixel.json').write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
    preview(im,out/'BottledRain_Review.png')
    metrics={'size':list(im.size),'visibleColors':len({p for p in pixels if p[3]}),
             'alpha':sorted({p[3] for p in pixels}),'bounds':list(im.getbbox()),
             'source':'ElainaModAlchemy/item/ExampleAssets/BottledRain.svg',
             'status':'integrated','opaqueComponents':1,
             'runtime':RUNTIME.relative_to(ROOT).as_posix()}
    (out/'metrics.json').write_text(json.dumps(metrics,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(metrics))


if __name__=='__main__': main()
