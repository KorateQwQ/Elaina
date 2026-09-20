"""Fixed 276x35 button masks with 3.5px corners; premultiplied RGBA for tML."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageChops

assets=Path(__file__).resolve().parents[2]/'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets'
w,h,scale=276,35,16
def mask(inset=0):
    im=Image.new('L',(w*scale,h*scale),0)
    ImageDraw.Draw(im).rounded_rectangle((inset*scale,inset*scale,(w-inset)*scale-1,(h-inset)*scale-1),radius=(3.5-inset)*scale,fill=255)
    return im
coverage=mask()
gradient=Image.new('L',coverage.size)
draw=ImageDraw.Draw(gradient)
for y in range(h*scale):
    draw.line((0,y,w*scale-1,y),fill=round(255*(1-.38*y/(h*scale-1))))
fill=ImageChops.multiply(coverage,gradient)
outline=ImageChops.subtract(coverage,mask(.75))
for name,alpha in [('UpgradeFill',fill),('UpgradeOutline',outline)]:
    alpha=alpha.resize((w*4,h*4),Image.Resampling.LANCZOS)
    Image.merge('RGBA',(alpha,alpha,alpha,alpha)).save(assets/(name+'.png'))
print('UpgradeFill/UpgradeOutline: 1104x140 premultiplied RGBA, 3.5 design-pixel corner radius.')
