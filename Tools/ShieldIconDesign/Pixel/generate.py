"""Draw an editable 56px barrier on an integer grid; Pillow is the only dependency.

Refine the existing pixel art directly; no SVG is loaded or consulted.
Draw a pure-white alpha source, compose it over a coloured glow layer, then
premultiply only the completed Skill texture for the local tML pipeline.
Only the separate glow is blurred; the original uses discrete alpha levels.
"""

from pathlib import Path
import json

from PIL import Image, ImageDraw, ImageFilter, ImageFont


OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
SIZE = (56, 56)
ALPHA_LEVELS = {
    "film": 10,
    "film_shade": 18,
    "film_light": 36,
    "reflection": 76,
    "fold": 108,
    "outline": 46,
    "deep": 82,
    "cyan": 134,
    "ice": 188,
    "pale": 228,
    "white": 255,
}
PALETTE = {name: (255, 255, 255, alpha) for name, alpha in ALPHA_LEVELS.items()}

# Native-grid rounded cuts: a small upper bite and a wider, shallower lower
# bowl, meeting in a pointed lip as in the supplied pixel sketch.
SHELL_BOUNDS = (5, 6, 50, 51)
DISSOLVE_CIRCLES = [(33, 7, 43, 17), (35, 15, 51, 29)]


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
    base = Image.new("RGBA", SIZE, (255, 255, 255, 0))
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
        d.point([p for p in points if inner3.getpixel(p)], fill=PALETTE[name])

    # Two tiny crosses frame one larger, filled diamond-shaped sparkle.
    for x, y, rx, ry in [(43,5,1,2), (49,24,1,1)]:
        d.line([(x-rx,y),(x+rx,y)],fill=PALETTE["ice"],width=1)
        d.line([(x,y-ry),(x,y+ry)],fill=PALETTE["pale"],width=1)
        d.point((x,y),fill=PALETTE["white"])
    d.polygon([(47,10),(48,13),(50,14),(48,15),
               (47,18),(46,15),(44,14),(46,13)], fill=PALETTE["ice"])
    d.polygon([(47,11),(48,14),(47,17),(46,14)], fill=PALETTE["pale"])
    d.point([(47,13),(46,14),(47,14),(48,14),(47,15)],fill=PALETTE["white"])
    # Attached glint on the lower-left edge.
    d.point([(10,39),(10,40),(10,41),(9,40),(11,40)],fill=PALETTE["pale"])
    d.point((10,40),fill=PALETTE["white"])
    return base


def make_glow(base):
    # Emission follows source alpha, never RGB: the original is entirely white.
    # Keep the low-opacity film quiet, and tint the rim and stars from this layer.
    source = base.getchannel("A").point(
        lambda a: 255 if a >= 228 else 190 if a >= 188 else
        135 if a >= 134 else 65 if a >= 82 else 0)
    near = source.filter(ImageFilter.GaussianBlur(1.05))
    far = source.filter(ImageFilter.GaussianBlur(2.5))
    alpha = Image.new("L", SIZE)
    alpha.putdata([min(235, round(s*.2+n*1.1+f*.5))
                   for s, n, f in zip(source.get_flattened_data(),
                                     near.get_flattened_data(), far.get_flattened_data())])
    glow = Image.new("RGBA", SIZE, (62, 221, 250, 0))
    glow.putalpha(alpha)
    return glow


def font(size):
    try:
        return ImageFont.truetype("segoeui.ttf", size)
    except OSError:
        return ImageFont.load_default(size=size)


