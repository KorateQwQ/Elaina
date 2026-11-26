"""Prefilter the premultiplied 2x artwork without premultiplying it a second time."""
import argparse
import hashlib
import io
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[2]
output = root / "ElainaModSkills/ElainaSkillUI/BattleBar"
check = argparse.ArgumentParser()
check.add_argument("--check", action="store_true")
args = check.parse_args()
manifest = {}
for name in ("Rack", "Ornaments"):
    source = output / f"{name}.png"
    master = Image.open(source).convert("RGBA")
    entries = []
    for index, factor in enumerate((.25, .375, .5, .625, .75)):
        size = tuple(round(side * factor) for side in master.size)
        # Filter the stored premultiplied channels independently, retaining covered area.
        image = Image.merge("RGBA", tuple(channel.resize(size, Image.Resampling.BOX) for channel in master.split()))
        stream = io.BytesIO()
        image.save(stream, format="PNG")
        data = stream.getvalue()
        path = output / f"{name}Lod{index}.png"
        if args.check:
            assert path.read_bytes() == data, f"Stale UI raster level: {path}"
        else:
            path.write_bytes(data)
        assert all(r <= a and g <= a and b <= a for r, g, b, a in image.getdata()), path
        entries.append({"file": path.name, "size": size, "sha256": hashlib.sha256(data).hexdigest()})
    manifest[name] = {"sourceSha256": hashlib.sha256(source.read_bytes()).hexdigest(), "levels": entries}
data = json.dumps(manifest, indent=2) + "\n"
path = output / "raster-manifest.json"
if args.check:
    assert path.read_text() == data, "Stale raster manifest"
else:
    path.write_text(data)
print("PASS: ten prefiltered artwork layers, source hashes and premultiplied alpha")
