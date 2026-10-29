"""Rebuild shared production icons and masks. Requires Pillow and the svg-to-png skill's dependencies.
Text uses KL FontManager at runtime; this script only generates non-text assets.
"""
from pathlib import Path
import json, re, math, subprocess, sys, tempfile
from PIL import Image

ROOT = Path(__file__).resolve().parent
REPO = ROOT.parents[1]
OUT = REPO / 'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets'
OUT.mkdir(exist_ok=True)
source = (REPO / 'ElainaModSkills/ElainaSkillUI/NewUIExample/elaina-constellation-origin.html').read_text(encoding='utf-8')
subprocess.run([sys.executable, str(ROOT / 'GenerateBackground.py')], check=True)
subprocess.run([sys.executable, str(ROOT / 'GenerateLinks.py')], check=True)

# Preserve the original inline SVG paths and rasterize at 4x with the skill converter.
converter = Path.home() / '.codex/skills/svg-to-png/scripts/convert_svg.py'
symbols = re.findall(r'<symbol id="i-([^"]+)" viewBox="([^"]+)">(.*?)</symbol>', source)
symbols.append(('check','0 0 24 24','<path d="m5 12 4 4L19 6"/>'))
sheet = Image.new('RGBA', (512, math.ceil(len(symbols)/5)*100))
icons = {}
with tempfile.TemporaryDirectory(prefix='elaina-icons-') as temp:
    for i, (name, box, body) in enumerate(symbols):
        svg = Path(temp) / (name + '.svg')
        png = svg.with_suffix('.png')
        svg.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="{box}" fill="none" stroke="white" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round">{body}</svg>', encoding='utf-8')
        subprocess.run([sys.executable, str(converter), str(svg), str(png), '--width', '96', '--height', '96'], check=True, capture_output=True)
        x,y = i%5*100, i//5*100
        sheet.paste(Image.open(png), (x,y))
        icons[name] = [x,y,96,96]
sheet.save(OUT / 'Icons.png')
(OUT / 'Icons.json').write_text(json.dumps(icons), encoding='utf-8')

# Disc uses the existing antialiased edge; preserve the authored pixel values.
disc = Image.new('RGBA', (128, 128))
pixels = disc.load()
for y in range(128):
    for x in range(128):
        alpha = round(255 * max(0, min(1, (.5 - math.hypot(x / 127 - .5, y / 127 - .5)) * 128)))
        pixels[x, y] = (255, 255, 255, alpha)
disc.save(OUT / 'Disc.png')
print(f'Generated {len(icons)} utility icons and shared masks in {OUT}')
