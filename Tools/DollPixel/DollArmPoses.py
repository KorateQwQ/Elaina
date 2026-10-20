"""玩偶控制 · 基于站立帧 wanou1 的挥镐手臂帧。

小熊脸朝左（行走帧也是向左走），所以"往前挥"是往左：手臂从头顶举起，经过身前，挥到左下方。
把站立帧右侧（画面右边、近侧）那条手臂抠掉，在肩膀处按角度重画，手臂压在身体上的边缘用描边色勾出，
再整体套 OutlineDollFrames 的外描边。角度以水平朝右为 0°、逆时针为正（90° 朝上，180° 朝左）。
只输出预览到 Tools/DollPixel/output；预览里额外画了一把示意镐子，正式帧里不含镐子。
"""

import math
from pathlib import Path

from PIL import Image

from OutlineDollFrames import add_outline, sheet

OUT = Path(__file__).resolve().parent / "output"

COLORS = {"M": (163, 147, 103), "D": (121, 104, 62), "L": (201, 185, 148), "E": (60, 46, 11)}

# 未描边的 wanou1 原图
STAND = [
    "................................",
    ".........MMM......MMMD..........",
    "........MMMMM....MMMMMD.........",
    "........MDDMMMMMMMMDDMD.........",
    "........MDDMMMMMMMMDDMD.........",
    "........MMMMMMMMMMMMMMD.........",
    ".........MMMMMMMMMMMDD..........",
    ".........MMEMMMMEMMMMD..........",
    ".........MMEMMMMEMMMMD..........",
    ".........MMMLLLMMMMMMD..........",
    ".........MMLDDLLMMMMMD..........",
    ".........DMLLLLLMMMMD...........",
    "..........DMLLLMMMDD............",
    "...........DDDDDDDMM............",
    "...........MDMMMMDMMM...........",
    "..........MMDLLLMMDMMM..........",
    "..........MDLLLLLMDMMMM.........",
    ".........MMDLLLLLMDMMMM.........",
    ".........MMDLLLLLMMDMMMM........",
    "........MMMDLLLLLMMDMMMM........",
    "........MMMDMLLLMMMDMMMMM.......",
    "........MMMDDMMMMDDDMMMMM.......",
    ".........MMDMDDDDMMM.MMM........",
    "............MMMDMMMM............",
    "............MMMDMMMMM...........",
    ".............MMDDMMMM...........",
    ".............MMMDMMMM...........",
    ".............MMMDMMMMM..........",
    ".............DMMDDMMMM..........",
    ".............DDMDDDMMM..........",
    "............MMMMDMMMMM..........",
    "............MMM..MMMM...........",
]

# 原右臂在每行的起始列（该列之前是身体，身体右缘那条暗线保留下来当身体的暗边）
ARM_FROM = {13: 18, 14: 18, 15: 19, 16: 19, 17: 19, 18: 20, 19: 20, 20: 20, 21: 20, 22: 21}

# 手绘的手臂：每行 (起始列, 结束列)（含两端）。前面一段是手掌（圆的 5x5），后面是手臂，接到肩膀。
# shade: 手臂背光的一侧（该方向上不是手臂的像素涂暗色）；hand: 手心坐标；angle: 手臂朝向，给示意镐子用
POSES = {
    "Swing1": dict(  # 举过头（在脑袋右边举起）
        rows={1: (23, 25), 2: (22, 26), 3: (22, 26), 4: (22, 26), 5: (23, 25),
              6: (22, 24), 7: (22, 24), 8: (21, 23), 9: (21, 23), 10: (21, 23),
              11: (20, 22), 12: (20, 22), 13: (19, 21), 14: (18, 21), 15: (17, 20)},
        shade=(1, 0), hand=(24.5, 3.5), angle=78),
    "Swing2": dict(  # 斜前上，从下巴底下伸到脸前
        rows={8: (7, 9), 9: (6, 10), 10: (6, 11), 11: (6, 13), 12: (7, 15),
              13: (12, 17), 14: (14, 18), 15: (16, 18)},
        shade=(0, 1), hand=(8.5, 10.5), angle=152),
    "Swing3": dict(  # 平伸向前
        rows={14: (5, 7), 15: (4, 17), 16: (4, 17), 17: (4, 17), 18: (5, 7)},
        shade=(0, 1), hand=(6.5, 16.5), angle=180),
    "Swing4": dict(  # 斜前下（挖下去）
        rows={16: (14, 17), 17: (13, 16), 18: (12, 15), 19: (11, 14), 20: (7, 13),
              21: (6, 12), 22: (6, 10), 23: (6, 10), 24: (7, 9)},
        shade=(1, 0), hand=(8.5, 22.5), angle=225),
}
PAW_ROWS = {"Swing1": range(1, 6), "Swing2": range(8, 13), "Swing3": range(14, 19), "Swing4": range(20, 25)}
PAW_COLS = {"Swing1": range(22, 27), "Swing2": range(6, 11), "Swing3": range(4, 9), "Swing4": range(6, 11)}


