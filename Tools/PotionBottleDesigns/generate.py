"""Build review-only bottle concepts; never writes production assets or catalog data."""
from pathlib import Path
from urllib.parse import quote, unquote
import argparse
import html
import json
import subprocess
import sys

from PIL import Image, ImageDraw, ImageFont

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.insert(0, str(REPO / 'Tools/AlchemyPlan'))
from pencil_style import convert

CONCEPTS = [
    dict(id='A', name='三角烧瓶', use='星力药水', old='StarPowerPotion', color='#6587d8', dark='#2e3777', light='#c8dfff',
         note='宽底三角轮廓，保留一大一小两颗十字星。', neck=14, width=12, level=36,
         body='M27 16H37V24L53 51Q55 56 49 56H15Q9 56 11 51L27 24Z',
         highlight='M24 31 16 48', facets='M27 26 22 53M37 26 45 53M14 52H49',
         emblem='<path d="m29 39 1.1 4.4 4.4 1.1-4.4 1.1-1.1 4.4-1.1-4.4-4.4-1.1 4.4-1.1Z"/><path d="m40 46 .7 2.8 2.8.7-2.8.7-.7 2.8-.7-2.8-2.8-.7 2.8-.7Z"/>'),
    dict(id='B', name='圆肚月纹瓶', use='圆瓶比例参考', old='MoonDewElixir', color='#8494d8', dark='#45447f', light='#e2dcff',
         note='仅比较圆肚比例；月露合剂保留现有圆瓶。', neck=13, width=12, level=36,
         body='M27 15H37V22A19 19 0 1 1 27 22Z',
         highlight='M21 28C16 31 14 36 14 41', facets='',
         emblem='<path d="M34 38a8 8 0 1 0 5 13c-8 1-12-7-5-13Z"/><path d="m41 36 .65 2.3 2.3.65-2.3.65-.65 2.3-.65-2.3-2.3-.65 2.3-.65Z"/>'),
    dict(id='C', name='菱形棱晶瓶', use='嗜血药水', old='BloodthirstPotion', color='#b55778', dark='#673b61', light='#edbbcb',
         note='尖底菱形和红色药液，轮廓更有攻击性。', neck=14, width=12, level=35,
         body='M27 16H37V24L51 37L33 59Q32 60 31 59L13 37L27 24Z',
         highlight='M25 29 18 36', facets='M27 25 22 37 32 57 42 37 37 25M14 37H50',
         emblem='<path d="M32 39c-1 3-5 6-5 9a5 5 0 0 0 10 0c0-3-4-6-5-9Z"/>'),
    dict(id='D', name='六角刻度瓶', use='集中药水', old='ConcentrationPotion', color='#ac80cb', dark='#5b3d81', light='#e7c6ef',
         note='平直瓶壁和刻度，适合强调精准与稳定。', neck=15, width=16, level=35,
         body='M25 17H39V22L48 29V49L40 57H24L16 49V29L25 22Z',
         highlight='M21 31V42', facets='M24 26V52M40 26V52',
         emblem='<g fill="none" stroke="#ffe3b7" stroke-width="1.1"><circle cx="32" cy="43" r="6"/><path d="M32 34v5m0 8v5M23 43h5m8 0h5"/><circle cx="32" cy="43" r="1.2"/></g><path d="M43 33h2m-2 5h2m-2 5h2" fill="none" stroke="#e9d9f3" stroke-width=".7"/>'),
    dict(id='E', name='扁圆共鸣瓶', use='共鸣药水', old='ResonancePotion', color='#aa79c6', dark='#634687', light='#e7c8f4',
         note='低重心的扁圆瓶身，中央刻着三道共鸣环。', neck=15, width=12, level=37,
         body='M27 17H37V28C49 29 57 34 57 43C57 54 46 58 32 58S7 54 7 43C7 34 15 29 27 28Z',
         highlight='M21 33C14 35 11 39 11 43', facets='',
         emblem='<g fill="none" stroke="#ffe6c4" stroke-width="1.1"><ellipse cx="32" cy="45" rx="5" ry="8"/><ellipse cx="26" cy="45" rx="5" ry="8"/><ellipse cx="38" cy="45" rx="5" ry="8"/></g>'),
    dict(id='F', name='细颈轻羽瓶', use='轻羽药水', old='FeatherlightPotion', color='#69b9b8', dark='#3f747f', light='#c7e8e4',
         note='细长滴形，配单侧飘带，整体更轻盈。', neck=13, width=9, level=38,
         body='M29 15H35V27C35 30 42 36 42 43C42 49 38 56 32 61C26 56 22 49 22 43C22 36 29 30 29 27Z',
         highlight='M29 34C26 38 25 42 26 46', facets='',
         emblem='<path d="M29 51c-3-6 0-12 9-14 1 7-3 13-9 14Z"/><path d="m27 54 8-12m-5 6 4-1m-3-2v-3" fill="none" stroke="#668c92" stroke-width=".7"/>'),
    dict(id='G', name='双泡葫芦瓶', use='净土露滴', old='PurificationDew', color='#98b982', dark='#50766d', light='#d5e8c6',
         note='上下双泡与收腰轮廓，用叶片强调净化。', neck=13, width=12, level=40,
         body='M27 15H37V22C46 25 45 32 38 35C49 39 51 47 46 54C41 61 23 61 18 54C13 47 15 39 26 35C19 32 18 25 27 22Z',
         highlight='M25 25q-3 3 0 6M22 42q-3 4-1 9', facets='',
         emblem='<path d="M31 52c-8-1-9-7-8-10 7 0 10 4 8 10Zm1-4c-1-7 3-11 9-11 1 6-2 11-9 11Z"/><path d="M31 54V45m1 3 6-7m-7 11-5-7" fill="none" stroke="#718764" stroke-width=".75"/>'),
    dict(id='H', name='心形药瓶', use='嗜血药水 · 另一方向', old='BloodthirstPotion', color='#c06d8c', dark='#7c3d65', light='#f2c5d6',
         note='心形双肩和尖底，突出生命主题与魔女气质。', neck=14, width=12, level=37,
         body='M27 16H37V27C45 19 56 26 55 37C54 45 43 53 32 60C21 53 10 45 9 37C8 26 19 19 27 27Z',
         highlight='M21 27q-8-1-8 8', facets='',
         emblem='<path d="M32 39c-1.8 3.6-5.5 7-5.5 10a5.5 5.5 0 0 0 11 0c0-3-3.7-6.4-5.5-10Z"/><path d="M29 48q-1 3 1 4" fill="none" stroke="#fff" stroke-width=".75"/>'),
    dict(id='I', name='直筒试剂管', use='新候选 · 实验室试管', old='ConcentrationPotion', color='#79b8aa', dark='#436f77', light='#ccebe1',
         note='向右倾斜 32°，直筒圆底；药液保持水平。', neck=13, width=16, level=33, tilt=32,
         body='M24 15H40V49A8 8 0 0 1 24 49Z',
         highlight='M26.5 20V46', facets='',
         emblem='<rect x="27" y="37" width="10" height="9" rx=".7" fill="#e5d9c1" stroke="#8e8192" stroke-width=".5"/><path d="M29 40h6m-6 2.5h3.5" fill="none" stroke="#847790" stroke-width=".65"/>'),
]
PREFERRED = {'A', 'B', 'C'}
VISIBLE = PREFERRED | {'I'}


