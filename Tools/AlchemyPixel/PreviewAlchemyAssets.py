"""Create offline source/pencil/pixel comparison sheets and a local catalog."""
import html
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
PREVIEW=HERE/'preview'
MANIFEST=json.loads((HERE/'manifest.json').read_text(encoding='utf-8'))
ASSETS=ROOT/MANIFEST['output']


def font(size):
    for file in ('C:/Windows/Fonts/msyh.ttc','/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'):
        if Path(file).exists():return ImageFont.truetype(file,size)
    return ImageFont.load_default(size=size)


def label(d,xy,text,size=13,color='#d7c8e4',anchor='mm'):
    d.text(xy,text,font=font(size),fill=color,anchor=anchor)


def centered(sheet,image,cx,cy):
    sheet.alpha_composite(image,(cx-image.width//2,cy-image.height//2))


def pixels(light=False):
    bg,panel,line,text=('#e8e3d9','#f3eee6','#b4a6b6','#48334f') if light else ('#201d2f','#2a253b','#54425f','#e5d6ec')
    sheet=Image.new('RGBA',(1080,884),bg);d=ImageDraw.Draw(sheet)
    label(d,(24,30),'炼金物品 · 像素贴图总览',23,text,'lm')
    label(d,(24,62),'每格左侧 4× 最近邻放大，右侧原尺寸 · 22 件物品',12,text,'lm')
    for row,(cat,name,en) in enumerate(MANIFEST['categories']):
        y=117+row*190
        label(d,(24,y-16),name+' / '+en,12,text,'lm')
        for col,item in enumerate(i for i in MANIFEST['items'] if i['cat']==cat):
            x=24+col*174
            d.rounded_rectangle((x,y,x+161,y+167),3,fill=panel,outline=line)
            im=Image.open(ASSETS/item['pixel']).convert('RGBA')
            centered(sheet,im.resize((im.width*4,im.height*4),Image.Resampling.NEAREST),x+65,y+70)
            centered(sheet,im,x+138,y+70)
            label(d,(x+81,y+145),item['name'],11,text)
            label(d,(x+81,y+159),f'{im.width}×{im.height} · {item["pixelColors"]} 色',9,text)
    target=PREVIEW/('pixels-light.png' if light else 'pixels-dark.png')
    sheet.save(target)
    return target


def comparison():
    sheet=Image.new('RGBA',(1208,1084),'#201d2f');d=ImageDraw.Draw(sheet)
    label(d,(24,28),'炼金图标 · 三种资产对照',23,'#ecdef2','lm')
    label(d,(24,56),'原始 SVG / 彩铅 PNG / 像素 PNG · 像素图使用 2× 最近邻放大',12,'#bca8ce','lm')
    for index,item in enumerate(MANIFEST['items']):
        x=24+(index%4)*296;y=82+(index//4)*160
        d.rounded_rectangle((x,y,x+280,y+145),4,fill='#2a253b',outline='#54425f')
        label(d,(x+140,y+19),item['name'],13)
        original=Image.open(PREVIEW/'source'/f'{item["stem"]}.png').convert('RGBA').resize((72,72),Image.Resampling.LANCZOS)
        pencil=Image.open(ASSETS/item['pencil']).convert('RGBA').resize((72,72),Image.Resampling.LANCZOS)
        pixel=Image.open(ASSETS/item['pixel']).convert('RGBA')
        pixel=pixel.resize((pixel.width*2,pixel.height*2),Image.Resampling.NEAREST)
        for image,cx in ((original,x+48),(pencil,x+140),(pixel,x+232)):centered(sheet,image,cx,y+78)
        for title,cx in (('原始',x+48),('彩铅',x+140),('像素',x+232)):label(d,(cx,y+126),title,10,'#b7a1c8')
    sheet.save(PREVIEW/'all-assets-comparison.png')


def catalog():
    cards=[]
    for item in MANIFEST['items']:
        prefix='../../../'+MANIFEST['output']+'/'
        images=[]
        for field,title in (('original','原始 SVG'),('pencil','彩铅 PNG'),('pixel','像素 PNG')):
            sizes=item['pixelSize'] if field=='pixel' else [72,72]
            style=f'width:{sizes[0]*2}px;height:{sizes[1]*2}px' if field=='pixel' else 'width:72px;height:72px'
            images.append(f'<figure><a href="{prefix+item[field]}"><img class="{field}" src="{prefix+item[field]}" style="{style}" alt="{html.escape(item["name"])} · {title}"></a><figcaption>{title}</figcaption></figure>')
        cards.append(f'<article><h2>{html.escape(item["name"])}</h2><div class="variants">{"".join(images)}</div><small>{item["stem"]} · 像素 {item["pixelSize"][0]}×{item["pixelSize"][1]} · {item["pixelColors"]} 色</small></article>')
    content='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>炼金物品资产对照</title><style>
    *{box-sizing:border-box}body{margin:0;background:#201d2f;color:#ecdef2;font:14px "Microsoft YaHei",sans-serif;padding:28px}header{display:flex;align-items:center;gap:20px;margin-bottom:24px}h1{font-size:24px;margin:0}p{color:#bca8ce}button{margin-left:auto;background:#473653;color:inherit;border:1px solid #8e709f;padding:10px 16px;border-radius:4px;cursor:pointer}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(285px,1fr));gap:16px}article{padding:16px 10px;border:1px solid #54425f;background:#2a253b;border-radius:5px;text-align:center}h2{font-size:15px;font-weight:400;margin:0 0 14px}.variants{display:flex;justify-content:space-around}figure{margin:0;width:30%}figure a{height:86px;display:flex;align-items:center;justify-content:center}img.pixel{image-rendering:pixelated}figcaption{font-size:11px;color:#bca8ce}small{display:block;font-size:10px;color:#a791b8;margin-top:18px}.light{background:#e8e3d9;color:#48334f}.light article{background:#f3eee6;border-color:#b4a6b6}.light figcaption,.light small,.light p{color:#705577}</style>
    <header><h1>炼金物品 · 三种资产</h1><button type="button" onclick="document.body.classList.toggle('light')">切换深浅背景</button></header><p>22 件物品，每件提供原始 SVG、彩铅 PNG 与像素 PNG。点击图标可查看对应文件；像素图以 2× 最近邻显示。</p><main class="grid">'''+''.join(cards)+'</main></html>'
    (PREVIEW/'index.html').write_text(content,encoding='utf-8')


if __name__=='__main__':
    PREVIEW.mkdir(parents=True,exist_ok=True)
    pixels();pixels(True);comparison();catalog()
    print('Wrote dark/light native pixel checks, 3-style comparison, and preview/index.html.')
