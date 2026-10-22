"""把 sources/ 中的绿幕熊玩偶帧扣成透明 PNG，写入 Kuma/ 正式目录。

素材为 1 倍像素图，背景是单一颜色 BACKGROUND，与角色和镐子都不重叠，按精确色值扣除，不产生半透明。
新画布中心 (64, 48) 与旧 32×32 移动帧的中心 (16, 16) 在熊身上是同一点，因此以画布中心为准对称裁剪：
DollPetProjectile.DrawFrame 以贴图中心为原点绘制，各帧尺寸不同也不会错位，水平/垂直翻转也不会偏移。

用法（模组根目录）：python Tools/DollControlPreview/cutout_kuma.py [--check]
"""
import sys
from pathlib import Path
from PIL import Image

BACKGROUND = (59, 108, 86)
SIZE = 48  # 镐子向左最远 23 像素、向上最远 22 像素，取对称的 48×48
ROOT = Path(__file__).resolve().parents[2]
SOURCES = Path(__file__).resolve().parent / "sources"
TARGET = ROOT / "ElainaModSkills/Skills/DollControl/Kuma"
FRAMES = {
    "Kuma_Idle_1": "xiongdaiji (1).png",
    "Kuma_Idle_2": "xiongdaiji (2).png",
    "Kuma_Idle_3": "xiongdaiji (3).png",
    "Kuma_Mine_1": "xiongwa1.png",
    "Kuma_Mine_2": "xiongwa2.png",
    "Kuma_Mine_3": "xiongwa3.png",
    "Kuma_Mine_4": "xiongwa4.png",
}


def cutout(source: Path) -> Image.Image:
    image = Image.open(source).convert("RGB")
    width, height = image.size
    if width % 2 or height % 2:
        raise ValueError(f"{source.name}: canvas must have an even size to keep an integer center")
    left, top = (width - SIZE) // 2, (height - SIZE) // 2
    pixels = image.load()
    result = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    out = result.load()
    for y in range(height):
        for x in range(width):
            color = pixels[x, y]
            if color == BACKGROUND:
                continue
            if not (left <= x < left + SIZE and top <= y < top + SIZE):
                raise ValueError(f"{source.name}: content at ({x}, {y}) falls outside the {SIZE}px crop")
            out[x - left, y - top] = (*color, 255)
    return result


def main() -> int:
    check = "--check" in sys.argv
    for name, source in FRAMES.items():
        frame = cutout(SOURCES / source)
        path = TARGET / f"{name}.png"
        if check:
            if not path.exists() or list(Image.open(path).convert("RGBA").getdata()) != list(frame.getdata()):
                print(f"STALE: {path.name} differs from sources/{source}")
                return 1
        else:
            frame.save(path)
    print(("CHECK OK" if check else "Wrote") + f": {len(FRAMES)} Kuma frames ({SIZE}x{SIZE})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
