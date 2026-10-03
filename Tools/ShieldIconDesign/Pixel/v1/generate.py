"""Draw an editable 56px barrier on an integer grid; Pillow is the only dependency.

The SVG is a visual reference only: it is never rasterized or sampled here.
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
    "film": (91, 174, 201, 24),
    "film_shade": (52, 135, 167, 48),
    "film_light": (142, 222, 238, 62),
    "reflection": (198, 245, 250, 92),
    "fold": (110, 214, 234, 130),
    "outline": (39, 99, 128, 255),
    "deep": (54, 143, 173, 255),
    "cyan": (86, 194, 217, 255),
    "ice": (138, 228, 240, 255),
    "pale": (195, 249, 253, 255),
    "white": (239, 255, 255, 255),
}

# Deliberately placed horizontal runs and stepped diagonals, including two
# inward curls in the top-right quadrant. All points lie on the final grid.
SHELL = [
    (25, 6), (30, 6), (34, 7), (37, 8),
    (35, 9), (33, 9), (32, 10), (32, 11), (33, 12),
    (35, 13), (38, 14), (41, 14), (43, 15),
    (41, 17), (40, 18), (40, 20), (41, 22), (43, 23),
    (46, 24), (49, 24), (49, 27), (50, 28), (50, 33),
    (49, 37), (47, 41), (44, 45), (40, 48),
    (36, 50), (32, 51), (23, 51), (19, 50),
    (15, 48), (11, 45), (8, 41), (6, 37),
    (5, 33), (5, 25), (6, 21), (8, 17),
    (11, 13), (15, 10), (19, 8), (23, 7),
]
CURL = SHELL[3:23]


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
    base = Image.new("RGBA", SIZE)
    d = ImageDraw.Draw(base)
    d.polygon(SHELL, fill=PALETTE["film"])
    # Flat translucent facets, not a continuous or downsampled SVG gradient.
    d.polygon([(8, 32), (11, 39), (16, 45), (23, 48), (32, 48),
               (39, 45), (44, 39), (47, 31), (47, 37), (43, 44),
               (36, 49), (24, 49), (17, 47), (11, 42)],
              fill=PALETTE["film_shade"])
    d.polygon([(9, 22), (13, 16), (18, 12), (24, 10), (29, 10),
               (31, 13), (36, 16), (29, 15), (24, 17), (19, 20),
               (14, 23), (9, 24)], fill=PALETTE["film_light"])
    d.polygon([(10, 21), (14, 16), (19, 12), (24, 11), (28, 11),
               (23, 13), (18, 16), (15, 19)], fill=PALETTE["reflection"])
    d.polygon([(39, 17), (37, 20), (38, 23), (42, 26), (45, 27),
               (42, 24), (39, 22)], fill=PALETTE["fold"])
    d.polygon([(32, 10), (30, 12), (30, 14), (33, 17), (36, 18),
               (33, 15), (32, 13)], fill=PALETTE["fold"])
    # Full silhouette uses a narrow dark cyan guard and luminous pixel core.
    d.line(SHELL+[SHELL[0]], fill=PALETTE["outline"], width=3)
    d.line(SHELL+[SHELL[0]], fill=PALETTE["cyan"], width=1)
    left = [(7, 35), (6, 32), (6, 25), (7, 21), (9, 17),
            (12, 13), (16, 10), (20, 8), (25, 7), (30, 7), (34, 8)]
    d.line(left, fill=PALETTE["pale"], width=1)
    d.line([(7, 25), (8, 21), (10, 17), (13, 13), (17, 10),
            (21, 8), (26, 7), (30, 7)], fill=PALETTE["white"], width=1)
    d.line(CURL, fill=PALETTE["ice"], width=1)
    d.line([(35, 9), (33, 9), (32, 10), (32, 11), (33, 12),
            (35, 13), (38, 14), (41, 14), (43, 15)],
           fill=PALETTE["white"], width=1)
    d.line([(40, 19), (40, 20), (41, 22), (43, 23), (46, 24), (49, 24)],
           fill=PALETTE["pale"], width=1)
    d.line([(14, 47), (19, 49), (23, 50), (32, 50), (36, 49),
            (40, 47), (44, 44), (47, 40)], fill=PALETTE["ice"], width=1)
    d.line([(23, 50), (32, 50), (36, 49)], fill=PALETTE["pale"], width=1)
    # Short separated inner arc adds thickness to the surviving membrane.
    d.line([(33, 46), (37, 45), (41, 42), (44, 38), (45, 34)],
           fill=PALETTE["deep"], width=1)
    d.line([(37, 45), (41, 42), (44, 38)], fill=PALETTE["ice"], width=1)
    d.line([(11, 22), (13, 18), (17, 14), (21, 12), (25, 11)],
           fill=PALETTE["pale"], width=1)
    # Two small detached curls and two clear star clusters.
    d.polygon([(38, 5), (40, 5), (42, 3), (42, 6), (43, 7), (40, 7)],
              fill=PALETTE["ice"])
    d.point((42, 3), fill=PALETTE["white"])
    d.line([(47, 20), (49, 21), (51, 20), (52, 19)], fill=PALETTE["ice"], width=1)
    for x, y, radius in [(47, 12, 2), (10, 40, 2)]:
        d.line([(x-radius, y), (x+radius, y)], fill=PALETTE["ice"], width=1)
        d.line([(x, y-radius), (x, y+radius)], fill=PALETTE["pale"], width=1)
        d.point((x, y), fill=PALETTE["white"])
    return base


def make_glow(base):
    # Emit only from opaque edge/light pixels; leave the transparent centre dim.
    source = Image.new("L", SIZE)
    source.putdata([255 if a == 255 and r >= 195 else
                    185 if a == 255 and r >= 138 else
                    105 if a == 255 and r >= 86 else 0
                    for r, g, b, a in base.get_flattened_data()])
    near = source.filter(ImageFilter.GaussianBlur(1.2))
    far = source.filter(ImageFilter.GaussianBlur(2.8))
    alpha = Image.new("L", SIZE)
    alpha.putdata([min(235, round(s*.24+n*1.12+f*.65))
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
    (OUT/"metrics.json").write_text(json.dumps(stats, indent=2), encoding="utf-8")
    for name, s in stats.items():
        print(f'{name}: {s["size"]}, {s["visible_colors"]} visible RGBA colors, bbox={s["visible_bbox"]}')


if __name__ == "__main__":
    main()
