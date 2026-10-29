"""把 Program 输出的逐帧画面裁成泳道，拼出白天/夜晚对比图与实时 GIF。"""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont

out = Path(sys.argv[1])
frames = out / 'frames'
hits = {}
for line in (out / 'layout.txt').read_text(encoding='utf-8').splitlines():
    name, x, y = line.split()
    hits[name] = (round(float(x)), round(float(y)))

LOOKS = [
    ('current', '旧版', '改版前的 KL 粒子'),
    ('hit', '普通命中', '多刺爆点 + 细线\n+ 飘散星点'),
    ('burst', '第五击花印爆发', '一圈爆点 + 溅射环\n+ 花瓣'),
]
TICKS = [1, 2, 3, 5, 8, 12, 20, 35, 55]
BURST_TICK = 12
CELL_W, CELL_H = 360, 300
LABEL_W, HEADER_H = 190, 40
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 18)
small = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 14)


def crop(image, look):
    x, y = hits[look]
    return image.crop((x - 170, y - 170, x + 190, y + 130))


for bg, title in (('day', '白天'), ('night', '夜晚')):
    columns = [(f'{bg}-hit-{t:03}.png', f'第 {t} 帧') for t in TICKS]
    columns.append((f'{bg}-burst-{BURST_TICK:03}.png', '连发（每 3 帧一发）'))
    sheet = Image.new('RGB', (LABEL_W + CELL_W * len(columns), HEADER_H + CELL_H * len(LOOKS)), '#15161d')
    draw = ImageDraw.Draw(sheet)
    draw.text((12, 9), f'魔法飞弹命中 · {title}', fill='#f0c8ec', font=font)
    for c, (_, label) in enumerate(columns):
        draw.text((LABEL_W + c * CELL_W + 12, 9), label, fill='#d8d8e0', font=font)
    for c, (name, _) in enumerate(columns):
        image = Image.open(frames / name).convert('RGB')
        for r, (look, _, _) in enumerate(LOOKS):
            sheet.paste(crop(image, look), (LABEL_W + c * CELL_W, HEADER_H + r * CELL_H))
    for r, (_, name, note) in enumerate(LOOKS):
        y = HEADER_H + r * CELL_H
        draw.text((12, y + 14), name, fill='#f0c8ec', font=font)
        draw.multiline_text((12, y + 46), note, fill='#a8a8b8', font=small, spacing=6)
        draw.line((0, y, sheet.width, y), fill='#2a2b36')
    draw.text((12, HEADER_H + 2 * CELL_H + 120), '连发列：4 发普通\n命中 + 第 5 发', fill='#a8a8b8', font=small)
    sheet.save(out / f'{bg}-compare.png')

    # 实时 GIF（20 ms ≈ 50 fps，接近游戏 60 fps）：四条泳道并排，末尾停顿后循环
    for kind, count in (('hit', 80), ('burst', 60)):
        gif = []
        for t in range(1, count + 1):
            image = Image.open(frames / f'{bg}-{kind}-{t:03}.png').convert('RGB')
            strip = Image.new('RGB', (CELL_W * len(LOOKS), CELL_H + 30), '#15161d')
            label = ImageDraw.Draw(strip)
            for i, (look, name, _) in enumerate(LOOKS):
                strip.paste(crop(image, look), (i * CELL_W, 30))
                label.text((i * CELL_W + 10, 4), name, fill='#f0c8ec', font=small)
            gif.append(strip)
        durations = [20] * (count - 1) + [500]
        gif[0].save(out / f'{bg}-{kind}.gif', save_all=True, append_images=gif[1:], duration=durations, loop=0)

# 普通命中 2 倍局部（接近游戏常用缩放），便于对照游戏截图
for bg in ('day', 'night'):
    ticks = [2, 3, 5, 8]
    zoom = Image.new('RGB', (440 * len(ticks), 340), '#15161d')
    draw = ImageDraw.Draw(zoom)
    x, y = hits['hit']
    for i, t in enumerate(ticks):
        image = Image.open(frames / f'{bg}-hit-{t:03}.png').convert('RGB')
        zoom.paste(image.crop((x - 70, y - 75, x + 150, y + 75)).resize((440, 300), Image.BILINEAR), (i * 440, 40))
        draw.text((i * 440 + 12, 9), f'第 {t} 帧 · 2 倍', fill='#d8d8e0', font=font)
    zoom.save(out / f'{bg}-hit-zoom.png')

