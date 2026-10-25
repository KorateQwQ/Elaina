import sys
from pathlib import Path
from PIL import Image,ImageDraw
out=Path(sys.argv[1]);frames=[Image.open(p).convert('RGB') for p in sorted(out.glob('frame-*.png'))]
frames[0].save(out/'broom-dash.gif',save_all=True,append_images=frames[1:],duration=[16,17,17]*25,loop=0,optimize=True)
names=['direction-right','direction-left','direction-up','direction-down','bright','zoom125']
canvas=Image.new('RGB',(1200,600),(17,23,34));draw=ImageDraw.Draw(canvas)
for i,name in enumerate(names):
 im=Image.open(out/(name+'.png'));im=im.crop((280,0,920,360)).resize((600,169))
 x=(i%2)*600;y=(i//2)*200;canvas.paste(im,(x,y+26));draw.text((x+12,y+7),name+' | FNA offline preview',fill='white')
canvas.save(out/'contact-sheet.png')
print(out/'broom-dash.gif');print(out/'contact-sheet.png')
