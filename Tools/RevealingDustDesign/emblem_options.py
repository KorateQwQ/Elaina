"""Make two gentle alternatives to the eye: revealed outline and dusty footprints."""
from pathlib import Path
import re
import sys
from urllib.parse import quote, unquote

from PIL import Image, ImageDraw, ImageFont

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from render import ROOT, ASSETS, convert, raster, main as sync_main

REVEAL = '''  <!-- A broken contour becomes a solid magical marker as dust touches it. -->
  <path d="m21.5 26.8 6.1 5.8-6.1 5.9" fill="none" stroke="#97779f" stroke-width="1.65" stroke-linecap="round" stroke-linejoin="round"/>
  <path d="m21.5 26.8 6.1 5.8-6.1 5.9" fill="none" stroke="#fff3d7" stroke-width="1" stroke-linecap="round" stroke-linejoin="round"/>
  <path d="m19.9 28.2-3.1 3m-.6 2.6 3.5 3.3" fill="none" stroke="#98739e" stroke-width=".82" stroke-linecap="round" stroke-dasharray=".5 1.5"/>
  <path d="m22.4 29.2 3.5 3.4-3.5 3.4" fill="none" stroke="#f6e3b7" stroke-width=".48" stroke-linecap="round" stroke-linejoin="round"/>
  <path d="m20.9 29.9.72 1.98 1.98.72-1.98.72-.72 1.98-.72-1.98-1.98-.72 1.98-.72Z" fill="#fff5de" stroke="#a480ab" stroke-width=".48" stroke-linejoin="round"/>
  <path d="m25.9 26.2.38 1.06 1.06.38-1.06.38-.38 1.06-.38-1.06-1.06-.38 1.06-.38Z" fill="#e9d2ee" stroke="#9b7aa5" stroke-width=".35" stroke-linejoin="round"/>
  <circle cx="15.6" cy="35.6" r=".56" fill="#e6c5e7"/>
  <circle cx="17.4" cy="37.7" r=".4" fill="#fff2cc"/>
  <circle cx="18.1" cy="29.6" r=".35" fill="#f8e7bf"/>
'''

FOOTPRINTS = '''  <!-- Two small boot prints emerge from a scatter of revealing dust. -->
  <path d="M17.6 36.5c-1.1-1.1-1.8-2.9-1.2-4.2.5-1.1 1.8-1.4 2.5-.6.6.7.6 1.9 1.2 2.7l.9 1.1Z" fill="#fff0ce" stroke="#96719e" stroke-width=".65" stroke-linejoin="round"/>
  <path d="m18 37.3 3-1 .55 1.1c.25.6-.15 1.2-1.1 1.5-.9.3-1.5.1-1.8-.45Z" fill="#fff0ce" stroke="#96719e" stroke-width=".6" stroke-linejoin="round"/>
  <path d="M22.5 32.3c.4-1.3 1-2.1.9-3.1-.1-1.1.5-1.8 1.4-1.7 1.4.1 1.8 1.5 1.3 3-.3.9-.8 1.6-1.4 2.5Z" fill="#fff3d8" stroke="#96719e" stroke-width=".65" stroke-linejoin="round"/>
  <path d="m22.2 33.2 2.2.8-.5 1.2c-.25.6-.8.8-1.5.55-.75-.25-1.05-.7-.85-1.25Z" fill="#fff3d8" stroke="#96719e" stroke-width=".6" stroke-linejoin="round"/>
  <path d="m20.8 25.7.4 1.2 1.2.4-1.2.4-.4 1.2-.4-1.2-1.2-.4 1.2-.4Z" fill="#f6e0f2" stroke="#a381aa" stroke-width=".4" stroke-linejoin="round"/>
  <circle cx="15.3" cy="38.8" r=".45" fill="#dec3e5"/>
  <circle cx="14.9" cy="35.9" r=".4" fill="#fff2d1"/>
  <circle cx="26.5" cy="35.1" r=".4" fill="#ecd3ee"/>
'''


