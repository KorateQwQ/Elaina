from pathlib import Path
from concurrent.futures import ThreadPoolExecutor, as_completed
from time import perf_counter
import json
import shutil
import sys
import argparse
from datetime import datetime, timezone
from uuid import uuid4
from math import cos, sin
from PIL import Image, ImageDraw, ImageStat

parser = argparse.ArgumentParser()
parser.add_argument('output', type=Path)
parser.add_argument('--png', action='store_true')
parser.add_argument('--verify-visual', action='store_true')
parser.add_argument('--updated-frames')
parser.add_argument('--updated-targets')
args = parser.parse_args()
out = args.output
png_playback = args.png
verify_visual = args.verify_visual
frame_extension = 'png' if png_playback else 'webp'
started = perf_counter()
viewer = out / 'viewer'
viewer.mkdir(exist_ok=True)
scenes = {'none': out, 'fishron': out / 'targets/fishron', 'golem': out / 'targets/golem'}
timing = {version: json.loads((out / version / 'normal/timing.json').read_text())
          for version in ('before', 'after')}
offset = timing['after'].get('chargeOffset', timing['after'].get('flightOffset', 0))
def after(tick):
    if tick == 31:
        return timing['after']['launchTick'] + 5
    if tick == 85:
        return timing['after']['launchTick'] + timing['after']['volleySpan'] // 2
    return tick + offset
updated_files = {scenes[npc] / 'after' / mode / f'{int(t):03d}.png'
                 for npc in args.updated_targets.split(',') for mode in ('normal', 'dark')
                 for t in args.updated_frames.split(',')} if args.updated_frames else set()
files = []
for scene, folder in scenes.items():
    for version in ('before', 'after'):
        for mode in ('normal', 'dark'):
            for tick in range(timing[version]['endTick'] + 1):
                file = folder / version / mode / f'{tick:03d}.png'
                if not file.is_file():
                    raise RuntimeError(f'Missing native frame: {file}')
                files.append(file)

switch_metadata = out / 'switch/timing.json'
if switch_metadata.exists():
    switch_timing = json.loads(switch_metadata.read_text())
    for mode in ('normal', 'dark'):
        for tick in range(switch_timing['endTick'] + 1):
            file = out / 'switch' / mode / f'{tick:03d}.png'
            if not file.is_file():
                raise RuntimeError(f'Missing switch demo frame: {file}')
            files.append(file)

def encode(file):
    destination = viewer / file.relative_to(out).with_suffix('.' + frame_extension)
    destination.parent.mkdir(parents=True, exist_ok=True)
    if file not in updated_files and destination.exists() and destination.stat().st_mtime_ns >= file.stat().st_mtime_ns:
        return
    if png_playback:
        # 直接播放 FNA PNG，省去无损 WebP 编码；原始像素不变。
        shutil.copy2(file, destination)
        return
    with Image.open(file) as source:
        if source.size != (2560, 1440):
            raise RuntimeError(f'Unexpected frame size: {file}: {source.size}')
        source.convert('RGB').save(destination, 'WEBP', lossless=True, method=1)

print(f'Preparing {len(files)} browser frames ({frame_extension.upper()})...', flush=True)
with ThreadPoolExecutor(max_workers=6) as pool:
    futures = [pool.submit(encode, file) for file in files]
    for done, future in enumerate(as_completed(futures), 1):
        future.result()
        if done % 200 == 0 or done == len(files):
            print(f'Browser frames {done}/{len(files)} | {perf_counter()-started:.1f}s', flush=True)

# 对照采用各版本对应的阶段，时长由 FNA 导出的元数据提供。
timing = {}
for version in ('before', 'after'):
    metadata = out / version / 'normal/timing.json'
    timing[version] = json.loads(metadata.read_text()) if metadata.exists() else {'endTick': 180, 'burstTick': 110, 'chargeStart': 106}
