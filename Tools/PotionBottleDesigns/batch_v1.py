"""Potion-specific review drafts using the four accepted bottle silhouettes.

Only SVG, pencil, and silhouette previews are generated. No pixel-art assets or
production files are written. Moon Dew's existing round bottle is preserved.
"""
from pathlib import Path
from urllib.parse import quote, unquote
import argparse
import copy
import json
import sys
import xml.etree.ElementTree as ET

from PIL import Image, ImageDraw, ImageFont

sys.dont_write_bytecode = True
import generate as bottles
import dropper

HERE = Path(__file__).resolve().parent
OUT = HERE / 'output/round1'
ASSETS = bottles.REPO / 'ElainaModAlchemy/item/ExampleAssets'
NS = {'s': 'http://www.w3.org/2000/svg'}

PROPOSALS = [
    dict(number='01', key='bloodlust', name='嗜血药水', art='BloodthirstPotion', shape='C', shape_name='菱形瓶',
         note='棱角呼应进攻性；保留红色药液，以一枚血滴作主标记。',
         emblem='<path d="M32 39c-1 3-5 6-5 9a5 5 0 0 0 10 0c0-3-4-6-5-9Z" fill="#ffe6dd"/><path d="M29.5 47q-1 3 1 4" fill="none" stroke="#fff" stroke-width=".8"/>'),
    dict(number='02', key='starpower', name='星力药水', art='StarPowerPotion', shape='A', shape_name='三角瓶',
         note='宽底三角配深蓝药液，保留一大一小两颗十字星。', emblem=None),
    dict(number='03', key='focus', name='集中药水', art='ConcentrationPotion', shape='I', shape_name='斜试管',
         note='试剂管强调精确；绿色药液配刻度，小标签上画准星。',
         emblem='<rect x="26.5" y="36.5" width="11" height="11" rx=".8" fill="#e7ddc9" stroke="#897a95" stroke-width=".55"/><g fill="none" stroke="#687f7c" stroke-width=".8"><circle cx="32" cy="42" r="2.4"/><path d="M32 37.8v2m0 4.4v2M27.8 42h2m4.4 0h2"/></g>'),
    dict(number='04', key='resonance', name='共鸣药水', art='ResonancePotion', shape='B', shape_name='圆肚瓶',
         note='圆肚容纳扩散的声波，粉紫药液与中心光点保持原意。',
         emblem='<circle cx="32" cy="45" r="1.5" fill="#fff"/><g fill="none" stroke="#fff" stroke-width="1.1"><path d="M28 41q-3 4 0 8m8-8q3 4 0 8"/><path d="M24 38q-6 7 0 14m16-14q6 7 0 14" stroke-opacity=".7"/></g>'),
    dict(number='05', key='featherlight', name='轻羽药水', art='FeatherlightPotion', shape='I', shape_name='斜试管',
         note='细长斜管显得轻巧；浅蓝药液内保留羽毛，减少装饰。',
         emblem='<path d="M28 49c-2-6 2-12 9-13 1 7-3 13-9 13Z" fill="#f1ebdb"/><path d="m27 52 8-12m-6 7 4-1m-2-2v-3" fill="none" stroke="#78919e" stroke-width=".65"/>'),
    dict(number='06', key='isolation', name='净土露滴', art='PurificationDew', shape='J', shape_name='滴管',
         note='橡胶吸头与细玻璃管，管尖落下一滴草绿药液；直接表达滴落净化。', emblem=None),
]


def make_design(proposal):
    base_shape = 'I' if proposal['shape'] == 'J' else proposal['shape']
    c = copy.deepcopy(next(c for c in bottles.CONCEPTS if c['id'] == base_shape))
    original = ET.parse(ASSETS / (proposal['art'] + '.svg')).getroot()
    gradient = original.find("s:defs/s:linearGradient[@id='l']", NS)
    if gradient is None:
        gradient = original.find("s:defs/s:linearGradient[@id='liquid']", NS)
    stops = gradient.findall('s:stop', NS)
    c.update(name=proposal['name'], use=proposal['name'] + '外观候选', note=proposal['note'],
             old=proposal['art'], light=stops[0].get('stop-color'), color=stops[1].get('stop-color'),
             dark=stops[-1].get('stop-color'))
    if proposal['emblem'] is not None:
        c['emblem'] = proposal['emblem']
    return c


def fit(im, size):
    return im.convert('RGBA').resize((size,size), Image.Resampling.LANCZOS)


