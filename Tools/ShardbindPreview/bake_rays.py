"""光片覆盖率遮罩：横向恒定，只有沿长度方向透明度衰减。"""
from pathlib import Path
from PIL import Image

target = Path(__file__).parent / 'Assets' / 'ChargeRay.png'
target.parent.mkdir(exist_ok=True)
image = Image.new('RGBA', (256, 128))
pixels = image.load()
for y in range(128):
    for x in range(256):
        u = x / 255
        # 从内部开始向外持续衰减，远端融入背景，不形成亮圆头。
        fade = max(0, min(1, (.90-u)/.84))
        fade = fade*fade*(3-2*fade)
        coverage = round(255*fade)
        # RGB 恒定白，只有 Alpha 变化；蓝色仅由整片统一的顶点色提供。
        pixels[x, y] = (255, 255, 255, coverage)
image.save(target)
print(target)
