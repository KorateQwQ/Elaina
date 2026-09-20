"""Premultiplied, supersampled line profiles matching the old HTML's SVG strokes.

Long profiles are sliced into two end caps and a stretchable middle at runtime.
Short dashed shadows use their actual 4px length to avoid blurring across gaps.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).with_name('Assets')
SCALE = 8

def profile(name, length, width, padding, blur):
    size = (round((length + padding * 2) * SCALE), round((width + padding * 2) * SCALE))
    mask = Image.new('L', size)
    draw = ImageDraw.Draw(mask)
    draw.rectangle((round(padding * SCALE), round(padding * SCALE),
                    round((padding + length) * SCALE) - 1, round((padding + width) * SCALE) - 1), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(blur * SCALE))
    Image.merge('RGBA', (mask, mask, mask, mask)).save(OUT / (name + '.png'))

for name, width in [('Locked', 1), ('Learned', 1.2), ('Related', 1.8)]:
    profile('Link' + name, 3, width, 1.5, .12)
profile('LinkLearnedGlow', 24, 1.2, 12, 4)
profile('LinkRelatedGlow', 18, 1.8, 9, 3)
profile('LinkRelatedDashGlow', 4, 1.8, 9, 3)
print('Generated six premultiplied SVG stroke profiles.')
