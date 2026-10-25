"""止痛药 buff 图标：胶囊、爱心十字或圆角十字，对应"持续恢复生命"。

Python + Pillow。形状用隐式函数逐像素中心采样，按左上光源分档上色，再统一加深红褐外描边；
配色取自止痛药物品像素图（橙色药液与奶油十字）。不做抗锯齿。预览与 --apply 写入流程见 buff_icon.py。
"""
import math
from buff_icon import ROOT, SIZE, outline, run, to_image

TARGET = ROOT / 'ElainaModAlchemy/Buffs/PainkillerBuff.png'

PALETTE = {
    'O': '#4a1a1c',  # 外描边
    'D': '#b9483a',  # 橙红暗部
    'M': '#e2733f',  # 橙色主色
    'L': '#f5a052',  # 橙色亮部
    'Y': '#ffd08a',  # 橙色高光
    'C': '#fff4dc',  # 奶油
    'E': '#e1bd8d',  # 奶油暗部
    'W': '#ffffff',  # 镜面高光
}


def shade(px, py, cx, cy, r, keys):
    """按到左上光点的距离分档；keys 由亮到暗。"""
    k = math.hypot(px - (cx - 0.4 * r), py - (cy - 0.4 * r)) / r
    for limit, key in zip((0.45, 0.95, 1.35), keys):
        if k < limit:
            return key
    return keys[-1]


def capsule():
    """斜放胶囊：左下橙色半、右上奶油半，接缝一道暗线。"""
    x1, y1, x2, y2, r = 10.0, 22.0, 22.0, 10.0, 5.0
    ux, uy = (x2 - x1), (y2 - y1)
    length = math.hypot(ux, uy)
    ux, uy = ux / length, uy / length
    cells = {}
    for y in range(SIZE):
        for x in range(SIZE):
            px, py = x + 0.5, y + 0.5
            t = (px - x1) * ux + (py - y1) * uy
            tc = max(0.0, min(length, t))
            v = (px - x1) * -uy + (py - y1) * ux  # v<0 为左上受光侧
            if math.hypot(px - x1 - ux * tc, py - y1 - uy * tc) > r:
                continue
            if abs(t - length / 2) < 0.6:
                cells[(x, y)] = 'O'
                continue
            orange = t < length / 2
            lit = v / r
            if orange:
                key = 'Y' if lit < -0.45 else 'L' if lit < -0.05 else 'M' if lit < 0.55 else 'D'
            else:
                key = 'W' if lit < -0.45 else 'C' if lit < 0.4 else 'E'
            cells[(x, y)] = key
    return cells


def heart(cx, cy, s):
    """隐式爱心 (x²+y²-1)³ - x²y³ <= 0，s 为缩放。"""
    cells = {}
    for y in range(SIZE):
        for x in range(SIZE):
            px, py = x + 0.5, y + 0.5
            hx, hy = (px - cx) / s, -(py - cy) / s
            if (hx * hx + hy * hy - 1) ** 3 - hx * hx * hy ** 3 <= 0:
                cells[(x, y)] = shade(px, py, cx, cy - 0.3 * s, 1.25 * s, 'YLMD')
    return cells


def plus(cx, cy, arm, half, round_=1.2):
    """圆角十字：两条圆角矩形相并。arm 为臂长半径，half 为臂宽半径。"""
    def rrect(px, py, hx, hy):
        qx, qy = abs(px - cx) - (hx - round_), abs(py - cy) - (hy - round_)
        return math.hypot(max(qx, 0), max(qy, 0)) <= round_
    cells = {}
    for y in range(SIZE):
        for x in range(SIZE):
            px, py = x + 0.5, y + 0.5
            if rrect(px, py, arm, half) or rrect(px, py, half, arm):
                cells[(x, y)] = (px, py)
    return cells


def variant_a():
    return to_image(outline(capsule()), PALETTE)


def variant_b():
    cells = outline(heart(16.0, 16.0, 9.2))
    # 心口一枚奶油十字，右下描暗边
    for (x, y) in plus(16.5, 17.5, 3.6, 1.6, 0.1):
        cells[(x, y)] = 'C'
    for (x, y) in plus(17.5, 18.5, 3.6, 1.6, 0.1):
        if cells.get((x, y)) != 'C' and (x, y) in cells and cells[(x, y)] != 'O':
            cells[(x, y)] = 'D'
    cells[(11, 11)] = 'W'
    cells[(10, 12)] = 'W'
    return to_image(cells, PALETTE)


def variant_c():
    cross = plus(15.5, 15.5, 9.6, 3.6, 1.0)
    cells = {}
    for p, (px, py) in cross.items():
        cells[p] = shade(px, py, 15.5, 15.5, 11.5, 'YLMD')
    cells = outline(cells)
    # 中心奶油小十字，呼应瓶身
    for p in plus(15.5, 15.5, 3.2, 1.2, 0.1):
        cells[p] = 'C'
    return to_image(cells, PALETTE)


if __name__ == '__main__':
    run('PainkillerBuff', {'A': variant_a(), 'B': variant_b(), 'C': variant_c()}, TARGET)
