"""Compose FNA-rendered frames; Pillow only adds captions and encodes GIFs."""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(sys.argv[1])
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 17)
small = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 13)

def caption(image, mode):
    image = image.convert('RGB')
    d = ImageDraw.Draw(image)
    d.text((24, 20), '小冰石块 · 原尺寸', font=font, fill='#e3f6f8')
    d.text((504, 20), '冰块细节 · 2 倍放大', font=font, fill='#e3f6f8')
    desc = '连续命中 · 每秒 5 发' if mode == 'continuous' else '单次破碎 · 正常速度'
    d.text((24, 449), desc, font=small, fill='#9ccbd0')
    d.text((680, 449), 'FNA 独立美术原型', font=small, fill='#9ccbd0')
    return image

for mode in ['single', 'continuous', 'dark']:
    frames = [caption(Image.open(p), mode) for p in sorted((root / mode).glob('*.png'))]
    # 固定调色板避免逐帧量化导致青色背景闪动。
    palette_source = Image.new('RGB', (960, 480 * 8))
    for i, index in enumerate([6, 11, 13, 15, 18, 21, 26, 32]):
        palette_source.paste(frames[index], (0, i * 480))
    palette = palette_source.quantize(colors=256)
    encoded = [im.quantize(palette=palette, dither=Image.Dither.NONE) for im in frames]
    encoded[0].save(root / f'{mode}.gif', save_all=True, append_images=encoded[1:],
                    duration=[30, 30, 40] * (len(frames) // 3), loop=0, disposal=2)
    if mode == 'single':
        frames[17].save(root / 'detail.png')
        sheet = Image.new('RGB', (960, 480 * 3))
        for row, index in enumerate([14, 18, 25]):
            im = frames[index].copy()
            ImageDraw.Draw(im).text((390, 65), f'+{(index / 30 - .4):.2f}s', font=small, fill='#e3f6f8')
            sheet.paste(im, (0, row * 480))
        sheet.save(root / 'stages.png')
print(root / 'single.gif')