def overview(records):
    canvas = Image.new('RGB', (1464,1196), '#211c2b')
    draw = ImageDraw.Draw(canvas)
    font = lambda size, bold=False: ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc' if bold else 'C:/Windows/Fonts/msyh.ttc',size)
    draw.text((32,24),'魔药外观 · 适配第一版',font=font(34,True),fill='#efe3ed')
    draw.text((34,79),'01—05 保留本版；06 净土露滴改试滴管。此阶段只做 SVG 与彩铅。',font=font(17),fill='#bba9c9')
    draw.text((1050,30),'月露合剂：圆瓶已确定',font=font(16),fill='#dfc99e')
    draw.text((1050,62),'止痛药：维持现有药片',font=font(14),fill='#ac9bbc')
    for index, p in enumerate(records):
        x, y = 24+(index%3)*480, 135+(index//3)*512
        draw.rounded_rectangle((x,y,x+456,y+488),radius=12,fill='#2d2539',outline='#51405f')
        draw.text((x+18,y+15),p['number'],font=font(23,True),fill='#e6cda4')
        draw.text((x+62,y+15),p['name'],font=font(23,True),fill='#e9dbed')
        draw.text((x+325,y+21),p['shape']+' · '+p['shape_name'],font=font(13),fill='#bba2cb')
        for col,key in enumerate(('raw','pencil')):
            im=fit(Image.open(OUT/'png'/f"{p['key']}-{key}.png"),184)
            canvas.paste(im,(x+28+col*216,y+62),im)
            draw.text((x+91+col*216,y+258),'SVG 原稿' if key=='raw' else '彩铅预览',font=font(13),fill='#c1acca')
        draw.line((x+20,y+295,x+436,y+295),fill='#4c3c59')
        for col,(key,label) in enumerate((('old','当前彩铅'),('small','候选 46px'),('tiny','候选 32px'),('mask','轮廓'))):
            im=Image.open(OUT/'png'/f"{p['key']}-{key}.png").convert('RGBA')
            canvas.paste(im,(x+35+col*105+(46-im.width)//2,y+315+(46-im.height)//2),im)
            draw.text((x+24+col*105,y+374),label,font=font(12),fill='#b7a1c2')
        line=''; lines=[]
        for ch in p['note']:
            if draw.textlength(line+ch,font=font(14)) > 407:
                lines.append(line); line=''
            line+=ch
        lines.append(line)
        for row,line in enumerate(lines): draw.text((x+20,y+425+row*23),line,font=font(14),fill='#c8b6d1')
    draw.text((32,1160),'设计候选 · 瓶型分配可继续调整 · 月露原图保留 · 尚未替换游戏资源',font=font(13),fill='#a08cac')
    canvas.save(OUT/'potion-batch-v1.png')


def gallery(records):
    cards=[]
    for p in records:
        k=p['key']
        thumbs=''.join(f'<div><img src="png/{k}-{key}.png" class="{key}" alt="{label}"><small>{label}</small></div>'
                       for key,label in (('old','当前'),('small','46px'),('tiny','32px'),('mask','轮廓')))
        cards.append(f'''<article class="card" data-key="{k}">
          <div class="heading"><b>{p['number']}</b><div><h2>{p['name']}</h2><small>{p['shape']} · {p['shape_name']} · {'本轮保留' if p['status']=='preferred' else '滴管新稿'}</small></div><button class="pick" aria-label="选择{p['name']}" aria-pressed="false">＋</button></div>
          <button class="compare" aria-label="放大{p['name']}"><span><img src="png/{k}-raw.png" alt="{p['name']} SVG 原稿"><small>SVG 原稿</small></span><span><img src="png/{k}-pencil.png" alt="{p['name']}彩铅预览"><small>彩铅预览</small></span></button>
          <div class="thumbs">{thumbs}</div><p>{p['note']}</p>
          <a href="svg/{p['art']}.svg" target="_blank">打开 SVG ↗</a>
        </article>''')
    template=(HERE/'batch-gallery.html').read_text(encoding='utf-8')
    page=template.replace('{{CARDS}}','\n'.join(cards)).replace('{{DATA}}',json.dumps(records,ensure_ascii=False))
    (OUT/'index.html').write_text(page,encoding='utf-8')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--converter',type=Path,default=Path.home()/'.codex/skills/svg-to-png/scripts/convert_svg.py')
    args=parser.parse_args()
    for folder in ('svg','pencil','png','reference'): (OUT/folder).mkdir(parents=True,exist_ok=True)
    records=[]
    for p in PROPOSALS:
        c=make_design(p)
        raw=OUT/'svg'/f"{p['art']}.svg"
        source=dropper.svg(c) if p['shape']=='J' else bottles.svg(c)
        raw.write_text(source,encoding='utf-8')
        pencil=OUT/'pencil'/f"{p['art']}_Pencil.svg"
        pencil.write_text(unquote(bottles.convert('data:image/svg+xml,'+quote(source),profile='hatched').split(',',1)[1]),encoding='utf-8')
        for size,key,path in ((256,'raw',raw),(256,'pencil',pencil),(100,'asset',pencil),(46,'small',pencil),(32,'tiny',pencil)):
            bottles.render(path,OUT/'png'/f"{p['key']}-{key}.png",args.converter,size)
        alpha=Image.open(OUT/'png'/f"{p['key']}-small.png").getchannel('A')
        mask=Image.new('RGBA',(46,46),'#bba4cc'); mask.putalpha(alpha)
        mask.save(OUT/'png'/f"{p['key']}-mask.png")
        fit(Image.open(ASSETS/f"{p['art']}_Pencil.png"),46).save(OUT/'png'/f"{p['key']}-old.png")
        record={k:v for k,v in p.items() if k!='emblem'}
        record.update(main_color=c['color'], tilt=c.get('tilt',0), status='review' if p['key']=='isolation' else 'preferred', pixel_status='deferred_until_style_selection')
        records.append(record)
        print(p['number'],p['art'],p['shape'])
    for name in ('MoonDewElixir','Painkiller'):
        (OUT/'reference'/f'{name}_Pencil.png').write_bytes((ASSETS/f'{name}_Pencil.png').read_bytes())
    manifest={'version':2,'accepted_shape_families':['A','B','C','I'],'confirmed_potion_shape':{'mana':'round'},
              'notes':['User likes the other five potion designs; retain this version.','Purification Dew is now a standalone dropper proposal, awaiting review.','No pixel artwork generated in this round.','Production assets and references are unchanged.'],
              'proposals':records}
    (OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    overview(records); gallery(records); dropper.detail(OUT)
    print(OUT/'potion-batch-v1.png')


if __name__=='__main__': main()
