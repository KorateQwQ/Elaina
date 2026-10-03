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

def rain():
    import importlib.util
    from pathlib import Path
    source = Path(__file__).resolve().parents[1] / 'CurioPixel/GenerateBottledRain.py'
    spec = importlib.util.spec_from_file_location('approved_bottled_rain', source)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.build_sprite()


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
    im,d=canvas((30,27))
    # Explicit mirrored rings keep a 2x2 opening and a one-pixel outline.
    # Draw behind the pot so both attachments meet the same depth of wall.
    handle_colors={'O':O,'H':'#c6b4d5','M':'#a793b8','S':'#725d83'}
    for y,row in enumerate(('.OOOOO','OHMMMO','OM..MO','OM..MO','OSSSSO','.OOOOO'),14):
        for x,symbol in enumerate(row):
            if symbol != '.':
                d.point((x,y),fill=handle_colors[symbol])
                d.point((29-x,y),fill=handle_colors[symbol])

    # All silhouette spans share x=14.5; widen the steps toward the rounded
    # base instead of letting a sloping polygon end in a stray bottom pixel.
    for y,left in [(13,4),(14,4),(15,4),(16,5),(17,5),(18,5),(19,5),(20,5),
                   (21,6),(22,6),(23,7),(24,8),(25,10)]:
        d.line([(left,y),(29-left,y)],fill=O)
    for y,left in [(16,6),(17,6),(18,6),(19,6),(20,6),(21,7),(22,7),(23,8),(24,10)]:
        d.line([(left,y),(29-left,y)],fill='#8e759d')
    for y,left,right in [(18,22,23),(19,22,23),(20,21,23),(21,20,22),
                         (22,18,22),(23,10,21),(24,10,19)]:
        d.line([(left,y),(right,y)],fill='#725d83')
    d.line([(6,18),(6,19)],fill='#c6b4d5')
    d.line([(7,20),(7,21)],fill='#c6b4d5')
    for y,left,right in [(21,8,8),(22,9,10),(23,11,18),(22,19,20),(21,21,21)]:
        d.line([(left,y),(right,y)],fill='#c9b092')

    # Center the rim over the bowl and expose the handle openings evenly.
    for y,left in [(11,6),(12,4),(13,3),(14,3),(15,4),(16,6),(17,8)]:
        d.line([(left,y),(29-left,y)],fill=O)
    for y,left in [(12,6),(13,4),(14,4),(15,5),(16,7)]:
        d.line([(left,y),(29-left,y)],fill='#ceb9d8')
    for y,left in [(13,7),(14,6),(15,9)]:
        d.line([(left,y),(29-left,y)],fill='#9e6241')
    d.rectangle((9,12,12,14),fill='#d6b374');d.rectangle((17,12,20,14),fill='#ae6871');d.point((15,14),fill='#99b079')
    d.line([(10,8),(8,6),(9,4),(11,2)],fill='#c6b4d5');d.line([(18,8),(20,6),(18,4),(19,1)],fill='#ad99be')
    return im

def brulee():
    im,d=canvas((28,21))
    # All three layers share x=13.5. The saucer is a symmetric six-row ellipse;
    # its two front rows remain visible below the ramekin rather than splitting
    # into disconnected pale fragments at either side of the cup.
    for y,left,right in [(15,6,21),(16,2,25),(17,0,27),(18,0,27),(19,2,25),(20,6,21)]:
        d.line([(left,y),(right,y)],fill=O)
    d.line([(4,16),(23,16)],fill='#cac1d7')
    d.line([(1,17),(26,17)],fill='#e1dae9')
    d.line([(1,18),(26,18)],fill='#d2c9e0')
    d.line([(4,19),(23,19)],fill='#afa1c2')
    d.line([(9,18),(18,18)],fill='#a89aba')  # contact shadow, not an empty gap

    # Taper the cup evenly toward its centered foot, one pixel above the old base.
    d.polygon([(2,7),(25,7),(24,12),(22,16),(20,17),(7,17),(5,16),(3,12)],fill=O)
    d.polygon([(3,8),(24,8),(23,12),(21,15),(19,16),(8,16),(6,15),(4,12)],fill='#ddd1b3')
    for points in [[(7,11),(8,15)],[(11,11),(11,16)],[(16,11),(16,16)],[(20,11),(19,15)]]:
        d.line(points,fill='#aa9c8e')
    # Raise the oval with the cup so the exposed cup wall keeps its height.
    d.polygon([(6,2),(21,2),(25,5),(25,8),(21,11),(6,11),(2,8),(2,5)],fill=O)
    d.polygon([(7,3),(20,3),(24,5),(24,7),(20,10),(7,10),(3,7),(3,5)],fill='#efce8a')
    d.polygon([(8,4),(19,4),(22,5),(23,7),(19,9),(8,9),(4,7),(5,5)],fill='#c38a41')
    d.line([(8,4),(10,4),(11,6),(9,8)],fill='#845231')
    d.line([(11,6),(16,5),(19,7),(21,6)],fill='#845231')
    d.line([(6,5),(8,4)],fill='#ffe5a4');d.point((17,8),fill='#efd084')
    return im

