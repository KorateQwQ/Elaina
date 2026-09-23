"""Draw a 28x20 honey-star bread item sprite on its native pixel grid.

The existing UI illustration supplies the round loaf, three scores, honey drip,
and star crumbs. Its pixels are not sampled or downscaled: this sprite has a
separate silhouette, a limited palette, and explicitly placed integer pixels.
Requires Pillow; running this file only writes the standalone art study.
"""

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "ElainaModAlchemy/item/HoneyStarBreadPixel.png"
PALETTE = {
    "outline": "#3b2a36",
    "crust_dark": "#683c31",
    "crust": "#945232",
    "bread": "#bf7b42",
    "gold": "#dda057",
    "light": "#f1c276",
    "crumb": "#ffe1a0",
    "honey_edge": "#a56725",
    "honey_shadow": "#d58e28",
    "honey": "#f2ba38",
    "honey_light": "#ffe477",
    "star": "#fff1c9",
}


def build_sprite():
    image = Image.new("RGBA", (28, 20))
    draw = ImageDraw.Draw(image)
    c = PALETTE

    # A wide, domed loaf with a flat baked heel, rather than a round bun.
    outline = [
        (9, 2), (18, 2), (18, 3), (21, 3), (21, 4), (23, 4),
        (23, 5), (24, 5), (24, 6), (25, 6), (25, 8), (26, 8),
        (26, 10), (27, 10), (27, 15), (26, 15), (26, 16),
        (24, 16), (24, 17), (3, 17), (3, 16), (1, 16),
        (1, 15), (0, 15), (0, 10), (1, 10), (1, 8), (2, 8),
        (2, 6), (3, 6), (3, 5), (5, 5), (5, 4), (7, 4),
        (7, 3), (9, 3),
    ]
    draw.polygon(outline, fill=c["outline"])
    inside = [
        (9, 3), (17, 3), (17, 4), (20, 4), (20, 5), (22, 5),
        (22, 6), (23, 6), (23, 7), (24, 7), (24, 9), (25, 9),
        (25, 11), (26, 11), (26, 14), (25, 14), (25, 15),
        (23, 15), (23, 16), (4, 16), (4, 15), (2, 15),
        (2, 14), (1, 14), (1, 11), (2, 11), (2, 9), (3, 9),
        (3, 7), (4, 7), (4, 6), (6, 6), (6, 5), (8, 5),
        (8, 4), (9, 4),
    ]
    draw.polygon(inside, fill=c["bread"])
    draw.polygon([(9, 3), (16, 3), (16, 4), (20, 4), (20, 6),
                  (22, 6), (22, 8), (24, 8), (24, 11), (21, 11),
                  (21, 12), (6, 12), (6, 11), (3, 11), (3, 8),
                  (4, 8), (4, 6), (6, 6), (6, 5), (8, 5), (8, 4),
                  (9, 4)], fill=c["gold"])
    draw.polygon([(9, 4), (15, 4), (15, 5), (18, 5), (18, 7),
                  (15, 7), (15, 8), (7, 8), (7, 9), (4, 9),
                  (4, 7), (6, 7), (6, 6), (8, 6), (8, 5),
                  (9, 5)], fill=c["light"])
    draw.line([(2, 14), (5, 15), (22, 15), (25, 14)], fill=c["crust"], width=1)
    draw.line([(4, 16), (23, 16)], fill=c["crust_dark"], width=1)
    draw.line([(6, 15), (21, 15)], fill=c["bread"], width=1)

    # Three hand-placed scoring clusters. Cream rims give the cuts depth.
    for rim, cut in [
        ([(7, 6), (7, 7), (8, 8), (8, 9)], [(8, 6), (8, 7), (9, 8), (9, 9)]),
        ([(12, 4), (12, 6), (13, 7), (13, 8)], [(13, 4), (13, 6), (14, 7), (14, 8)]),
        ([(17, 5), (17, 6), (18, 7), (18, 9)], [(18, 5), (18, 6), (19, 7), (19, 9)]),
    ]:
        draw.line(rim, fill=c["crumb"], width=1)
        draw.line(cut, fill=c["crust"], width=1)

    # A small amber smear flows over the right shoulder in a single thick drip.
    draw.polygon([(18, 3), (20, 3), (20, 4), (23, 4), (23, 5),
                  (24, 5), (24, 7), (25, 7), (25, 10), (24, 11),
                  (23, 11), (23, 8), (22, 8), (22, 6), (20, 6),
                  (20, 5), (18, 5)], fill=c["honey_edge"])
    draw.polygon([(19, 4), (22, 4), (22, 5), (23, 5), (23, 6),
                  (24, 6), (24, 10), (23, 10), (23, 7), (22, 7),
                  (22, 5), (19, 5)], fill=c["honey"])
    draw.line([(19, 4), (21, 4), (22, 5)], fill=c["honey_light"], width=1)
    draw.point((23, 8), fill=c["honey_light"])
    draw.point((24, 10), fill=c["honey_shadow"])

    # A readable three-pixel star plus two crumbs; no detached glow pixels.
    draw.line([(7, 11), (7, 13)], fill=c["star"], width=1)
    draw.line([(6, 12), (8, 12)], fill=c["star"], width=1)
    draw.point((17, 12), fill=c["honey_light"])
    draw.point((20, 13), fill=c["crumb"])
    return image


if __name__ == "__main__":
    sprite = build_sprite()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(OUTPUT)
    print(f"Saved {OUTPUT.name}: {sprite.width}x{sprite.height}, RGBA")
