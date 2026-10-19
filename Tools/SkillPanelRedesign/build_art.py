"""把伊蕾娜立绘烘焙成星图背景用的两种版本（只用 Pillow）。

· 夜色版：明度映射到手札色板（暗紫 → 薰衣草 → 墨白），只留 25% 原色
· 线稿版：只取比周围暗的描线，换成浅薰衣草色；再垫一层极淡的剪影

python build_art.py         → assets/Elaina_{Night,Line,Color}.png（预览用：整图、无遮罩，淡出由 index.html 的 SVG 遮罩完成）
python build_art.py --game  → 正式 ConstellationSkillPanel/Assets/PortraitNight.png（游戏只用夜色版）：
    · 把预览的两道遮罩烘焙进去（下半段 50%→93% 渐隐；右侧页面 x 690→750 收进竖分栏），裁掉全透明的边
    · 1.5× 设计尺寸（面板 Scale 封顶 1，常见 UI 缩放下仍是缩小采样）、预乘 Alpha（tML 读 PNG 不预乘）
    · 打印裁切后的设计坐标矩形，对应 ConstellationDrawing.PortraitArea
"""
import pathlib
import shutil
import sys

from PIL import Image, ImageChops, ImageFilter, ImageOps

here = pathlib.Path(__file__).resolve().parent
root = here.parents[1]
src = next((root / "ElainaModSkills").glob("sincos,_artist_uno_ryoku*elaina*.png"))

# index.html 的 ART：立绘在页面上的设计坐标
ART_X, ART_Y, ART_H = 428, 188, 620
FADE_V = (.5, .93)          # 图高比例：开始渐隐 → 完全透明
FADE_H = (690, 750)         # 页面 x：开始收边 → 完全透明
GAME_SCALE = 1.5


def night(im):
    lum = im.convert("L")
    out = ImageOps.colorize(lum, black=(24, 20, 34), mid=(146, 124, 186), white=(246, 240, 252), midpoint=110)
    out = Image.blend(out, im.convert("RGB"), .25)
    out.putalpha(im.getchannel("A"))
    return out


def line(im):
    alpha = im.getchannel("A")
    # 先压到白底再取明度：剪影外缘的黑描线才会被当成「比周围暗」
    on_white = Image.alpha_composite(Image.new("RGBA", im.size, (255, 255, 255, 255)), im).convert("L")
    # 描线强度 = 局部均值 − 自身明度（只保留比周围暗的部分），放大后做软阈值
    edge = ImageChops.subtract(on_white.filter(ImageFilter.GaussianBlur(2.2)), on_white)
    edge = edge.point(lambda v: 0 if v < 6 else min(255, int((v - 6) * 5.5)))
    edge = ImageChops.multiply(edge, alpha)
    # 极淡剪影：让大面积的衣服也能隐约读出轮廓
    out = Image.new("RGBA", im.size, (232, 220, 248, 0))
    out.putalpha(ImageChops.lighter(edge, alpha.point(lambda v: int(v * .07))))
    return out


def preview(im):
    out = here / "assets"
    out.mkdir(exist_ok=True)
    night(im).save(out / "Elaina_Night.png", optimize=True)
    line(im).save(out / "Elaina_Line.png", optimize=True)
    shutil.copyfile(src, out / "Elaina_Color.png")
    print("written:", *(p.name for p in sorted(out.glob("Elaina_*.png"))))


def game(im):
    assets = root / "ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets"
    w, h = round(im.width / im.height * ART_H * GAME_SCALE), round(ART_H * GAME_SCALE)
    design_w = ART_H * im.width / im.height
    mask = Image.new("L", (w, h))
    mp = mask.load()
    for y in range(h):
        v = (y + .5) / h
        av = 1 if v <= FADE_V[0] else max(0, 1 - (v - FADE_V[0]) / (FADE_V[1] - FADE_V[0]))
        for x in range(w):
            page_x = ART_X + (x + .5) / w * design_w
            ah = 1 if page_x <= FADE_H[0] else max(0, 1 - (page_x - FADE_H[0]) / (FADE_H[1] - FADE_H[0]))
            mp[x, y] = round(255 * av * ah)
    art = night(im).resize((w, h), Image.LANCZOS)
    a = ImageChops.multiply(art.getchannel("A"), mask)
    box = a.getbbox()
    # 预乘：RGB × A
    rgb = [ImageChops.multiply(c, a) for c in art.split()[:3]]
    Image.merge("RGBA", (*rgb, a)).crop(box).save(assets / "PortraitNight.png", optimize=True)
    print(f"PortraitNight.png: {box[2] - box[0]} x {box[3] - box[1]}, premultiplied RGBA")
    k = design_w / w
    print(f"PortraitArea = ({ART_X + box[0] * k:.2f}f, {ART_Y + box[1] * k:.2f}f, {(box[2] - box[0]) * k:.2f}f, {(box[3] - box[1]) * k:.2f}f)")


if __name__ == "__main__":
    image = Image.open(src).convert("RGBA")
    game(image) if "--game" in sys.argv else preview(image)
