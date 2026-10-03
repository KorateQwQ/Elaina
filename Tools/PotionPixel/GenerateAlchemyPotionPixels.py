"""Rebuild alchemy sprites from their native pixel drawings.

The sprites are intentionally hand-authored integer drawings. They share
one outline/light language while keeping each approved bottle silhouette:
diamond, triangle, upright tube, round bottle, and pearl-rainbow dropper.
Painkiller and the Moon Dew material remain unchanged. Run explicitly to update production
ExampleAssets; no resampling or antialiasing is used.
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "ElainaModAlchemy/item/ExampleAssets"
OUT_PREVIEW = ROOT / "Tools/PotionPixel/preview"

O = "#332f43"
GLASS_DARK, GLASS, GLASS_LIGHT, WHITE = "#526f86", "#789eb6", "#b9d9e2", "#edf8f4"
GOLD_DARK, GOLD, GOLD_LIGHT = "#8b643d", "#bd9560", "#f0d29a"
PINK_DARK, PINK, PINK_LIGHT = "#7b3b64", "#bf6386", "#f0a6be"
BLUE_DARK, BLUE, BLUE_LIGHT = "#24275b", "#41479b", "#8698e2"
PURPLE_DARK, PURPLE, PURPLE_LIGHT = "#51366d", "#9564af", "#d3a8df"
TEAL_DARK, TEAL, TEAL_LIGHT = "#3e6876", "#6ab3b5", "#d1ece2"
RAIN_BLUE, RAIN_PURPLE, RAIN_PINK, RAIN_GOLD = "#94d6e9", "#b3a2ed", "#e8b4d7", "#f2d99c"


def sprite():
    image = Image.new("RGBA", (20, 28), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def poly(d, points, color):
    d.polygon(points, fill=color)


def line(d, points, color, width=1):
    d.line(points, fill=color, width=width)


def cap(d, x0=6, x1=13):
    poly(d, [(x0, 0), (x1, 0), (x1 + 1, 1), (x1, 4), (x0, 4), (x0 - 1, 2)], O)
    poly(d, [(x0 + 1, 1), (x1 - 1, 1), (x1, 2), (x1 - 1, 3), (x0, 3), (x0, 2)], GOLD)
    line(d, [(x0 + 1, 1), (x1 - 1, 1)], GOLD_LIGHT)
    poly(d, [(x0 - 1, 4), (x1 + 1, 4), (x1 + 1, 6), (x0 - 1, 6)], O)
    line(d, [(x0, 5), (x1, 5)], GLASS_LIGHT)


def bow(d, x, y, color=PINK):
    """Two stepped ribbon loops and a knot, matching the approved round SVG."""
    poly(d, [(x, y), (x - 3, y - 1), (x - 4, y), (x - 3, y + 2), (x, y + 1)], PINK_DARK)
    poly(d, [(x, y), (x - 2, y), (x - 3, y), (x - 2, y + 1), (x, y + 1)], color)
    poly(d, [(x + 1, y), (x + 4, y - 1), (x + 5, y), (x + 4, y + 2), (x + 1, y + 1)], PINK_DARK)
    poly(d, [(x + 1, y), (x + 3, y), (x + 4, y), (x + 3, y + 1), (x + 1, y + 1)], color)
    poly(d, [(x, y), (x + 1, y), (x + 1, y + 2), (x, y + 2)], PINK_LIGHT)


def diamond_bottle(liquid, emblem):
    im, d = sprite(); cap(d)
    poly(d, [(5, 6), (14, 6), (18, 12), (11, 27), (8, 27), (2, 12)], O)
    poly(d, [(6, 7), (13, 7), (16, 12), (10, 25), (4, 12)], GLASS_DARK)
    poly(d, [(6, 12), (10, 13), (15, 11), (16, 13), (10, 25), (4, 12)], liquid)
    poly(d, [(7, 11), (10, 12), (14, 11), (15, 13), (10, 18), (5, 13)], liquid)
    line(d, [(6, 8), (5, 11), (4, 12)], GLASS_LIGHT)
    line(d, [(10, 13), (10, 24)], GLASS_LIGHT)
    emblem(d, 10, 16)
    return im


def triangle_bottle(liquid, emblem):
    im, d = sprite(); cap(d)
    poly(d, [(5, 6), (14, 6), (18, 25), (16, 27), (3, 27), (1, 25)], O)
    poly(d, [(6, 7), (13, 7), (16, 25), (4, 25)], GLASS_DARK)
    poly(d, [(4, 15), (16, 15), (18, 25), (16, 27), (4, 27), (2, 25)], liquid)
    line(d, [(5, 8), (3, 24)], GLASS_LIGHT)
    line(d, [(13, 8), (16, 24)], GLASS)
    line(d, [(4, 15), (16, 15)], GLASS_LIGHT)
    emblem(d, 10, 20)
    bow(d, 14, 6, PINK_LIGHT)
    return im


def diagonal_tube(liquid, emblem):
    im, d = sprite()
    # The 32 degree slope is stepped with a consistent two-pixel glass edge.
    poly(d, [(13, 1), (17, 3), (7, 25), (4, 27), (2, 25), (12, 2)], O)
    poly(d, [(13, 3), (15, 4), (6, 24), (5, 25), (4, 24)], GLASS_DARK)
    poly(d, [(13, 7), (15, 8), (8, 23), (6, 24), (5, 23)], liquid)
    line(d, [(13, 3), (5, 23)], GLASS_LIGHT)
    line(d, [(15, 5), (7, 24)], GLASS)
    # Rubber bulb and metal collar at the upper-right end.
    poly(d, [(11, 1), (14, 0), (17, 2), (18, 4), (16, 6), (13, 5)], O)
    poly(d, [(13, 1), (15, 1), (17, 3), (16, 4), (13, 4)], GLASS_LIGHT)
    poly(d, [(11, 5), (16, 6), (15, 8), (10, 7)], O)
    line(d, [(12, 6), (15, 7)], GOLD_LIGHT)
    emblem(d, 9, 17)
    return im


def round_bottle(liquid, emblem):
    im, d = sprite(); cap(d, 7, 12)
    # Explicit stepped rows preserve the round silhouette at native size.
    outline = {7: (7, 12), 8: (5, 14), 9: (4, 15), 10: (3, 16),
               11: (2, 17), 12: (2, 17), 13: (1, 18), 14: (1, 18),
               15: (1, 18), 16: (1, 18), 17: (1, 18), 18: (1, 18),
               19: (1, 18), 20: (1, 18), 21: (2, 17), 22: (2, 17),
               23: (3, 16), 24: (4, 15), 25: (5, 14), 26: (7, 12)}
    for y, (left, right) in outline.items():
        line(d, [(left, y), (right, y)], O)
        if y not in (7, 26): line(d, [(left + 1, y), (right - 1, y)], GLASS_DARK)
    for y in range(16, 26):
        left, right = outline[y]
        line(d, [(left + 1, y), (right - 1, y)], liquid)
    line(d, [(4, 10), (3, 14), (3, 19)], GLASS_LIGHT)
    emblem(d, 10, 20)
    bow(d, 14, 6, PINK_LIGHT)
    return im


def blood_drop(d, x, y):
    poly(d, [(x, y - 3), (x + 2, y), (x + 2, y + 2), (x + 1, y + 3), (x - 1, y + 3), (x - 2, y + 1)], PINK_LIGHT)
    d.point((x, y), fill=WHITE)


def stars(d, x, y):
    line(d, [(x - 2, y), (x + 2, y)], GOLD_LIGHT)
    line(d, [(x, y - 2), (x, y + 2)], GOLD_LIGHT)
    d.point((x + 4, y + 3), fill=GOLD_LIGHT)


def reticle(d, x, y):
    line(d, [(x - 2, y), (x + 2, y)], TEAL_LIGHT)
    line(d, [(x, y - 2), (x, y + 2)], TEAL_LIGHT)
    d.point((x, y), fill=WHITE)


def focus_label(d, x, y):
    poly(d, [(x - 3, y - 3), (x + 3, y - 3), (x + 3, y + 3), (x - 3, y + 3)], O)
    poly(d, [(x - 2, y - 2), (x + 2, y - 2), (x + 2, y + 2), (x - 2, y + 2)], "#e7ddc9")
    reticle(d, x, y)


def resonance(d, x, y):
    d.point((x, y), fill=WHITE)
    line(d, [(x - 2, y - 2), (x - 3, y), (x - 2, y + 2)], PURPLE_LIGHT)
    line(d, [(x + 2, y - 2), (x + 3, y), (x + 2, y + 2)], PURPLE_LIGHT)


def feather(d, x, y):
    poly(d, [(x - 2, y + 3), (x - 1, y - 3), (x + 2, y - 5), (x + 3, y - 1), (x + 1, y + 3)], TEAL_LIGHT)
    line(d, [(x - 2, y + 4), (x + 2, y - 4)], TEAL_DARK)
    line(d, [(x - 1, y), (x + 1, y - 1)], TEAL_DARK)
    line(d, [(x, y - 2), (x + 2, y - 3)], TEAL_DARK)


def pearl_drop(d, x, y):
    poly(d, [(x, y - 3), (x + 2, y), (x + 2, y + 2), (x, y + 3), (x - 2, y + 2), (x - 2, y)], RAIN_PURPLE)
    d.point((x, y), fill=RAIN_BLUE)
    d.point((x + 1, y + 1), fill=RAIN_PINK)


def authored_sprite(filename, function='build_sprite'):
    import importlib.util
    spec = importlib.util.spec_from_file_location(Path(filename).stem, Path(__file__).with_name(filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return getattr(module, function)()


def rainbow_dropper():
    return authored_sprite('GeneratePurificationDewUprightPixel.py')


def build_all():
    # This separately reviewed sprite has no dedicated Python generator here;
    # keep its approved design PNG instead of restoring the old placeholder.
    with Image.open(ROOT / 'Tools/PotionBottleDesigns/output/round1/pixel/BloodthirstPotion.png') as image:
        bloodthirst = image.convert('RGBA')
    return {
        "BloodthirstPotion": bloodthirst,
        "StarPowerPotion": authored_sprite('GenerateStarAndMoonPotionPixels.py', 'starpower'),
        "MoonDewElixir": authored_sprite('GenerateStarAndMoonPotionPixels.py', 'moon_dew'),
        "ConcentrationPotion": authored_sprite('GenerateConcentrationPotionPixel.py'),
        "ResonancePotion": authored_sprite('GenerateResonanceAndFeatherlightPixels.py', 'resonance'),
        "FeatherlightPotion": authored_sprite('GenerateResonanceAndFeatherlightPixels.py', 'featherlight'),
        "PurificationDew": rainbow_dropper(),
    }


def save_preview(sprites):
    OUT_PREVIEW.mkdir(parents=True, exist_ok=True)
    row_height = max(image.height for image in sprites.values()) * 3 + 24
    canvas = Image.new("RGBA", (480, row_height * ((len(sprites) + 2) // 3)), (35, 29, 45, 255))
    draw = ImageDraw.Draw(canvas)
    for i, (name, image) in enumerate(sprites.items()):
        x, y = (i % 3) * 160, (i // 3) * row_height
        enlarged = image.resize((image.width * 3, image.height * 3), Image.Resampling.NEAREST)
        canvas.alpha_composite(enlarged, (x + 40, y + 2))
        draw.text((x + 8, y + row_height - 18), name.replace("Potion", ""), fill="#e9dceb")
    canvas.save(OUT_PREVIEW / "alchemy-potions-pixel-grid.png")
    for name, image in sprites.items():
        image.resize((image.width * 8, image.height * 8), Image.Resampling.NEAREST).save(OUT_PREVIEW / f"{name}_Pixel_x8.png")


def main():
    sprites = build_all()
    save_preview(sprites)
    for name, image in sprites.items():
        target = OUTPUT / f"{name}_Pixel.png"
        image.save(target)
        colors = {pixel for pixel in image.getdata() if pixel[3]}
        expected_size = {"PurificationDew": (19, 34), "ConcentrationPotion": (19, 38),
                         "ResonancePotion": (31, 39), "FeatherlightPotion": (19, 38),
                         "StarPowerPotion": (31, 36), "MoonDewElixir": (33, 40),
                         "BloodthirstPotion": (30, 40)}.get(name, (20, 28))
        assert image.size == expected_size
        assert all(pixel[3] == 255 for pixel in image.getdata() if pixel[3])
        assert image.getchannel("A").getbbox() is not None
        print(f"Saved {target.name}: {image.width}x{image.height}, {len(colors)} opaque colors")
    print(f"Preview: {OUT_PREVIEW / 'alchemy-potions-pixel-grid.png'}")


if __name__ == "__main__":
    main()
