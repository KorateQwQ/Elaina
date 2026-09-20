"""Rebuild preview-only assets. Requires Pillow and the svg-to-png skill's dependencies.
Text uses KL FontManager at runtime; this script only generates non-text assets.
"""
from pathlib import Path
import json, re, math, subprocess, sys, tempfile
from PIL import Image, ImageDraw, ImageChops

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'Assets'
OUT.mkdir(exist_ok=True)
source = (ROOT.parent / 'NewUIExample/elaina-constellation-origin.html').read_text(encoding='utf-8')
# The legacy atlas is still used for utility symbols; updated skill data/art comes from the new reference.
subprocess.run(['node', str(ROOT / 'ImportReference.cjs')], check=True)
subprocess.run([sys.executable, str(ROOT / 'GenerateBackground.py')], check=True)
subprocess.run([sys.executable, str(ROOT / 'GenerateLinks.py')], check=True)

# Preserve the original inline SVG paths and rasterize at 4x with the skill converter.
converter = Path.home() / '.codex/skills/svg-to-png/scripts/convert_svg.py'
symbols = re.findall(r'<symbol id="i-([^"]+)" viewBox="([^"]+)">(.*?)</symbol>', source)
symbols.append(('check','0 0 24 24','<path d="m5 12 4 4L19 6"/>'))
sheet = Image.new('RGBA', (512, math.ceil(len(symbols)/5)*100))
icons = {}
with tempfile.TemporaryDirectory(prefix='elaina-icons-') as temp:
    for i, (name, box, body) in enumerate(symbols):
        svg = Path(temp) / (name + '.svg')
        png = svg.with_suffix('.png')
        svg.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="{box}" fill="none" stroke="white" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round">{body}</svg>', encoding='utf-8')
        subprocess.run([sys.executable, str(converter), str(svg), str(png), '--width', '96', '--height', '96'], check=True, capture_output=True)
        x,y = i%5*100, i//5*100
        sheet.paste(Image.open(png), (x,y))
        icons[name] = [x,y,96,96]
sheet.save(OUT / 'Icons.png')
(OUT / 'Icons.json').write_text(json.dumps(icons), encoding='utf-8')

def texture(name,w,h,fn,radius=0,border=None):
    im=Image.new('RGBA',(w,h)); pix=im.load()
    for y in range(h):
        for x in range(w): pix[x,y]=fn(x/max(w-1,1),y/max(h-1,1))
    if radius:
        mask=Image.new('L',(w*2,h*2)); draw=ImageDraw.Draw(mask)
        draw.rounded_rectangle((0,0,w*2-1,h*2-1),radius*2,fill=255)
        im.putalpha(ImageChops.multiply(im.getchannel('A'),mask.resize((w,h),Image.Resampling.LANCZOS)))
    if border: ImageDraw.Draw(im).rounded_rectangle((0,0,w-1,h-1),radius,outline=border,width=1)
    im.save(OUT/(name+'.png'))

def blend(a,b,t): return tuple(round(a[i]+(b[i]-a[i])*max(0,min(1,t))) for i in range(3))+(255,)
texture('MapPanel',1110,695,lambda x,y:blend((21,20,33),(40,34,58),max(0,1-math.hypot((x-.46)*1.6,(y-.56)*1.6))*.65),12,(43,38,55,255))
texture('DetailPanel',320,695,lambda x,y:blend((23,21,32),(36,29,47),max(0,1-y*1.8)*(.65+x*.35)),12,(49,40,61,255))
texture('Unlock',528,88,lambda x,y:blend((215,194,235),(184,154,217),x),10,(224,204,244,255))
texture('Button',256,64,lambda x,y:(255,255,255,255),8)
texture('Disc',128,128,lambda x,y:(255,255,255,round(255*max(0,min(1,(.5-math.hypot(x-.5,y-.5))*128)))))
# Glow.png is generated with premultiplied RGB by GenerateBackground.py above.

def node(name, core=False, hidden=False, fill=(35,29,46), light=(75,61,91), border=(163,147,189)):
    N=312 if core else 224
    im=Image.new('RGBA',(N,N)); draw=ImageDraw.Draw(im)
    if core:
        draw.ellipse((2,2,N-3,N-3),outline=(151,130,185,90),width=3)
        box=(24,24,N-25,N-25); rad=(N-48)//2
    else:
        draw.ellipse((2,2,N-3,N-3),outline=(165,149,187,42),width=3)
        box=(32,32,N-33,N-33); rad=36
    side=box[2]-box[0]+1
    face=Image.new('RGBA',(side,side)); p=face.load()
    for y in range(side):
        for x in range(side): p[x,y]=blend(fill,light,max(0,1-math.hypot(x/side-.35,y/side-.3)))
    mask=Image.new('L',(side,side)); md=ImageDraw.Draw(mask)
    if core or hidden: md.ellipse((0,0,side-1,side-1),fill=255)
    else: md.rounded_rectangle((0,0,side-1,side-1),radius=rad,fill=255)
    face.putalpha(mask); fd=ImageDraw.Draw(face)
    if core: fd.ellipse((1,1,side-2,side-2),outline=border+(255,),width=3)
    elif hidden:
        for a in range(0,360,24): fd.arc((1,1,side-2,side-2),a,a+11,fill=border+(180,),width=3)
    else: fd.rounded_rectangle((1,1,side-2,side-2),radius=rad,outline=border+(255,),width=3)
    if not core and not hidden: face=face.rotate(45,resample=Image.Resampling.BICUBIC,expand=True)
    im.alpha_composite(face,((N-face.width)//2,(N-face.height)//2))
    im.save(OUT/(name+'.png'))
node('Nodelearned',fill=(51,42,72),light=(96,82,119),border=(199,183,223))
node('Nodeavailable',fill=(41,32,48),light=(70,57,72),border=(214,183,130))
node('Nodelocked',fill=(28,25,41),light=(28,25,41),border=(95,83,110))
node('Nodehidden',hidden=True,fill=(31,26,43),light=(31,26,43),border=(104,87,120))
node('NodeCore',core=True,fill=(38,32,50),light=(85,69,101),border=(209,183,221))
print(f'Updated demo data and artwork; generated {len(icons)} utility icons in {OUT}')
