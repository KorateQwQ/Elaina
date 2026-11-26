"""Inspect native FNA output, compose lock animation and compare preserved downstream frames."""
from pathlib import Path
import json
import hashlib
from PIL import Image, ImageChops, ImageDraw

root = Path(__file__).resolve().parents[2]
work = root / '.vissandbox/shardbind-preview'
frames = work / 'html-v2'
viewer = frames / 'viewer'
baseline = work / 'hover-baseline'
stages = (4, 12, 20, 44, 45, 55, 66, 69)
sheet = Image.new('RGB', (1280, 720), '#0c1623')
draw = ImageDraw.Draw(sheet)
for i, tick in enumerate(stages):
    with Image.open(frames / 'targets/fishron/after/normal' / f'{tick:03}.png') as source:
        patch = source.crop((960, 400, 1600, 1040)).resize((320, 320), Image.Resampling.LANCZOS)
    x, y = (i % 4) * 320, (i // 4) * 360
    sheet.paste(patch, (x, y+40))
    draw.text((x+12, y+12), f'{tick/60:.3f}s | frame {tick} | 50%', fill='#caedff')
sheet.save(viewer / 'hover-transition-stages.png')

size_baseline = work / 'hover-size-baseline/044.png'
if size_baseline.exists():
    comparison = Image.new('RGB', (1280,680), '#0c1623')
    labels = ImageDraw.Draw(comparison)
    for i, path in enumerate((size_baseline, frames/'targets/fishron/after/normal/044.png')):
        with Image.open(path) as source:
            comparison.paste(source.crop((960,400,1600,1040)), (640*i,40))
        labels.text((640*i+12,12), ['Previous hover scale ~1.52','Historical settled scale ~0.86'][i],fill='#caedff')
    comparison.save(viewer/'hover-size-comparison.png')

# 单一全局调色板；隔帧采样为30fps，GIF的10ms时间单位用30/40/30ms周期。
animated = []
for tick in range(0, 124, 2):
    with Image.open(frames / 'targets/fishron/after/normal' / f'{tick:03}.png') as source:
        animated.append(source.convert('RGB').crop((920, 360, 1640, 1080)).resize((540,540), Image.Resampling.LANCZOS))
palette_source = Image.new('RGB', (540*8, 540))
for i, frame in enumerate(animated[::8]):
    palette_source.paste(frame, (i*540, 0))
palette = palette_source.quantize(colors=256)
gif = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in animated]
durations = [30 if i % 3 != 1 else 40 for i in range(len(gif))]
durations[-1] += 500
gif[0].save(viewer / 'hover-transition.gif', save_all=True, append_images=gif[1:],
            duration=durations, loop=0, optimize=False, disposal=2)

checks = {'source': 'FNA offline', 'motionFrames': [], 'preservedDownstream': [], 'productionUnchanged': []}
# 无NPC画面只含准星，排除NPC自身游动掩盖单帧问题。
for a, b in ((4,12), (12,20), (20,32), (32,44), (44,45), (45,46), (65,66), (66,67), (112,116)):
    with Image.open(frames/'after/normal'/f'{a:03}.png') as first, Image.open(frames/'after/normal'/f'{b:03}.png') as second:
        if ImageChops.difference(first.convert('RGB'), second.convert('RGB')).getbbox() is None:
            raise RuntimeError(f'Reticle is static: {a}->{b}')
    checks['motionFrames'].append([a,b])

if baseline.exists():
    for npc in ('none', 'fishron', 'golem'):
        prefix = Path() if npc == 'none' else Path('targets') / npc
        for mode in ('normal', 'dark'):
            for tick in (120,123,137,151,208,209,225,277):
                old = baseline / npc / mode / f'{tick:03}.png'
                new = frames / prefix / 'after' / mode / f'{tick:03}.png'
                with Image.open(old) as a, Image.open(new) as b:
                    if a.tobytes() != b.tobytes():
                        raise RuntimeError(f'Downstream animation changed: {npc}/{mode}/{tick}')
                checks['preservedDownstream'].append(f'{npc}/{mode}/{tick}')
    for source in json.loads((baseline/'production-hashes.json').read_text(encoding='utf-8-sig')):
        file = Path(source['Path'])
        # These two production entry points now contain the approved animation.
        if file.name in ('ShardbindReticle.cs', 'IceConeTargetSystem.cs'):
            continue
        if hashlib.sha256(file.read_bytes()).hexdigest().upper() != source['Hash']:
            raise RuntimeError(f'Production file changed: {file}')
        checks['productionUnchanged'].append(str(file.relative_to(root)))
else:
    print('No saved hover-baseline: downstream/production comparison not run; only current animation checked.')
(viewer/'reticle-verification.json').write_text(json.dumps(checks, indent=2), encoding='utf-8')
print(f'PASS reticle: {len(checks["motionFrames"])} frame pairs animated; '
      f'{len(checks["preservedDownstream"])} downstream frames identical; '
      f'{len(checks["productionUnchanged"])} production files unchanged.')
print(viewer/'hover-transition.gif')
