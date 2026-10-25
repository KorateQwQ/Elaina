"""把提示特效动画帧合成为循环 GIF：assemble-fx.py <帧目录> <输出 GIF>。"""
import sys
from pathlib import Path
from PIL import Image

frames = [Image.open(p).convert("RGB") for p in sorted(Path(sys.argv[1]).glob("frame-*.png"))]
if not frames:
    sys.exit("no frames")
frames[0].save(sys.argv[2], save_all=True, append_images=frames[1:], duration=42, loop=0)
print(sys.argv[2])
