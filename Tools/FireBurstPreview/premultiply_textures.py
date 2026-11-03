"""把直通 alpha 的火焰粒子贴图预乘后写成正式贴图，供 AlphaBlend（顶点 alpha 作覆盖度）+ 同层 Bloom 绘制。

tML 加载 PNG 时不再预乘，正式贴图须直接存预乘后的 RGBA：rgb = round(rgb * a / 255)。
源图保持不变：
- KL/Dusts/Fire/Eff_Fire_10.png -> 伊蕾娜/ElainaModSkills/Skills/Fire/FireBurstFlameDust.png
- KL/Dusts/Fire/FireDust2.png   -> KL/Dusts/Fire/FireDust2_Premultiplied.png（同 KL Dusts/Spark_Premultiplied 的做法）
用法：python -I Tools/FireBurstPreview/premultiply_textures.py [--check]
"""
import sys
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[2]
kl = root.parent / 'KL'
PAIRS = [
    (kl / 'Dusts' / 'Fire' / 'Eff_Fire_10.png', root / 'ElainaModSkills' / 'Skills' / 'Fire' / 'FireBurstFlameDust.png'),
    (kl / 'Dusts' / 'Fire' / 'FireDust2.png', kl / 'Dusts' / 'Fire' / 'FireDust2_Premultiplied.png'),
]


def premultiply(image: Image.Image) -> Image.Image:
    rgba = image.convert('RGBA')
    result = Image.new('RGBA', rgba.size)
    result.putdata([(round(r * a / 255), round(g * a / 255), round(b * a / 255), a) for r, g, b, a in rgba.getdata()])
    return result


failed = False
for source, target in PAIRS:
    expected = premultiply(Image.open(source))
    if '--check' not in sys.argv:
        expected.save(target)
        print(f'wrote {target}')
        continue
    actual = Image.open(target).convert('RGBA')
    if actual.size != expected.size or list(actual.getdata()) != list(expected.getdata()):
        print(f'MISMATCH: {target.name} 与预乘后的 {source.name} 不一致，请重新运行本脚本')
        failed = True
    else:
        print(f'OK: {target.name} 为 {source.name} 的预乘版 {actual.size}')
sys.exit(1 if failed else 0)