def remove_arm():
    g = [list(r) for r in STAND]
    for y, x0 in ARM_FROM.items():
        for x in range(x0, 32):
            g[y][x] = "."
    return g


def draw_arm(g, name):
    pose = POSES[name]
    body = {(x, y) for y in range(32) for x in range(32) if g[y][x] != "."}
    arm = {(x, y) for y, (x0, x1) in pose["rows"].items() for x in range(x0, x1 + 1)}
    sx, sy = pose["shade"]
    for x, y in arm:
        g[y][x] = "D" if (x + sx, y + sy) not in arm else "M"
    # 手臂压在身体/头上时，紧挨手臂的那圈身体像素涂成描边色，手臂本身保持完整
    for x, y in body - arm:
        if any((x + dx, y + dy) in arm for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1))):
            g[y][x] = "E"
    paw = {(x, y) for x, y in arm if y in PAW_ROWS[name] and x in PAW_COLS[name]}
    return g, pose["hand"], math.radians(pose["angle"]), paw


def render(g):
    im = Image.new("RGBA", (32, 32))
    for y, row in enumerate(g):
        for x, c in enumerate(row):
            if c != ".":
                im.putpixel((x, y), COLORS[c] + (255,))
    return im


def pickaxe_layer(hand, a, scale, pad):
    """示意镐子（仅预览）：镐柄顺着手臂方向从手心伸出，镐头垂直于镐柄。"""
    from PIL import ImageDraw
    hx, hy = hand[0] + pad, hand[1] + pad
    dx, dy = math.cos(a), -math.sin(a)
    ex, ey = hx + dx * 10, hy + dy * 10
    px, py = -dy, dx
    layer = Image.new("RGBA", ((32 + pad * 2) * scale, (32 + pad * 2) * scale))
    d = ImageDraw.Draw(layer)
    S = lambda x, y: (x * scale, y * scale)
    d.line([S(hx - dx * 1.5, hy - dy * 1.5), S(ex, ey)], fill=(52, 38, 14), width=int(scale * 2.2))
    d.line([S(hx - dx * 1.5, hy - dy * 1.5), S(ex, ey)], fill=(140, 96, 52), width=int(scale * 1.0))
    head = [S(ex + px * 5 - dx * 2, ey + py * 5 - dy * 2), S(ex + px * 2, ey + py * 2), S(ex, ey),
            S(ex - px * 2, ey - py * 2), S(ex - px * 5 - dx * 2, ey - py * 5 - dy * 2)]
    d.line(head, fill=(52, 38, 14), width=int(scale * 2.4), joint="curve")
    d.line(head, fill=(176, 120, 82), width=int(scale * 1.1), joint="curve")
    return layer


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for old in OUT.glob("Doll_Arm*"):
        old.unlink()
    stand = add_outline(render([list(r) for r in STAND]))
    poses, tools = [], []
    scale, pad = 10, 12
    for name in POSES:
        g, hand, a, paw = draw_arm(remove_arm(), name)
        im = add_outline(render(g))
        im.save(OUT / f"Doll_{name}.png")
        poses.append(im)
        tools.append((pickaxe_layer(hand, a, scale, pad), paw))
        print(name, "手心约", tuple(round(v, 1) for v in hand))

    bg = (88, 84, 112, 255)
    sheet(poses + [stand], scale, bg).save(OUT / "Doll_Swing_x10.png")

    # 带示意镐子的挥动 GIF：镐子盖在身体上，再把手掌盖回镐柄上，相当于"手握着柄"
    frames = []
    for im, (tool, paw) in zip(poses, tools):
        c = Image.new("RGBA", ((32 + pad * 2) * scale, (32 + pad * 2) * scale), bg)
        c.alpha_composite(im.resize((32 * scale, 32 * scale), Image.NEAREST), (pad * scale, pad * scale))
        c.alpha_composite(tool)
        hand = Image.new("RGBA", (32, 32))
        for x, y in paw:
            hand.putpixel((x, y), im.getpixel((x, y)))
        c.alpha_composite(add_outline(hand).resize((32 * scale, 32 * scale), Image.NEAREST),
                          (pad * scale, pad * scale))
        frames.append(c)
    frames[0].save(OUT / "Doll_SwingPickaxe_x10.gif", save_all=True, append_images=frames[1:],
                   duration=[220, 70, 70, 260], loop=0)
    strip = Image.new("RGBA", (frames[0].width * 4, frames[0].height), bg)
    for i, f in enumerate(frames):
        strip.alpha_composite(f, (i * f.width, 0))
    strip.resize((strip.width // 2, strip.height // 2), Image.NEAREST).save(OUT / "Doll_SwingPickaxe_Strip.png")


if __name__ == "__main__":
    main()
