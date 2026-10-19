"""Build the Painkiller notebook pencil drawing and apply the hand-authored pixel jar."""
import argparse
from pathlib import Path
import subprocess
import sys
from urllib.parse import quote, unquote

from PIL import Image

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / 'ElainaModAlchemy/item/ExampleAssets'
UI = ROOT / 'ElainaModAlchemy/UI/Assets'
GENERATED = ROOT / 'Tools/AlchemyPlan/generated'
sys.path.insert(0, str(ROOT / 'Tools/AlchemyPlan'))
sys.path.insert(0, str(ROOT / 'Tools/AlchemyPreview'))
from pencil_style import convert
from prepare_pencil import premultiply
sys.path.insert(0, str(Path(__file__).resolve().parent))
import GeneratePainkillerPixel


def write_pixel():
    """The pixel jar lives in GeneratePainkillerPixel.py; apply it to the live assets."""
    sprite, grid = GeneratePainkillerPixel.build_sprite()
    GeneratePainkillerPixel.apply(sprite, grid)
    print(f'Painkiller pixel: {sprite.size}, bounds {sprite.getbbox()}')


def write_pencil(converter):
    original = (ASSETS / 'Painkiller.svg').read_text(encoding='utf-8')
    pencil = unquote(convert('data:image/svg+xml,' + quote(original),
                             profile='hatched', preserve_colors=True).split(',', 1)[1])
    source = GENERATED / 'Painkiller_Pencil.svg'
    source.write_text(pencil.rstrip() + '\n', encoding='utf-8')
    png = ASSETS / 'Painkiller_Pencil.png'
    subprocess.run([sys.executable, str(converter), str(source), str(png),
                    '--width', '100', '--height', '100', '--overwrite'], check=True)
    with Image.open(png) as image:
        ui = premultiply(image)
        ui.save(UI / 'Pencil/Painkiller_Pencil.png')
        alpha = ui.getchannel('A')
        Image.merge('RGBA', (alpha, alpha, alpha, alpha)).save(
            UI / 'Silhouettes/Painkiller_Silhouette.png')
    print('Painkiller pencil: 100x100 SVG, PNG, notebook copy and silhouette')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--converter', type=Path,
                        default=Path.home() / '.codex/skills/svg-to-png/scripts/convert_svg.py')
    args = parser.parse_args()
    write_pixel()
    write_pencil(args.converter)


if __name__ == '__main__':
    main()
