"""Synchronize approved potion silhouettes with the notebook's pencil artwork.

Python + Pillow + resvg_py. Original round1 compositions and rotations are
preserved under output/round1/approved and installed as SVG, straight
PNG, premultiplied UI PNG and matching locked silhouette. Item pixels are not
rendered from these SVGs and are never changed by this command.
"""
import argparse
import importlib.util
from io import BytesIO
from pathlib import Path
from PIL import Image
import resvg_py

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
REFERENCE = HERE/'output/round1'
APPROVED = REFERENCE/'approved'
ARTS = ('StarPowerPotion', 'ConcentrationPotion', 'ResonancePotion', 'FeatherlightPotion', 'PurificationDew')
def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def source_for(art):
    path = REFERENCE/('palettes/PurificationDew_Iridescent.svg' if art == 'PurificationDew' else f'svg/{art}.svg')
    return path.read_text(encoding='utf-8')


def pencil_for(art):
    # Notebook artwork keeps the original SVG composition, including its tilt.
    # Pixel art has an independent upright grid for clean item-scale edges.
    path = REFERENCE/('palettes/PurificationDew_Iridescent_Pencil.svg' if art == 'PurificationDew' else f'pencil/{art}_Pencil.svg')
    return path.read_text(encoding='utf-8')


def apply(names):
    exporter = load('pencil_export',ROOT/'Tools/AlchemyPreview/prepare_pencil.py')
    APPROVED.mkdir(parents=True,exist_ok=True)
    assets = ROOT/'ElainaModAlchemy/item/ExampleAssets'
    for art in names:
        source = source_for(art)
        pencil = pencil_for(art)
        for folder in (APPROVED,assets):
            (folder/f'{art}.svg').write_text(source,encoding='utf-8')
            (folder/f'{art}_Pencil.svg').write_text(pencil,encoding='utf-8')
        png = resvg_py.svg_to_bytes(svg_string=pencil,width=100,height=100)
        straight = Image.open(BytesIO(png)).convert('RGBA')
        straight.save(assets/f'{art}_Pencil.png')
        straight.save(APPROVED/f'{art}_Pencil.png')
        ui = exporter.premultiply(straight)
        ui.save(ROOT/f'ElainaModAlchemy/UI/Assets/Pencil/{art}_Pencil.png')
        alpha = ui.getchannel('A')
        Image.merge('RGBA',(alpha,alpha,alpha,alpha)).save(ROOT/f'ElainaModAlchemy/UI/Assets/Silhouettes/{art}_Silhouette.png')
        print('Applied notebook artwork:',art)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--only',nargs='+',choices=ARTS,help='Limit export to selected artwork.')
    args = parser.parse_args()
    apply(args.only or ARTS)


if __name__ == '__main__':
    main()
