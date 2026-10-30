"""Assemble water ball burst frames from `run.ps1 -BallFrames` into GIFs."""
import os
import sys
from PIL import Image, ImageDraw, ImageFont

root = os.path.abspath(os.path.join(os.path.dirname(__file__), '../..'))
out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(root, '.vissandbox/water-laser-preview/out-clear')
frames_dir = os.path.join(out, 'frames')
# Probe centers: circle at (600, 350), cone/launch at (300, 350). Red boxes in cone are the T-shaped hitbox.
crops = {'circle': (270, 20, 930, 680), 'cone': (170, 20, 1010, 680), 'launch': (170, 20, 830, 680)}
labels = {'circle': '近距离爆炸（圆形）', 'cone': '飞行后爆炸（锥形）', 'launch': '发射'}
scale = 0.6
panel = int(660 * scale)
widths = {kind: int((box[2] - box[0]) * scale) for kind, box in crops.items()}
order = ('launch', 'circle', 'cone')
try:
    font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 20)
except OSError:
    font = ImageFont.load_default()

frames = []
for tick in range(33):
    sheet = Image.new('RGB', (sum(widths.values()), panel))
    draw = ImageDraw.Draw(sheet)
    x = 0
    for kind in order:
        image = Image.open(os.path.join(frames_dir, f'{kind}-{tick:02d}.png')).convert('RGB')
        sheet.paste(image.crop(crops[kind]).resize((widths[kind], panel), Image.LANCZOS), (x, 0))
        draw.text((x + 10, 8), labels[kind], font=font, fill=(240, 240, 240))
        x += widths[kind]
    draw.text((sheet.width - 110, sheet.height - 32), f'tick {tick:02d}', font=font, fill=(240, 240, 240))
    frames.append(sheet)

for name, duration in (('water-ball-burst.gif', 20), ('water-ball-burst-slow.gif', 60)):
    # Hold the last (empty) frame briefly so the loop restart is readable.
    durations = [duration] * (len(frames) - 1) + [400]
    frames[0].save(os.path.join(out, name), save_all=True, append_images=frames[1:], duration=durations, loop=0)
    print(os.path.join(out, name))
