"""Draw an editable 56px barrier on an integer grid; Pillow is the only dependency.

Version 2 refines the existing pixel art directly; no SVG is loaded or consulted.
The two game-ready PNGs use premultiplied alpha, matching the local tML pipeline.
Only the separate glow layer is blurred. The base sprite uses a discrete palette.
"""

from pathlib import Path
import json

from PIL import Image, ImageDraw, ImageFilter, ImageFont


OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
SIZE = (56, 56)
PALETTE = {
    "film": (88, 174, 200, 24),
    "film_shade": (53, 136, 164, 42),
    "film_light": (128, 217, 234, 60),
    "reflection": (186, 241, 249, 108),
    "fold": (120, 225, 239, 138),
    "outline": (43, 98, 122, 255),
    "deep": (52, 134, 161, 255),
    "cyan": (80, 180, 206, 255),
    "ice": (129, 222, 236, 255),
    "pale": (189, 244, 250, 255),
    "white": (235, 254, 255, 255),
}

# The intact shell and both erosion cuts are pixel-grid circles. The cuts
# have different diameters (11 and 19 pixels), not hand-stepped zigzag paths.
SHELL_BOUNDS = (5, 6, 50, 51)
DISSOLVE_CIRCLES = [(31, 4, 41, 14), (39, 11, 57, 29)]