print('written:', ', '.join(p.name for p in sorted(out.glob('*.png')) + sorted(out.glob('*.gif'))))

# 正式拖尾 + 命中，直线、弯曲轨迹、花印中心与接触点错开 45 px。
for bg in ('day', 'night'):
    ticks = [1, 4, 8, 12, 16, 21, 27]
    names = ['普通命中 · 直线', '普通命中 · 弯曲轨迹', '花印爆发 · 接触点偏移']
    sheet = Image.new('RGB', (520 * 3, 210 * len(ticks)), '#15161d')
    draw = ImageDraw.Draw(sheet)
    gif = []
    for t in range(1, 51):
        frame = Image.open(frames / f'{bg}-trail-{t:03}.png').convert('RGB')
        strip = Image.new('RGB', (1560, 210), '#15161d')
        strip.paste(frame.crop((0, 230, 1560, 410)), (0, 30))
        label = ImageDraw.Draw(strip)
        for i, name in enumerate(names):
            label.text((i * 520 + 12, 6), f'{name} · 第 {t} 帧', font=small, fill='#f0c8ec')
        gif.append(strip)
        if t in ticks:
            sheet.paste(strip, (0, ticks.index(t) * 210))
    sheet.save(out / f'{bg}-trail-sheet.png')
    gif[0].save(out / f'{bg}-trail.gif', save_all=True, append_images=gif[1:],
        duration=[20] * 49 + [500], loop=0)
    zoom_frames = [frame.crop((0, 0, 520, 210)).resize((1040, 420), Image.Resampling.BILINEAR) for frame in gif]
    zoom_frames[0].save(out / f'{bg}-trail-zoom.gif', save_all=True, append_images=zoom_frames[1:],
        duration=[20] * 49 + [500], loop=0)

# 头部爆点消融：无命中特效遮挡，3 倍放大；行为昼夜 × 三条泳道，列为消亡后第 1~19 帧。
for bg in ('day', 'night'):
    ticks = [1, 4, 7, 10, 13, 16, 19]
    sheet = Image.new('RGB', (240 * len(ticks), 240 * 3 + 30), '#15161d')
    draw = ImageDraw.Draw(sheet)
    for c, t in enumerate(ticks):
        draw.text((c * 240 + 12, 6), f'第 {t} 帧', font=small, fill='#f0c8ec')
        frame = Image.open(frames / f'{bg}-head-{t:03}.png').convert('RGB')
        for lane in range(3):
            x = lane * 520 + 345
            sheet.paste(frame.crop((x - 50, 290, x + 30, 370)).resize((240, 240), Image.NEAREST), (c * 240, 30 + lane * 240))
    sheet.save(out / f'{bg}-head-dissolve.png')

# 拖尾消亡方案对照：正式（尾端缩短 45%²）与完全不缩短，各一份 GIF，上白天下夜晚，按游戏速度播放。
for prefix, name, title in (('trail', 'trail-option-shrink', '正式：尾端轻微缩短 + 截面收窄'),
                             ('noshrink', 'trail-option-noshrink', '对照：完全不缩短，纯靠截面收窄')):
    gif = []
    for t in range(1, 31):
        strip = Image.new('RGB', (1560, 30 + 180 * 2), '#15161d')
        label = ImageDraw.Draw(strip)
        label.text((12, 6), f'{title} · 消亡后第 {t} 帧（共 22 帧）', font=small, fill='#f0c8ec')
        for row, bg in enumerate(('day', 'night')):
            frame = Image.open(frames / f'{bg}-{prefix}-{t:03}.png').convert('RGB')
            strip.paste(frame.crop((0, 230, 1560, 410)), (0, 30 + row * 180))
        gif.append(strip)
    gif[0].save(out / f'{name}.gif', save_all=True, append_images=gif[1:], duration=[17] * 29 + [600], loop=0)
    # 0.4 倍速、2 倍放大的直线泳道，便于看清断开与收窄过程
    slow = [g.crop((0, 0, 520, 390)).resize((1040, 780), Image.Resampling.BILINEAR) for g in gif]
    slow[0].save(out / f'{name}-slow.gif', save_all=True, append_images=slow[1:], duration=[42] * 29 + [800], loop=0)

