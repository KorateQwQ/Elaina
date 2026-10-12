"""Render the flight SVG and compare its native-size readability to the original."""
from pathlib import Path
import subprocess
import sys
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
SOURCE = ROOT / 'ElainaModSkills/Skills/Flight/FlightSkill.svg'
CONVERTER = Path.home() / '.codex/skills/svg-to-png/scripts/convert_svg.py'

def render(source, output, size):
    subprocess.run([sys.executable, str(CONVERTER), str(source), str(output),
                    '--width', str(size), '--overwrite'], check=True)

render(SOURCE, SOURCE.with_suffix('.png'), 56)
render(SOURCE, HERE / 'FlightSkill.large.png', 560)
render(HERE / 'FlightSkill.before.svg', HERE / 'FlightSkill.before.png', 280)
preview = Image.new('RGBA', (560, 560), '#252332')
preview.alpha_composite(Image.open(HERE / 'FlightSkill.large.png').convert('RGBA'))
preview.convert('RGB').save(HERE / 'FlightSkill.preview.png')

proof = Image.new('RGBA', (920, 550), '#211f2c')
draw = ImageDraw.Draw(proof)
font_path = 'C:/Windows/Fonts/msyh.ttc'
def font(size):
    return ImageFont.truetype(font_path, size)
draw.text((30, 22), '飞行魔法 / 伊蕾娜的扫帚', font=font(25), fill='#f1edf6')
draw.text((30, 61), '白色线稿 · 透明底 · 弯柄 / 银边包布 / 蝴蝶结 / 枝束', font=font(14), fill='#aca7ba')
for x, label in [(30, '原图'), (330, '重绘'), (644, '尺寸检查')]:
    draw.text((x, 112), label, font=font(17), fill='#ded7ed')
proof.alpha_composite(Image.open(HERE / 'FlightSkill.before.png').convert('RGBA'), (22, 157))
proof.alpha_composite(Image.open(HERE / 'FlightSkill.large.png').convert('RGBA').resize((280,280), Image.Resampling.LANCZOS), (323, 157))
new = Image.open(SOURCE.with_suffix('.png')).convert('RGBA')
for x, size in [(653,32),(716,40),(789,56)]:
    draw.rounded_rectangle((x-8,177,x+size+8,177+72),radius=8,fill='#302d41',outline='#484259')
    img = new if size == 56 else new.resize((size,size),Image.Resampling.LANCZOS)
    proof.alpha_composite(img,(x,185+(56-size)//2))
    draw.text((x,263),str(size)+' px',font=font(12),fill='#b8b0c8')
proof.alpha_composite(new.resize((168,168),Image.Resampling.NEAREST),(681,315))
draw.text((672,490),'56 px · 最近邻放大 3 倍',font=font(12),fill='#b8b0c8')
draw.text((30,490),'离线 SVG 渲染预览',font=font(13),fill='#8f879e')
proof.convert('RGB').save(HERE / 'FlightSkill.comparison.png')
assert new.size == (56,56)
a = new.getchannel('A')
assert a.getextrema()[0] == 0
bbox = a.getbbox()
assert bbox and bbox[0] > 0 and bbox[1] > 0 and bbox[2] < 56 and bbox[3] < 56, bbox
assert all((r,g,b)==(255,255,255) for r,g,b,alpha in new.getdata() if alpha)
print('Verified: 56x56 RGBA, white-only visible pixels, transparent padding, content bounds', bbox)
