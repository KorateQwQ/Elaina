"""冷却（CD）词条标识符生成器。

与 Snippets/MagicStarIcon.png 同风格：20×20，单像素白色核心线条 + #72B8EA 系淡蓝辉光，
末端 alpha 渐隐；最终贴图按 RGB × alpha / 255 预乘存储（与项目 tML 管线一致）。

造型：右上缺口的圆环表示冷却扫过，12 点与 3 点两根指针，缺口处放一颗小四角星
呼应魔力图标。

执行 `python Tools/CooldownIcon/generate.py` 重建：
- Snippets/CooldownIcon.png                     游戏用贴图（预乘）
- Tools/CooldownIcon/CooldownIcon_Straight.png  straight alpha，便于普通软件查看
- Tools/CooldownIcon/Preview.png                与魔力图标并排的放大预览
"""

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT_GAME = ROOT / "Snippets" / "CooldownIcon.png"
OUT_DIR = Path(__file__).resolve().parent
REFERENCE = ROOT / "Snippets" / "MagicStarIcon.png"

SIZE = 20

GLOW = (115, 184, 234)  # 取自 MagicStarIcon 边缘反预乘后的颜色
WHITE = (255, 255, 255)

# 辉光：核心像素的 4 邻 / 对角邻 alpha
GLOW_EDGE = 84
GLOW_DIAG = 26

# 圆环（像素坐标，圆心落在像素中心）
RING_C = (9, 11)
# 缺口角度（数学坐标，y 轴向上，度）
GAP_FROM, GAP_TO = 14.0, 82.0
# 圆环核心略带淡蓝，让指针与星芒的纯白更突出（仿魔力图标星芒外段）
RING_TINT = 0.82
# 缺口两端由外向内渐隐的 (alpha, 白色程度)
RING_TAIL = [(170, 0.45), (235, 0.7)]

# 指针：从圆心出发的方向与长度（不含圆心）
HANDS = [((0, -1), 4), ((1, 0), 3)]
HAND_TIP = (215, 0.75)

# 小四角星：中心与每根星芒由内向外的 (alpha, 白色程度)
SPARK_C = (15, 4)
SPARK_ARM = [(255, 1.0), (205, 0.6), (90, 0.05)]
SPARK_DIAG = 70


# 手绘的半径 6 像素圆（12 行 13 列），每行列出该行的轮廓像素 x
RING_ROWS = {
    5: range(7, 12),
    6: (5, 6, 12, 13),
    7: (4, 14),
    **{y: (3, 15) for y in range(8, 15)},
    15: (4, 14),
    16: (5, 6, 12, 13),
    17: range(7, 12),
}


def ring_pixels():
    """返回圆环像素 [(x, y, angle)]。"""
    cx, cy = RING_C
    return [(x, y, math.degrees(math.atan2(cy - y, x - cx)) % 360)
            for y, xs in RING_ROWS.items() for x in xs]


def build_core():
    """返回 {(x, y): (alpha, 白色程度)} 的核心像素层。"""
    core = {}

    def put(x, y, a, t):
        if 0 <= x < SIZE and 0 <= y < SIZE:
            old = core.get((x, y))
            if old is None or old[0] < a:
                core[(x, y)] = (a, t)

    ring = [p for p in ring_pixels() if not (GAP_FROM <= p[2] <= GAP_TO)]
    for x, y, _ in ring:
        put(x, y, 255, RING_TINT)
    # 缺口两端渐隐：按距缺口的角度由近到远
    after_gap = sorted((p for p in ring if p[2] > GAP_TO), key=lambda p: p[2])
    before_gap = sorted((p for p in ring if p[2] < GAP_FROM), key=lambda p: -p[2])
    for seq in (after_gap, before_gap):
        for (x, y, _), (a, t) in zip(seq, RING_TAIL):
            core[(x, y)] = (a, t)

    cx, cy = RING_C
    put(cx, cy, 255, 1.0)
    for (dx, dy), length in HANDS:
        for i in range(1, length + 1):
            a, t = HAND_TIP if i == length else (255, 1.0)
            put(cx + dx * i, cy + dy * i, a, t)

    sx, sy = SPARK_C
    put(sx, sy, 255, 1.0)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        for i, (a, t) in enumerate(SPARK_ARM, 1):
            put(sx + dx * i, sy + dy * i, a, t)
    for dx, dy in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
        put(sx + dx, sy + dy, SPARK_DIAG, 0.0)
    return core


def render():
    core = build_core()
    solid = {k for k, (a, _) in core.items() if a >= 235}
    img = Image.new("RGBA", (SIZE, SIZE))
    px = img.load()
    for y in range(SIZE):
        for x in range(SIZE):
            if (x, y) in core:
                a, t = core[(x, y)]
            else:
                # 辉光只依附于接近不透明的核心像素
                edge = any((x + dx, y + dy) in solid for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
                diag = any((x + dx, y + dy) in solid for dx, dy in ((1, 1), (1, -1), (-1, 1), (-1, -1)))
                a = GLOW_EDGE if edge else GLOW_DIAG if diag else 0
                t = 0.0
            if a:
                rgb = tuple(round(GLOW[i] + (WHITE[i] - GLOW[i]) * t) for i in range(3))
                px[x, y] = (*rgb, a)
    return img


def premultiply(img):
    out = img.copy()
    p = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = p[x, y]
            p[x, y] = (r * a // 255, g * a // 255, b * a // 255, a)
    return out


def unpremultiply(img):
    out = img.copy()
    p = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = p[x, y]
            p[x, y] = (0, 0, 0, 0) if a == 0 else tuple(min(255, round(v * 255 / a)) for v in (r, g, b)) + (a,)
    return out


def on_bg(img, color, scale):
    bg = Image.new("RGBA", img.size, color)
    bg.alpha_composite(img)
    return bg.resize((img.width * scale, img.height * scale), Image.NEAREST)


def preview(straight):
    ref = unpremultiply(Image.open(REFERENCE).convert("RGBA"))
    scale = 12
    dark, light = (30, 30, 40, 255), (200, 196, 186, 255)
    tiles = [on_bg(ref, dark, scale), on_bg(straight, dark, scale),
             on_bg(ref, light, scale), on_bg(straight, light, scale)]
    w = SIZE * scale
    gap = 8
    sheet = Image.new("RGBA", (w * 4 + gap * 5, w + gap * 2 + 60), (18, 18, 24, 255))
    for i, t in enumerate(tiles):
        sheet.paste(t, (gap + i * (w + gap), gap))

    # 原尺寸文本内效果模拟
    d = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("msyh.ttc", 16)
    except OSError:
        font = ImageFont.load_default()
    y = w + gap * 2 + 12
    x = gap
    for icon, label, col in ((ref, "魔力", (76, 145, 255)), (straight, "冷却", (180, 220, 255))):
        sheet.alpha_composite(icon, (x, y))
        d.text((x + 22, y - 1), label, font=font, fill=col)
        x += 90
    return sheet


def main():
    straight = render()
    straight.save(OUT_DIR / "CooldownIcon_Straight.png")
    premultiply(straight).save(OUT_GAME)
    preview(straight).save(OUT_DIR / "Preview.png")
    print(f"wrote {OUT_GAME}")


if __name__ == "__main__":
    main()
