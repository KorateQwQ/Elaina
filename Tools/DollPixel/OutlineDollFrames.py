"""玩偶控制 · 给现有小熊待机/移动帧（Kuma/Kuma_Idle、Kuma_Move_1..4，32x32）加描边。

在透明像素上补一圈四邻接外描边；贴到画布边缘的像素（脚底）没有外圈空间，就把它本身改成描边色。
默认只输出预览到 Tools/DollPixel/output；确认后加 --apply 覆盖正式文件。
"""

import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent / "output"
TARGET = ROOT / "ElainaModSkills/Skills/DollControl/Kuma"
FRAME_NAMES = ["Kuma_Idle.png"] + [f"Kuma_Move_{i}.png" for i in range(1, 5)]

OUTLINE = (52, 38, 14, 255)  # 比原图最暗色 (60,46,11) 再深一点的暗棕
NEIGHBOURS = ((-1, 0), (1, 0), (0, -1), (0, 1))


def add_outline(src):
    w, h = src.size
    a = src.getchannel("A").load()
    out = src.copy()
    px = out.load()
    for y in range(h):
        for x in range(w):
            if a[x, y]:
                if x in (0, w - 1) or y in (0, h - 1):
                    px[x, y] = OUTLINE
                continue
            if any(0 <= x + dx < w and 0 <= y + dy < h and a[x + dx, y + dy] for dx, dy in NEIGHBOURS):
                px[x, y] = OUTLINE
    return out


def sheet(images, scale, bg, gap=2):
    size = images[0].width
    w = len(images) * (size + gap) - gap
    out = Image.new("RGBA", (w * scale, size * scale), bg)
    for i, im in enumerate(images):
        out.alpha_composite(im.resize((size * scale, size * scale), Image.NEAREST), (i * (size + gap) * scale, 0))
    return out


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    old = [Image.open(TARGET / name).convert("RGBA") for name in FRAME_NAMES]
    if any(OUTLINE in (c for _, c in im.getcolors(1024)) for im in old):
        sys.exit("正式文件里已经有描边色了（已 --apply 过），不要重复描边")
    new = [add_outline(im) for im in old]

    for name, im in zip(FRAME_NAMES, new):
        im.save(OUT / f"Outlined_{name}")

    # 原图 / 加描边，深色与浅色背景各一组
    rows = []
    for bg in ((88, 84, 112, 255), (186, 210, 230, 255)):
        rows += [sheet(old, 8, bg), sheet(new, 8, bg)]
    cmp = Image.new("RGBA", (rows[0].width, sum(r.height for r in rows) + 12 * (len(rows) - 1)), (30, 28, 40, 255))
    y = 0
    for r in rows:
        cmp.alpha_composite(r, (0, y))
        y += r.height + 12
    cmp.save(OUT / "Outlined_Compare_x8.png")

    if "--apply" in sys.argv:
        for name, im in zip(FRAME_NAMES, new):
            im.save(TARGET / name)
        print("已写入", TARGET)


if __name__ == "__main__":
    main()