def make_preview(base, glow, combined):
    canvas = Image.new("RGB", (960, 590), "#0e1724")
    d = ImageDraw.Draw(canvas)
    d.text((32, 21), "ELAINA / MAGIC BARRIER", font=font(23), fill="#eefaff")
    d.text((32, 53), "56 x 56 px   /   white + alpha source > glow composite > premultiplied Skill", font=font(14), fill="#8fa8bb")
    for i, (im, label) in enumerate([(base, "WHITE / ALPHA SOURCE"), (glow, "COLOURED GLOW"), (combined, "COMPOSITED SKILL")]):
        x = 24+i*312
        d.rounded_rectangle((x, 89, x+288, 373), radius=15, fill="#162638", outline="#2a4255")
        zoom = im.resize((224, 224), Image.Resampling.NEAREST)
        canvas.paste(zoom, (x+32, 102), zoom)
        d.text((x+20, 343), label+" / 4x", font=font(14), fill="#aec7d6")
    d.text((32, 395), "NATIVE SIZE / SOURCE + FINAL", font=font(14), fill="#aec7d6")
    for i, color in enumerate(["#132234", "#e3ecee"]):
        x=24+i*228
        d.rounded_rectangle((x, 424, x+210, 560), radius=12, fill=color)
        canvas.paste(base, (x+28, 447), base)
        canvas.paste(combined, (x+123, 447), combined)
        ink="#829aaf" if i == 0 else "#4d6574"
        d.text((x+31, 519), "SOURCE", font=font(12), fill=ink)
        d.text((x+133, 519), "FINAL", font=font(12), fill=ink)
    d.text((515, 395), "EXISTING SKILL ICONS / NATIVE SIZE", font=font(14), fill="#aec7d6")
    for i, name in enumerate(["Water/WaterBallSkill", "Fire/FireBurstSkill", "MagicMissile/MagicMissileSkill"]):
        p=ROOT/"ElainaModSkills"/"Skills"/(name+".png")
        im=unpremultiply(Image.open(p).convert("RGBA"))
        x=534+i*126
        canvas.paste(im, (x, 449), im)
        d.text((x-5, 519), ["WATER", "FIRE", "MISSILE"][i], font=font(12), fill="#829aaf")
    canvas.save(OUT/"Preview.png")


def make_comparison(combined):
    previous = OUT/"before-layer-fix"
    if not (previous/"MagicBarrierSkill.png").exists():
        return
    old_base = unpremultiply(Image.open(previous/"MagicBarrierSkill.png").convert("RGBA"))
    canvas = Image.new("RGB", (768, 506), "#0e1724")
    d = ImageDraw.Draw(canvas)
    d.text((24, 16), "SKILL EXPORT / 56 x 56", font=font(20), fill="#ebf8ff")
    for i, (sprite, title) in enumerate([(old_base,"PREVIOUS EXPORT"), (combined,"COMPOSITED SKILL")]):
        x=20+i*376
        d.rounded_rectangle((x,58,x+352,361),radius=14,fill="#162638",outline="#2a4255")
        d.text((x+20,74),title,font=font(17),fill="#bddeec")
        z=sprite.resize((224,224),Image.Resampling.NEAREST)
        canvas.paste(z,(x+64,112),z)
        d.text((x+8,375),"NATIVE SIZE / DARK + LIGHT",font=font(12),fill="#8da9bb")
        for col,bg in enumerate(["#162638","#e3ecee"]):
            bx=x+col*182
            d.rounded_rectangle((bx,397,bx+170,486),radius=10,fill=bg)
            canvas.paste(sprite,(bx+57,414),sprite)
    canvas.save(OUT/"Comparison.png")


def main():
    base = make_base()
    glow = make_glow(base)
    # Both source layers use straight alpha. White source goes over blue glow;
    # premultiplication occurs once, only after this composition is complete.
    combined = Image.alpha_composite(glow, base)
    final = premultiply(combined)
    assert all((r,g,b) == (255,255,255) for r,g,b,a in base.get_flattened_data())
    stats = {}
    exports = [
        ("MagicBarrierSkill_Original.png", base, "straight"),
        ("MagicBarrierSkill_Mask.png", glow, "straight"),
        ("MagicBarrierSkill_Composite.png", combined, "straight"),
        ("MagicBarrierSkill.png", final, "premultiplied"),
    ]
    for name, im, encoding in exports:
        im.save(OUT/name)
        pixels=list(im.get_flattened_data())
        stats[name] = {"size": list(im.size), "visible_colors": len({p for p in pixels if p[3]}),
                       "alpha_levels": sorted({p[3] for p in pixels}), "visible_bbox": list(im.getbbox()),
                       "alpha_encoding": encoding}
        if encoding == "premultiplied":
            assert all(max(r, g, b) <= a for r, g, b, a in pixels)
    make_preview(base, glow, combined)
    make_comparison(combined)
    (OUT/"metrics.json").write_text(json.dumps(stats, indent=2), encoding="utf-8")
    for name, s in stats.items():
        print(f'{name}: {s["size"]}, {s["visible_colors"]} visible RGBA colors, bbox={s["visible_bbox"]}')


if __name__ == "__main__":
    main()
