"""Hand-authored 56px doll icon; export identical editable SVG and RGBA PNG."""
from pathlib import Path
from collections import defaultdict
import json
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
DEST = ROOT / 'ElainaModSkills/Skills/DollControl/DollControlSkill'
im = Image.new('RGBA', (56, 56))
draw = ImageDraw.Draw(im)
layers = []

def line(points, alpha=224, name='outline'):
    draw.line(points, fill=(255,255,255,alpha), width=1)
    layers.append({'name':name,'points':points,'alpha':alpha})

def dots(points, alpha=255, name='face'):
    for p in points:
        draw.point(p, fill=(255,255,255,alpha))
    layers.append({'name':name,'pixels':points,'alpha':alpha})

def cross(x,y):
    dots([(x,y),(x+2,y),(x+1,y+1),(x,y+2),(x+2,y+2)])

# Three distinct hanging strings. The fourth toy hangs from the central one.
line([(28,8),(12,24)],80,'strings')
line([(28,8),(41,17)],80,'strings')
line([(28,9),(28,18)],80,'strings')
line([(28,33),(28,34)],80,'strings')
line([(9,39),(11,42),(15,44)],80,'motion')
line([(46,39),(44,42),(41,44)],80,'motion')

# Cat: deliberate horizontal/vertical steps and two separated stitched eyes.
line([(4,23),(7,24),(9,26),(13,25),(16,23),(16,28),
      (17,30),(17,33),(15,35),(8,35),(5,33),(4,29),(4,23)])
line([(6,26),(7,27)],144,'cat-inner-ear')
line([(14,27),(14,26)],144,'cat-inner-ear')
cross(7,29)
cross(12,29)
dots([(10,32),(10,33)])
dots([(9,33),(11,33)],224,'cat-mouth')

# Dog: cap, long drooping ears, and a small lower jaw.
line([(24,21),(25,20),(31,20),(33,22)])
line([(24,22),(22,22),(21,24),(21,28),(22,30),(23,30),(24,28),(24,22)])
line([(33,22),(35,23),(36,25),(36,29),(35,30),(34,30),(33,28),(33,22)])
line([(25,29),(26,31),(31,31),(32,29)])
line([(26,23),(26,24)],255,'dog-eye')
line([(31,23),(31,24)],255,'dog-eye')
dots([(27,27),(28,27),(29,27),(28,28)])
dots([(28,29)],224,'dog-mouth')

# Rabbit: ears remain separate; each eye occupies a complete 3x3 grid.
line([(40,27),(39,23),(39,19),(40,17),(41,17),(42,19),(43,26),
      (45,26),(48,21),(50,20),(51,21),(50,24),(48,28),
      (51,29),(52,31),(51,34),(49,36),(43,36),(40,34),(39,31),(40,27)])
line([(41,20),(41,23)],144,'rabbit-inner-ear')
line([(48,24),(47,25)],144,'rabbit-inner-ear')
cross(41,29)
cross(47,29)
dots([(45,32),(45,33)])
dots([(44,34),(46,34)],224,'rabbit-mouth')

# Teddy: prominent circular ears and broad forehead, no oval or boxed snout.
line([(20,39),(18,37),(18,35),(20,33),(22,33),(24,35),
      (31,35),(33,33),(35,33),(37,35),(37,37),(35,39),
      (36,41),(36,46),(34,49),(31,51),(24,51),(21,49),
      (19,46),(19,42),(20,39)])
line([(20,36),(21,35),(22,36)],144,'bear-inner-ear')
line([(33,36),(34,35),(35,36)],144,'bear-inner-ear')
cross(22,39)
cross(30,39)
# A triangular embroidered nose, with an open W mouth three rows below.
dots([(26,43),(27,43),(28,43),(27,44)])
line([(27,45),(27,46)],224,'bear-mouth')
line([(24,46),(25,47),(26,47),(27,46),(28,47),(29,47),(30,46)],224,'bear-mouth')

# Small controller star; all decorative marks use explicit integer pixels.
line([(28,4),(28,9)],224,'controller')
line([(25,7),(31,7)],224,'controller')
dots([(28,7)])
line([(8,10),(8,12)],144,'sparkle')
line([(7,11),(9,11)],144,'sparkle')
line([(47,10),(47,12)],144,'sparkle')
line([(46,11),(48,11)],144,'sparkle')
dots([(37,5),(12,49),(45,48)],80,'sparkle')

im.save(DEST.with_suffix('.png'))

# Integer horizontal runs produce a compact SVG with an exact 1:1 PNG match.
runs = defaultdict(list)
for y in range(56):
    x = 0
    while x < 56:
        alpha=im.getpixel((x,y))[3]
        if not alpha:
            x += 1
            continue
        end=x+1
        while end < 56 and im.getpixel((end,y))[3] == alpha:
            end += 1
        runs[alpha].append(f'M{x} {y}h{end-x}v1h-{end-x}Z')
        x=end
svg=['<svg xmlns="http://www.w3.org/2000/svg" width="56" height="56" viewBox="0 0 56 56" shape-rendering="crispEdges">',
     '  <title>人偶操控 · 像素线稿</title>',
     '  <desc>四只悬丝玩偶。整数像素轮廓、清晰叉形眼睛、无封闭口鼻框的小熊脸。透明底，无预烘焙辉光。</desc>']
for alpha,commands in sorted(runs.items()):
    svg.append(f'  <path fill="#fff" fill-opacity="{alpha/255:.8f}" d="{"".join(commands)}"/>')
svg.append('</svg>')
DEST.with_suffix('.svg').write_text('\n'.join(svg)+'\n',encoding='utf-8')
(HERE/'DollControlSkill.pixel.json').write_text(json.dumps({
    'size':[56,56], 'source_note':(HERE/'pixel-design.md').read_text(encoding='utf-8'),
    'layers':layers},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Exported hand-authored 56x56 SVG, PNG and editable pixel layers.')
