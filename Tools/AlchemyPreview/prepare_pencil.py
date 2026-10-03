"""Premultiply active ExampleAssets artwork for tML's rawimg/AlphaBlend pipeline.

Keep original straight-alpha PNGs unchanged: HTML continues to use them directly.
Only referenced notebook artwork is exported; item Pixel textures are not touched.
"""
from pathlib import Path
import argparse
import json
from PIL import Image


def premultiply(image):
    image = image.convert('RGBA')
    image.putdata([((r*a+127)//255, (g*a+127)//255, (b*a+127)//255, a)
                   for r, g, b, a in image.get_flattened_data()])
    return image


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Fail on stale or non-premultiplied UI copies without writing files.')
    parser.add_argument('--only', nargs='+', metavar='ART', help='Limit to named artwork, e.g. BloodthirstPotion PurificationDew.')
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    plan = json.loads((repo / 'Tools/AlchemyPlan/plan.json').read_text(encoding='utf-8'))
    source = repo / 'ElainaModAlchemy/item/ExampleAssets'
    target = repo / 'ElainaModAlchemy/UI/Assets/Pencil'
    names = {entry['art'] for entry in plan['items']}
    names.update(material.get('art') or 'Materials/' + material['id'] for material in plan['materials'])
    if args.only:
        unknown = set(args.only) - names
        if unknown:
            parser.error('Unknown artwork: ' + ', '.join(sorted(unknown)))
        names = set(args.only)
    stale = []
    for name in sorted(names):
        with Image.open(source / (name + '_Pencil.png')) as original:
            expected = premultiply(original)
        destination = target / (name + '_Pencil.png')
        if destination.exists():
            with Image.open(destination) as actual:
                if actual.size == expected.size and actual.convert('RGBA').tobytes() == expected.tobytes():
                    continue
        stale.append(name)
        if not args.check:
            destination.parent.mkdir(parents=True, exist_ok=True)
            expected.save(destination)
    if args.check and stale:
        parser.exit(1, 'Stale or incorrect UI pencil alpha: ' + ', '.join(stale) +
                    '\nRun python Tools/AlchemyPreview/prepare_pencil.py to rebuild from the straight-alpha originals.\n')
    if args.check:
        print(f'Checked {len(names)} pencil textures: exact premultiplied copies of source.')
    else:
        print(f'Prepared {len(names)} pencil textures; updated {len(stale)}: ' + ', '.join(stale))


if __name__ == '__main__':
    main()
