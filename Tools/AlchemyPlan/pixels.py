"""Native-grid drawings for the new planned alchemy objects and ingredients."""
from PIL import Image, ImageDraw

O='#332f43'; W='#eaf5ff'; H='#b1cde8'; G='#7c9dbc'; S='#57799a'

def canvas(size):
    image=Image.new('RGBA',size);return image,ImageDraw.Draw(image)

def star(d,x,y,color=W):
    d.line([(x-1,y),(x+1,y)],fill=color);d.line([(x,y-1),(x,y+1)],fill=color)

def dropper():
    import importlib.util
    from pathlib import Path
    source = Path(__file__).resolve().parents[1] / 'CurioPixel/GenerateAetherDropper.py'
    spec = importlib.util.spec_from_file_location('upright_aether_dropper', source)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.build_sprite()

def revealing_dust():
    import importlib.util
    from pathlib import Path
    source = Path(__file__).resolve().parents[1] / 'CurioPixel/GenerateRevealingDust.py'
    spec = importlib.util.spec_from_file_location('approved_revealing_dust', source)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.build_sprite()

def approved(script):
    """Sprites redrawn by hand in Tools/PotionPixel; reuse them so a full rebuild keeps the approved art."""
    import importlib.util
    import sys
    from pathlib import Path
    folder = Path(__file__).resolve().parents[1] / 'PotionPixel'
    if str(folder) not in sys.path:
        sys.path.insert(0, str(folder))  # the generators share pixel_common.py
    source = folder / script
    spec = importlib.util.spec_from_file_location(source.stem, source)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.build_sprite()[0]

def rain():
    return approved('GenerateBottledRainPixel.py')


def croissant():
    im,d=canvas((28,20))
    d.polygon([(3,16),(1,13),(1,10),(4,6),(8,3),(12,2),(17,2),(21,4),(25,8),(27,12),(26,15),(24,17),(21,16),(18,11),(17,10),(11,10),(9,12),(7,16),(5,18)],fill=O)
    d.polygon([(3,15),(2,12),(3,9),(5,6),(9,4),(13,3),(17,3),(21,5),(24,8),(26,12),(25,15),(24,16),(22,15),(18,10),(17,9),(11,9),(8,12),(6,16),(5,17)],fill='#c58a49')
    d.polygon([(3,11),(5,7),(9,5),(13,4),(17,4),(20,6),(23,8),(24,11),(22,12),(18,8),(11,8),(8,10),(6,14),(4,14)],fill='#e2ad65')
    d.line([(5,8),(9,6),(12,5),(16,5)],fill='#f6d08c')
    for pts in [[(7,5),(9,8),(9,10)],[(12,3),(13,6),(13,8)],[(18,4),(17,7),(17,9)],[(22,7),(20,9),(20,11)]]:
        d.line(pts,fill='#8f5935')
    d.line([(3,14),(4,16),(5,16)],fill='#945632')
    d.line([(23,15),(25,14)],fill='#945632')
    d.polygon([(20,5),(22,6),(24,8),(25,11),(24,14),(23,13),(23,10),(21,8),(19,7)],fill='#f2bc42')
    d.line([(21,6),(23,8)],fill='#ffe28c');d.point((24,11),fill='#ffe28c')
    return im

def stew():
    return approved('GenerateBeefStewPixel.py')

def brulee():
    return approved('GenerateCaramelBruleePixel.py')

def moon_dew():
    return approved('GenerateMoonDewPixel.py')

def resin():
    im,d=canvas((25,24))
    d.polygon([(3,14),(6,9),(12,7),(19,11),(23,18),(21,22),(8,23),(1,20)],fill=O)
    d.polygon([(4,14),(7,10),(12,8),(18,12),(22,18),(20,21),(8,22),(2,19)],fill='#b78138')
    d.polygon([(5,13),(11,9),(16,11),(14,16),(7,17)],fill='#e4b153')
    d.polygon([(15,16),(18,13),(21,18),(19,20),(10,21)],fill='#d4983e')
    d.line([(7,11),(6,14)],fill='#f6d685');d.line([(16,17),(18,19)],fill='#f6d685')
    d.line([(12,7),(11,3)],fill='#9b724a',width=2)
    d.polygon([(14,8),(13,4),(17,1),(21,1),(20,5),(17,8)],fill=O)
    d.polygon([(14,5),(17,2),(20,2),(19,5),(16,7)],fill='#91a774');d.line([(14,7),(18,3)],fill='#5f795a')
    return im

def ingredient(name):
    """Merchant ingredients redrawn together in Tools/PotionPixel/GenerateIngredientPixels.py."""
    import importlib.util
    import sys
    from pathlib import Path
    folder = Path(__file__).resolve().parents[1] / 'PotionPixel'
    if str(folder) not in sys.path:
        sys.path.insert(0, str(folder))
    spec = importlib.util.spec_from_file_location('ingredient_pixels', folder / 'GenerateIngredientPixels.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.build_sprite(name)[0]

def flour():
    return ingredient('Flour')

def sugar():
    return ingredient('Sugar')

def egg():
    return ingredient('Egg')

def beef():
    return ingredient('Beef')

def potato():
    return ingredient('Potato')

def shimmer():
    im,d=canvas((20,28))
    d.polygon([(9,0),(11,3),(13,7),(18,15),(19,21),(17,25),(13,27),(6,27),(2,24),(0,20),(1,15),(5,8)],fill=O)
    d.polygon([(9,2),(11,6),(13,9),(17,16),(18,21),(16,24),(12,26),(6,26),(3,23),(1,20),(2,15),(6,8)],fill='#aa8ace')
    d.polygon([(9,3),(11,7),(12,11),(8,16),(3,20),(2,18),(5,11)],fill='#d6a2d8')
    d.polygon([(14,14),(17,18),(17,21),(15,24),(11,25),(6,24),(9,21)],fill='#7ebdc6')
    d.line([(6,11),(4,15),(3,19)],fill='#ebcce9');star(d,11,19,'#eee4fb');d.point((14,23),fill='#d0edf0')
    return im

DRAW={'AetherDropper':dropper,'RevealingDust':revealing_dust,'BottledRain':rain,'HoneyCroissant':croissant,
      'BeefStew':stew,'CaramelBrulee':brulee,'MoonDew':moon_dew,'WarmResin':resin,'Flour':flour,
      'Sugar':sugar,'Egg':egg,'Beef':beef,'Potato':potato,'ShimmerDroplet':shimmer}
