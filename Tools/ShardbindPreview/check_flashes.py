"""检查实际 FNA 输出的终爆亮度衔接，并生成星闪与终爆阶段图。"""
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageStat

root = Path(__file__).resolve().parents[2] / '.vissandbox/shardbind-preview/html-v2'
timing = json.loads((root / 'after/normal/timing.json').read_text())
# 阶段直接来自本次正式排程，避免沿用旧版212帧终爆的绝对偏移。
offset = timing['burstTick'] - 212
results = {}
for npc in ('none', 'fishron', 'golem'):
    scene = root if npc == 'none' else root / 'targets' / npc
    for mode in ('normal', 'dark'):
        folder = scene / 'after' / mode
        with Image.open(folder / f'{timing["endTick"]:03}.png') as source:
            background = source.convert('RGB')
        energy = {}
        for tick in (211, 212, 213, 214, 218, 224, 240):
            with Image.open(folder / f'{tick+offset:03}.png') as source:
                difference = ImageChops.subtract(source.convert('RGB'), background)
                mean = ImageStat.Stat(difference).mean
                energy[tick] = sum(a * b for a, b in zip(mean, (.2126, .7152, .0722)))
        if energy[212] <= energy[211]:
            raise RuntimeError(f'{npc}/{mode}: 终爆首帧比爆前一帧暗：{energy}')
        sequence = [energy[t] for t in (212, 213, 214, 218, 224, 240)]
        if any(b >= a for a, b in zip(sequence, sequence[1:])):
            raise RuntimeError(f'{npc}/{mode}: 终爆亮度未随时间衰减：{energy}')
        results[f'{npc}/{mode}'] = {'relativeLuminance': energy,
                                    'burstToChargeRatio': energy[212] / energy[211]}

ticks = (163, 167, 171, 211, 212, 214, 218, 224, 232)
sheet = Image.new('RGB', (1920, 1152))
draw = ImageDraw.Draw(sheet)
for i, tick in enumerate(ticks):
    x, y = i % 3 * 640, i // 3 * 384
    with Image.open(root / 'after/dark' / f'{tick+offset:03}.png') as source:
        sheet.paste(source.resize((640, 360)), (x, y))
    draw.text((x + 10, y + 364), f'tick {tick+offset}', fill='white')
sheet.save(root / 'flash-burst-review.png')
(root / 'flash-verification.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
for name, result in results.items():
    print(f'PASS {name}: burst/charge luminance = {result["burstToChargeRatio"]:.3f}; decay verified.')
