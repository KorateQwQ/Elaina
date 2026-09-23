"""Extract existing alchemy reference ornaments, rasterize with resvg, and premultiply for tML."""
from pathlib import Path
import argparse
import re
import subprocess
import sys
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument('--converter', type=Path, default=Path.home()/'.codex/skills/svg-to-png/scripts/convert_svg.py')
parser.add_argument('--only', choices=('AlchemyCrest', 'SelectionQuill'), help='Rebuild a single ornament.')
args = parser.parse_args()
repo = Path(__file__).resolve().parents[2]
assets = repo/'ElainaModAlchemy/UI/Assets'
work = repo/'.vissandbox/alchemy-preview/art'
assets.mkdir(parents=True, exist_ok=True)
work.mkdir(parents=True, exist_ok=True)
html = (repo/'ElainaModSkills/ElainaSkillUI/NewUIExample/elaina-battle-eight-skills-alchemy.html').read_text(encoding='utf-8')
crest = re.search(r'<svg class="alchemy-crest".*?</svg>', html, re.S).group()
crest = crest.replace('<svg ', '<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" ', 1)
# The selection marker uses the quiet footer quill from the HTML, including its
# narrow shaft and star details. The older colored selection-quill.svg is distinct.
symbol = re.search(r'<symbol id="notebook-quill" viewBox="([^"]+)">(.*?)</symbol>', html, re.S)
note_style = re.search(r'\.plan-page-note>svg\{([^}]+)\}', html)
if symbol is None or note_style is None:
    raise ValueError('The notebook-quill symbol or its footer CSS is missing from the reference HTML.')
quill_color = re.search(r'(?:^|;)color:([^;]+)', note_style.group(1)).group(1)
quill_opacity = re.search(r'(?:^|;)opacity:([^;]+)', note_style.group(1)).group(1)
quill = (f'<svg xmlns="http://www.w3.org/2000/svg" width="80" height="140" '
         f'viewBox="{symbol.group(1)}" color="{quill_color}" opacity="{quill_opacity}">'
         f'{symbol.group(2)}</svg>\n')
for name, source, width, height, color in [('AlchemyCrest', crest, 128, 128, '#ffffff'),
                                           ('SelectionQuill', quill, 160, 280, quill_color)]:
    if args.only is not None and name != args.only:
        continue
    svg = assets/(name + '.svg')
    svg.write_text(source, encoding='utf-8')
    raw = work/(name + '-straight.png')
    subprocess.run([sys.executable, str(args.converter), str(svg), str(raw), '--width', str(width), '--height', str(height), '--color', color, '--overwrite'], check=True)
    image = Image.open(raw).convert('RGBA')
    image.putdata([(round(r*a/255), round(g*a/255), round(b*a/255), a) for r,g,b,a in image.get_flattened_data()])
    image.save(assets/(name + '.png'))
    print(f'{name}: {width} x {height}, premultiplied RGBA.')
