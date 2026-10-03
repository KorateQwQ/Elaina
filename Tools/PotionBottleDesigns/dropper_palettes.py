"""Two sacred-color studies for the accepted normal pipette silhouette.

Keep the geometry fixed. Preserve colored gradients beneath pencil hatching so
the rainbow study does not collapse to the midpoint color of a single pigment.
"""
from pathlib import Path
from urllib.parse import quote, unquote
import argparse
import copy
import json
import sys
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw, ImageFont

sys.dont_write_bytecode=True
import batch_v1 as batch
import generate as bottles
import dropper

OUT=Path(__file__).resolve().parent/'output/round1/palettes'
NS='http://www.w3.org/2000/svg'
def tag(name): return '{'+NS+'}'+name
ET.register_namespace('',NS)

PALETTES=[
    dict(id='A',key='Iridescent',name='虹彩珍珠',
         note='珍珠白吸头、淡金套环；药液由冰蓝、淡紫过渡到粉金。',
         liquid=['#f5fcff','#a5dced','#baaaf1','#efb9db','#f5dfa0'],
         bulb=['#fff7e9','#e1d9ed','#ada4c5'],collar=['#fff2ce','#e3c38b','#b49774'],
         outline='#82738f',shadow='#a394b1',drop_edge='#a18bb7'),
    dict(id='B',key='IvoryGold',name='象牙白金',
         note='乳白药液和香槟金微光，更安静、纯净，强调祝福感。',
         liquid=['#fffdf0','#f5edce','#ecdaa5','#e2c57f','#bda272'],
         bulb=['#fff9e9','#e7dfcc','#b9aa95'],collar=['#fff0be','#d6b473','#a78959'],
         outline='#8c7c86',shadow='#b4a48f',drop_edge='#ae956b'),
]


def recolor(p):
    c=batch.make_design(next(v for v in batch.PROPOSALS if v['key']=='isolation'))
    root=ET.fromstring(dropper.svg(c))
    root.find(tag('title')).text='净土露滴 · '+p['name']
    root.find(tag('desc')).text=p['note']
    defs=root.find(tag('defs'))
    def stops(gradient,colors):
        for child in list(gradient): gradient.remove(child)
        for index,color in enumerate(colors):
            ET.SubElement(gradient,tag('stop'),{'offset':str(index/(len(colors)-1)),'stop-color':color})
    liquid=defs.find(f"{tag('linearGradient')}[@id='liquid']")
    liquid.set('x1','20'); liquid.set('y1','33'); liquid.set('x2','25'); liquid.set('y2','52')
    stops(liquid,p['liquid'])
    stops(defs.find(f"{tag('linearGradient')}[@id='rubber']"),p['bulb'])
    stops(defs.find(f"{tag('linearGradient')}[@id='collar']"),p['collar'])
    # The hanging drop gets its own full color range, independent of the tube's
    # longer world-space gradient. At 32px it still reads as a pearly drop.
    dew=ET.SubElement(defs,tag('linearGradient'),{'id':'dew','x1':'0','y1':'0','x2':'1','y2':'1'})
    stops(dew,p['liquid'])
    for element in root.iter():
        if element.get('stroke')=='#4b395d': element.set('stroke',p['outline'])
        elif element.get('stroke')=='#617d69':
            element.set('stroke',p['drop_edge']);element.set('fill','url(#dew)')
        elif element.get('stroke')=='#e2ecdc': element.set('stroke','#fff')
        elif element.get('stroke')==c['light']: element.set('stroke',p['liquid'][0])
        elif element.get('stroke')=='#3f5c52': element.set('stroke',p['shadow'])
        elif element.get('stroke')=='#705b71': element.set('stroke',p['collar'][-1])
        if element.get('fill') in ['url(#liquid)','url(#rubber)','url(#collar)','url(#dew)']:
            element.set('data-palette-fill',element.get('fill'))
    return ET.tostring(root,encoding='unicode')


