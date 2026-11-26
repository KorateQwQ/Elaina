"""比较接入前留存的指定原生帧与正式代码重放；不将目录中未重建的旧帧计入结果。"""
import argparse
import json
from pathlib import Path
from PIL import Image, ImageChops

parser = argparse.ArgumentParser()
parser.add_argument('reference', type=Path)
parser.add_argument('--frames', default='0,44,45,60,66,90,110,119,120,123,137,142,151,156,180,208,209,220,277')
args = parser.parse_args()
current = Path(__file__).resolve().parents[2] / '.vissandbox/shardbind-preview/html-v2'
frames = [int(value) for value in args.frames.split(',')]
different = []
checked = 0
for scene in (Path(), Path('targets/fishron'), Path('targets/golem')):
    for mode in ('normal', 'dark'):
        for tick in frames:
            relative = scene / 'after' / mode / f'{tick:03}.png'
            with Image.open(args.reference / relative) as a, Image.open(current / relative) as b:
                if ImageChops.difference(a.convert('RGBA'), b.convert('RGBA')).getbbox(alpha_only=False):
                    different.append(str(relative))
            checked += 1
result = {'checked': checked, 'different': different, 'frames': frames,
          'reference': str(args.reference.resolve()), 'current': str(current.resolve())}
(current / 'viewer/integration-comparison.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
if different:
    raise RuntimeError(f'{len(different)}/{checked} 帧有差异：{different}')
print(f'PASS: {checked} freshly rendered native frames exactly match the pre-integration reference (RGBA).')
