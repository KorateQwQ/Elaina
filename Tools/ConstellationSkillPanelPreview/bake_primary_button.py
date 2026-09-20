"""Bake a bookmark-like main action with 6px cuts, no inner rectangular frame."""
from pathlib import Path
import math
from PIL import Image, ImageDraw, ImageChops

assets=Path(__file__).resolve().parents[2]/'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets'
w,h,scale=276,38,8
def shape(inset=0):
    c=6
    points=[(c,inset),(w-c,inset),(w-inset,c),(w-inset,h-c),(w-c,h-inset),(c,h-inset),(inset,h-c),(inset,c)]
    im=Image.new('L',(w*scale,h*scale),0)
    ImageDraw.Draw(im).polygon([(round(x*scale),round(y*scale)) for x,y in points],fill=255)
    return im
coverage=shape()
gradient=Image.new('L',coverage.size)
pixels=gradient.load()
for y in range(h*scale):
    for x in range(w*scale):
        center=math.exp(-((x/(w*scale)-.5)/.36)**2)
        vertical=.93-.18*y/(h*scale)
        pixels[x,y]=round(255*(.6+.4*center)*vertical)
fill=ImageChops.multiply(coverage,gradient)
trim=ImageChops.subtract(coverage,shape(.75))
draw=ImageDraw.Draw(trim)
# Disconnected horizontal accents and short end strokes replace the old inset box.
for x in range(16*scale,(w-16)*scale):
    opacity=round(80*max(0,1-abs(x/(w*scale)-.5)*2))
    draw.line((x,3*scale,x,3.5*scale),fill=opacity)
    draw.line((x,(h-3.5)*scale,x,(h-3)*scale),fill=opacity)
for x in [3,w-3]:
    draw.line((x*scale,12*scale,x*scale,26*scale),fill=110,width=scale//2)
for name,alpha in [('PrimaryActionFill',fill),('PrimaryActionTrim',trim)]:
    alpha=alpha.resize((w*4,h*4),Image.Resampling.LANCZOS)
    Image.merge('RGBA',(alpha,alpha,alpha,alpha)).save(assets/(name+'.png'))
print('Primary action: 1104x152 premultiplied RGBA masks, 6px cut corners.')