def pencil(source):
    styled=unquote(bottles.convert('data:image/svg+xml,'+quote(source),profile='hatched').split(',',1)[1])
    root=ET.fromstring(styled)
    defs=root.find(tag('defs'))
    pattern=ET.SubElement(defs,tag('pattern'),{'id':'sacred-hatch','width':'3.6','height':'3.6','patternUnits':'userSpaceOnUse'})
    ET.SubElement(pattern,tag('path'),{'d':'M-.5 3.55 1.1 2.06 2.2 1.24 3.6-.5M3.1 4.1 4.1 3.1','fill':'none','stroke':'#896799','stroke-width':'.45','opacity':'.38'})
    ET.SubElement(pattern,tag('path'),{'d':'M.05 3.6 1.5 2.3 2.5 1.35 3.6.2','fill':'none','stroke':'#fff5f3','stroke-width':'.2','opacity':'.65'})
    for parent in list(root.iter()):
        for element in list(parent):
            gradient=element.get('data-palette-fill')
            if not gradient or element.get('fill')=='none': continue
            element.set('fill',gradient)
            overlay=copy.deepcopy(element)
            overlay.attrib.pop('id',None);overlay.attrib.pop('data-palette-fill',None)
            overlay.set('fill','url(#sacred-hatch)');overlay.set('stroke','none')
            parent.insert(list(parent).index(element)+1,overlay)
    return ET.tostring(root,encoding='unicode')