def ribbon(c):
    y = c['neck'] + 4
    if c['id'] == 'I':
        return '<path d="M35.5 22h2.5m-1.5 5h1.5m-2.5 5h2.5m-1.5 18h1.5" fill="none" stroke="#dce8e6" stroke-width=".75" stroke-linecap="round"/>'
    if c['id'] == 'F':
        return f'<path d="M34 {y+2}c9-1 9 7 17 5l-3 4c-8 1-10-5-15-6Z" fill="#ccb3dc" stroke="#72547e" stroke-width=".7"/><path d="M28 {y}h8v2h-8Z" fill="#d2b48a" stroke="#6f5266" stroke-width=".65"/>'
    if c['id'] in ('C', 'D'):
        return f'<path d="M26 {y}h12v3H26Z" fill="#d3b186" stroke="#776077" stroke-width=".7"/><path d="M29 {y+1}h6" stroke="#fff1d8" stroke-width=".65"/>'
    if c['id'] == 'G':
        return f'<path d="M26 {y+1}h12m-7 1 8 6m-6-6-6 6" fill="none" stroke="#c5a178" stroke-width="1.1"/><path d="M37 {y+3}q7-5 8 2-6 3-8-2Z" fill="#c0cda8" stroke="#718466" stroke-width=".6"/>'
    return f'<path d="M26 {y}h12v2.5H26Z" fill="#cba5cd" stroke="#805785" stroke-width=".65"/><path d="M36 {y+1}q8-5 10 0-1 4-10 1Zm0 1q4 4 8 6l-1-4 3 1q-3-4-10-3Z" fill="#d6b7d7" stroke="#805785" stroke-width=".65"/><path d="m31 {y+2}-3 7" stroke="#d3b186" stroke-width=".7"/><circle cx="28" cy="{y+10}" r="2.2" fill="#edd4a8" stroke="#937285" stroke-width=".6"/>'


