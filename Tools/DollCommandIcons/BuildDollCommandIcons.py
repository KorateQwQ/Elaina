"""玩偶指令轮盘图标：先写 SVG（svg/），再用 resvg 栅格化成 40x40 PNG。

用法：
    python Tools/DollCommandIcons/BuildDollCommandIcons.py          # 只输出 svg/ 与 output/ 预览
    python Tools/DollCommandIcons/BuildDollCommandIcons.py --apply  # 确认后写入正式目录（只写解除玩偶）

除解除玩偶外的指令图标已改用像素版（BuildPixelDollCommandIcons.py），这里 --apply 不再覆盖它们。

画法参考泰拉瑞亚工具贴图：深色描边 + 分面明暗；外层再加一圈指令色的柔光，
保证在轮盘深色底上也能看清轮廓。

tML 的 rawimg 管线不做预乘，游戏内用 AlphaBlend 绘制，因此写入正式目录的 PNG 预乘 alpha；
output/ 里的预览图保留直通 alpha，方便直接查看与 PIL 合成。
"""
import base64
import argparse
import io
import math
import os

import resvg_py
from PIL import Image

SIZE = 40
HERE = os.path.dirname(os.path.abspath(__file__))
SVG_DIR = os.path.join(HERE, "svg")
OUT = os.path.join(HERE, "output")
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
APPLY_DIR = os.path.join(ROOT, "ElainaModSkills", "Skills", "DollControl", "Commands")

OL = "#1b1222"  # 描边色


def doc(body, glow):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="{SIZE}" height="{SIZE}" viewBox="0 0 {SIZE} {SIZE}">
<defs>
  <filter id="glow" x="-25%" y="-25%" width="150%" height="150%">
    <feGaussianBlur in="SourceAlpha" stdDeviation="1.4" result="b"/>
    <feFlood flood-color="{glow}" flood-opacity="0.65"/>
    <feComposite in2="b" operator="in" result="g"/>
    <feMerge><feMergeNode in="g"/><feMergeNode in="SourceGraphic"/></feMerge>
  </filter>