for npc, folder in scenes.items():
    for mode in ('normal', 'dark'):
        if json.loads((folder / 'after' / mode / 'timing.json').read_text()) != timing['after']:
            raise RuntimeError(f'Timing differs: {npc}/{mode}')
timing['frameExtension'] = frame_extension
if switch_metadata.exists():
    timing['switch'] = switch_timing
def publish():
    # Publish the completion marker last; live pages keep their current build until then.
    shutil.copyfile(Path(__file__).with_name('index.html'), viewer / 'index.html')
    shutil.copyfile(Path(__file__).with_name('preview-refresh.js'), viewer / 'preview-refresh.js')
    timing['buildId'] = uuid4().hex
    timing['generated'] = datetime.now(timezone.utc).isoformat()
    timing['update'] = {'mode': 'partial', 'frames': [int(t) for t in args.updated_frames.split(',')],
                        'targets': args.updated_targets.split(',')} if args.updated_frames else {'mode': 'full'}
    pending = viewer / 'timing.pending.json'
    pending.write_text(json.dumps(timing), encoding='utf-8')
    pending.replace(viewer / 'timing.json')
    # Migrate the discontinued separate viewer, including tabs that still poll frames.json.
    legacy = viewer / 'selected'
    legacy.mkdir(exist_ok=True)
    shutil.copyfile(Path(__file__).with_name('selected.html'), legacy / 'index.html')
    pending = legacy / 'frames.pending.json'
    pending.write_text(json.dumps({'buildId': timing['buildId'], 'generated': timing['generated']}), encoding='utf-8')
    pending.replace(legacy / 'frames.json')
print('Preparing representative frames...', flush=True)

def sheet(name, targets, stages):
    image = Image.new('RGB', (480 * len(targets), 360 * len(stages)), '#101925')
    draw = ImageDraw.Draw(image)
    for row, stage in enumerate(stages):
        for col, (npc, version) in enumerate(targets):
            tick = stage[version]
            with Image.open(scenes[npc] / version / 'normal' / f'{tick:03d}.png') as frame:
                image.paste(frame.crop((1040, 540, 1520, 860)), (col * 480, row * 360 + 40))
            draw.text((col * 480 + 12, row * 360 + 12), f'{npc} | {version} | tick {tick} | {tick/60:.2f}s', fill='#bce5fa')
    image.save(viewer / name)

stages = [dict(before=31,after=after(31)), dict(before=65,after=after(85)), dict(before=105,after=after(140)),
          dict(before=109,after=after(176)), dict(before=109,after=after(211)), dict(before=126,after=after(228))]
sheet('comparison.png', [('none','before'),('none','after')], stages)
sheet('npc-comparison.png', [('fishron','after'),('golem','after')], stages)
sheet('charge-stages.png', [('none','after'),('fishron','after'),('golem','after')],
      [dict(after=after(t)) for t in (140,158,176,194,211,212)])
sheet('lock-stages.png', [('fishron','after'),('golem','after')],
      [dict(after=t) for t in (0, timing['after']['confirmTick'], timing['after']['confirmTick'] + 21,
                              timing['after']['launchTick'] - 1, timing['after']['launchTick'], after(31))])
# 光束需保留整幅视野；原来的球体局部图无法显示外射范围。
ray_sheet = Image.new('RGB', (2400, 980), '#101925')
ray_draw = ImageDraw.Draw(ray_sheet)
for index, tick in enumerate((140, 176, 194, 211, 212, 228)):
    tick = after(tick)
    x, y = index % 3 * 800, index // 3 * 490
    with Image.open(out / 'after' / 'normal' / f'{tick:03d}.png') as source:
        ray_sheet.paste(source.resize((800,450), Image.Resampling.LANCZOS), (x,y+40))
    ray_draw.text((x+14,y+12), f'tick {tick} | {tick/60:.2f}s | FNA offline', fill='#bce5fa')
