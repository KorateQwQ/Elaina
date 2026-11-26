"""可复现的森林像素场景，沿用 GoldenShockwavePreview 的物块/远山构图。

仅用于预览，不是 Terraria 世界截图。FNA 在特效之前绘制此图，参与真实合成。
"""
from pathlib import Path
from math import sin
from random import Random
from PIL import Image, ImageDraw

out = Path(__file__).parent / 'Assets'
out.mkdir(exist_ok=True)
W, H, GROUND, T = 1280, 720, 504, 8

def shade(c, k):
    return tuple(max(0, min(255, round(v*k))) for v in c)

for night in (False, True):
    rng = Random(318)
    sky0, sky1 = ((7, 13, 31), (38, 54, 79)) if night else ((58, 119, 174), (143, 192, 205))
    far, near = ((30, 44, 65), (26, 50, 59)) if night else ((92, 139, 155), (69, 122, 125))
    grass, dirt, stone = ((34, 72, 47), (49, 43, 41), (48, 53, 61)) if night else ((79, 126, 47), (100, 72, 51), (92, 96, 101))
    trunk, leaf = ((49, 45, 40), (32, 67, 48)) if night else ((100, 76, 49), (56, 108, 54))
    im = Image.new('RGB', (W, H))
    d = ImageDraw.Draw(im)
    for y in range(H):
        u = min(1, y/GROUND)
        d.line((0,y,W,y), fill=tuple(round(a+(b-a)*u) for a,b in zip(sky0,sky1)))
    if night:
        for i in range(180):
            x,y = rng.randrange(W),rng.randrange(360)
            d.rectangle((x,y,x+int(i%9==0),y+int(i%9==0)),fill=shade((172,190,213),rng.uniform(.4,1)))
        d.ellipse((1000,70,1029,99),fill=(208,220,225))
        d.ellipse((1007,72,1015,80),fill=(171,190,203))
    else:
        d.ellipse((1010,76,1040,106),fill=(245,233,177))
        for x,y in ((120,124),(440,80),(780,172),(1120,210)):
            for i in range(8):
                dx,dy = rng.randrange(-35,36),rng.randrange(-10,11)
                d.rectangle((x+dx,y+dy,x+dx+40,y+dy+9),fill=(198,219,224))
    for col,base,amp,freq in ((far,390,72,.008),(near,458,47,.017)):
        points=[(0,H)]+[(x,round((base-amp*(.5+.5*sin(x*freq)))/2)*2) for x in range(0,W+4,4)]+[(W,H)]
        d.polygon(points,fill=col)
    # 近景树留出中央靶区；枝叶与土层能明确显示光片的透光程度。
    def top(col):
        return GROUND if abs(col-80)<22 else GROUND-T*round(1.4*sin(col*.13)+sin(col*.37+1))
    for col in range(160):
        x,y0=col*T,top(col)
        for row,y in enumerate(range(y0,H,T)):
            base=grass if row==0 else stone if row>9 and rng.random()<.75 else dirt
            d.rectangle((x,y,x+7,y+7),fill=shade(base,rng.uniform(.9,1.1)))
            dx,dy=rng.randrange(1,5),rng.randrange(2,6)
            d.rectangle((x+dx,y+dy,x+dx+2,y+dy+1),fill=shade(base,.75))
            if row==0:d.rectangle((x,y,x+7,y+1),fill=shade(grass,1.25))
        if abs(col-80)>22 and col%11==3:
            h=rng.randrange(68,126)
            d.rectangle((x+2,y0-h,x+5,y0),fill=trunk)
            d.line((x+3,y0-h+28,x-9,y0-h+16),fill=trunk,width=3)
            d.line((x+4,y0-h+45,x+14,y0-h+30),fill=trunk,width=3)
            for i in range(18):
                dx,dy=rng.randrange(-15,16)//2*2,rng.randrange(-20,20)//2*2
                d.rectangle((x+dx,y0-h+dy,x+dx+18,y0-h+dy+12),fill=shade(leaf,rng.uniform(.85,1.18)))
        if col%3==0:
            d.line((x+3,y0-1,x+1,y0-5),fill=grass,width=1)
            d.line((x+3,y0-1,x+5,y0-6),fill=shade(grass,1.3),width=1)
    path=out/('ForestNight.png' if night else 'ForestDay.png')
    im.resize((2560,1440),Image.Resampling.NEAREST).save(path)
    print(path)
