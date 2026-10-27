from pathlib import Path
import hashlib
import io
import json
import re
import struct
import sys
import xml.etree.ElementTree as ET
import zlib

repo = Path(__file__).resolve().parents[2]
patterns = next(line.split('=', 1)[1] for line in (repo / 'build.txt').read_text(encoding='utf-8-sig').splitlines() if line.startswith('buildIgnore'))
masks = [re.compile('^' + re.escape(mask.strip()).replace(r'\*', '.*').replace(r'\?', '.') + '$', re.I) for mask in patterns.split(',')]
def ignored(path):
    return path.startswith(('.', 'bin/', 'obj/')) or any(mask.match(path) for mask in masks)

compiled = json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig'))['Items']['Compile']
compiled = {item['Identity'].replace('\\', '/') for item in compiled}
legacy = ET.parse(repo / 'Directory.Build.targets').find('.//Compile').get('Remove').split(';')
for mask in legacy:
    for path in repo.glob(mask):
        rel = path.relative_to(repo).as_posix()
        assert ignored(rel), f'Missing buildIgnore: {rel}'
        assert rel not in compiled, f'Legacy source still compiled: {rel}'
for rel in compiled:
    text = (repo / rel).read_text(encoding='utf-8-sig')
    text = re.sub(r'/\*.*?\*/|//[^\n]*', '', text, flags=re.S)
    assert '伊蕾娜.Projectiles' not in text, rel
    assert not re.search(r'"(?:伊蕾娜/)?Projectiles/', text), rel
    for retired in ('LegacySupport/', '蓝量共享', 'SayoNecklace', 'SayoProtectCD', 'GiveNikehBookToOldPlayer', 'ExtraBossDamage', 'TheBossJustDefeat'):
        assert retired not in text, (rel, retired)
assert 'Items/gItem.cs' not in compiled
for name in ('魔力共享戒指', '白河豚项链', '妮可的冒险谭'):
    assert f'Items/accessories/{name}.cs' not in compiled
assert 'Items/accessories/ElainaHat.cs' in compiled
assert 'ElainaModSkills/Skills/Ice/IceCycleSystem.cs' in compiled

stream = io.BytesIO(Path(sys.argv[1]).read_bytes())
def integer(): return struct.unpack('<i', stream.read(4))[0]
def string():
    length = shift = 0
    while True:
        byte = stream.read(1)[0]
        length |= (byte & 127) << shift
        if byte < 128: break
        shift += 7
    return stream.read(length).decode('utf-8')
assert stream.read(4) == b'TMOD'
version = string()
digest = stream.read(20)
stream.read(256)
length = integer()
assert len(stream.getbuffer()) - stream.tell() == length
assert hashlib.sha1(stream.getbuffer()[stream.tell():]).digest() == digest
name, mod_version = string(), string()
entries = [(string(), integer(), integer()) for _ in range(integer())]
files = {}
for path, size, compressed in entries:
    data = stream.read(compressed)
    files[path] = zlib.decompress(data, -15) if size != compressed else data
    assert not path.startswith(('Projectiles/', 'ElainaModSkills/UI参考/', 'Tools/', '.vissandbox/')), path
    assert '/LegacySupport/' not in path, path
    assert not ignored(path), f'Ignored entry in package: {path}'
    if path.endswith('.rawimg'):
        assert not ignored(path[:-7] + '.png'), f'Ignored texture in package: {path}'
assert files[name + '.dll'] == (repo / f'bin/Debug/net8.0/{name}.dll').read_bytes()
assert 'Items/accessories/ElainaHat.rawimg' in files
print(f'PASS: {len(compiled)} compiled sources checked; legacy sources excluded from MSBuild and build.txt.')
print(f'PASS: {len(files)} package entries checked; Projectiles/UI references absent; current DLL included.')
print('PASS: retired accessories, gItem and temporary support assets absent; Elaina hat and KL freeze integration retained.')
