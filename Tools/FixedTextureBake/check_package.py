from pathlib import Path
import hashlib
import io
import json
import struct
import sys
import zlib

repo = Path(__file__).resolve().parents[2]
package = Path(sys.argv[1])
stream = io.BytesIO(package.read_bytes())
def integer(): return struct.unpack('<i', stream.read(4))[0]
def string():
    length = shift = 0
    while True:
        byte = stream.read(1)[0]; length |= (byte & 127) << shift
        if byte < 128: break
        shift += 7
    return stream.read(length).decode('utf-8')

assert stream.read(4) == b'TMOD'
version = string(); digest = stream.read(20); stream.read(256)
length = integer(); start = stream.tell()
assert len(stream.getbuffer()) - start == length
assert hashlib.sha1(stream.getbuffer()[start:]).digest() == digest
name, mod_version = string(), string()
entries = [(string(), integer(), integer()) for _ in range(integer())]
manifest = json.loads((repo / 'Effects/Tex/Baked/manifest.json').read_text(encoding='utf-8'))
expected = {str(Path(item['Path']).with_suffix('.rawimg')).replace('\\', '/'): item for item in manifest['textures']}
found = set()
for path, size, compressed in entries:
    data = stream.read(compressed)
    assert not path.startswith(('Tools/', '.vissandbox/')), path
    if path in expected or path == f'{name}.dll':
        if size != compressed: data = zlib.decompress(data, -15)
        if path == f'{name}.dll':
            assert data == (repo / f'bin/Debug/net8.0/{name}.dll').read_bytes(), 'Packaged assembly is stale'
            continue
        item = expected[path]
        assert struct.unpack('<iii', data[:12]) == (1, item['Width'], item['Height']), path
        assert hashlib.sha256(data[12:]).hexdigest() == item['Sha256'], f'Packing changed texture RGBA: {path}'
        found.add(path)
assert found == set(expected), set(expected) - found
print(f'PASS package: all {len(found)} rawimg assets exactly match old runtime pixels; current DLL included; tools excluded; TMOD hash valid.')
