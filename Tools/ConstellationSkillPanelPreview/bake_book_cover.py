"""Extract the reference's text-free cover SVGs and bake tML premultiplied PNGs."""
from pathlib import Path
import argparse
import subprocess
import sys
import xml.etree.ElementTree as ET
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument('--converter', type=Path, default=Path.home()/'.codex/skills/svg-to-png/scripts/convert_svg.py')
args = parser.parse_args()
repo = Path(__file__).resolve().parents[2]
assets = repo/'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets'
work = repo/'.vissandbox/constellation-skill-panel'
work.mkdir(parents=True, exist_ok=True)
source = (assets.parent.parent/'NewUIExample/elaina-battle-eight-skills.html').read_text(encoding='utf-8')
ET.register_namespace('', 'http://www.w3.org/2000/svg')
for side in ('front', 'back'):
    start = source.index(f'<svg id="book-{side}-art"')
    if side == 'front':
        end = source.index('<svg id="book-back-art"', start)
    else:
        end = source.index('</svg>', start) + len('</svg>')
    root = ET.fromstring(source[start:end].strip().replace(' hidden>', '>'))
    root.attrib.pop('hidden', None)
    root.attrib.pop('id', None)
    for parent in root.iter():
        for child in list(parent):
            if child.tag.endswith('}text'):
                parent.remove(child)  # The live project font supplies the title.
    name = 'BookCover' + side.title()
    svg = assets/(name + '.svg')
    ET.ElementTree(root).write(svg, encoding='utf-8', xml_declaration=True)
    raw = work/(name + '-straight.png')
    subprocess.run([sys.executable, str(args.converter), str(svg), str(raw), '--overwrite'], check=True)
    image = Image.open(raw).convert('RGBA')
    image.putdata([(round(r*a/255), round(g*a/255), round(b*a/255), a) for r,g,b,a in image.get_flattened_data()])
    image.save(assets/(name + '.png'))
    print(f'{name}: {image.width} x {image.height}, premultiplied RGBA; no baked text.')
