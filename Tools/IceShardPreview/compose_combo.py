"""Caption and encode actual FNA renders; detail panels are nearest-neighbor crops."""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root=Path(sys.argv[1])
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',17)
small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',13)
for mode in ['single','continuous','dark']:
    frames=[]
    for path in sorted((root/mode).glob('*.png')):
        image=Image.open(path).convert('RGB')
        d=ImageDraw.Draw(image)
        d.text((24,20),'当前冰锥 + 原有命中特效 + 预烘焙白冰块',font=font,fill='#e3f6f8')
        d.text((24,449),'连续攻击 · 每秒 5 发' if mode=='continuous' else '单次攻击 · 正常速度 · 原尺寸',font=small,fill='#9ccbd0')
        d.text((690,449),'FNA 组合预览 · 模拟命中点',font=small,fill='#9ccbd0')
        frames.append(image)
    atlas=Image.new('RGB',(960,480*10))
    for row,index in enumerate([9,15,19,20,21,22,23,25,28,34]): atlas.paste(frames[index],(0,row*480))
    palette=atlas.quantize(colors=256)
    encoded=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in frames]
    encoded[0].save(root/f'{mode}.gif',save_all=True,append_images=encoded[1:],duration=[30,30,40]*(len(frames)//3),loop=0,disposal=2)
    if mode=='single':
        encoded[0].save(root/'slow.gif',save_all=True,append_images=encoded[1:45],duration=100,loop=0,disposal=2)
        frames[25].save(root/'detail.png')
        sheet=Image.new('RGB',(960,360*4))
        for row,index in enumerate([22,23,26,32]):
            image=Image.open(root/mode/f'{index:03d}.png').convert('RGB').crop((440,157,920,337)).resize((960,360),Image.Resampling.NEAREST)
            d=ImageDraw.Draw(image)
            d.text((20,18),f'{index/30-.75:+.2f}s · 命中点 2 倍放大',font=font,fill='#e3f6f8')
            sheet.paste(image,(0,row*360))
        sheet.save(root/'stages.png')
print(root/'single.gif')
