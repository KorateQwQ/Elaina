"""核对本次 FNA 帧的悬停静止/可见性，保存锁定与发射阶段图。"""
import json
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw

root = Path(__file__).resolve().parents[2] / '.vissandbox/shardbind-preview/html-v2'
timing = json.loads((root / 'after/normal/timing.json').read_text())
confirm, launch, end = (timing[k] for k in ('confirmTick', 'launchTick', 'endTick'))
for mode in ('normal', 'dark'):
    folder = root / 'after' / mode
    with Image.open(folder / '000.png') as source:
        initial = source.convert('RGB')
    with Image.open(folder / f'{end:03}.png') as source:
        difference = ImageChops.difference(initial, source.convert('RGB'))
        if difference.getbbox() is None:
            raise RuntimeError(f'{mode}: 悬停锁定框完全不可见。')
    for tick in range(1, confirm + 1):
        with Image.open(folder / f'{tick:03}.png') as source:
            if ImageChops.difference(initial, source.convert('RGB')).getbbox() is not None:
                raise RuntimeError(f'{mode}/{tick}: 确认前锁定框或冰锥已产生动画。')
    print(f'PASS {mode}: hover visible and unchanged through frame {confirm}.')

ticks = [0, confirm + 10, confirm + 21, confirm + 55, launch - 1, launch, launch + 3,
         timing['chargeStart'], timing['burstTick']]
sheet = Image.new('RGB', (1920, 1170), '#101925')
draw = ImageDraw.Draw(sheet)
for index, tick in enumerate(ticks):
    x, y = index % 3 * 640, index // 3 * 390
    with Image.open(root / 'targets/fishron/after/normal' / f'{tick:03}.png') as source:
        # 保留完整冰锥环绕范围；这是同一 FNA 完整原生帧的 25% 缩图。
        sheet.paste(source.resize((640, 360), Image.Resampling.LANCZOS), (x, y + 30))
    draw.text((x + 12, y + 8), f'tick {tick} | {tick/60:.2f}s | FNA offline (25%)', fill='#bce5fa')
sheet.save(root / 'viewer/preparation-stages.png')
(root / 'viewer/preparation-verification.json').write_text(json.dumps({
    'timing': timing, 'hoverStaticFrames': confirm + 1,
    'backgrounds': ['normal', 'dark'], 'nativeFrameSize': [2560, 1440],
    'scope': 'FNA replay; fixed targets, no world AI or networking'
}, indent=2), encoding='utf-8')
print(root / 'viewer/preparation-stages.png')

# 单独检查短齐射的多个方向和重叠命中，不让总览跳过不到半秒的发射段。
volley = Image.new('RGB', (1920, 1170), '#101925')
draw = ImageDraw.Draw(volley)
for index, relative in enumerate((-1, 0, 3, 6, 12, 18, 24, 28, 31)):
    tick = launch + relative
    x, y = index % 3 * 640, index // 3 * 390
    with Image.open(root / 'targets/fishron/after/dark' / f'{tick:03}.png') as source:
        volley.paste(source.resize((640, 360), Image.Resampling.LANCZOS), (x, y + 30))
    draw.text((x + 12, y + 8), f'tick {tick} | volley {relative:+d} | FNA offline (25%)', fill='#bce5fa')
volley.save(root / 'viewer/volley-stages.png')
print(root / 'viewer/volley-stages.png')

depth = Image.new('RGB', (1920, 1170), '#101925')
draw = ImageDraw.Draw(depth)
for index, tick in enumerate((80, 94, 100, 110, 119, 120, 121, 123, 128)):
    x, y = index % 3 * 640, index // 3 * 390
    with Image.open(root / 'targets/fishron/after/normal' / f'{tick:03}.png') as source:
        depth.paste(source.resize((640, 360), Image.Resampling.LANCZOS), (x, y + 30))
    draw.text((x + 12, y + 8), f'tick {tick} | depth / retreat / launch | FNA (25%)', fill='#bce5fa')
depth.save(root / 'viewer/depth-stages.png')
print(root / 'viewer/depth-stages.png')

overlap = Image.new('RGB', (1920, 780), '#101925')
draw = ImageDraw.Draw(overlap)
first_ray = timing['firstRayTick']
for index, tick in enumerate((first_ray - 1, first_ray, first_ray + 4,
                              launch + timing['volleySpan'] + 3, timing['burstTick'] - 1, timing['burstTick'])):
    x, y = index % 3 * 640, index // 3 * 390
    with Image.open(root / 'targets/fishron/after/normal' / f'{tick:03}.png') as source:
        overlap.paste(source.resize((640, 360), Image.Resampling.LANCZOS), (x, y + 30))
    draw.text((x + 12, y + 8), f'tick {tick} | overlapping charge / burst | FNA (25%)', fill='#bce5fa')
overlap.save(root / 'viewer/charge-overlap-stages.png')
print(root / 'viewer/charge-overlap-stages.png')
