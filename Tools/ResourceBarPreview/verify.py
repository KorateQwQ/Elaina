"""Validate the shipped resource art, native references, and optionally a staged .tmod."""
import argparse
import hashlib
import json
import struct
import zlib
from pathlib import Path
from PIL import Image, ImageChops, ImageStat, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'ElainaAttribute/ResourceBarAssets'
WORK = ROOT / '.vissandbox/resource-bars'


def read_package(path):
    with path.open('rb') as f:
        def string():
            size = shift = 0
            while True:
                byte = f.read(1)[0]
                size |= (byte & 127) << shift
                if byte < 128:
                    return f.read(size).decode('utf-8')
                shift += 7
        assert f.read(4) == b'TMOD'
        string()
        f.seek(280, 1)
        string(); string()
        count, = struct.unpack('<i', f.read(4))
        entries = [(string(), *struct.unpack('<ii', f.read(8))) for _ in range(count)]
        result = {}
        for name, size, packed in entries:
            data = f.read(packed)
            assert not name.startswith(('Tools/', '.vissandbox/')), f'Development artifact in package: {name}'
            if 'ResourceBarAssets/' in name or name == 'Effects/Content/ResourceBarClip.xnb':
                result[name] = zlib.decompress(data, -15) if packed != size else data
        return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--package', type=Path)
    args = parser.parse_args()
    manifest = json.loads((ART / 'manifest.json').read_text(encoding='utf-8'))
    assert hashlib.sha256((ROOT / manifest['source']).read_bytes()).hexdigest() == manifest['sourceSha256'], 'Rebake changed HTML'
    packaged = read_package(args.package) if args.package else None
    memory = 0
    for entry in manifest['assets']:
        image = Image.open(ART / (entry['name'] + '.png')).convert('RGBA')
        assert image.size == (entry['width'], entry['height'])
        pixels = image.tobytes()
        assert all(max(pixels[i:i+3]) <= pixels[i+3] for i in range(0, len(pixels), 4)), f'Incorrect premultiplication: {entry["name"]}'
        memory += len(pixels)
        if packaged is not None:
            raw = packaged['ElainaAttribute/ResourceBarAssets/' + entry['name'] + '.rawimg']
            assert struct.unpack('<iii', raw[:12]) == (1, *image.size)
            assert raw[12:] == pixels, 'Packaged rawimg differs from FNA preview art'
    if packaged is not None:
        assert packaged['Effects/Content/ResourceBarClip.xnb'] == (ROOT / 'Effects/Content/ResourceBarClip.xnb').read_bytes()

    comparisons = []
    for reference in sorted((WORK / 'reference').glob('*.png')):
        native = WORK / 'output' / reference.name
        a, b = Image.open(reference).convert('RGB'), Image.open(native).convert('RGB')
        assert a.size == b.size
        mae = sum(ImageStat.Stat(ImageChops.difference(a, b)).mean) / 3
        comparisons.append({'name': reference.name, 'mean_rgb_error_255': round(mae, 4)})
    assert len(comparisons) == 36, 'Missing preview states'
    # Antialiasing differs between Canvas and FNA; catch material/palette/layout regressions, not exact pixels.
    assert max(c['mean_rgb_error_255'] for c in comparisons) < 8, 'Unexpected visual divergence; inspect native output'
    report = {'asset_count': len(manifest['assets']), 'rgba_bytes': memory, 'package_checked': packaged is not None,
              'comparisons': comparisons, 'note': 'Shared production renderer; FNA offline captures, not live game screenshots.'}
    (WORK / 'validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')

    font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 18)
    sheet = Image.new('RGB', (1014, 578), '#14121d')
    draw = ImageDraw.Draw(sheet)
    for y, label, name in [(18, 'FNA 实际绘制 · 正常状态', 'normal-dark-3.0'), (302, 'FNA 实际绘制 · 受伤残影', 'damage-dark-3.0')]:
        draw.text((24, y), label, font=font, fill='#e7deef')
        sheet.paste(Image.open(WORK / 'output' / (name + '.png')), (24, y+33))
    sheet.save(ROOT / 'Tools/ResourceBarPreview/native-preview.png')
    print(f'PASS: {len(manifest["assets"])} premultiplied assets ({memory / 1048576:.2f} MiB), 36 FNA comparisons, package={packaged is not None}')


if __name__ == '__main__':
    main()