def main():
    original = (HERE / 'RevealingDust_EyeVersion.svg').read_text(encoding='utf-8')
    original = re.sub(r'    <linearGradient id="iris".*?</linearGradient>\n', '', original, flags=re.S)
    marker = r'  <!-- The open eye.*?(?=  <!-- A restrained revealing trace)'
    variants = [
        ('RevealingDust_ContourDraft', REVEAL, 'A honey-gold pouch of revealing powder. Stardust turns a faint dotted contour into a clear luminous marker.'),
        ('RevealingDust_Footprints', FOOTPRINTS, 'A honey-gold pouch of revealing powder, embroidered with two small boot prints and soft magical dust.'),
    ]
    for name, emblem, description in variants:
        svg, count = re.subn(marker, lambda _: emblem + '\n', original, flags=re.S)
        assert count == 1, 'Eye block not found exactly once'
        svg = re.sub(r'<desc id="revealing-desc">.*?</desc>', '<desc id="revealing-desc">' + description + '</desc>', svg)
        (HERE / (name + '.svg')).write_text(svg, encoding='utf-8', newline='\n')
    sync_main()
    alternative = HERE / 'RevealingDust_Footprints.svg'
    raster(alternative, HERE / 'RevealingDust_Footprints_500.png', 500)
    pencil = unquote(convert('data:image/svg+xml,' + quote(alternative.read_text(encoding='utf-8')), profile='hatched').split(',', 1)[1])
    pencil = '\n'.join(line.rstrip() for line in pencil.splitlines()).rstrip() + '\n'
    alt_pencil = HERE / 'RevealingDust_Footprints_Pencil.svg'
    alt_pencil.write_text(pencil, encoding='utf-8', newline='\n')
    raster(alt_pencil, HERE / 'RevealingDust_Footprints_Pencil.png', 100)
    proof = Image.new('RGBA', (900, 690), '#272237')
    draw = ImageDraw.Draw(proof)
    font_path = Path('C:/Windows/Fonts/msyh.ttc')
    def font(size):
        return ImageFont.truetype(str(font_path), size)
    draw.text((36, 26), '显迹尘  /  更温和的两种表达', font=font(25), fill='#efe4f2')
    draw.text((36, 68), '用星尘与显现的痕迹，表达侦察和发现。', font=font(16), fill='#b9a9c6')
    options = [
        ('A · 星尘显影', '虚线轮廓被粉尘逐渐点亮', HERE/'RevealingDust_500.png', ASSETS/'RevealingDust_Pencil.png'),
        ('B · 星光足迹', '小小的足印从尘埃中浮现', HERE/'RevealingDust_Footprints_500.png', HERE/'RevealingDust_Footprints_Pencil.png'),
    ]
    for i, (title, subtitle, large, small) in enumerate(options):
        x = 28 + i*442
        draw.rounded_rectangle((x, 109, x+400, 663), radius=14, fill='#2d273e', outline='#5e4d6c', width=1)
        draw.text((x+200, 127), title, font=font(23), anchor='mt', fill='#eee0ef')
        draw.text((x+200, 165), subtitle, font=font(15), anchor='mt', fill='#c0aeca')
        with Image.open(large) as image:
            proof.alpha_composite(image.resize((300,300), Image.Resampling.LANCZOS), (x+50,192))
        draw.line((x+30,506,x+370,506), fill='#574663')
        draw.text((x+200, 522), '手记彩铅 · 实际 100 × 100', font=font(14), anchor='mt', fill='#b7a6c3')
        with Image.open(small) as image:
            proof.alpha_composite(image.convert('RGBA'), (x+150,549))
    proof.convert('RGB').save(HERE/'RevealingDust_EmblemOptions.png')
    print('Saved exploratory drafts and comparison; the approved source SVG is preserved.')


if __name__ == '__main__':
    main()
