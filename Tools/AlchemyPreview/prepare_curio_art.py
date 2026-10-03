"""Rebuild the two reviewed curio drawings without changing their SVG originals."""
import argparse
from pathlib import Path
import subprocess
import sys
from urllib.parse import quote, unquote

from PIL import Image

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/AlchemyPlan'))
from pencil_style import convert
from prepare_pencil import premultiply


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--converter', type=Path, default=Path.home() / '.codex/skills/svg-to-png/scripts/convert_svg.py')
    args = parser.parse_args()
    assets = ROOT / 'ElainaModAlchemy/item/ExampleAssets'
    ui = ROOT / 'ElainaModAlchemy/UI/Assets'
    for name in ('AshenFacsimileDust', 'BottledRain'):
        original = (assets / (name + '.svg')).read_text(encoding='utf-8')
        pencil = unquote(convert('data:image/svg+xml,' + quote(original), profile='hatched').split(',', 1)[1])
        source = ROOT / 'Tools/AlchemyPlan/generated' / (name + '_Pencil.svg')
        source.write_text(pencil, encoding='utf-8')
        png = assets / (name + '_Pencil.png')
        subprocess.run([sys.executable, str(args.converter), str(source), str(png),
                        '--width', '100', '--height', '100', '--overwrite'], check=True)
        with Image.open(png) as image:
            premultiply(image).save(ui / 'Pencil' / png.name)
            alpha = image.convert('RGBA').getchannel('A')
            Image.merge('RGBA', (alpha, alpha, alpha, alpha)).save(ui / 'Silhouettes' / (name + '_Silhouette.png'))


if __name__ == '__main__':
    main()
