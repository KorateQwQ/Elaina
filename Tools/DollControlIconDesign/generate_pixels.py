"""Solid primary strokes, 104-alpha secondary detail, transparent hollow faces."""
from pathlib import Path
from collections import defaultdict
import io
import json
import resvg_py
from PIL import Image, ImageDraw

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
DEST=ROOT/'ElainaModSkills/Skills/DollControl/DollControlSkill'
REFERENCE=HERE/'DollControlSkill.before.svg'
source=Image.open(io.BytesIO(resvg_py.svg_to_bytes(svg_path=str(REFERENCE)))).convert('RGBA')
source.save(HERE/'DollControlSkill.before-raw.png')

# Masks identify material only; final support and every shape come from the original.
regions={
 'cat':[(2,24),(7,25),(13,22),(17,25),(19,30),(21,31),(21,38),(6,38),(2,35)],
 'dog':[(20,23),(25,20),(26,18),(32,18),(35,21),(38,23),(38,31),(34,34),(24,31),(20,30)],
 'rabbit':[(38,29),(41,25),(41,20),(43,17),(45,17),(47,25),(49,22),(52,19),(54,19),(54,23),(51,28),(52,31),(52,35),(49,37),(43,37),(39,35)],
 'bear':[(14,33),(16,31),(20,31),(23,34),(26,34),(30,30),(34,30),(37,31),(37,37),(34,39),(35,43),(35,47),(32,50),(29,51),(22,51),(17,48),(16,43),(17,40),(14,37)]
}
body=Image.new('1',(56,56)); painter=ImageDraw.Draw(body)
for polygon in regions.values():painter.polygon(polygon,fill=1)

# Copied from the original alpha-104 feature pixels, not replaced with a shared symbol.
eyes={
 'cat-left':[(6,30),(5,31),(7,31)],
 'cat-right':[(12,28),(11,29),(13,29)],
 'dog-left':[(27,22)],
 'dog-right':[(32,23)],
 'rabbit-left':[(41,29),(43,29),(42,30),(41,31),(43,31)],
 'rabbit-right':[(47,30),(49,30),(48,31),(47,32),(49,32)],
 'bear-left':[(20,40),(22,40),(21,41),(20,42),(22,42)],
 'bear-right':[(29,39),(31,39),(30,40),(29,41),(31,41)]
}
muzzles={'cat':(8,32,12,35),'dog':(27,24,31,27),'bear':(23,44,29,49)}
noses=[(10,34),(29,25),(25,46),(26,46),(27,46),(26,47)]
rabbit_mouth=[(44,33),(45,33),(46,33),(45,34)]
ears=[(17,34),(18,34),(17,35),(18,35),(32,33),(33,33),(32,34),(33,34)]

im=Image.new('RGBA',(56,56))
for y in range(56):
 for x in range(56):
  a=source.getpixel((x,y))[3]
  if not a:continue
  if body.getpixel((x,y)):
   a=0 if a==255 else 255
  elif y<14:
   a=255 if a==255 else 104
  else:
   a=104
  im.putpixel((x,y),(255,255,255,a)if a else(0,0,0,0))

for x0,y0,x1,y1 in muzzles.values():
 for y in range(y0,y1+1):
  for x in range(x0,x1+1):
   if source.getpixel((x,y))[3]==104:im.putpixel((x,y),(255,255,255,104))
for point in ears:
 assert source.getpixel(point)[3]==104,point
 im.putpixel(point,(255,255,255,104))
for points in [*eyes.values(),noses,rabbit_mouth]:
 for point in points:
  assert source.getpixel(point)[3]==104,point
  im.putpixel(point,(255,255,255,255))

# Local cat edits only: sharpen existing ear tips by 1-2 pixels, add two short
# cheek whiskers, and replace the tiny closed muzzle with a split kitten mouth.
cat_edits={
 (3,22):255,(3,23):255,(4,23):104,(4,24):255,(5,25):104,
 (13,21):255,(12,22):255,(14,22):255,
 (4,26):104,(5,27):104,(13,24):104,(12,25):104,
 (5,33):104,(6,33):104,(14,31):104,(15,31):104,
}
for y in range(32,36):
 for x in range(8,13):
  cat_edits[(x,y)]=0
cat_edits.update({(9,32):255,(10,32):255,(11,32):255,(10,33):255,
                  (10,34):255,(8,34):255,(9,35):255,(11,35):255,(12,34):255})
for p,a in cat_edits.items():im.putpixel(p,(255,255,255,a)if a else(0,0,0,0))

# Half-tone is tied to actual inner-ear/collar forms, never to y-bands.
secondary_lines={
 'dog-left-ear-seam':[(24,25),(24,26),(24,27),(24,28)],
 'dog-right-ear-seam':[(34,y)for y in range(23,30)],
 'dog-neck-behind-bear':[(24,30),(24,31),(24,32),(24,33),(25,34),(26,34),(27,33)],
 'rabbit-inner-ear':[(44,y)for y in range(19,26)],
}
secondary_patches={
 'dog-ear-fold':[(22,y)for y in range(25,29)]+[(23,28)],
 'rabbit-ear-fold':[(43,y)for y in range(21,25)],
}
for points in [*secondary_lines.values(),*secondary_patches.values()]:
 for p in points:
  if source.getpixel(p)[3]:im.putpixel(p,(255,255,255,104))
assert all(source.getpixel((x,y))[3]or not im.getpixel((x,y))[3]
           for y in range(56)for x in range(56)if(x,y)not in cat_edits)
for points in eyes.values():
 for point in points:assert im.getpixel(point)==(255,255,255,255)
im.save(DEST.with_suffix('.png'))

runs=defaultdict(list)
for y in range(56):
 x=0
 while x<56:
  a=im.getpixel((x,y))[3]
  if not a:x+=1;continue
  end=x+1
  while end<56 and im.getpixel((end,y))[3]==a:end+=1
  runs[a].append(f'M{x} {y}h{end-x}v1h-{end-x}Z')
  x=end
svg=['<svg xmlns="http://www.w3.org/2000/svg" width="56" height="56" viewBox="0 0 56 56" shape-rendering="crispEdges">',
 '<title>人偶操控 · 实白主线与半透明细节</title>',
 '<desc>保留原版眼形和猫咪微调。实白主轮廓及表情，104/255 半透明内耳、口鼻边线和悬丝，透明面部，未烘焙辉光。</desc>']
for a,commands in sorted(runs.items()):svg.append(f'<path fill="#fff" fill-opacity="{a/255:.8f}" d="{"".join(commands)}"/>')
svg.append('</svg>')
DEST.with_suffix('.svg').write_text('\n'.join(svg)+'\n',encoding='utf-8')
(HERE/'DollControlSkill.pixel.json').write_text(json.dumps({
 'size':[56,56],'source':'DollControlSkill.before.svg',
 'source_note':(HERE/'pixel-design.md').read_text(encoding='utf-8-sig'),
 'body_regions':regions,'eyes':eyes,'muzzles':muzzles,'noses':noses,
 'cat_edits':[{'xy':p,'alpha':a}for p,a in cat_edits.items()],
 'secondary_lines':secondary_lines,'secondary_patches':secondary_patches,
 'contour_alpha':{'primary':255,'secondary':104,'fill':0},
 'alpha_grid':[[im.getpixel((x,y))[3] for x in range(56)]for y in range(56)]},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Preserved all 8 eye shapes/positions; geometry edits confined to cat ears, cheeks and muzzle.')