ray_sheet.save(viewer / 'ray-stages.png')
if not verify_visual:
    (viewer / 'verification.json').write_text(json.dumps({'frames': len(files),
        'timing': timing, 'visualChecks': 'not requested'}, indent=2), encoding='utf-8')
    publish()
    print(f'HTML preview ready: {viewer / "index.html"} (visual assertions disabled)')
    sys.exit(0)
# 直接检查 FNA 像素：蓄能各阶段确实变化，后半段球体中心持续增亮。
brightness = {}
radial_fade = {}
cross_section = {}
# 遮罩的 RGB 必须全图恒定；横向也不能暗藏白芯或椭圆覆盖率。
with Image.open(Path(__file__).with_name('Assets') / 'ChargeRay.png') as mask:
    rgba = mask.convert('RGBA')
    if rgba.convert('RGB').getextrema() != ((255,255), (255,255), (255,255)):
        raise RuntimeError('Sheet mask changes RGB instead of alpha.')
    alpha = rgba.getchannel('A')
    row = [alpha.getpixel((x,0)) for x in range(alpha.width)]
    if row[0] != 255 or row[-1] != 0 or any(b>a for a,b in zip(row,row[1:])):
        raise RuntimeError('Sheet alpha does not fade continuously outwards.')
    if any(alpha.getpixel((x,y)) != row[x] for y in range(alpha.height) for x in range(alpha.width)):
        raise RuntimeError('Sheet mask contains transverse shading.')
for npc, folder in scenes.items():
    for mode in ('normal', 'dark'):
        values=[]
        for tick in (158,176,194,211):
            tick = after(tick)
            with Image.open(folder / 'after' / mode / f'{tick:03d}.png') as source:
                patch=source.convert('RGB').crop((1140,580,1420,860))
                values.append(sum(ImageStat.Stat(patch).mean)/3)
        if any(b <= a for a,b in zip(values,values[1:])):
            raise RuntimeError(f'Charge brightness not increasing: {npc}/{mode}: {values}')
        brightness[f'{npc}/{mode}']=values
        with Image.open(folder / 'after' / mode / f'{after(211):03}.png') as source, Image.open(folder / 'after' / mode / f'{timing["after"]["endTick"]:03}.png') as base:
            # 减去同位置的森林背景；绝对 RGB 不适合跨天空/树木/地形比较。
            def delta(x,y):
                return tuple(a-b for a,b in zip(source.convert('RGB').getpixel((x,y)), base.convert('RGB').getpixel((x,y))))
            samples = [delta(round(1280+cos(-2.68)*r), round(720+sin(-2.68)*r))
                       for r in (320,560,800,1040,1250)]
            radius = 400
            half_width = 410 * radius / 1420
            across = [delta(
                round(1280+cos(-2.68)*radius-sin(-2.68)*v*half_width),
                round(720+sin(-2.68)*radius+cos(-2.68)*v*half_width)) for v in (-.7, 0, .7)]
        if any(sum(b) >= sum(a) for a,b in zip(samples,samples[1:])):
            raise RuntimeError(f'Light sheet does not fade outwards: {npc}/{mode}: {samples}')
        radial_fade[f'{npc}/{mode}'] = samples
        # 横向数据只记录，不强迫绘制白芯来满足旧版断言。
        cross_section[f'{npc}/{mode}'] = across
(viewer / 'verification.json').write_text(json.dumps({'frames': len(files), 'size': [2560,1440],
    'targets': list(scenes), 'timing': timing, 'charge_brightness': brightness, 'sheet_radial_fade': radial_fade,
    'sheet_cross_section': cross_section, 'mask_rgb_constant': True, 'mask_transverse_constant': True,
    'background': 'FNA composited simulated forest day/night', 'bloom': 'KL original Bloom.xnb + ReColor.xnb; strength=1, iterations=5'}, indent=2), encoding='utf-8')
print(f'HTML preview: {viewer / "index.html"}')
publish()
print(f'PASS {len(files)} frames: three targets, charge brightness increases across all backgrounds.')
