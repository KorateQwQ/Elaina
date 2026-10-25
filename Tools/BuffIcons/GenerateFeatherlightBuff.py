"""轻羽药水 buff 图标：内容层单独绘制（透明底），定稿写入时再合成到统一 buff 背景上。

Python + Pillow。羽毛在沿羽轴的 (u, v) 坐标里定义轮廓，逐像素中心采样后统一加深色外描边；
不做抗锯齿，所有可见像素不透明。预览与 --apply 写入流程见 buff_icon.py。
"""
import math
from buff_icon import ROOT, SIZE, outline, run, to_image

TARGET = ROOT / 'ElainaModAlchemy/Buffs/FeatherlightBuff.png'

PALETTE = {
    'O': '#1f2a52',  # 外描边：深靛蓝，浅蓝底上比纯黑柔和
    'H': '#fffbed',  # 受光羽片
    'P': '#f2ecdb',  # 羽片主色（与药水里的象牙白羽毛一致）
    'Q': '#c9c8b7',  # 背光羽片
    'B': '#a2ddee',  # 羽缘浅蓝（药水药液色）
    'M': '#70b8da',  # 羽缘中蓝
    'R': '#7b5761',  # 羽轴
}


def feather(variant):
    """返回 {(x, y): key}。羽根在左下，羽尖朝右上。"""
    cx, cy = 15.5, 16.0
    ang = math.radians(variant.get('angle', -52))  # 羽轴方向（屏幕坐标 y 向下）
    ax, ay = math.cos(ang), math.sin(ang)
    nx, ny = -ay, ax  # v>0 为羽轴右下侧
    u0, u1, vane0 = -13.5, 13.0, -7.0
    bend = variant.get('bend', 0.018)
    cells = {}
    for y in range(SIZE):
        for x in range(SIZE):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            u = dx * ax + dy * ay
            v = dx * nx + dy * ny
            # 羽轴向右下微弯，尖端略垂
            v -= bend * (u + 2) ** 2 - bend * 4
            if u < u0 or u > u1:
                continue
            # 羽轴：贯穿全长，羽根段略粗
            if abs(v) < (0.75 if u < vane0 else 0.55) and u < u1 - 1.5:
                cells[(x, y)] = 'R'
                continue
            if u < vane0:
                continue
            t = (u - vane0) / (u1 - vane0)
            # 左上侧羽片窄、右下侧宽，尖端圆收
            wmax = variant.get('wl', 3.4) if v < 0 else variant.get('wr', 4.3)
            hw = wmax * math.sin(math.pi * min(1.0, t ** 0.75)) ** 0.7
            if t < 0.08:
                hw *= t / 0.08
            if abs(v) > hw:
                continue
            # 羽枝缺口：斜向羽尖的 V 形切口
            cut = False
            for side, uc in variant['cuts']:
                if (v > 0) == (side > 0) and abs(v) > hw * 0.3:
                    if abs(u - (uc + 0.8 * abs(v))) < 0.75:
                        cut = True
            if cut:
                continue
            edge = abs(v) / max(hw, 0.01)
            if v < 0:
                key = 'H' if edge < 0.55 else 'P'
            else:
                key = 'P' if edge < 0.45 else 'Q'
            # 羽枝纹：右下侧每隔几格一道斜向暗纹
            if variant.get('barbs') and v > 0 and 0.35 < edge < 0.85 and (u - 0.8 * v) % 3.2 < 0.7:
                key = 'Q'
            if variant.get('blue_tip') and edge > 0.72:
                key = 'B' if v < 0 else 'M'
            cells[(x, y)] = key
    return cells


CUTS = [(1, -0.5), (1, 6.0), (-1, 3.0)]
VARIANTS = {
    'A': dict(cuts=CUTS, barbs=True),
    'B': dict(cuts=CUTS, barbs=True, blue_tip=True),
}


def render(variant):
    return to_image(outline(feather(variant)), PALETTE)


if __name__ == '__main__':
    run('FeatherlightBuff', {k: render(v) for k, v in VARIANTS.items()}, TARGET)
