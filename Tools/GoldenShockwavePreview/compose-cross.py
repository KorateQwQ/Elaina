from pathlib import Path
from PIL import Image

output = Path(__file__).parent / "cross-preview"
frames = [Image.open(p).convert("RGB") for p in sorted((output / "frames").glob("*.png"))]
if not frames:
    raise RuntimeError("先运行 capture-cross.cjs 导出实际页面连续帧。")
durations = [30 if i % 3 != 2 else 40 for i in range(len(frames))]
frames[0].save(output / "cross-animation.gif", save_all=True, append_images=frames[1:],
               duration=durations, loop=0, optimize=False)
