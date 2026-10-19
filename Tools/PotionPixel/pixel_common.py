"""Shared helpers for the hand-authored alchemy pixel sprites (see README.md)."""

from PIL import Image

NOTEBOOK_PAPER = (226, 220, 238)


def blacken_silhouette(grid, keep=""):
    """Every opaque pixel touching transparency or the canvas edge becomes pure black "O".

    Symbols in `keep` (detached, deliberately unoutlined motes such as steam) are left as they are
    and count as transparency for their neighbours.
    """
    h, w = len(grid), len(grid[0])

    def clear(x, y):
        return not (0 <= x < w and 0 <= y < h) or grid[y][x] == "." or grid[y][x] in keep

    edge = [(x, y) for y in range(h) for x in range(w) if grid[y][x] != "." and grid[y][x] not in keep
            and any(clear(nx, ny) for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))]
    for x, y in edge:
        grid[y][x] = "O"
    return grid


def render(grid, palette):
    image = Image.new("RGBA", (len(grid[0]), len(grid)))
    for y, row in enumerate(grid):
        for x, symbol in enumerate(row):
            value = palette[symbol].lstrip("#")
            value += "ff" * (len(value) == 6)
            image.putpixel((x, y), tuple(int(value[i:i + 2], 16) for i in range(0, 8, 2)))
    return image


def outline_ring(image):
    """One-pixel four-neighbour ring on a +1 padded canvas, as bake_redesign.py bakes it."""
    w, h = image.size
    alpha = image.getchannel("A").load()
    ring = Image.new("RGBA", (w + 2, h + 2))
    for y in range(h + 2):
        for x in range(w + 2):
            sx, sy = x - 1, y - 1
            if 0 <= sx < w and 0 <= sy < h and alpha[sx, sy]:
                continue
            if any(0 <= nx < w and 0 <= ny < h and alpha[nx, ny]
                   for nx, ny in ((sx - 1, sy), (sx + 1, sy), (sx, sy - 1), (sx, sy + 1))):
                ring.putpixel((x, y), (255, 255, 255, 255))
    return ring


def compare(sprites, scales=(1, 2, 8), backgrounds=((58, 52, 82), NOTEBOOK_PAPER)):
    """Old and new side by side at each scale on dark and notebook-paper backgrounds."""
    pad = 4
    cell_w = max(s.width for s in sprites)
    cell_h = max(s.height for s in sprites)
    blocks = []
    for bg in backgrounds:
        for scale in scales:
            w = (cell_w * scale + pad * scale) * len(sprites) + pad * scale
            h = cell_h * scale + pad * 2 * scale
            block = Image.new("RGBA", (w, h), bg + (255,))
            x = pad * scale
            for sprite in sprites:
                big = sprite.resize((sprite.width * scale, sprite.height * scale), Image.Resampling.NEAREST)
                block.alpha_composite(big, (x, h - big.height - pad * scale))
                x += cell_w * scale + pad * scale
            blocks.append(block)
    out = Image.new("RGBA", (max(b.width for b in blocks), sum(b.height for b in blocks)))
    y = 0
    for block in blocks:
        out.alpha_composite(block, (0, y))
        y += block.height
    return out


def write_previews(out, name, old, sprite):
    """The study previews every generator writes: native PNG, 8x, on-card 4x and old/new compare."""
    out.mkdir(parents=True, exist_ok=True)
    sprite.save(out / f"{name}_Pixel_New.png")
    sprite.resize((sprite.width * 8, sprite.height * 8), Image.Resampling.NEAREST).save(out / f"{name}_Pixel_x8.png")
    card = Image.new("RGBA", (sprite.width + 16, sprite.height + 16), NOTEBOOK_PAPER + (255,))
    card.alpha_composite(sprite, (8, 8))
    card.resize((card.width * 4, card.height * 4), Image.Resampling.NEAREST).save(out / f"{name}_OnCard_x4.png")
    compare([old, sprite]).save(out / f"{name}_Compare.png")
