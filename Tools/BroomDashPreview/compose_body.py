import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
out = Path(sys.argv[1])
labels = {'C': 'C 现状（扫帚尾部）', 'E': 'E 整体气流带', 'F': 'F 尾流扩散', 'G': 'G 速度线'}
scenes = {'right': '水平冲刺', 'diag': '左下冲刺（截图角度）', 'turn': '右上冲刺后按下转向', 'freeze': '同上 · 冲刺后拖尾留在原地收拢'}
ticks = [3, 8, 14, 22, 28, 34]
# 转向场景冲刺在 tick 24 结束，多取转弯阶段。
scene_ticks = {'turn': [8, 14, 22, 26, 29, 32], 'freeze': [8, 14, 22, 26, 29, 32]}
try:
    font = ImageFont.truetype('msyh.ttc', 15)
except Exception:
    font = None
for s in scenes:
    for v in 'CEFG':
        frames = [Image.open(out / f'{s}-{v}-{t:03d}.png').convert('RGB') for t in range(46)]
        frames[0].save(out / f'{s}-{v}.gif', save_all=True, append_images=frames[1:], duration=[16, 17, 17] * 15 + [17], loop=0)
    # 每个方案一列，同一 tick 横向对齐。
    cw, ch = 400, 233
    sheet = Image.new('RGB', (cw * 4, 30 + ch * len(ticks)), (12, 16, 24))
    d = ImageDraw.Draw(sheet)
    ts = scene_ticks.get(s, ticks)
    for c, v in enumerate('CEFG'):
        d.text((c * cw + 8, 6), f'{labels[v]} · {scenes[s]}', fill='white', font=font)
        for r, t in enumerate(ts):
            im = Image.open(out / f'{s}-{v}-{t:03d}.png').convert('RGB').resize((cw, ch), Image.LANCZOS)
            sheet.paste(im, (c * cw, 30 + r * ch))
            d.text((c * cw + 6, 30 + r * ch + 4), f'tick {t}', fill=(200, 200, 200), font=font)
    sheet.save(out / f'compare-{s}.png')
# 1.5× 近景（接近游戏缩放）：人物在画面 (480,280)+冲刺方向×170，取人物与身后拖尾。
import math
dirs = {'right': (1, 0), 'diag': (-.7071, .7071), 'turn': (.7071, -.7071), 'freeze': (.7071, -.7071)}
picks = [('right', 14), ('diag', 8), ('diag', 14), ('diag', 27), ('turn', 26), ('turn', 29), ('freeze', 26), ('freeze', 29), ('freeze', 32)]
cw, ch, z = 300, 200, 1.5
close = Image.new('RGB', (4 * int(cw * z), 30 + len(picks) * int(ch * z)), (12, 16, 24))
d = ImageDraw.Draw(close)
for c, v in enumerate('CEFG'):
    d.text((c * cw * z + 8, 6), labels[v], fill='white', font=font)
for row, (s, t) in enumerate(picks):
    dx, dy = dirs[s]
    rx, ry = 480 + dx * 170, 280 + dy * 170
    cx, cy = rx - dx * 90, ry - dy * 90
    box = tuple(int(x) for x in (cx - cw / 2, cy - ch / 2, cx + cw / 2, cy + ch / 2))
    for c, v in enumerate('CEFG'):
        im = Image.open(out / f'{s}-{v}-{t:03d}.png').convert('RGB').crop(box).resize((int(cw * z), int(ch * z)), Image.NEAREST)
        close.paste(im, (int(c * cw * z), 30 + row * int(ch * z)))
        d.text((c * cw * z + 6, 30 + row * ch * z + 4), f'{s} tick {t}', fill=(230, 230, 230), font=font)
close.save(out / 'closeup.png')
print(out / 'closeup.png')
