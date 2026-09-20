"""Bake contiguous antialiased masks; RGB=alpha for the tML rawimg pipeline."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageChops

assets = Path(__file__).resolve().parents[2]/'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets'
export_scale, supersample = 4, 4
scale = export_scale * supersample

def rounded_mask(width, height, inset=0):
    mask = Image.new('L', (width*scale, height*scale), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((inset*scale,inset*scale,(width-inset)*scale-1,(height-inset)*scale-1),
                           radius=(height/2-inset)*scale,fill=255)
    return mask

def save(name, mask, width, height):
    mask = mask.resize((width*export_scale,height*export_scale),Image.Resampling.LANCZOS)
    Image.merge('RGBA',(mask,mask,mask,mask)).save(assets/(name+'.png'))

track = rounded_mask(37,21)
save('ToggleTrack',track,37,21)
save('ToggleOutline',ImageChops.subtract(track,rounded_mask(37,21,1)),37,21)
save('ToggleThumb',rounded_mask(15,15),15,15)
print('Toggle masks: track/outline 148x84, thumb 60x60; premultiplied RGBA, no overlapping track pieces.')
