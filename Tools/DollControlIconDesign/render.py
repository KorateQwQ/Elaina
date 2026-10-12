"""Review localized cat refinement and translucent contours against the original."""
from pathlib import Path
import json
import subprocess
import sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
SOURCE=ROOT/'ElainaModSkills/Skills/DollControl/DollControlSkill.svg'
CONVERTER=Path.home()/'.codex/skills/svg-to-png/scripts/convert_svg.py'
def render(source,destination,size):
 subprocess.run([sys.executable,str(CONVERTER),str(source),str(destination),'--width',str(size),'--overwrite'],check=True)
render(SOURCE,SOURCE.with_suffix('.png'),56)
render(SOURCE,HERE/'DollControlSkill.large.png',560)
render(HERE/'DollControlSkill.before.svg',HERE/'DollControlSkill.before-raw.png',56)
render(ROOT/'ElainaModSkills/Skills/Flight/FlightSkill.svg',HERE/'FlightSkill.reference.png',56)
old=Image.open(HERE/'DollControlSkill.before-raw.png').convert('RGBA')
new=Image.open(SOURCE.with_suffix('.png')).convert('RGBA')
broom=Image.open(HERE/'FlightSkill.reference.png').convert('RGBA')
def stats(im):
 a=list(im.getchannel('A').get_flattened_data())
 return {'alpha_weighted_area':round(sum(a)/255,2),'pixels_alpha_at_least_230':sum(v>=230 for v in a)}
metrics=dict(zip(['original','redrawn','broom'],map(stats,[old,new,broom])))
metrics['area_reduction_percent']=round(100*(1-metrics['redrawn']['alpha_weighted_area']/metrics['original']['alpha_weighted_area']),1)
metrics['changed_occupied_pixel_positions']=sum(bool(a[3])!=bool(b[3])for a,b in zip(old.get_flattened_data(),new.get_flattened_data()))
(HERE/'metrics.json').write_text(json.dumps(metrics,indent=2)+'\n',encoding='utf-8')
print(json.dumps(metrics))

def bloom(im):
 a=im.getchannel('A')
 bright=a.point(lambda v:max(0,(v-64)*255/191))
 near=bright.filter(ImageFilter.GaussianBlur(.85))
 far=bright.filter(ImageFilter.GaussianBlur(2.1))
 out=Image.new('RGB',im.size)
 out.putdata([tuple(min(255,round(29+base*.91+(n+f*.85)*t))for t in (1,.46,.94))
              for base,n,f in zip(a.get_flattened_data(),near.get_flattened_data(),far.get_flattened_data())])
 return out

def on_bg(im,bg='#211f2c'):
 out=Image.new('RGBA',im.size,bg);out.alpha_composite(im)
 return out.convert('RGB')
def font(n):return ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',n)
proof=Image.new('RGB',(860,650),'#211f2c');draw=ImageDraw.Draw(proof)
draw.text((26,18),'人偶操控 / 实白主线与半透明细节',font=font(25),fill='#f1edf6')
draw.text((26,60),'猫：上挑眯眼  ·  犬：点眼  ·  兔 / 熊：错位叉眼',font=font(15),fill='#c8c0d3')
for x,label,im in [(26,'原版人偶（造型基准）',old),(456,'实白主轮廓 · 局部半透明',new)]:
 draw.text((x,106),label,font=font(17),fill='#eee8f4')
 proof.paste(on_bg(im),(x+314,166))
 draw.text((x+302,233),'56 × 56',font=font(12),fill='#aaa1b9')
 proof.paste(on_bg(im).resize((224,224),Image.Resampling.NEAREST),(x,146))
 draw.text((x,381),'原稿 · 最近邻放大 4 倍',font=font(13),fill='#aaa1b9')
 proof.paste(bloom(im).resize((168,168),Image.Resampling.NEAREST),(x,418))
 proof.paste(bloom(im),(x+216,467))
 draw.text((x,595),'相同参数模拟 Bloom',font=font(13),fill='#c4a6c7')
draw.text((26,628),'保留全部原版眼形与猫咪微调。半透明用于内耳、口鼻边线、遮挡处与悬丝；未预烘焙光晕。',font=font(12),fill='#a59aac')
for name in ['DollControlSkill.comparison.png','DollControlSkill.pixel-comparison.png','DollControlSkill.original-comparison.png']:
 proof.save(HERE/name)
