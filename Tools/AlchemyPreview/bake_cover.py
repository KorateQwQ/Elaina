"""Bake the alchemy cover with the local svg-to-png skill and premultiplied RGBA.

    python Tools/AlchemyPreview/bake_cover.py
    python Tools/AlchemyPreview/bake_cover.py --from-html

The first command renders the editable Assets/AlchemyCoverFront.svg. The second
re-extracts the reference HTML's complete nested SVG, removing baked text before
rendering. The title is drawn at runtime with the same KL font as the skill book.
Pass --converter if the svg-to-png skill is installed at a different location.
"""
from pathlib import Path
import argparse
import os
import re
import subprocess
import sys
from PIL import Image


def extract_cover(html: str) -> str:
    start = re.search(r'<svg\b[^>]*\bid="alchemy-front-art"[^>]*>', html)
    if start is None:
        raise ValueError('The HTML does not contain alchemy-front-art.')
    depth = 0
    for tag in re.finditer(r'</?svg\b[^>]*>', html[start.start():]):
        depth += -1 if tag.group().startswith('</') else 1
        if depth == 0:
            svg = html[start.start():start.start() + tag.end()]
            # hidden is an HTML boolean attribute, and must not hide the exported SVG.
            svg = re.sub(r'\s+hidden(?:="[^"]*")?(?=\s|>)', '', svg, count=1)
            svg = re.sub(r'<text\b[^>]*>.*?</text\s*>', '', svg, flags=re.S)
            return ('<!-- From alchemy-front-art in elaina-battle-eight-skills-alchemy.html.\n'
                    '     Title is intentionally drawn at runtime by AlchemyBookArt.PaintCover. -->\n'
                    + svg.strip() + '\n')
    raise ValueError('The alchemy-front-art SVG is not closed.')


def main() -> None:
    repo = Path(__file__).resolve().parents[2]
    assets = repo / 'ElainaModAlchemy/UI/Assets'
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--from-html', action='store_true', help='Refresh the editable SVG from the HTML reference.')
    parser.add_argument('--converter', type=Path,
                        default=Path(os.environ.get('CODEX_HOME', Path.home() / '.codex')) /
                        'skills/svg-to-png/scripts/convert_svg.py')
    args = parser.parse_args()
    source = assets / 'AlchemyCoverFront.svg'
    if args.from_html or not source.exists():
        html = (repo / 'ElainaModSkills/ElainaSkillUI/NewUIExample/elaina-battle-eight-skills-alchemy.html').read_text(encoding='utf-8-sig')
        assets.mkdir(parents=True, exist_ok=True)
        source.write_text(extract_cover(html), encoding='utf-8', newline='\n')

    work = repo / '.vissandbox/alchemy-preview/art'
    work.mkdir(parents=True, exist_ok=True)
    straight = work / 'AlchemyCoverFront-straight.png'
    subprocess.run([sys.executable, str(args.converter), str(source), str(straight),
                    '--width', '1100', '--height', '800', '--overwrite'], check=True)
    with Image.open(straight) as raw:
        image = raw.convert('RGBA')
    if image.size != (1100, 800):
        raise ValueError(f'Unexpected cover size: {image.size}')
    image.putdata([((r * a + 127) // 255, (g * a + 127) // 255, (b * a + 127) // 255, a)
                   for r, g, b, a in image.get_flattened_data()])
    output = assets / 'AlchemyCoverFront.png'
    image.save(output)
    print(f'{output}: 1100 x 800, premultiplied RGBA; runtime title excluded.')


if __name__ == '__main__':
    main()
