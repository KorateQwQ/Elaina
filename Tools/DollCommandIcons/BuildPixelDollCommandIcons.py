"""玩偶指令轮盘图标（像素版）：20x20 像素网格逐层绘制，2x 最近邻放大成 40x40。

用法：
    python Tools/DollCommandIcons/BuildPixelDollCommandIcons.py          # 只输出 output/pixel/ 预览
    python Tools/DollCommandIcons/BuildPixelDollCommandIcons.py --apply  # 确认后写入正式目录

与 SVG 版的区别：不做抗锯齿和外发光，每层自动描 1px 深色外轮廓，明暗只用 2~3 阶平涂。
20x20 网格放大 2x 后与熊头像（Kuma_Idle_1 截图 2x）的像素密度一致。
解除玩偶图标不在这里生成，沿用 SVG 版。
"""
import argparse
import math
import os

from PIL import Image

from BuildDollCommandIcons import APPLY_DIR, OUT, premultiply

GRID = 20
SCALE = 2
PIXEL_OUT = os.path.join(OUT, "pixel")

PALETTE = {
    "K": "#1b1222",  # 描边
    # 铁（镐头、锤头）
    "L": "#e4e8f4", "M": "#a3a9bd", "D": "#727891", "k": "#5c6178",
    # 木柄
    "w": "#e0a468", "W": "#a8652e", "d": "#6e3d1c",
    # 石块
    "s": "#a9a6ba", "S": "#7d7a8e", "t": "#5e5b6f", "z": "#4e4b5e", "Z": "#666379",
    # 金矿
    "y": "#ffe28a", "Y": "#e0a832", "o": "#a8701a", "r": "#fff0b0",
    # 砖墙
    "b": "#b4644f", "B": "#8e4a3c", "m": "#4a2420", "c": "#ffb088", "C": "#d8704c", "R": "#ffe0a8",
    # 取消（紫）
    "p": "#e6d9ff", "P": "#9f7ae8", "q": "#6a4bb0",
    # 剑刃 / 剑柄
    "e": "#eef1ff", "E": "#aeb6d6", "f": "#7d86ad",
    "h": "#ffa6c2", "H": "#e2507e", "j": "#a8335a",
    # 嫩芽 / 花盆
    "g": "#c3df8c", "G": "#91be65", "n": "#5f8a45",
    "a": "#f3b880", "A": "#d88456", "u": "#bb6947", "U": "#874633", "x": "#51352d",
}