def moon_dew():
    im,d=canvas((20,28))
    d.polygon([(5,4),(14,4),(14,10),(18,15),(18,25),(16,27),(3,27),(1,25),(1,15),(5,10)],fill=O)
    d.polygon([(6,5),(13,5),(13,11),(17,15),(17,24),(15,26),(4,26),(2,24),(2,15),(6,11)],fill=S)
    d.rectangle((3,18,16,24),fill='#79b8cb');d.line([(4,25),(15,25)],fill='#4a829e')
    d.line([(3,18),(8,17),(12,18),(16,17)],fill='#c0e4e9')
    d.line([(4,13),(3,15),(3,22)],fill=W)
    d.rectangle((4,1,15,5),fill=O);d.rectangle((5,2,14,4),fill='#b3aed0');d.line([(6,2),(13,2)],fill='#e2dff0')
    d.line([(11,19),(9,20),(9,23),(11,24),(13,23)],fill=W)
    d.line([(10,20),(10,22),(12,23)],fill=W);d.point((14,19),fill=H)
    return im

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

def flour():
    im,d=canvas((20,26))
    d.polygon([(5,1),(16,2),(14,7),(17,20),(16,24),(12,25),(4,24),(2,21),(5,7)],fill=O)
    d.polygon([(6,2),(15,3),(13,7),(16,20),(15,23),(12,24),(5,23),(3,21),(6,7)],fill='#cfbb96')
    d.polygon([(6,7),(11,8),(12,22),(5,22),(4,19)],fill='#e7d7b5')
    d.line([(5,6),(14,6)],fill='#a8906d');d.line([(5,9),(4,19)],fill='#f8edcf')
    d.rectangle((5,12,14,21),fill='#cba675')
    d.line([(10,19),(10,13)],fill='#855f3c')
    for y in [14,17]:d.line([(7,y),(10,y+2),(13,y)],fill='#855f3c')
    return im

def sugar():
    im,d=canvas((24,22))
    for x,y in [(1,10),(12,11),(7,2)]:
        d.polygon([(x,y+3),(x+5,y),(x+10,y+3),(x+10,y+9),(x+5,y+11),(x,y+8)],fill=O)
        d.polygon([(x+1,y+3),(x+5,y+1),(x+9,y+3),(x+5,y+5)],fill='#f1ebf3')
        d.polygon([(x+1,y+4),(x+5,y+6),(x+5,y+10),(x+1,y+8)],fill='#d1c8df')
        d.polygon([(x+6,y+6),(x+9,y+4),(x+9,y+8),(x+6,y+10)],fill='#b2a4c4')
    return im

def egg():
    im,d=canvas((18,24))
    d.polygon([(8,1),(10,1),(13,5),(16,12),(17,17),(16,21),(13,23),(5,23),(2,21),(1,17),(2,12),(5,5)],fill=O)
    d.polygon([(8,2),(10,2),(12,5),(15,12),(16,17),(15,20),(12,22),(5,22),(3,20),(2,17),(3,12),(6,5)],fill='#d8c5a4')
    d.polygon([(8,3),(10,3),(12,8),(13,14),(12,19),(9,21),(5,20),(3,17),(4,11),(6,6)],fill='#efe2c6')
    d.line([(7,6),(5,10),(4,15)],fill='#fff4dd');d.point((13,19),fill='#b29a80')
    return im

def beef():
    im,d=canvas((26,22))
    d.polygon([(6,3),(13,1),(20,3),(24,7),(25,14),(22,19),(16,21),(8,20),(3,17),(1,10),(3,6)],fill=O)
    d.polygon([(6,4),(13,2),(20,4),(23,7),(24,14),(21,18),(16,20),(8,19),(4,16),(2,10),(4,6)],fill='#e5beb7')
    d.polygon([(7,5),(13,3),(19,5),(22,8),(23,13),(20,17),(15,19),(8,18),(5,15),(3,10),(5,7)],fill='#b46270')
    d.polygon([(7,6),(13,4),(17,5),(15,9),(13,15),(8,16),(5,12)],fill='#d48990')
    for points in [[(8,6),(6,10),(7,13)],[(12,5),(10,10),(11,16)],[(16,6),(14,10),(15,13)]]:d.line(points,fill='#f1c9c1')
    d.polygon([(17,12),(20,12),(21,14),(20,16),(17,17),(15,15)],fill='#eee0c9');d.point((18,14),fill='#b19482')
    return im

def potato():
    im,d=canvas((26,22))
    d.polygon([(16,1),(21,1),(25,5),(25,11),(22,15),(17,16),(13,12),(12,6)],fill=O)
    d.polygon([(16,2),(21,2),(24,5),(24,10),(21,14),(17,15),(14,11),(13,6)],fill='#ac844c')
    d.line([(17,4),(20,3)],fill='#d6b474');d.point((21,8),fill='#785c40')
    d.polygon([(6,5),(12,5),(17,8),(20,13),(19,18),(15,21),(8,21),(3,18),(1,12),(2,8)],fill=O)
    d.polygon([(6,6),(12,6),(16,9),(19,13),(18,17),(15,20),(8,20),(4,17),(2,12),(3,9)],fill='#bb9154')
    d.polygon([(6,7),(11,7),(15,10),(15,16),(12,18),(7,17),(4,12)],fill='#d4ad6e')
    d.line([(5,9),(4,12)],fill='#edca8a')
    for xy in [(7,9),(12,12),(9,17),(16,15),(5,15)]:d.point(xy,fill='#85613d')
    return im

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
