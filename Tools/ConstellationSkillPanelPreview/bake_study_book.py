"""Rasterize the editable SVG, then encode premultiplied RGBA for tML's rawimg loader."""
from pathlib import Path
import argparse
import subprocess
import sys
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument('--converter', type=Path, default=Path.home()/'.codex/skills/svg-to-png/scripts/convert_svg.py')
args = parser.parse_args()
repo = Path(__file__).resolve().parents[2]
assets = repo/'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets'
work = repo/'.vissandbox/constellation-skill-panel'
work.mkdir(parents=True, exist_ok=True)
raw = work/'StudyBook-straight.png'
subprocess.run([sys.executable, str(args.converter), str(assets/'StudyBook.svg'), str(raw),
                '--width', '128', '--height', '128', '--overwrite'], check=True)
image = Image.open(raw).convert('RGBA')
image.putdata([(round(r*a/255), round(g*a/255), round(b*a/255), a) for r,g,b,a in image.getdata()])
image.save(assets/'StudyBook.png')
print('StudyBook.png: 128 x 128, transparent, premultiplied RGBA; displayed at 27 design pixels.')