on_bg(Image.open(HERE/'DollControlSkill.large.png').convert('RGBA')).save(HERE/'DollControlSkill.preview.png')
qa=Image.new('RGB',(800,420),'#302d3c')
for x,bg in [(20,'#211f2c'),(420,'#aaa5b1')]:
 for y,im in [(20,new),(220,new.transpose(Image.Transpose.FLIP_LEFT_RIGHT))]:
  tile=on_bg(im,bg)
  qa.paste(tile.resize((168,168),Image.Resampling.NEAREST),(x,y))
  qa.paste(tile,(x+200,y+50))
qa.save(HERE/'DollControlSkill.pixel-qa.png')

assert new.size==(56,56)
assert all((r,g,b)==(255,255,255)for r,g,b,a in new.get_flattened_data()if a)
data=json.loads((HERE/'DollControlSkill.pixel.json').read_text(encoding='utf-8'))
cat_edits={tuple(p['xy'])for p in data['cat_edits']}
assert all(old.getpixel((x,y))[3]or not new.getpixel((x,y))[3]
           for y in range(56)for x in range(56)if(x,y)not in cat_edits)
assert all(new.getpixel((x,y))[3]>0 for y in range(56)for x in range(56)
           if old.getpixel((x,y))[3]==104 and(x,y)not in cat_edits)
assert set(new.getchannel('A').get_flattened_data())=={0,104,255}
assert len(data['eyes'])==8
for name,points in data['eyes'].items():
 for xy in points:
  p=tuple(xy)
  assert old.getpixel(p)[3]==104,(name,p)
  assert new.getpixel(p)[3]==255,(name,p)
 # Check no additional eye pixels inside each original eye's bounding box.
 x0=min(p[0]for p in points);x1=max(p[0]for p in points)
 y0=min(p[1]for p in points);y1=max(p[1]for p in points)
 assert {(x,y)for y in range(y0,y1+1)for x in range(x0,x1+1)if new.getpixel((x,y))[3]==255}==set(map(tuple,points))
assert [[new.getpixel((x,y))[3]for x in range(56)]for y in range(56)]==data['alpha_grid']
print('Verified original structural pixels and all 8 eye shapes; alpha palette 0/104/255; exact SVG/PNG agreement.')

# The immediately preceding revision is saved as SVG even if its PNG was absent.
render(HERE/'before-line-hierarchy/DollControlSkill.svg',HERE/'before-line-hierarchy/DollControlSkill.png',56)
prior=Image.open(HERE/'before-line-hierarchy/DollControlSkill.png').convert('RGBA')
review=Image.new('RGB',(820,565),'#211f2c');rd=ImageDraw.Draw(review)
rd.text((24,18),'人偶线条 / 实白与半透明的分工',font=font(25),fill='#f1edf6')
rd.text((24,59),'实白突出主轮廓与表情；半透明用于内层细节、遮挡与悬丝。',font=font(14),fill='#c8c0d3')
for x,label,im in [(24,'上一版：轮廓整体偏淡',prior),(430,'本次：实白主线 + 半透明细节',new)]:
 rd.text((x,103),label,font=font(18),fill='#eee8f4')
 review.paste(on_bg(im),(x,141))
 rd.text((x+74,161),'56 × 56 原尺寸',font=font(13),fill='#aaa1b9')
 review.paste(on_bg(im).resize((168,168),Image.Resampling.NEAREST),(x,222))
 review.paste(bloom(im).resize((168,168),Image.Resampling.NEAREST),(x+188,222))
 rd.text((x,406),'像素原稿 · 3 倍',font=font(13),fill='#aaa1b9')
 rd.text((x+188,406),'相同参数模拟 Bloom',font=font(13),fill='#c4a6c7')
rd.text((24,466),'与参考一致的三档 alpha：实白 255、半透明 104、全透明 0。',font=font(15),fill='#d8cfe3')
rd.text((24,510),'透明底 PNG 未预烘焙光晕；模拟 Bloom 仅供横向比较。',font=font(13),fill='#a59aac')
review.save(HERE/'DollControlSkill.line-comparison.png')
