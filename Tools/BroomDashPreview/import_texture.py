"""Convert the author's CoVFX premultiplied wind mask to a packaged straight-alpha PNG.
Run from any directory: python Tools/BroomDashPreview/import_texture.py [source.png]
The production renderer never creates or converts textures at runtime.
"""
import sys,hashlib
from pathlib import Path
from PIL import Image
repo=Path(__file__).resolve().parents[2]
source=Path(sys.argv[1]) if len(sys.argv)>1 else repo.parent/'CoVFX/Content/Projectiles/SakuraDashProj.png'
target=repo/'Mounts/Assets/BroomDashWind.png'
im=Image.open(source).convert('RGBA');converted=[]
for r,g,b,a in im.get_flattened_data():
 converted.append((min(255,round(r*255/a)),min(255,round(g*255/a)),min(255,round(b*255/a)),a) if a else (0,0,0,0))
result=Image.new('RGBA',im.size);result.putdata(converted);result.save(target)
assert im.getchannel('A').tobytes()==result.getchannel('A').tobytes()
print(f'Source SHA256: {hashlib.sha256(source.read_bytes()).hexdigest()}')
print(f'Straight-alpha PNG: {target} {im.size}; alpha channel preserved.')