</defs>
<g filter="url(#glow)" stroke-linejoin="round" stroke-linecap="round">
{body}
</g>
</svg>\n'''


def outlined_line(x0, y0, x1, y1, color, w, hi=None, hi_off=(0, 0)):
    s = f'<line x1="{x0}" y1="{y0}" x2="{x1}" y2="{y1}" stroke="{OL}" stroke-width="{w + 2}"/>\n'
    s += f'<line x1="{x0}" y1="{y0}" x2="{x1}" y2="{y1}" stroke="{color}" stroke-width="{w}"/>\n'
    if hi:
        dx, dy = hi_off
        s += f'<line x1="{x0 + dx}" y1="{y0 + dy}" x2="{x1 + dx}" y2="{y1 + dy}" stroke="{hi}" stroke-width="1"/>\n'
    return s


def wood_handle(x0, y0, x1, y1):
    # 柄沿 ↗ 方向，高光偏左上
    return outlined_line(x0, y0, x1, y1, "#a8652e", 3, hi="#e0a468", hi_off=(-0.7, -0.7))


# ---------- 挖矿：铁镐 + 石块（镐头与锤头同一套铁色） ----------

def stone_block(x, y, s):
    b = f'<rect x="{x}" y="{y}" width="{s}" height="{s}" rx="1.6" fill="#7d7a8e" stroke="{OL}" stroke-width="2" paint-order="stroke"/>\n'
    # 顶面亮、底面暗，做出方块体积
    b += f'<rect x="{x + 1}" y="{y + 1}" width="{s - 2}" height="3" rx="0.8" fill="#a9a6ba"/>\n'
    b += f'<rect x="{x + 1}" y="{y + s - 3.5}" width="{s - 2}" height="2.5" rx="0.8" fill="#5e5b6f"/>\n'
    # 裂纹
    b += (f'<path d="M{x + 4} {y + 5} l2.5 2.2 l-0.8 3 M{x + 6.5} {y + 7.2} l3.5 -0.8" '
          f'fill="none" stroke="#4a4759" stroke-width="1"/>\n')
    # 金矿点
    for (dx, dy, r) in ((s - 3.4, 5.6, 1.2), (3.4, s - 5.0, 1.1), (s * 0.62, s * 0.66, 0.9)):
        b += f'<circle cx="{x + dx}" cy="{y + dy}" r="{r}" fill="#ffd25e" stroke="#8a5a12" stroke-width="0.6"/>\n'
    return b


def pickaxe_head(cx, cy, mid=4.5, arm=11.5):
    """泰拉瑞亚 / 我的世界式镐头：中间一段垂直于柄的直段，两端各向柄的方向折 45°。
    局部坐标里柄朝 -y，中段沿 x 轴；转回屏幕后左臂水平、右臂竖直。"""
    r2 = math.sqrt(0.5)
    t = math.tan(math.radians(22.5))
    out_h, in_h, tip_h = 3.3, 2.9, 2.3  # 外缘 / 内缘半厚，臂端半厚

    def side(sx):
        d = (sx * r2, r2)            # 臂的方向
        n = (sx * r2, -r2)           # 臂的外侧法线
        tip = (sx * mid + d[0] * arm, d[1] * arm)
        o1 = (sx * (mid + out_h * t), -out_h)
        o2 = (tip[0] + n[0] * tip_h, tip[1] + n[1] * tip_h)
        point = (tip[0] + d[0] * 1.0, tip[1] + d[1] * 1.0)
        i2 = (tip[0] - n[0] * tip_h, tip[1] - n[1] * tip_h)
        i1 = (sx * (mid - in_h * t), in_h)
        hi_end = (sx * mid + d[0] * arm * 0.85 + n[0] * 1.0, d[1] * arm * 0.85 + n[1] * 1.0)
        lo_end = (sx * mid + d[0] * arm * 0.85 - n[0] * 1.2, d[1] * arm * 0.85 - n[1] * 1.2)
        return [o1, o2, point, i2, i1], hi_end, lo_end

    rp, rhi, rlo = side(1)
    lp, lhi, llo = side(-1)
    f = lambda q: f"{q[0]:.2f} {q[1]:.2f}"
    # 外缘：左尖 → 左折角 → 右折角 → 右尖；内缘反向回来
    poly = [lp[2], lp[1], lp[0], rp[0], rp[1], rp[2], rp[3], rp[4], lp[4], lp[3]]
    shape = "M" + " L".join(f(q) for q in poly) + " Z"
    hc = mid + 1.9 * t
    lc = mid - 1.8 * t
    s = f'<g transform="translate({cx} {cy}) rotate(45)" stroke-linejoin="miter">\n'
    s += f'<path d="{shape}" fill="#a3a9bd" stroke="{OL}" stroke-width="2" paint-order="stroke"/>\n'
    # 外缘高光、内缘暗面
    s += f'<path d="M{f(lhi)} L{-hc:.2f} -1.9 L{hc:.2f} -1.9 L{f(rhi)}" fill="none" stroke="#e4e8f4" stroke-width="1.5"/>\n'
    s += f'<path d="M{f(llo)} L{-lc:.2f} 1.8 L{lc:.2f} 1.8 L{f(rlo)}" fill="none" stroke="#727891" stroke-width="1.3"/>\n'
    s += '</g>\n'
    return s


def pickaxe():
    # 柄穿过镐头中段，顶端露出一小截
    return wood_handle(4, 33, 27.6, 9.4) + pickaxe_head(23.5, 13.5, mid=6, arm=9.5)


def icon_mine():
    return doc(stone_block(23, 27, 12) + pickaxe(), "#ff9a3c")


def chain_cluster(cells, lit, draw_cell, draw_lit, ring, gap_fill=None, gap=0.5):
    """消消乐式高亮：lit 中的格子共用一圈亮边（只描整组的外轮廓），其余格子照常画。
    cells / lit 为 (x, y, w, h) 列表；draw_cell / draw_lit 画单个格子。"""
    s = "".join(draw_cell(*c) for c in cells if c not in lit)
    # 先给每个高亮格描一圈粗亮边，再把高亮格本体盖上去，格子之间的亮边被盖掉，只剩整组外轮廓
    s += "".join(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="none" stroke="{ring}" stroke-width="3.2"/>\n'
                 for (x, y, w, h) in lit)
    if gap_fill:
        # 格子之间有缝（如砖缝）时先铺缝色，免得亮边从缝里透出来
        s += "".join(f'<rect x="{x - gap}" y="{y - gap}" width="{w + 2 * gap}" height="{h + 2 * gap}" fill="{gap_fill}"/>\n'
                     for (x, y, w, h) in lit)
    s += "".join(draw_lit(*c) for c in lit)
    return s


def ore_cell(x, y, w, h):
    s = f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="#e0a832" stroke="{OL}" stroke-width="0.9"/>\n'
    s += f'<rect x="{x + 0.6}" y="{y + 0.6}" width="{w - 1.2}" height="1.4" fill="#ffe28a"/>\n'
    s += f'<rect x="{x + 0.6}" y="{y + h - 1.6}" width="{w - 1.2}" height="1.0" fill="#a8701a"/>\n'
    s += f'<rect x="{x + w * 0.55}" y="{y + h * 0.4}" width="1.2" height="1.2" fill="#fff6d0"/>\n'
    return s


def dim_stone_cell(x, y, w, h):
    s = f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="#565366" stroke="{OL}" stroke-width="0.9"/>\n'
    s += f'<rect x="{x + 0.6}" y="{y + 0.6}" width="{w - 1.2}" height="1.2" fill="#6c6980"/>\n'
    return s


def icon_mine_chain():
    # 右下 3x3 方块：同类金矿连成一组整体高亮，其余石块压暗
    ox, oy, c = 17.5, 18.5, 6.4
    ore = {(0, 0), (1, 0), (1, 1), (2, 1), (2, 2)}
    cells = [(ox + i * c, oy + j * c, c, c) for j in range(3) for i in range(3)]
    lit = [(ox + i * c, oy + j * c, c, c) for (i, j) in sorted(ore)]
    body = chain_cluster(cells, lit, dim_stone_cell, ore_cell, "#fff0b0")
    body += f'<g transform="translate(-1 -1) scale(0.74)">\n{pickaxe()}</g>\n'
    return doc(body, "#ffc24a")


# ---------- 拆墙：铁锤 + 砖墙 ----------

def bricks(x0, y0, x1, y1, row_h=5, brick_w=7):
    out = []
    for r, y in enumerate(range(int(y0), int(y1), row_h)):
        x = x0 + (0 if r % 2 == 0 else -brick_w / 2)
        while x < x1:
            out.append((x + 0.5, y + 0.5, brick_w - 1, row_h - 1))
            x += brick_w
    return out


def brick_cell(x, y, w, h):
    return (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="0.6" fill="#8e4a3c"/>\n'
            f'<rect x="{x}" y="{y}" width="{w}" height="1" fill="#b4644f"/>\n')


def lit_brick_cell(x, y, w, h):
    return (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="0.6" fill="#d8704c"/>\n'
            f'<rect x="{x}" y="{y}" width="{w}" height="1.2" fill="#ffb088"/>\n')


def brick_wall(x0, y0, x1, y1, lit_pred=None, clip_id="wallclip"):
    """lit_pred(cx, cy) 为真的砖块整组高亮（连锁锤墙用）。"""
    w, h = x1 - x0, y1 - y0
    b = f'<rect x="{x0}" y="{y0}" width="{w}" height="{h}" rx="1.2" fill="#3c1f1f" stroke="{OL}" stroke-width="2" paint-order="stroke"/>\n'
    b += f'<clipPath id="{clip_id}"><rect x="{x0}" y="{y0}" width="{w}" height="{h}" rx="1.2"/></clipPath>\n<g clip-path="url(#{clip_id})">\n'
    cells = bricks(x0, y0, x1, y1)
    lit = [c for c in cells if lit_pred and lit_pred(c[0] + c[2] / 2, c[1] + c[3] / 2)]
    b += chain_cluster(cells, lit, brick_cell, lit_brick_cell, "#ffe0a8", gap_fill="#5a2a22")
    b += '</g>\n'
    return b


def hammer():
    s = wood_handle(5, 36, 20.5, 20.5)
    # 锤头：垂直于柄的长方块（旋转后 x 轴朝 ↘）
    s += '<g transform="translate(21.5 19.5) rotate(45)">\n'
    s += f'<rect x="-9.5" y="-4.6" width="19" height="9.2" rx="1.4" fill="#a3a9bd" stroke="{OL}" stroke-width="2" paint-order="stroke"/>\n'
    s += '<rect x="-8.6" y="-3.8" width="17.2" height="2.6" rx="0.8" fill="#e4e8f4"/>\n'
    s += '<rect x="-8.6" y="1.6" width="17.2" height="2.2" rx="0.8" fill="#727891"/>\n'
    s += '<rect x="-2" y="-4.6" width="4" height="9.2" fill="#666b82"/>\n'
    s += f'<line x1="-9.5" y1="-3.4" x2="-9.5" y2="3.4" stroke="{OL}" stroke-width="0.8"/>\n'
    s += '</g>\n'
    return s


def icon_wall():
    body = brick_wall(17, 3, 37, 37)
    # 锤击处裂纹与缺口
    body += ('<path d="M17.8 12.5 L21.5 15.2 L20.2 19 L23.2 22.6 M21.5 15.2 L26.5 14.2 M20.2 19 L17.8 20.5" '
             f'fill="none" stroke="{OL}" stroke-width="1.3"/>\n')
    body += hammer()
    return doc(body, "#ff8a5c")


def icon_wall_chain():
    # 锤击点附近一片相连的砖整体高亮
    blob = lambda cx, cy: (cx - 28) ** 2 / 1.0 + (cy - 16) ** 2 / 1.6 < 70
    body = brick_wall(17, 3, 37, 37, lit_pred=blob)
    body += hammer()
    return doc(body, "#ffa060")


# ---------- 攻击：交叉双剑 ----------

def sword(transform):
    """局部坐标：剑刃沿 +x，护手在原点。"""
    s = f'<g transform="{transform}">\n'
    # 剑刃：上半亮、下半暗，中间血槽
    s += f'<polygon points="0,-2.3 21,-2.3 25,0 21,2.3 0,2.3" fill="#aeb6d6" stroke="{OL}" stroke-width="2" paint-order="stroke"/>\n'
    s += '<polygon points="0.5,-1.7 20.8,-1.7 24.1,0 0.5,0" fill="#eef1ff"/>\n'
    s += '<line x1="2" y1="0.2" x2="19" y2="0.2" stroke="#7d86ad" stroke-width="0.8"/>\n'
    # 握柄
    s += f'<rect x="-5.8" y="-1.4" width="4.6" height="2.8" rx="0.8" fill="#a8335a" stroke="{OL}" stroke-width="2" paint-order="stroke"/>\n'
    s += '<line x1="-4.6" y1="-1.2" x2="-3.8" y2="1.2" stroke="#6e1a38" stroke-width="0.7"/>\n'
    s += '<line x1="-3.0" y1="-1.2" x2="-2.2" y2="1.2" stroke="#6e1a38" stroke-width="0.7"/>\n'
    # 护手与剑首
    s += f'<rect x="-1.5" y="-4.8" width="3.0" height="9.6" rx="1.3" fill="#e2507e" stroke="{OL}" stroke-width="2" paint-order="stroke"/>\n'
    s += '<rect x="-0.9" y="-4.0" width="1.0" height="8" rx="0.6" fill="#ffa6c2"/>\n'
    s += f'<circle cx="-7.3" cy="0" r="1.8" fill="#e2507e" stroke="{OL}" stroke-width="2" paint-order="stroke"/>\n'
    s += '<circle cx="-7.7" cy="-0.5" r="0.6" fill="#ffa6c2"/>\n'
    s += '</g>\n'
    return s


def icon_attack():
    # 右侧那把是左侧的镜像，先画它，让左侧剑压在上面
    body = sword("translate(30 30) scale(-1 1) rotate(-45)")
    body += sword("translate(10 30) rotate(-45)")
    return doc(body, "#ff5a8c")


# ---------- 取消：圆圈叉（取消全部行动，回到跟随） ----------

def icon_cancel():
    body = f'<circle cx="20" cy="20" r="13" fill="none" stroke="{OL}" stroke-width="6"/>\n'
    body += '<circle cx="20" cy="20" r="13" fill="none" stroke="#9f7ae8" stroke-width="3.8"/>\n'
    body += '<path d="M8.8 15 A12 12 0 0 1 15 8.8" fill="none" stroke="#e6d9ff" stroke-width="1"/>\n'
    x = "M14 14 L26 26 M26 14 L14 26"
    body += f'<path d="{x}" stroke="{OL}" stroke-width="6.2"/>\n'
    body += f'<path d="{x}" stroke="#9f7ae8" stroke-width="4"/>\n'
    body += '<path d="M14.6 13.8 L25.4 24.6 M25.4 13.8 L14.6 24.6" stroke="#e6d9ff" stroke-width="1" opacity="0.8"/>\n'
    return doc(body, "#a77bff")


# ---------- 解除玩偶：熊头像 + 解除标记 ----------

KUMA_SPRITE = os.path.join(ROOT, "ElainaModSkills", "Skills", "DollControl", "Kuma", "Kuma_Idle_1.png")
KUMA_HEAD_BOX = (15, 9, 32, 22)  # 熊头在 48x48 待机帧里的范围（含耳朵，不含脖子）


def kuma_head(scale=2):
    """从熊的待机帧截出头像，补齐下巴的描边，最近邻放大。"""
    src = Image.open(KUMA_SPRITE).convert("RGBA")
    head = src.crop(KUMA_HEAD_BOX)
    w, h = head.size
    dark = min((head.getpixel((x, y)) for x in range(w) for y in range(h) if head.getpixel((x, y))[3] > 0),
               key=lambda c: c[0] + c[1] + c[2])
    out = Image.new("RGBA", (w, h + 1), (0, 0, 0, 0))
    out.alpha_composite(head)
    # 截断处（最后一行）若不是描边色，在它下面补一行描边
    for x in range(w):
        c = head.getpixel((x, h - 1))
        if c[3] > 0 and sum(c[:3]) > sum(dark[:3]) + 60:
            out.putpixel((x, h), dark)
    return out.resize((out.width * scale, out.height * scale), Image.NEAREST)


def png_data_uri(im):
    buf = io.BytesIO()
    im.save(buf, "PNG")
    return "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode("ascii")


def kuma_image(x, y, im):
    return (f'<image x="{x}" y="{y}" width="{im.width}" height="{im.height}" '
            f'image-rendering="optimizeSpeed" href="{png_data_uri(im)}"/>\n')


def icon_dismiss():
    head = kuma_head()
    body = kuma_image(2, 3, head)
    # 右下角红色解除徽章
    cx, cy, r = 30.5, 30.5, 7
    body += f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="#e2454f" stroke="{OL}" stroke-width="2" paint-order="stroke"/>\n'
    body += f'<path d="M{cx - 4.6} {cy - 1.8} A{r - 1.6} {r - 1.6} 0 0 1 {cx - 1.8} {cy - 4.6}" fill="none" stroke="#ff9aa0" stroke-width="1"/>\n'
    d = 2.6
    body += f'<path d="M{cx - d} {cy - d} L{cx + d} {cy + d} M{cx + d} {cy - d} L{cx - d} {cy + d}" stroke="#fff3f3" stroke-width="2"/>\n'
    return doc(body, "#ff6070")


ICONS = {
    "DollCommand_Attack": icon_attack,
    "DollCommand_Mine": icon_mine,
    "DollCommand_BreakWall": icon_wall,
    "DollCommand_MineChain": icon_mine_chain,
    "DollCommand_BreakWallChain": icon_wall_chain,
    "DollCommand_Cancel": icon_cancel,
    "DollCommand_Dismiss": icon_dismiss,
}


# 这些图标的正式贴图由 BuildPixelDollCommandIcons.py 生成，SVG 版只保留预览
PIXEL_ICONS = set(ICONS) - {"DollCommand_Dismiss"}


def render(svg):
    data = resvg_py.svg_to_bytes(svg_string=svg, width=SIZE, height=SIZE)
    return Image.open(io.BytesIO(bytes(data))).convert("RGBA")


def premultiply(im):
    """RGB 乘以 alpha（四舍五入），柔光边缘的低 alpha 像素不再以满饱和色叠加。"""
    out = Image.new("RGBA", im.size)
    out.putdata([((r * a + 127) // 255, (g * a + 127) // 255, (b * a + 127) // 255, a)
                for r, g, b, a in im.getdata()])
    return out


def preview(images):
    scale, pad = 6, 14
    bg = (24, 18, 32, 255)
    w = len(images) * (SIZE * scale + pad) + pad
    h = SIZE * scale + pad * 3 + SIZE * 2
    sheet = Image.new("RGBA", (w, h), bg)
    for i, (_, im) in enumerate(images):
        x = pad + i * (SIZE * scale + pad)
        sheet.alpha_composite(im.resize((SIZE * scale, SIZE * scale), Image.NEAREST), (x, pad))
        sheet.alpha_composite(im, (x, SIZE * scale + pad * 2 + SIZE // 2))
        sheet.alpha_composite(im.resize((SIZE * 2, SIZE * 2), Image.NEAREST), (x + SIZE + pad, SIZE * scale + pad * 2))
    return sheet


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--only", choices=ICONS, help="只重新生成指定图标")
    args = parser.parse_args()
    apply = args.apply
    os.makedirs(SVG_DIR, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)
    images = []
    for name, fn in ICONS.items():
        if args.only and name != args.only:
            continue
        svg = fn()
        with open(os.path.join(SVG_DIR, name + ".svg"), "w", encoding="utf-8") as f:
            f.write(svg)
        im = render(svg)
        im.save(os.path.join(OUT, name + ".png"))
        images.append((name, im))
        if apply and name not in PIXEL_ICONS:
            os.makedirs(APPLY_DIR, exist_ok=True)
            premultiply(im).save(os.path.join(APPLY_DIR, name + ".png"))
    preview_name = "_preview_" + args.only + ".png" if args.only else "_preview.png"
    preview(images).save(os.path.join(OUT, preview_name))
    print("written", len(images), "icons", "(applied)" if apply else "(preview only)")


if __name__ == "__main__":
    main()