def svg(c):
    n, w, y = c['neck'], c['width'], c['level']
    half = w / 2
    tilt = c.get('tilt', 0)
    wave = f'M-40 {y}Q-4 {y-.6} 32 {y}T104 {y}' if tilt else f'M5 {y}Q18 {y-3} 32 {y}T59 {y}'
    liquid_close = 'V104H-40Z' if tilt else 'V64H5Z'
    liquid_axis = f'gradientUnits="userSpaceOnUse" x1="0" y1="{y}" x2="0" y2="59"' if tilt else 'x1="0" y1="0" x2="0" y2="1"'
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100" viewBox="0 0 64 64">
  <title>{c['id']} · {c['name']}</title>
  <desc>候选瓶型，建议用于{c['use']}。{c['note']}</desc>
  <defs>
    <clipPath id="body"><path d="{c['body']}"/></clipPath>
    <linearGradient id="liquid" {liquid_axis}><stop stop-color="{c['light']}"/><stop offset=".38" stop-color="{c['color']}"/><stop offset="1" stop-color="{c['dark']}"/></linearGradient>
    <linearGradient id="cork" x2="0" y2="1"><stop stop-color="#e7c8a0"/><stop offset=".5" stop-color="#bc9273"/><stop offset="1" stop-color="#8c6271"/></linearGradient>
  </defs>
  <g transform="rotate({tilt} 32 32)">
  <path d="{c['body']}" fill="#e6e0ff" fill-opacity=".3"/>
  <g clip-path="url(#body)">
    <g transform="rotate({-tilt} 32 32)">
      <path d="{wave}{liquid_close}" fill="url(#liquid)"/>
      <path d="{wave}" fill="none" stroke="{c['light']}" stroke-width=".9"/>
    </g>
    <path d="{c['facets']}" fill="none" stroke="{c['light']}" stroke-width=".65" stroke-opacity=".7"/>
    <g fill="#f6e4be" stroke="#917487" stroke-width=".4" stroke-linejoin="round">{c['emblem']}</g>
    <circle cx="39" cy="{y+5}" r=".9" fill="none" stroke="#f4e8ff" stroke-width=".65"/>
    <circle cx="23" cy="{y+12}" r=".6" fill="#efdef1"/>
  </g>
  <path d="{c['body']}" fill="none" stroke="#4b395d" stroke-width="1.4" stroke-linejoin="round"/>
  <path d="{c['highlight']}" fill="none" stroke="#fff" stroke-width="1.5" stroke-opacity=".8" stroke-linecap="round"/>
  <path d="M{32-half+2} {n+2}v4" stroke="#fff" stroke-opacity=".55" stroke-width=".85" stroke-linecap="round"/>
  <rect x="{32-half}" y="{n-6}" width="{w}" height="6" rx="1.7" fill="url(#cork)" stroke="#634555" stroke-width=".8"/>
  <path d="M{34-half} {n-4}h{w-4}" stroke="#f0cf9c" stroke-width=".8" stroke-linecap="round"/>
  <circle cx="{31-half+3}" cy="{n-1.4}" r=".45" fill="#785162"/>
  <rect x="{31-half}" y="{n}" width="{w+2}" height="2.8" rx="1.2" fill="#d8c9e6" stroke="#6c537e" stroke-width=".85"/>
  {ribbon(c)}
  </g>
