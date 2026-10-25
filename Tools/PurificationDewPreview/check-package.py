"""Read-only verification of the built TMOD's dew effect and game assembly."""
from pathlib import Path
import hashlib
import io
import struct
import zlib
import sys

repo = Path(__file__).resolve().parents[2]
package = Path(sys.argv[1]) if len(sys.argv) > 1 else repo.parent.parent / 'Mods' / f'{repo.name}.tmod'
stream = io.BytesIO(package.read_bytes())

def integer():
    return struct.unpack('<i', stream.read(4))[0]

def string():
    length = shift = 0
    while True:
        byte = stream.read(1)[0]
        length |= (byte & 127) << shift
        if byte < 128:
            break
        shift += 7
    return stream.read(length).decode('utf-8')

assert stream.read(4) == b'TMOD'
version = string()
digest = stream.read(20)
stream.read(256)
length = integer()
payload_start = stream.tell()
assert len(stream.getbuffer()) - payload_start == length
assert hashlib.sha1(stream.getbuffer()[payload_start:]).digest() == digest
name, mod_version = string(), string()
entries = [(string(), integer(), integer()) for _ in range(integer())]
wanted = {
    'Effects/Content/PurificationDew.xnb': repo / 'Effects/Content/PurificationDew.xnb',
    f'{name}.dll': repo / 'bin/Debug/net8.0' / f'{name}.dll',
}
verified = []
for filename, size, compressed in entries:
    data = stream.read(compressed)
    assert not filename.startswith(('Tools/', '.vissandbox/')), filename
    if filename in wanted:
        if compressed != size:
            data = zlib.decompress(data, -15)
        assert data == wanted[filename].read_bytes(), f'Packaged file differs: {filename}'
        verified.append(filename)
assert set(verified) == set(wanted), verified
print(f'PASS packaged production dew shader + current mod assembly; tools excluded; TMOD SHA-1 valid ({version}).')
print(package)