def premultiply(image):
    result = Image.new("RGBA", image.size)
    result.putdata([(r*a//255, g*a//255, b*a//255, a)
                    for r, g, b, a in image.get_flattened_data()])
    return result


def unpremultiply(image):
    result = Image.new("RGBA", image.size)
    result.putdata([(min(255, round(r*255/a)), min(255, round(g*255/a)),
                     min(255, round(b*255/a)), a) if a else (0, 0, 0, 0)
                    for r, g, b, a in image.get_flattened_data()])
    return result


def make_base():
    def shell_mask(inset):
        m = Image.new("L", SIZE)
        painter = ImageDraw.Draw(m)
        x0, y0, x1, y1 = SHELL_BOUNDS
        painter.ellipse((x0+inset,y0+inset,x1-inset,y1-inset), fill=255)
        for x0, y0, x1, y1 in DISSOLVE_CIRCLES:
            painter.ellipse((x0-inset,y0-inset,x1+inset,y1+inset), fill=0)
        return m

    shape, inner1, inner2, inner3 = [shell_mask(i) for i in range(4)]
    base = Image.new("RGBA", SIZE)
    d = ImageDraw.Draw(base)
    for y in range(56):
        for x in range(56):
            if not shape.getpixel((x,y)):
                continue
            crown = y < 36 and x+y < 45
            bottom = y >= 43
            curl = any(((x-(x0+x1)/2)**2 + (y-(y0+y1)/2)**2)
                       <= ((x1-x0)/2+3)**2
                       for x0,y0,x1,y1 in DISSOLVE_CIRCLES)
            if not inner1.getpixel((x,y)):
                color = "cyan" if curl else "deep" if crown or bottom else "outline"
            elif not inner2.getpixel((x,y)):
                color = ("white" if curl or crown and y < 23 else
                         "pale" if crown and y < 29 or y >= 49 else
                         "ice" if crown or bottom else "cyan")
            elif not inner3.getpixel((x,y)):
                color = "ice" if crown and y < 23 else "fold" if curl else "cyan" if bottom else "film_shade"
            else:
                color = "film"
            d.point((x,y), fill=PALETTE[color])

    # Short reflective facets replace the long stacked arc and triangular glare.
    highlights = {
        "film_light": [(13,22,25),(14,20,24),(15,18,22),(16,17,20),
                       (17,16,19),(18,15,17),(19,14,15)],
        "reflection": [(13,23,25),(14,21,23),(15,19,21),(16,18,19)],
        "film_shade": [(44,21,32),(45,24,32),(43,34,37),(42,37,39)],
        "fold": [(14,32,33),(15,33,35),(16,35,37),(23,39,40),(24,41,43),(25,43,45)],
    }
    for name, rows in highlights.items():
        for y, start, end in rows:
            for x in range(start,end+1):
                if inner3.getpixel((x,y)):
                    d.point((x,y), fill=PALETTE[name])

    # Lower-right glint: a single tapered cluster, with spaced endpoints.
    for name, points in [
        ("film_light", [(44,33),(44,34),(43,35),(43,36),(42,37),
                        (41,38),(40,39),(39,40),(38,41),(37,42),(35,43),(36,43)]),
        ("fold", [(43,36),(42,37),(41,38),(40,39),(39,40),(38,41),(37,42)]),
        ("ice", [(42,37),(41,38),(40,39),(39,40)]),
    ]:
        d.point(points, fill=PALETTE[name])

    # Three cross-shaped sparkles, with varied arm lengths and bright centres.
    # Their spacing keeps each star separate from the dissolving membrane.
    for x, y, rx, ry in [(41,5,1,2), (47,12,2,3), (50,21,1,1)]:
        d.line([(x-rx,y),(x+rx,y)],fill=PALETTE["ice"],width=1)
        d.line([(x,y-ry),(x,y+ry)],fill=PALETTE["pale"],width=1)
        d.point((x,y),fill=PALETTE["white"])
    # Attached glint on the lower-left edge.
    d.point([(10,39),(10,40),(10,41),(9,40),(11,40)],fill=PALETTE["pale"])
    d.point((10,40),fill=PALETTE["white"])
    return base


def make_glow(base):
    # Emit only from opaque edge/light pixels; leave the transparent centre dim.
    source = Image.new("L", SIZE)
    source.putdata([255 if a == 255 and r >= 185 else
                    160 if a == 255 and r >= 125 else
                    72 if a == 255 and r >= 75 else 0
                    for r, g, b, a in base.get_flattened_data()])
    near = source.filter(ImageFilter.GaussianBlur(1.05))
    far = source.filter(ImageFilter.GaussianBlur(2.5))
    alpha = Image.new("L", SIZE)
    alpha.putdata([min(235, round(s*.2+n*1.1+f*.5))
                   for s, n, f in zip(source.get_flattened_data(),
                                     near.get_flattened_data(), far.get_flattened_data())])
    glow = Image.new("RGBA", SIZE, (79, 225, 250, 0))
    glow.putalpha(alpha)
    return glow


def font(size):
    try:
        return ImageFont.truetype("segoeui.ttf", size)
    except OSError:
        return ImageFont.load_default(size=size)


def make_preview(base, glow):
    canvas = Image.new("RGB", (960, 590), "#0e1724")
    d = ImageDraw.Draw(canvas)
    d.text((32, 21), "ELAINA / MAGIC BARRIER", font=font(23), fill="#eefaff")
    d.text((32, 53), "56 x 56 px   /   hand-placed pixels + separate glow mask", font=font(14), fill="#8fa8bb")
    combined = Image.alpha_composite(glow, base)
    for i, (im, label) in enumerate([(base, "PIXEL BASE"), (glow, "GLOW MASK"), (combined, "LAYERED PREVIEW")]):
        x = 24+i*312
        d.rounded_rectangle((x, 89, x+288, 373), radius=15, fill="#162638", outline="#2a4255")
        zoom = im.resize((224, 224), Image.Resampling.NEAREST)
        canvas.paste(zoom, (x+32, 102), zoom)
        d.text((x+20, 343), label+" / 4x", font=font(14), fill="#aec7d6")
    d.text((32, 395), "NATIVE SIZE / BASE + GLOW", font=font(14), fill="#aec7d6")
    for i, color in enumerate(["#132234", "#e3ecee"]):
        x=24+i*228
        d.rounded_rectangle((x, 424, x+210, 560), radius=12, fill=color)
        canvas.paste(base, (x+28, 447), base)
        canvas.paste(combined, (x+123, 447), combined)
        ink="#829aaf" if i == 0 else "#4d6574"
        d.text((x+37, 519), "BASE", font=font(12), fill=ink)
        d.text((x+119, 519), "+ GLOW", font=font(12), fill=ink)
    d.text((515, 395), "EXISTING SKILL ICONS / NATIVE SIZE", font=font(14), fill="#aec7d6")
    for i, name in enumerate(["Water/WaterBallSkill", "Fire/FireBurstSkill", "MagicMissile/MagicMissileSkill"]):
        p=ROOT/"ElainaModSkills"/"Skills"/(name+".png")
        im=unpremultiply(Image.open(p).convert("RGBA"))
        x=534+i*126
        canvas.paste(im, (x, 449), im)
        d.text((x-5, 519), ["WATER", "FIRE", "MISSILE"][i], font=font(12), fill="#829aaf")
    canvas.save(OUT/"Preview.png")


def make_comparison(base, glow):
    previous = OUT/"v1"
    if not (previous/"MagicBarrierSkill.png").exists():
        return
    old_base = unpremultiply(Image.open(previous/"MagicBarrierSkill.png").convert("RGBA"))
    old_glow = unpremultiply(Image.open(previous/"MagicBarrierSkill_Mask.png").convert("RGBA"))
    canvas = Image.new("RGB", (768, 506), "#0e1724")
    d = ImageDraw.Draw(canvas)
    d.text((24, 16), "PIXEL REFINEMENT / 56 x 56", font=font(20), fill="#ebf8ff")
    for i, (sprite, mask, title) in enumerate([(old_base,old_glow,"V1"), (base,glow,"V2 / REFINED")]):
        x=20+i*376
        d.rounded_rectangle((x,58,x+352,361),radius=14,fill="#162638",outline="#2a4255")
        d.text((x+20,74),title,font=font(17),fill="#bddeec")
        z=sprite.resize((224,224),Image.Resampling.NEAREST)
        canvas.paste(z,(x+64,112),z)
        d.text((x+8,375),"NATIVE / BASE + GLOW",font=font(12),fill="#8da9bb")
        combined=Image.alpha_composite(mask,sprite)
        for col,bg in enumerate(["#162638","#e3ecee"]):
            bx=x+col*182
            d.rounded_rectangle((bx,397,bx+170,486),radius=10,fill=bg)
            canvas.paste(sprite,(bx+18,414),sprite)
            canvas.paste(combined,(bx+97,414),combined)
    canvas.save(OUT/"Comparison.png")


def main():
    base = make_base()
    glow = make_glow(base)
    stats = {}
    for name, im in [("MagicBarrierSkill.png", base), ("MagicBarrierSkill_Mask.png", glow)]:
        encoded = premultiply(im)
        encoded.save(OUT/name)
        pixels=list(encoded.get_flattened_data())
        stats[name] = {"size": list(im.size), "visible_colors": len({p for p in pixels if p[3]}),
                       "alpha_levels": sorted({p[3] for p in pixels}), "visible_bbox": list(im.getbbox()),
                       "alpha_encoding": "premultiplied"}
        assert all(max(r, g, b) <= a for r, g, b, a in pixels)
    make_preview(base, glow)
    make_comparison(base, glow)
    (OUT/"metrics.json").write_text(json.dumps(stats, indent=2), encoding="utf-8")
    for name, s in stats.items():
        print(f'{name}: {s["size"]}, {s["visible_colors"]} visible RGBA colors, bbox={s["visible_bbox"]}')


if __name__ == "__main__":
    main()
