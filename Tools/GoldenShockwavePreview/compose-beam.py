"""将 check-beam.cjs 的原生导帧拼成可复查分镜和局部，不重新绘制效果。"""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).parent / "beam-checks"
frames = (3, 16, 24, 48, 52, 56)
sheet = Image.new("RGB", (1920, 300 * len(frames)))
for row, frame in enumerate(frames):
    image = Image.open(root / f"beam-640-{frame}.png").convert("RGB")
    crop = image.crop((0, 260, 1920, 560))
    ImageDraw.Draw(crop).text((16, 12), f"FRAME {frame}", fill="white")
    sheet.paste(crop, (0, row * 300))
sheet.save(root / "sequence.png")
for frame in (52, 56):
    image = Image.open(root / f"beam-640-{frame}.png")
    image.crop((220, 375, 900, 445)).resize((1360, 140)).save(root / f"residue-{frame}-x2.png")
image = Image.open(root / "resolution-3840.png")
image.crop((1520, 450, 2320, 1250)).save(root / "resolution-3840-native.png")

# 保留页面缩小后的星芒与收束滞后，避免只看放大的孤立粒子。
stars = Image.new("RGB", (1212, 240 * 4))
for row, frame in enumerate((16, 24, 40, 48)):
    image = Image.open(root / f"short-1600-{frame}.png").convert("RGB")
    crop = image.crop((0, 220, 1920, 600)).resize((1212, 240), Image.Resampling.LANCZOS)
    ImageDraw.Draw(crop).text((12, 10), f"FRAME {frame}", fill="white")
    stars.paste(crop, (0, row * 240))
stars.save(root / "star-lag-sequence.png")

fade_frames = [Image.open(root / f"fade-{frame}.png").convert("RGB") for frame in range(16, 49)]
fade_frames[0].save(root / "star-fade.gif", save_all=True, append_images=fade_frames[1:], duration=50, loop=0)
fade_sheet = Image.new("RGB", (960, 180 * 6))
for row, frame in enumerate((28, 29, 30, 31, 32, 33)):
    crop = Image.open(root / f"fade-{frame}.png").convert("RGB")
    ImageDraw.Draw(crop).text((12, 10), f"FRAME {frame}", fill="white")
    fade_sheet.paste(crop, (0, row * 180))
fade_sheet.save(root / "star-fade-frames.png")
