from pathlib import Path
from PIL import Image
import hashlib
import json
import sys

repo, output = map(Path, sys.argv[1:3])
check = '--check' in sys.argv
entries = json.loads((output / 'pixels.json').read_text(encoding='utf-8-sig'))
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
assert table.read_bytes() == generated, 'Cloud reveal thresholds are stale'
manifest = repo / 'Effects/Tex/Baked/manifest.json'
document = {'version': 1, 'alpha': 'Original RGBA bytes. ImageIO.ToRaw must not premultiply again.', 'textures': entries}
if not check:
    manifest.write_text(json.dumps(document, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
assert json.loads(manifest.read_text(encoding='utf-8')) == document
print(f'PASS: {len(entries)} PNGs byte-match original generated pixels; cloud reveal table matches.')