class Layer:
    def __init__(self):
        self.px = {}

    def set(self, x, y, c):
        if 0 <= x < GRID and 0 <= y < GRID:
            self.px[(x, y)] = c

    def rect(self, x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.set(x, y, c)

    def each(self, pred_color):
        """pred_color(x, y) 返回颜色或 None。"""
        for y in range(GRID):
            for x in range(GRID):
                c = pred_color(x, y)
                if c:
                    self.set(x, y, c)

    def mirrored(self):
        out = Layer()
        out.px = {(GRID - 1 - x, y): c for (x, y), c in self.px.items()}
        return out

    def outline(self):
        """在本层像素外侧补一圈 4 邻接描边。"""
        ring = {}
        for (x, y) in self.px:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                q = (x + dx, y + dy)
                if q not in self.px and 0 <= q[0] < GRID and 0 <= q[1] < GRID:
                    ring[q] = "K"
        return ring


def compose(*layers):
    """后面的层盖在前面的层上；每层先铺自己的描边再铺本体，从而和下层分开。"""
    canvas = {}
    for layer in layers:
        canvas.update(layer.outline())
        canvas.update(layer.px)
    im = Image.new("RGBA", (GRID, GRID), (0, 0, 0, 0))
    for (x, y), c in canvas.items():
        h = PALETTE[c]
        im.putpixel((x, y), (int(h[1:3], 16), int(h[3:5], 16), int(h[5:7], 16), 255))
    return im.resize((GRID * SCALE, GRID * SCALE), Image.NEAREST)


# ---------- 工具部件 ----------

def handle_ne(sum0, t0, t1):
    """沿 ↗ 的三格宽木柄：x+y ∈ {sum0-1, sum0, sum0+1}，x-y ∈ [t0, t1]；左上一侧亮、右下一侧暗。"""
    layer = Layer()
    layer.each(lambda x, y: ({sum0 - 1: "w", sum0: "W", sum0 + 1: "d"}.get(x + y) if t0 <= x - y <= t1 else None))
    return layer


def pickaxe_head(axis, ay, x0, x1):
    """折角镐头：只画左半（水平臂 + 半段 ↘ 斜段），另一半沿木柄中线 x+y=axis 镜像，两臂严格对称。
    水平臂占 ay..ay+2 三行、x0..x1 列，外缘亮、内缘暗；左端在中间一行收尖。"""
    layer = Layer()
    tone = ("L", "M", "D")
    for i, c in enumerate(tone):
        layer.rect(x0, ay + i, x1, ay + i, c)
        # 斜段：x-y 依次为 k, k-1, k-2，只取轴线左上一侧
        k = x1 + 1 - ay - i
        layer.each(lambda x, y, s=k, c=c: c if x - y == s and x > x1 and x + y <= axis else None)
    layer.set(x0 - 1, ay + 1, "M")
    # 镜像：(x, y) -> (axis - y, axis - x)，明暗随之对应（外缘仍亮、内缘仍暗）
    for (x, y), c in list(layer.px.items()):
        layer.px.setdefault((axis - y, axis - x), c)
    return layer


def hammer_head(cx, cy, half_len=6, half_w=3):
    """沿 ↘ 摆放的锤头（垂直于 ↗ 木柄）：↗ 一侧亮、↙ 一侧暗，中间一道柄箍。"""
    def color(x, y):
        u, v = (x - cx) + (y - cy), (x - cx) - (y - cy)
        if abs(u) > half_len or abs(v) > half_w:
            return None
        if abs(u) <= 1:
            return "k" if v < half_w else "D"
        if v >= half_w - 1:
            return "L"
        if v <= -(half_w - 1):
            return "D"
        return "M"
    layer = Layer()
    layer.each(color)
    return layer


# ---------- 图标 ----------

def icon_mine():
    stone = Layer()
    stone.rect(12, 14, 18, 18, "S")
    stone.rect(12, 14, 18, 14, "s")
    stone.rect(12, 14, 12, 18, "s")
    stone.rect(12, 18, 18, 18, "t")
    stone.rect(18, 14, 18, 18, "t")
    for (x, y, c) in ((14, 16, "y"), (15, 16, "Y"), (14, 17, "Y"), (16, 15, "Y"), (17, 16, "y"), (16, 17, "t")):
        stone.set(x, y, c)
    return compose(stone, handle_ne(18, -16, 7), pickaxe_head(18, 3, 7, 11))


def ore_grid():
    """3x3 方块，同类金矿连成一组：组内缝隙并成金色，组外一圈亮边。"""
    ore = {(0, 0), (1, 0), (1, 1), (2, 1), (2, 2)}
    ox = oy = 8
    layer = Layer()
    ore_px = set()
    for j in range(3):
        for i in range(3):
            x0, y0 = ox + 4 * i, oy + 4 * j
            if (i, j) in ore:
                layer.rect(x0, y0, x0 + 2, y0 + 2, "Y")
                layer.rect(x0, y0, x0 + 2, y0, "y")
                layer.set(x0 + 2, y0 + 2, "o")
                ore_px |= {(x0 + a, y0 + b) for a in range(3) for b in range(3)}
            else:
                layer.rect(x0, y0, x0 + 2, y0 + 2, "z")
                layer.rect(x0, y0, x0 + 2, y0, "Z")
    for y in range(oy - 1, oy + 12):
        for x in range(ox - 1, ox + 12):
            if (x, y) in ore_px:
                continue
            if ((x - 1, y) in ore_px and (x + 1, y) in ore_px) or ((x, y - 1) in ore_px and (x, y + 1) in ore_px):
                layer.set(x, y, "Y")
            elif any((x + dx, y + dy) in ore_px for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                layer.set(x, y, "r")
            elif ox <= x < ox + 11 and oy <= y < oy + 11 and (x, y) not in layer.px:
                layer.set(x, y, "K")
    return layer


def icon_mine_chain():
    return compose(ore_grid(), handle_ne(11, -9, 3), pickaxe_head(11, 1, 4, 5))


def brick_wall(x0, y0, x1, y1, lit=None):
    """3 像素一行（亮面 / 砖体 / 灰缝），砖宽 3 + 1 道竖缝，隔行错开。
    lit(bx, by) 为真的砖块整组提亮，组外缝隙换成亮边。"""
    layer = Layer()
    lit_px = set()
    for y in range(y0, y1 + 1):
        row, k = divmod(y - y0, 3)
        for x in range(x0, x1 + 1):
            col = (x - x0 + (2 if row % 2 else 0))
            if k == 2 or col % 4 == 3:
                layer.set(x, y, "m")
                continue
            bx = col // 4
            on = lit and lit(bx, row)
            if on:
                lit_px.add((x, y))
            layer.set(x, y, ("c" if on else "b") if k == 0 else ("C" if on else "B"))
    if lit:
        for (x, y), c in list(layer.px.items()):
            if c != "m":
                continue
            if ((x - 1, y) in lit_px and (x + 1, y) in lit_px) or ((x, y - 1) in lit_px and (x, y + 1) in lit_px):
                layer.set(x, y, "C")
            elif any((x + dx, y + dy) in lit_px for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                layer.set(x, y, "R")
    return layer


def hammer():
    return handle_ne(19, -15, 1), hammer_head(10, 9)


def icon_wall():
    wall = brick_wall(9, 1, 18, 18)
    for (x, y) in ((11, 4), (12, 5), (12, 6), (13, 7), (14, 7), (13, 11), (14, 12), (14, 13), (15, 14)):
        wall.set(x, y, "K")
    return compose(wall, *hammer())


def icon_wall_chain():
    lit = lambda bx, row: (bx, row) in {(1, 1), (2, 1), (1, 2), (2, 2), (3, 2), (2, 3), (1, 3), (2, 4)}
    return compose(brick_wall(9, 1, 18, 18, lit), *hammer())


def sword():
    """↗ 方向的剑：剑刃沿 x+y=19，剑首在左下角。"""
    layer = Layer()
    blade = {-1: "e", 0: "E", 1: "f"}

    def color(x, y):
        s, t = x + y - 19, x - y
        if -8 <= t <= 13 and s in blade and (s == 0 or t <= 11):
            return blade[s]
        if t in (-9, -10) and abs(s) <= 3:       # 护手
            return "h" if s < 0 else "H"
        if -13 <= t <= -11 and s in (0, 1):      # 握柄
            return "j"
        if t in (-14, -15) and abs(s) <= 1:      # 剑首
            return "h" if s < 0 else "H"
        return None
    layer.each(color)
    return layer


def icon_attack():
    a = sword()
    return compose(a.mirrored(), a)


def icon_cancel():
    ring, cross = Layer(), Layer()
    c = 9.5

    def ring_color(x, y):
        dx, dy = x + .5 - c - .5, y + .5 - c - .5
        d = (dx * dx + dy * dy) ** .5
        if not 6.0 <= d <= 8.2:
            return None
        if d >= 7.4 and dx + dy < -2:
            return "p"
        if d <= 6.8 and dx + dy > 2:
            return "q"
        return "P"
    ring.each(ring_color)

    def cross_color(x, y):
        if max(abs(x - c), abs(y - c)) > 4.1:
            return None
        if x - y in (-1, 0, 1) or x + y in (18, 19, 20):
            return "p" if (x - y == -1 or x + y == 18) else "P"
        return None
    cross.each(cross_color)
    return compose(ring, cross)


def leaf(layer, base, tip, half_w, bend):
    """杏仁形嫩叶：两头尖、中间鼓，叶尖略向上翘；下沿压一道暗边，中间一道浅色叶脉。"""
    bx, by = base
    dx, dy = tip[0] - bx, tip[1] - by
    length = (dx * dx + dy * dy) ** .5
    nx, ny = dy / length, -dx / length
    if ny > 0:
        nx, ny = -nx, -ny  # 法线取朝上的一侧

    def color(x, y):
        px, py = x - bx, y - by
        u = (px * dx + py * dy) / (length * length)
        if not 0 <= u <= 1:
            return None
        v = px * nx + py * ny - bend * u * u  # 相对于上翘中线的偏移，正值朝上
        w = half_w * u ** .5 * (1 - u) / .385  # 根部圆润、叶尖收成尖角，最宽处在 1/3
        if abs(v) > w:
            return None
        if abs(v) < .5 and .2 < u < .8:
            return "g"
        return "n" if v < -(w - 1) else "G"
    layer.each(color)


ICONS = {
    "DollCommand_Attack": icon_attack,
    "DollCommand_Mine": icon_mine,
    "DollCommand_BreakWall": icon_wall,
    "DollCommand_MineChain": icon_mine_chain,
    "DollCommand_BreakWallChain": icon_wall_chain,
    "DollCommand_Cancel": icon_cancel,
}


def comparison(names, old, new):
    """上：旧 SVG 版 6x；中：像素版 6x；下：游戏内尺寸（50px 线性缩放）对比。"""
    scale, pad, size = 6, 14, 40
    bg = (22, 18, 31, 255)  # 轮盘扇区底色 SkillWheelDrawing.BandColor
    cell = size * scale + pad
    w = len(names) * cell + pad
    h = 2 * size * scale + 4 * pad + 50
    sheet = Image.new("RGBA", (w, h), bg)
    for i, n in enumerate(names):
        x = pad + i * cell
        sheet.alpha_composite(old[n].resize((size * scale,) * 2, Image.NEAREST), (x, pad))
        sheet.alpha_composite(new[n].resize((size * scale,) * 2, Image.NEAREST), (x, size * scale + pad * 2))
        y = 2 * size * scale + pad * 3
        sheet.alpha_composite(old[n].resize((50, 50), Image.BILINEAR), (x + 30, y))
        sheet.alpha_composite(new[n].resize((50, 50), Image.BILINEAR), (x + 30 + 80, y))
    return sheet


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    os.makedirs(PIXEL_OUT, exist_ok=True)
    new, old = {}, {}
    for name, fn in ICONS.items():
        im = fn()
        im.save(os.path.join(PIXEL_OUT, name + ".png"))
        new[name] = im
        old_path = os.path.join(OUT, name + ".png")
        old[name] = Image.open(old_path).convert("RGBA") if os.path.exists(old_path) else Image.new("RGBA", im.size)
        if args.apply:
            premultiply(im).save(os.path.join(APPLY_DIR, name + ".png"))
    comparison(list(ICONS), old, new).save(os.path.join(PIXEL_OUT, "_compare.png"))
    print("written", len(new), "icons", "(applied)" if args.apply else "(preview only)")


if __name__ == "__main__":
    main()
