"""Bake RuriPotionEmpty (svg + png) and RuriPotionLiquidMask.png from RuriPotion.svg.

Mask channels: R = how much liquid shows through (flask interior minus the
overlay drawn above the liquid), G = flask interior, B = 0, A = 255.
"""
import re
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[2]
ITEM = REPO / "ElainaModAlchemy" / "item"
CONVERT = Path.home() / ".cursor" / "skills" / "svg-to-png" / "scripts" / "convert_svg.py"
SIZE = 50

LIQUID_GROUP = re.compile(r'\s*<!-- ruri liquid -->\s*<g clip-path="url\(#flask\)">.*?</g>', re.S)
LIQUID_DEFS = re.compile(r'\s*<(linearGradient|radialGradient) id="(ruri|ruriGlow)".*?</\1>', re.S)
GLASS_BODY = re.compile(r'\s*<!-- glass body -->\s*<path [^>]*/>', re.S)
FLASK_PATH = re.search(r'<clipPath id="flask">\s*<path d="([^"]+)"', (ITEM / "RuriPotion.svg").read_text("utf-8")).group(1)


def render(svg: str, out: Path) -> Image.Image:
    src = out.with_suffix(".svg")
    src.write_text(svg, "utf-8", newline="\n")
    subprocess.run([sys.executable, str(CONVERT), str(src), str(out), "--width", str(SIZE), "--height", str(SIZE), "--overwrite"],
                   check=True, stdout=subprocess.DEVNULL)
    return Image.open(out).convert("RGBA")


def main() -> None:
    source = (ITEM / "RuriPotion.svg").read_text("utf-8")
    empty = LIQUID_DEFS.sub("", LIQUID_GROUP.sub("", source))
    assert "ruri" not in empty, "liquid parts were not fully removed"
    (ITEM / "RuriPotionEmpty.svg").write_text(empty, "utf-8", newline="\n")

    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        render(empty, ITEM / "RuriPotionEmpty.png")
        overlay = render(GLASS_BODY.sub("", empty), tmp / "overlay.png").getchannel("A")
        interior = render(
            f'<svg xmlns="http://www.w3.org/2000/svg" width="{SIZE}" height="{SIZE}" viewBox="0 0 {SIZE} {SIZE}">'
            f'<path d="{FLASK_PATH}" fill="#ffffff"/></svg>', tmp / "interior.png").getchannel("A")

    r = Image.new("L", (SIZE, SIZE))
    r.putdata([i * (255 - o) // 255 for i, o in zip(interior.getdata(), overlay.getdata())])
    mask = Image.merge("RGBA", (r, interior, Image.new("L", (SIZE, SIZE), 0), Image.new("L", (SIZE, SIZE), 255)))
    mask.save(ITEM / "RuriPotionLiquidMask.png")

    rows = [y for y in range(SIZE) if any(interior.getpixel((x, y)) > 127 for x in range(SIZE))]
    print(f"interior rows {rows[0]}..{rows[-1]} (v {rows[0] / SIZE:.3f}..{(rows[-1] + 1) / SIZE:.3f})")


if __name__ == "__main__":
    main()