</svg>'''


def render(source, target, converter, size):
    subprocess.run([sys.executable, str(converter), str(source), str(target), '--width', str(size), '--height', str(size), '--overwrite'],
                   check=True, capture_output=True)


def contact_sheet(out):
    regular = 'C:/Windows/Fonts/msyh.ttc'
    bold = 'C:/Windows/Fonts/msyhbd.ttc'
    font = lambda size, weight=False: ImageFont.truetype(bold if weight else regular, size)
    canvas = Image.new('RGB', (1440, 630), '#201c2a')
    draw = ImageDraw.Draw(canvas)
    draw.text((38, 25), '保留 A / B / C，新增 I · 实验室试管', font=font(34, True), fill='#eee2ed')
    draw.text((40, 77), '直筒圆底，简单塞口与刻度    ·    SVG 与彩铅对照    ·    新像素版将在选定瓶型后同步制作', font=font(15), fill='#b7a8c5')
    for i, c in enumerate(c for c in CONCEPTS if c['id'] in VISIBLE):
        x, y = 30+(i % 4)*352, 126+(i//4)*466
        draw.rounded_rectangle((x, y, x+324, y+442), radius=12, fill='#2b2538', outline='#4b3c5b', width=1)
        draw.text((x+18, y+17), c['id'], font=font(25, True), fill='#e1c79f')
        draw.text((x+57, y+20), c['name'], font=font(20, True), fill='#e6d9eb')
        draw.text((x+19, y+53), c['use'], font=font(14), fill='#b9aac7')
        for col, key in enumerate(('raw', 'pencil')):
            image = Image.open(out/'png'/f"{c['id']}-{key}.png").convert('RGBA').resize((142,142), Image.Resampling.LANCZOS)
            canvas.paste(image, (x+14+col*154, y+85), image)
            draw.text((x+51+col*154, y+232), 'SVG 原稿' if key=='raw' else '彩铅预览', font=font(13), fill='#b6a5c5')
        draw.line((x+18,y+265,x+306,y+265), fill='#4b3c5b')
        for col, key in enumerate(('old', 'old-pixel', 'small', 'mask')):
            image = Image.open(out/'png'/f"{c['id']}-{key}.png").convert('RGBA')
            canvas.paste(image,(x+18+col*74+(64-image.width)//2,y+280+(64-image.height)//2),image)
            draw.text((x+22+col*74,y+349), ('原彩铅','原像素','候选46px','剪影')[col],font=font(11),fill='#ac9aba')
        text=c['note']
        line=''; lines=[]
        for char in text:
            if draw.textlength(line+char,font=font(13)) > 284:
                lines.append(line); line=''
            line+=char
        lines.append(line)
        for row,line in enumerate(lines): draw.text((x+18,y+387+row*21),line,font=font(13),fill='#bfaecb')
    draw.text((38,596), '原像素列为当前资源对照，尚不是新瓶型的像素版 · 游戏资源尚未替换',font=font(13),fill='#9686a5')
    canvas.save(out/'bottle-shortlist.png')


def gallery(out):
    cards=[]
    for c in CONCEPTS:
        ident=c['id']
        selected=ident in PREFERRED
        cards.append(f'''<article class="card{' selected' if selected else ''}" data-id="{ident}">
          <div class="card-heading"><b>{ident}</b><div><h2>{c['name']}</h2><span>{c['use']}</span></div><button class="pick" aria-pressed="{str(selected).lower()}" aria-label="选择 {ident} {c['name']}">{'✓' if selected else '＋'}</button></div>
          <button class="art" aria-label="放大 {ident} {c['name']}"><img src="png/{ident}-pencil.png" alt="{c['name']}"/><span>点击放大</span></button>
          <div class="small-row"><div><img src="png/{ident}-old.png" alt="现有彩铅图标"/><small>原彩铅</small></div><div><img class="pixel" src="png/{ident}-old-pixel.png" alt="现有物品像素图"/><small>原像素</small></div><div><img src="png/{ident}-small.png" alt="46像素候选"/><small>候选 · 46px</small></div><div><img src="png/{ident}-mask.png" alt="轮廓剪影"/><small>剪影</small></div></div>
          <p>{c['note']}</p><a href="svg/{ident}-{c['name']}.svg" target="_blank">打开 SVG ↗</a>
        </article>''')
    template=(HERE/'gallery.html').read_text(encoding='utf-8')
    page=template.replace('{{CARDS}}','\n'.join(cards)).replace('{{DATA}}',json.dumps(CONCEPTS,ensure_ascii=False))
    (out/'index.html').write_text(page,encoding='utf-8')


def test_tube_detail(out):
    canvas=Image.new('RGB',(780,466),'#272031')
    draw=ImageDraw.Draw(canvas)
    font=lambda size: ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',size)
    draw.text((30,20),'I · 斜放试剂管',font=font(27),fill='#eddfee')
    draw.text((30,65),'向右倾斜 32° · 直筒圆底 · 药液保持水平',font=font(15),fill='#bfa9c9')
    for i,key in enumerate(('raw','pencil')):
        image=Image.open(out/'png'/f'I-{key}.png').convert('RGBA')
        canvas.paste(image,(20+i*256,108),image)
        draw.text((95+i*256,385),'SVG 原稿' if i==0 else '彩铅预览',font=font(15),fill='#cdb5d7')
    for i,key in enumerate(('small','mask')):
        image=Image.open(out/'png'/f'I-{key}.png').convert('RGBA')
        canvas.paste(image,(619,163+i*113),image)
        draw.text((592,218+i*113),'46px 实际大小' if i==0 else '轮廓剪影',font=font(12),fill='#cdb5d7')
    draw.text((30,434),'设计候选 · 像素版尚未制作 · 游戏资源尚未替换',font=font(12),fill='#9781a4')
    canvas.save(out/'test-tube-detail.png')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--converter',type=Path,default=Path.home()/'.codex/skills/svg-to-png/scripts/convert_svg.py')
    args=parser.parse_args()
    out=HERE/'output'
    for folder in ('svg','pencil','png'): (out/folder).mkdir(parents=True,exist_ok=True)
    for c in CONCEPTS:
        source=svg(c)
        raw=out/'svg'/f"{c['id']}-{c['name']}.svg"
        raw.write_text(source,encoding='utf-8')
        pencil=out/'pencil'/f"{c['id']}-Pencil.svg"
        # Use the notebook's already established hatch treatment, unchanged.
        styled=unquote(convert('data:image/svg+xml,'+quote(source),profile='hatched').split(',',1)[1])
        pencil.write_text(styled,encoding='utf-8')
        render(raw,out/'png'/f"{c['id']}-raw.png",args.converter,256)
        render(pencil,out/'png'/f"{c['id']}-pencil.png",args.converter,256)
        render(pencil,out/'png'/f"{c['id']}-small.png",args.converter,46)
        alpha=Image.open(out/'png'/f"{c['id']}-small.png").convert('RGBA').getchannel('A')
        mask=Image.new('RGBA',(46,46),'#b29ac5'); mask.putalpha(alpha); mask.save(out/'png'/f"{c['id']}-mask.png")
        alpha_large=Image.open(out/'png'/f"{c['id']}-pencil.png").convert('RGBA').getchannel('A')
        mask_large=Image.new('RGBA',(256,256),'#b29ac5'); mask_large.putalpha(alpha_large); mask_large.save(out/'png'/f"{c['id']}-mask-large.png")
        old=Image.open(REPO/'ElainaModAlchemy/item/ExampleAssets'/f"{c['old']}_Pencil.png").convert('RGBA')
        old.resize((46,46),Image.Resampling.LANCZOS).save(out/'png'/f"{c['id']}-old.png")
        pixel=Image.open(REPO/'ElainaModAlchemy/item/ExampleAssets'/f"{c['old']}_Pixel.png").convert('RGBA')
        factor=max(1,min(64//pixel.width,64//pixel.height))
        pixel=pixel.resize((pixel.width*factor,pixel.height*factor),Image.Resampling.NEAREST)
        pixel_canvas=Image.new('RGBA',(64,64))
        pixel_canvas.alpha_composite(pixel,((64-pixel.width)//2,(64-pixel.height)//2))
        pixel_canvas.save(out/'png'/f"{c['id']}-old-pixel.png")
        print(c['id'],c['name'])
    (out/'manifest.json').write_text(json.dumps({'constraints': ['月露合剂保留现有圆瓶及各版本资源。', '任何选定替换必须统一 SVG、彩铅、原生像素贴图和未解锁剪影的瓶身轮廓、瓶口、主色与标志。', '本轮只有 SVG 与彩铅设计；原像素列用于对照，新像素图在选型后制作。'], 'concepts': CONCEPTS},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    (out/'shortlist.json').write_text(json.dumps({'preferred': sorted(PREFERRED), 'new_candidate': 'I', 'deferred': ['D','E','F','G','H'], 'feedback': '用户认可 A/B/C，其余造型不够自然。新增普通实验室试管 I：直筒、圆底、简单塞口，等待选择。', 'production_replaced': False},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    contact_sheet(out); gallery(out); test_tube_detail(out)
    print(out/'bottle-shortlist.png')


if __name__=='__main__': main()
