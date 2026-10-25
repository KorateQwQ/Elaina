"""月露合剂 buff 图标：金色月牙与四芒星。

Python + Pillow。月牙是两圆相减，四芒星是内凹超椭圆，逐像素中心采样后统一加深紫外描边；
配色取自月露合剂物品像素图。不做抗锯齿。预览与 --apply 写入流程见 buff_icon.py。
"""
import math
from buff_icon import ROOT, SIZE, outline, run, to_image

TARGET = ROOT / 'ElainaModAlchemy/Buffs/MoonDewElixirBuff.png'

PALETTE = {
    'O': '#2c2560',  # 外描边：瓶内最深紫
    'C': '#fff3c4',  # 亮面
    'G': '#efc979',  # 暗面
    'W': '#fff5f1',  # 星心高光
}


def crescent(cx, cy, r, ox, oy, r2):
    """两圆相减的月牙；靠缺口一侧为暗面。"""
    cells = {}
    for y in range(SIZE):
        for x in range(SIZE):
            px, py = x + 0.5, y + 0.5
            d1 = math.hypot(px - cx, py - cy)
            d2 = math.hypot(px - cx - ox, py - cy - oy)
            if d1 <= r and d2 > r2:
                cells[(x, y)] = 'G' if d2 < r2 + 1.0 else 'C'
    return cells


def star(cx, cy, r, p=0.55):
    """四芒星：|dx|^p + |dy|^p <= r^p，p<1 时四边内凹；芒尖暗、中心亮。

    中心须在像素中心（x.5），否则 1 像素宽的芒臂采样不到，会塌成圆点。
    """
    cells = {}
    for y in range(SIZE):
        for x in range(SIZE):
            dx, dy = abs(x + 0.5 - cx), abs(y + 0.5 - cy)
            k = (dx ** p + dy ** p) / r ** p
            if k <= 1.0:
                cells[(x, y)] = 'W' if max(dx, dy) < 0.6 else 'C' if k < 0.62 else 'G'
    return cells


def merge(*layers):
    """各层先各自描边再按顺序叠放，后层盖住前层的描边。"""
    cells = {}
    for layer in layers:
        cells.update(outline(layer))
    return cells


def variant_c():
    """一弯大月牙抱一颗大四芒星。"""
    moon = crescent(14.0, 16.5, 10.6, 5.2, -4.0, 8.9)
    return to_image(merge(moon, star(21.5, 10.5, 5.6)), PALETTE)


def variant_d():
    """月牙稍小，配一大两小三颗星。"""
    moon = crescent(13.0, 17.5, 9.0, 4.6, -3.4, 7.6)
    return to_image(merge(moon, star(21.5, 10.5, 4.6), star(25.5, 18.5, 2.6), star(9.5, 6.5, 2.6)), PALETTE)


if __name__ == '__main__':
    run('MoonDewElixirBuff', {'C': variant_c(), 'D': variant_d()}, TARGET)
