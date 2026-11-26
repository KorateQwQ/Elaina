"""Verify shipped UI resources against the exact files used by the FNA previews."""
import argparse
import struct
import zlib
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument("package", type=Path)
args = parser.parse_args()
assets = root / "ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets"
files = list((assets / "FontRaster").rglob("*.png")) + list((assets / "FontRaster").rglob("*.bin"))
files += [assets / "StarsRaster.png", assets / "StarsRaster.json", root / "Effects/Content/UIShape.xnb"]
files += list((root / "ElainaModSkills/ElainaSkillUI/BattleBar").glob("*.png"))
files += [root / "ElainaModSkills/ElainaSkillUI/BattleBar/raster-manifest.json"]
expected = {}
for path in files:
    name = path.relative_to(root).as_posix()
    if path.suffix == ".png":
        image = Image.open(path).convert("RGBA")
        name = name[:-4] + ".rawimg"
        expected[name] = struct.pack("<iii", 1, *image.size) + image.tobytes()
    else:
        expected[name] = path.read_bytes()

with args.package.open("rb") as stream:
    def string():
        size = shift = 0
        while True:
            byte = stream.read(1)[0]
            size |= (byte & 127) << shift
            if byte < 128:
                return stream.read(size).decode("utf-8")
            shift += 7
    assert stream.read(4) == b"TMOD"
    string()
    stream.seek(280, 1)
    string(); string()
    count, = struct.unpack("<i", stream.read(4))
    entries = [(string(), *struct.unpack("<ii", stream.read(8))) for _ in range(count)]
    found = set()
    for name, size, packed in entries:
        assert not name.startswith(("Tools/", ".vissandbox/")), f"Development artifact packaged: {name}"
        if name in expected:
            data = stream.read(packed)
            if size != packed:
                data = zlib.decompress(data, -15)
            assert data == expected[name], f"Packaged resource differs: {name}"
            found.add(name)
        else:
            stream.seek(packed, 1)
    assert found == expected.keys(), f"Missing resources: {expected.keys() - found}"
print(f"PASS: {len(found)} UI resources match preview bytes; Tools and .vissandbox excluded")
