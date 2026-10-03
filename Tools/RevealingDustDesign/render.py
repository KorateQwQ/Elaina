"""Export the reviewed RevealingDust SVG and its notebook derivatives only."""
from pathlib import Path
import subprocess
import sys
from urllib.parse import quote, unquote

from PIL import Image, ImageDraw, ImageFont

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
ASSETS = ROOT / 'ElainaModAlchemy/item/ExampleAssets'
UI = ROOT / 'ElainaModAlchemy/UI/Assets'
CONVERTER = Path.home() / '.codex/skills/svg-to-png/scripts/convert_svg.py'
sys.path.insert(0, str(ROOT / 'Tools/AlchemyPlan'))
sys.path.insert(0, str(ROOT / 'Tools/AlchemyPreview'))
from pencil_style import convert
from prepare_pencil import premultiply


def raster(source, destination, size):
    subprocess.run([sys.executable, str(CONVERTER), str(source), str(destination),
                    '--width', str(size), '--height', str(size), '--overwrite'], check=True)


def main():
    name = 'RevealingDust'
    original = (HERE / (name + '.svg')).read_text(encoding='utf-8').rstrip() + '\n'
    (HERE / (name + '.svg')).write_text(original, encoding='utf-8', newline='\n')
    (ASSETS / (name + '.svg')).write_text(original, encoding='utf-8', newline='\n')
    pencil = unquote(convert('data:image/svg+xml,' + quote(original), profile='hatched').split(',', 1)[1])
    source = ROOT / 'Tools/AlchemyPlan/generated' / (name + '_Pencil.svg')
    pencil = '\n'.join(line.rstrip() for line in pencil.splitlines()).rstrip() + '\n'
    source.write_text(pencil, encoding='utf-8', newline='\n')
    (ASSETS / (name + '_Pencil.svg')).write_text(pencil, encoding='utf-8', newline='\n')
    raster(source, ASSETS / (name + '_Pencil.png'), 100)
    with Image.open(ASSETS / (name + '_Pencil.png')) as image:
        ui = premultiply(image)
        ui.save(UI / 'Pencil' / (name + '_Pencil.png'))
        alpha = ui.getchannel('A')
        Image.merge('RGBA', (alpha, alpha, alpha, alpha)).save(UI / 'Silhouettes' / (name + '_Silhouette.png'))

    raster(ASSETS / (name + '.svg'), HERE / (name + '_500.png'), 500)
    raster(source, HERE / (name + '_Pencil_500.png'), 500)
    raster(ASSETS / 'AshenFacsimileDust.svg', HERE / 'AshenFacsimileDust_Reference.png', 500)
    raster(HERE / (name + '_Before.svg'), HERE / (name + '_Before_500.png'), 500)
    # This is an offline asset comparison, not a game screenshot.
    proof = Image.new('RGBA', (1140, 750), '#272237')
    draw = ImageDraw.Draw(proof)
    font_path = next((p for p in [Path('C:/Windows/Fonts/msyh.ttc'), Path('C:/Windows/Fonts/simhei.ttf')] if p.exists()), None)
    def font(size):
        return ImageFont.truetype(str(font_path), size) if font_path else ImageFont.load_default(size=size)
    draw.text((40, 26), '显迹尘  /  SVG 造型与手记彩铅对照', font=font(26), fill='#efe4f2')
    draw.text((40, 67), '离线资产预览 · SVG 保持 50 × 50 画布 · 透明背景', font=font(15), fill='#b6a7c4')
    columns = [
        ('灰赝尘 · 风格参考', HERE / 'AshenFacsimileDust_Reference.png', ASSETS / 'AshenFacsimileDust_Pencil.png'),
        ('显迹尘 · 原版', HERE / (name + '_Before_500.png'), HERE / (name + '_Before_Pencil.png')),
        ('显迹尘 · 重绘', HERE / (name + '_500.png'), ASSETS / (name + '_Pencil.png')),
    ]
    for index, (label, svg_png, pencil_png) in enumerate(columns):
        left = 30 + index * 370
        draw.rounded_rectangle((left, 108, left + 340, 706), radius=12, fill='#2d273e', outline='#534562', width=1)
        draw.text((left + 170, 132), label, anchor='mt', font=font(21), fill='#e7daef')
        with Image.open(svg_png) as icon:
            proof.alpha_composite(icon.convert('RGBA').resize((300, 300), Image.Resampling.LANCZOS), (left + 20, 169))
        draw.line((left + 28, 486, left + 312, 486), fill='#534562', width=1)
        draw.text((left + 170, 505), '手记彩铅 · 实际 100 × 100', anchor='mt', font=font(15), fill='#b6a7c4')
        draw.ellipse((left + 100, 546, left + 240, 686), outline='#665674', width=1)
        with Image.open(pencil_png) as icon:
            assert icon.size == (100, 100)
            proof.alpha_composite(icon.convert('RGBA'), (left + 120, 566))
    proof.convert('RGB').save(HERE / 'RevealingDust_Comparison.png')
    pencil_comparison()
    print('Updated RevealingDust only; preview saved to', HERE / 'RevealingDust_Comparison.png')


def pencil_comparison():
    before = HERE / 'RevealingDust_BeforePencilRefine_Pencil.svg'
    if not before.exists():
        return
    before_large = HERE / 'RevealingDust_BeforePencilRefine_500.png'
    raster(before, before_large, 500)
    proof = Image.new('RGBA', (900, 726), '#272237')
    draw = ImageDraw.Draw(proof)
    font_path = next(p for p in (Path('C:/Windows/Fonts/msyh.ttc'), Path('C:/Windows/Fonts/simhei.ttf')) if p.exists())
    def font(size):
        return ImageFont.truetype(str(font_path), size)
    draw.text((34, 24), '显迹尘  /  星尘显影 · 彩铅细化', font=font(25), fill='#efe4f2')
    draw.text((34, 64), '断续叠描  ·  疏密排线  ·  星纹留白', font=font(16), fill='#beaccb')
    variants = [
        ('调整前', '规则斜线与较硬的轮廓', before_large, HERE/'RevealingDust_BeforePencilRefine_Pencil.png'),
        ('调整后', '细铅笔触与渐显星纹', HERE/'RevealingDust_Pencil_500.png', ASSETS/'RevealingDust_Pencil.png'),
    ]
    for i, (title, subtitle, large, small) in enumerate(variants):
        x = 28 + i * 442
        draw.rounded_rectangle((x, 106, x+400, 698), radius=13, fill='#2d273e', outline='#5b4a69')
        draw.text((x+200, 124), title, font=font(21), fill='#eaddee', anchor='mt')
        draw.text((x+200, 158), subtitle, font=font(14), fill='#b9a8c6', anchor='mt')
        with Image.open(large) as image:
            proof.alpha_composite(image.convert('RGBA').resize((310,310), Image.Resampling.LANCZOS), (x+45,183))
        draw.line((x+29, 515, x+371, 515), fill='#574765')
        draw.text((x+200, 532), '手记显示 · 100 × 100', font=font(14), fill='#b7a6c3', anchor='mt')
        draw.ellipse((x+138, 559, x+262, 683), outline='#63516f')
        with Image.open(small) as image:
            proof.alpha_composite(image.convert('RGBA'), (x+150,571))
    proof.convert('RGB').save(HERE/'RevealingDust_PencilRefinement.png')


if __name__ == '__main__':
    main()
