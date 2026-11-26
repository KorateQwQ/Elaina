from pathlib import Path
from PIL import Image
import hashlib
import json
import sys

repo, output = map(Path, sys.argv[1:3])
check = '--check' in sys.argv
entries = json.loads((output / 'pixels.json').read_text(encoding='utf-8-sig'))
# Same generator as the standalone WindVortex material bake.
from runpy import run_path
vortex_image = run_path(str(repo / 'Tools/WindBladeVortexPreview/bake_noise.py'))['build_noise']()
vortex_path = 'ElainaModSkills/Skills/Wind/Assets/CoreNoise.png'
vortex_pixels = vortex_image.tobytes()
(output / 'WindVortexNoise.rgba').write_bytes(vortex_pixels)
entries.append(dict(Path=vortex_path, Width=vortex_image.width, Height=vortex_image.height,
                    Raw='WindVortexNoise.rgba', Sha256=hashlib.sha256(vortex_pixels).hexdigest(), Encoding='opaque-data'))
for item in entries:
    raw = (output / item['Raw']).read_bytes()
    path = repo / item['Path']
    if not check:
        path.parent.mkdir(parents=True, exist_ok=True)
        # Installed tML ImageIO.ToRaw preserves RGB; store the original premultiplied bytes verbatim.
        Image.frombytes('RGBA', (item['Width'], item['Height']), raw).save(path)
    image = Image.open(path).convert('RGBA')
    assert image.size == (item['Width'], item['Height']), path
    assert image.tobytes() == raw, f'PNG pixels differ from original FNA generator: {path}'
    assert hashlib.sha256(raw).hexdigest() == item['Sha256']
    del item['Raw']
table = repo / 'ElainaModAlchemy/Rain/RainCloudBakedData.cs'
generated = (output / table.name).read_bytes()
if not check:
    table.write_bytes(generated)
# Git may normalize source-file line endings; compare C# text without changing the checked-in file.
assert table.read_text(encoding='utf-8-sig') == generated.decode('utf-8-sig').replace('\r\n', '\n'), 'Cloud reveal thresholds are stale'
manifest = repo / 'Effects/Tex/Baked/manifest.json'
# FNA shader-derived external bake. Rebuild with Tools/ActionTrailPreview/run.ps1 -BakeHalo.
halo_path = 'ElainaActions/Assets/WandStarHalo.png'
with Image.open(repo / halo_path) as halo:
    assert halo.mode == 'RGBA' and halo.size == (128, 128)
    entries.append(dict(Path=halo_path, Width=halo.width, Height=halo.height,
                        Sha256=hashlib.sha256(halo.tobytes()).hexdigest(), Encoding='premultiplied-RGBA'))
document = {'version': 1, 'alpha': 'Original RGBA bytes. ImageIO.ToRaw must not premultiply again.', 'textures': entries}
if not check:
    manifest.write_text(json.dumps(document, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
assert json.loads(manifest.read_text(encoding='utf-8')) == document
print(f'PASS: {len(entries)} PNGs byte-match original generated pixels; cloud reveal table matches.')
