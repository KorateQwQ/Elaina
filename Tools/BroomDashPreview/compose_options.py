import sys
from pathlib import Path
from PIL import Image, ImageDraw
out = Path(sys.argv[1])
labels = {'A': 'A 现状（起点双环）', 'B': 'B 环接力', 'C': 'C 起点双环 + 气流拖尾', 'D': 'D 环接力 + 气流拖尾'}
ticks = [2, 6, 12, 20, 27, 34]
for v in 'ABCD':
    frames = [Image.open(out / f'{v}-{t:03d}.png').convert('RGB') for t in range(46)]
    frames[0].save(out / f'{v}.gif', save_all=True, append_images=frames[1:], duration=[16, 17, 17] * 15 + [17], loop=0)
# 每个方案一列：同一 tick 横向对齐，便于比较。
cw, ch = 400, 150
sheet = Image.new('RGB', (cw * 4, 30 + ch * len(ticks)), (12, 16, 24))
d = ImageDraw.Draw(sheet)
try:
    from PIL import ImageFont
    font = ImageFont.truetype('msyh.ttc', 15)
except Exception:
    font = None
for c, v in enumerate('ABCD'):
    d.text((c * cw + 8, 6), labels[v], fill='white', font=font)
    for r, t in enumerate(ticks):
        im = Image.open(out / f'{v}-{t:03d}.png').convert('RGB').crop((200, 60, 1000, 360)).resize((cw, ch // 1 - 0))
        sheet.paste(im, (c * cw, 30 + r * ch))
        d.text((c * cw + 6, 30 + r * ch + 4), f'tick {t}', fill=(200, 200, 200), font=font)
sheet.save(out / 'compare.png')
extra = Image.new('RGB', (1200, 360), (12, 16, 24))
for i, name in enumerate(['C-diag', 'D-diag', 'C-bright', 'D-bright']):
    im = Image.open(out / f'{name}.png').convert('RGB').crop((200, 0, 1000, 360)).resize((600, 180))
    extra.paste(im, ((i % 2) * 600, (i // 2) * 180))
    ImageDraw.Draw(extra).text(((i % 2) * 600 + 8, (i // 2) * 180 + 6), name, fill='white', font=font)
extra.save(out / 'extra.png')
# 2× 近景：检查拖尾是否从扫帚毛长出（人物在 x=760,y=190）。
close = Image.new('RGB', (1200, 720), (12, 16, 24))
for i, name in enumerate(['C-006', 'C-012', 'D-012', 'C-left']):
    im = Image.open(out / f'{name}.png').convert('RGB')
    box = (520, 130, 820, 250) if 'left' not in name else (700, 130, 1000, 250)
    close.paste(im.crop(box).resize((600, 240), Image.NEAREST), ((i % 2) * 600, (i // 2) * 240))
    ImageDraw.Draw(close).text(((i % 2) * 600 + 8, (i // 2) * 240 + 6), name, fill='white', font=font)
for i, name in enumerate(['C-diag', 'D-diag']):
    im = Image.open(out / f'{name}.png').convert('RGB')
    close.paste(im.crop((560, 100, 960, 260)).resize((600, 240), Image.NEAREST), (i * 600, 480))
    ImageDraw.Draw(close).text((i * 600 + 8, 486), name, fill='white', font=font)
close.save(out / 'closeup.png')
print(out / 'compare.png')