def preview():
    canvas=Image.new('RGB',(1100,730),'#241e2e')
    d=ImageDraw.Draw(canvas)
    font=lambda n,b=False:ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc' if b else 'C:/Windows/Fonts/msyh.ttc',n)
    d.text((28,22),'净土露滴 · 神圣感试色',font=font(30,True),fill='#f0e5ed')
    d.text((30,72),'滴管造型保持一致，用珍珠白与淡金取代深绿，再比较虹彩和白金两种方向。',font=font(15),fill='#baa6c8')
    for index,p in enumerate(PALETTES):
        x,y=20+index*540,119
        d.rounded_rectangle((x,y,x+520,y+559),radius=12,fill='#2e263b',outline='#594363')
        d.text((x+22,y+17),p['id']+' · '+p['name'],font=font(24,True),fill='#e9d3a7')
        for col,key in enumerate(('raw','pencil')):
            im=Image.open(OUT/f"{p['key']}-{key}.png").convert('RGBA')
            canvas.paste(im,(x+4+col*256,y+64),im)
            d.text((x+92+col*256,y+331),'SVG 原稿' if key=='raw' else '彩铅预览',font=font(13),fill='#c9b2d2')
        d.line((x+22,y+367,x+498,y+367),fill='#513c5f')
        for col,(key,label) in enumerate((('old','原深绿'),('small','新色 46px'),('tiny','新色 32px'))):
            im=Image.open(OUT/'Green-reference.png' if key=='old' else OUT/f"{p['key']}-{key}.png").convert('RGBA')
            canvas.paste(im,(x+64+col*164+(46-im.width)//2,y+387+(46-im.height)//2),im)
            d.text((x+60+col*164,y+445),label,font=font(12),fill='#bfa5c9')
        text=p['note']; split=25
        d.text((x+22,y+496),text[:split],font=font(14),fill='#cbb9d4')
        d.text((x+22,y+520),text[split:],font=font(14),fill='#cbb9d4')
    d.text((30,697),'颜色候选 · SVG 与彩铅 · 尚未制作像素图',font=font(12),fill='#a18bad')
    canvas.save(OUT/'purification-sacred-colors.png')


def gallery():
    cards=[]
    for p in PALETTES:
        key=p['key']
        cards.append(f'''<article><h2>{p['id']} · {p['name']}</h2><p>{p['note']}</p><div class="pair"><figure><img src="{key}-raw.png" alt="{p['name']}原稿"><figcaption>SVG 原稿</figcaption></figure><figure><img src="{key}-pencil.png" alt="{p['name']}彩铅"><figcaption>彩铅预览</figcaption></figure></div><div class="sizes"><figure><img src="Green-reference.png" alt="原绿色版本"><figcaption>原深绿</figcaption></figure><figure><img src="{key}-small.png" alt="46px候选"><figcaption>新色 · 46px</figcaption></figure><figure><img class="tiny" src="{key}-tiny.png" alt="32px候选"><figcaption>新色 · 32px</figcaption></figure></div><a href="PurificationDew_{key}.svg" target="_blank">打开此版 SVG ↗</a></article>''')
    page='''<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>净土露滴 · 神圣感试色</title><style>
    *{box-sizing:border-box}body{margin:0;background:radial-gradient(ellipse at 25% 0,#3a2c47,transparent 60%),#211a2a;color:#ecdfed;font:15px/1.7 'Microsoft YaHei',sans-serif}header,main,footer{max-width:1200px;margin:auto;padding:24px}header{padding-top:35px}header small{color:#e0c497;letter-spacing:3px;font-size:11px}h1{font-family:SimSun,serif;font-weight:400;letter-spacing:3px;margin:10px 0;font-size:35px}p{color:#bfabc9}header p{margin:4px 0}main{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px;padding-top:0}article{border:1px solid #5a4368;border-radius:12px;padding:22px;background:linear-gradient(145deg,#352a43,#2a2135)}h2{font-size:22px;font-weight:500;margin:0;color:#ead3a9}article>p{min-height:50px;font-size:13px;margin:9px 0}.pair,.sizes{display:flex;justify-content:space-around;align-items:center}.pair figure{width:50%;margin:0;text-align:center}.pair img{width:100%;max-width:256px;aspect-ratio:1;object-fit:contain}figcaption{font-size:12px;color:#baa0c7}.sizes{margin-top:24px;padding:20px 0;border-top:1px solid #513b61}.sizes figure{display:grid;grid-template-rows:46px 22px;align-items:center;justify-items:center;gap:8px;margin:0}.sizes img{width:46px;height:46px}.sizes img.tiny{width:32px;height:32px}a{font-size:13px;color:#d4b3e1;text-decoration:none}a:hover{text-decoration:underline}footer{font-size:12px;color:#a68eb5}@media(max-width:770px){main{grid-template-columns:1fr}h1{font-size:28px}}
    </style></head><body><header><small>ELAINA · PURIFICATION / COLOR STUDY</small><h1>净土露滴，换一种更纯净的颜色</h1><p>保持滴管造型。A 用珍珠虹彩，B 用乳白与香槟金。</p><p>原稿与彩铅均保留多色层次，下方可查看实际小图。</p></header><main>'''+''.join(cards)+'''</main><footer><a href="../index.html">← 返回整套魔药</a>　颜色评审稿 · 其他五款保持原样 · 像素图待定</footer></body></html>'''
    (OUT/'index.html').write_text(page,encoding='utf-8')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--converter',type=Path,default=Path.home()/'.codex/skills/svg-to-png/scripts/convert_svg.py')
    args=parser.parse_args();OUT.mkdir(parents=True,exist_ok=True)
    (OUT/'Green-reference.png').write_bytes((batch.OUT/'png/isolation-small.png').read_bytes())
    for p in PALETTES:
        source=recolor(p)
        raw=OUT/f"PurificationDew_{p['key']}.svg";raw.write_text(source,encoding='utf-8')
        colored=OUT/f"PurificationDew_{p['key']}_Pencil.svg";colored.write_text(pencil(source),encoding='utf-8')
        for size,key,path in ((256,'raw',raw),(256,'pencil',colored),(100,'asset',colored),(46,'small',colored),(32,'tiny',colored)):
            bottles.render(path,OUT/f"{p['key']}-{key}.png",args.converter,size)
    (OUT/'manifest.json').write_text(json.dumps({'status':'color_review','geometry':'same diagonal dropper as round1','pixel_art_generated':False,'variants':PALETTES},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    preview();gallery();print(OUT/'purification-sacred-colors.png')


if __name__=='__main__': main()
