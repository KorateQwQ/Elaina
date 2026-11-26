"""Replace selected native frames in the existing animation and publish its build ID."""
from pathlib import Path
import json
import shutil
import subprocess
import sys
from PIL import Image

out, full = map(Path, sys.argv[1:3])
ticks = [int(value) for value in sys.argv[3].split(',')]
target = sys.argv[4]
targets = ['none', 'fishron', 'golem'] if target == 'all' else ['fishron', 'golem'] if target == 'bosses' else [target]
metadata = full / 'viewer/timing.json'
if not metadata.is_file():
    raise RuntimeError('指定帧更新需要已有完整动画，请先不带 -Frames 运行 rebuild。')
timing = json.loads(metadata.read_text(encoding='utf-8'))
copies = []
for npc in targets:
    prefix = Path() if npc == 'none' else Path('targets') / npc
    for mode in ('normal', 'dark'):
        folder = prefix / 'after' / mode
        current = json.loads((out / folder / 'timing.json').read_text(encoding='utf-8'))
        if current != timing['after']:
            raise RuntimeError('动画时序已改变，请不带 -Frames 完整重建后再更新指定帧。')
        for tick in ticks:
            source = out / folder / f'{tick:03d}.png'
            with Image.open(source) as frame:
                if frame.size != (2560, 1440):
                    raise RuntimeError(f'Unexpected frame size: {source}: {frame.size}')
                frame.verify()
            copies.append((source, full / folder / source.name))

# Check the animation before changing any of its frames.
for npc in ('none', 'fishron', 'golem'):
    prefix = Path() if npc == 'none' else Path('targets') / npc
    for version in ('before', 'after'):
        for mode in ('normal', 'dark'):
            for tick in range(timing[version]['endTick'] + 1):
                file = full / prefix / version / mode / f'{tick:03d}.png'
                if not file.is_file():
                    raise RuntimeError(f'完整动画缺少 {file}，请先不带 -Frames 运行 rebuild。')
for source, destination in copies:
    shutil.copy2(source, destination)

command = [sys.executable, '-I', str(Path(__file__).with_name('compose_html.py')), str(full),
           '--updated-frames', ','.join(map(str, ticks)), '--updated-targets', ','.join(targets)]
if timing.get('frameExtension') == 'png':
    command.append('--png')
subprocess.run(command, check=True)
print(f'Updated {len(copies)} frames in the existing animation: {full / "viewer/index.html"}')
