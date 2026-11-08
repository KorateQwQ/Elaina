"""把领域预览的动画帧合成为 GIF：python compose_domain.py <out 目录>"""
import sys
from pathlib import Path
from PIL import Image

out = Path(sys.argv[1]) / "domain"
for folder in sorted(out.glob("frames-*")):
    variant = folder.name[len("frames-"):]
    frames = [Image.open(p).convert("RGB") for p in sorted(folder.glob("*.png"))]
    if not frames:
        raise SystemExit(f"no frames for variant {variant}")
    small = [f.resize((f.width * 2 // 3, f.height * 2 // 3), Image.LANCZOS) for f in frames]
    small[0].save(out / f"{variant}-domain.gif", save_all=True, append_images=small[1:], duration=50, loop=0)
    print(f"{variant}: {len(frames)} frames -> {variant}-domain.gif")
