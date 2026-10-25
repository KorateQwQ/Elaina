"""右键交互提示图标生成器（回城魔法阵悬停提示用）。

与 Snippets/MagicStarIcon.png、CooldownIcon.png 同风格：20×20，白色核心线条 + #72B8EA 系淡蓝辉光；
最终贴图按 RGB × alpha / 255 预乘存储（与项目 tML 管线一致）。

造型：鼠标轮廓，右键填实高亮表示「右键」，右上角附点击标记。

执行 `python Tools/RightClickIcon/generate.py` 只出预览到 Tools/RightClickIcon/output/；
确认方案后 `python Tools/RightClickIcon/generate.py --apply <方案名>` 写入：
- Snippets/RightClickIcon.png                       游戏用贴图（预乘）
- Tools/RightClickIcon/RightClickIcon_Straight.png  straight alpha，便于普通软件查看
"""

import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT_GAME = ROOT / "Snippets" / "RightClickIcon.png"
TOOL_DIR = Path(__file__).resolve().parent
OUT_DIR = TOOL_DIR / "output"
REFERENCES = [ROOT / "Snippets" / "MagicStarIcon.png", ROOT / "Snippets" / "CooldownIcon.png"]

SIZE = 20
GLOW = (115, 184, 234)
WHITE = (255, 255, 255)
GLOW_EDGE = 84
GLOW_DIAG = 26
OUTLINE_TINT = 0.82

# '#' 轮廓，'R' 右键填实；中线把左右键分开
MOUSE = [
    "..#####..",
    ".#..#RR#.",
    "#...#RRR#",
    "#...#RRR#",
    "#...#RRR#",
    "#########",
    "#.......#",
    "#.......#",
    "#.......#",
    "#.......#",
    "#.......#",
    "#.......#",
    ".#.....#.",
    "..#####..",
]


def mouse(core, ox, oy):
    for y, row in enumerate(MOUSE):
        for x, c in enumerate(row):
            if c == "#":
                core[(ox + x, oy + y)] = (255, OUTLINE_TINT)
            elif c == "R":
                core[(ox + x, oy + y)] = (255, 1.0)


def put(core, x, y, a, t):
    if 0 <= x < SIZE and 0 <= y < SIZE and core.get((x, y), (0, 0))[0] < a:
        core[(x, y)] = (a, t)


def spark(core, sx, sy):
    """小四角星，呼应魔力图标。"""
    put(core, sx, sy, 255, 1.0)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        for i, (a, t) in enumerate([(255, 1.0), (205, 0.6), (90, 0.05)], 1):
            put(core, sx + dx * i, sy + dy * i, a, t)
    for dx, dy in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
        put(core, sx + dx, sy + dy, 70, 0.0)


def rays(core):
    """右键右上方三道短点击线，由内向外渐隐。"""
    for pts in ([(13, 3), (14, 2)], [(14, 5), (15, 5)], [(11, 2), (11, 1)]):
        for (x, y), (a, t) in zip(pts, [(255, 1.0), (150, 0.4)]):
            put(core, x, y, a, t)


def variant_plain():
    core = {}
    mouse(core, 5, 3)
    return core


def variant_spark():
    core = {}
    mouse(core, 3, 4)
    spark(core, 16, 3)
    return core


def variant_rays():
    core = {}
    mouse(core, 3, 4)
    rays(core)
    return core


VARIANTS = {"plain": variant_plain, "spark": variant_spark, "rays": variant_rays}


def outside(core):
    """从画布边缘泛洪得到轮廓外的像素；辉光只画在外侧，保持鼠标内部干净。"""
    seen, stack = set(), [(x, y) for x in range(SIZE) for y in (0, SIZE - 1)] +         [(x, y) for y in range(SIZE) for x in (0, SIZE - 1)]
    while stack:
        x, y = stack.pop()
        if not (0 <= x < SIZE and 0 <= y < SIZE) or (x, y) in seen or (x, y) in core:
            continue
        seen.add((x, y))
        stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    return seen


def render(core):
    solid = {k for k, (a, _) in core.items() if a >= 235}
    out = outside(core)
    img = Image.new("RGBA", (SIZE, SIZE))
    px = img.load()
    for y in range(SIZE):
        for x in range(SIZE):
            if (x, y) in core:
                a, t = core[(x, y)]
            elif (x, y) not in out:
                a, t = 0, 0.0
            else:
                edge = any((x + dx, y + dy) in solid for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
                diag = any((x + dx, y + dy) in solid for dx, dy in ((1, 1), (1, -1), (-1, 1), (-1, -1)))
                a = GLOW_EDGE if edge else GLOW_DIAG if diag else 0
                t = 0.0
            if a:
                px[x, y] = (*(round(GLOW[i] + (WHITE[i] - GLOW[i]) * t) for i in range(3)), a)
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


def preview(icons):
    """上排：参考图标 + 各方案 12× 放大；下排：模拟游戏内悬停文字（原尺寸 / 2×）。"""
    refs = [unpremultiply(Image.open(p).convert("RGBA")) for p in REFERENCES]
    scale, gap = 12, 10
    w = SIZE * scale
    tiles = refs + [icon for _, icon in icons]
    try:
        font = ImageFont.truetype("msyh.ttc", 16)
        label_font = ImageFont.truetype("msyh.ttc", 18)
    except OSError:
        font = label_font = ImageFont.load_default()
    sheet = Image.new("RGBA", (gap + len(tiles) * (w + gap), w + gap * 2 + 40 + 120), (18, 18, 24, 255))
    d = ImageDraw.Draw(sheet)
    names = ["参考:魔力", "参考:冷却"] + [n for n, _ in icons]
    for i, (t, n) in enumerate(zip(tiles, names)):
        sheet.paste(on_bg(t, (30, 30, 40, 255), scale), (gap + i * (w + gap), gap))
        d.text((gap + i * (w + gap), w + gap + 6), n, font=label_font, fill=(220, 220, 230))

    # 悬停提示模拟：图标 + 文字，按 16px 显示（klicon/16），再 2× 放大一份便于看清
    line_y = w + gap * 2 + 44
    strip = Image.new("RGBA", (len(icons) * 200, 26), (18, 18, 24, 255))
    sd = ImageDraw.Draw(strip)
    for i, (n, icon) in enumerate(icons):
        x = 6 + i * 200
        strip.alpha_composite(icon.resize((16, 16), Image.BILINEAR), (x, 5))
        sd.text((x + 19, 2), "使用回城魔法阵", font=font, fill=(255, 255, 255))
    sheet.alpha_composite(strip, (gap, line_y))
    sheet.alpha_composite(strip.resize((strip.width * 2, strip.height * 2), Image.NEAREST), (gap, line_y + 36))
    return sheet


def main():
    icons = [(name, render(build())) for name, build in VARIANTS.items()]
    if len(sys.argv) >= 3 and sys.argv[1] == "--apply":
        icon = dict(icons)[sys.argv[2]]
        icon.save(TOOL_DIR / "RightClickIcon_Straight.png")
        premultiply(icon).save(OUT_GAME)
        print(f"wrote {OUT_GAME}")
        return
    OUT_DIR.mkdir(exist_ok=True)
    for name, icon in icons:
        icon.save(OUT_DIR / f"RightClickIcon_{name}.png")
    preview(icons).save(OUT_DIR / "Preview.png")
    print(f"wrote {OUT_DIR / 'Preview.png'}")


if __name__ == "__main__":
    main()
