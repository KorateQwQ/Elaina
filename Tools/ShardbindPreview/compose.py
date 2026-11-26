from pathlib import Path
import sys
import json
from PIL import Image, ImageDraw

out = Path(sys.argv[1])
timing = json.loads((out / 'normal/timing.json').read_text())
offset = timing.get('chargeOffset', timing.get('flightOffset', 0))
for mode in ('normal', 'dark'):
    frames = []
    for tick in range(0, timing['endTick'] + 1, 2):
        with Image.open(out / mode / f'{tick:03d}.png') as source:
            frames.append(source.convert('RGB').resize((960, 540), Image.Resampling.LANCZOS))
    samples = Image.new('RGB', (960, 540 * 12))
    for index, tick in enumerate(range(0, 180, 15)):
        samples.paste(frames[tick // 2], (0, index * 540))
    palette = samples.quantize(colors=256)
    encoded = [im.quantize(palette=palette, dither=Image.Dither.NONE) for im in frames]
    durations = ([30, 30, 40] * (len(encoded)//3 + 1))[:len(encoded)-1] + [600]
    encoded[0].save(out / f'{mode}.gif', save_all=True, append_images=encoded[1:], duration=durations, loop=0, disposal=2, optimize=False)
    if mode == 'normal':
        encoded[0].save(out / 'slow.gif', save_all=True, append_images=encoded[1:], duration=100, loop=0, disposal=2, optimize=False)

ticks = [0, timing['confirmTick'] + 1, timing['launchTick'] - 2, timing['launchTick'],
         140 + offset, 176 + offset, 212 + offset, 228 + offset]
sheet = Image.new('RGB', (960 * 4, 574 * 2), '#111b28')
draw = ImageDraw.Draw(sheet)
for index, tick in enumerate(ticks):
    x, y = (index % 4) * 960, (index // 4) * 574
    with Image.open(out / 'normal' / f'{tick:03d}.png') as source:
        sheet.paste(source.resize((960, 540), Image.Resampling.LANCZOS), (x, y + 34))
    draw.text((x + 18, y + 10), f'tick {tick:03d} | {tick / 60:.2f}s | FNA offline', fill='#b6d9ed')
sheet.save(out / 'stages.png')
with Image.open(out / 'normal' / f'{212+offset:03}.png') as source:
    source.crop((800, 400, 1760, 1040)).save(out / 'burst-detail.png')
print(out / 'normal.gif')
