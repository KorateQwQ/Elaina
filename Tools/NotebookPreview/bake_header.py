"""Bake only the reference's fixed, text-free notebook background above the chart."""
from pathlib import Path
import argparse
import re
import subprocess
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument('--chrome', default=r'C:\Program Files\Google\Chrome\Application\chrome.exe')
args = parser.parse_args()
repo = Path(__file__).resolve().parents[2]
root = repo / 'ElainaModSkills/ElainaSkillUI/ConstellationPreview'
work = repo / '.vissandbox/notebook-header'
work.mkdir(parents=True, exist_ok=True)
html = (root.parent / 'NewUIExample/elaina-battle-eight-skills.html').read_text(encoding='utf-8')
rule = re.search(r'\.grimoire\{([^}]+)\}', html).group(1)
background = re.search(r'(?:^|;)background:([^;]+)', rule).group(1)
shadow = re.search(r'inset 0 0 85px #[0-9a-f]+', rule).group(0)
# Use the entire 1100x800 gradient box, then take the top strip. Cropping first
# would change CSS gradient radii, angle and inset-shadow falloff.
reference = work / 'header-surface.html'
reference.write_text(f'''<!doctype html><style>
html,body{{margin:0;background:#14151c;overflow:hidden}}
div{{width:1100px;height:800px;background:{background};box-shadow:{shadow}}}
</style><div></div>''', encoding='utf-8')
capture = work / 'header-surface.png'
subprocess.run([args.chrome, '--headless', '--disable-gpu', '--no-first-run',
    '--hide-scrollbars', '--force-device-scale-factor=1', '--window-size=1100,900',
    '--user-data-dir='+str(work/'chrome-profile'), '--screenshot='+str(capture), reference.as_uri()],
    stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=45,
    creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0), check=True)
image = Image.open(capture).convert('RGB').crop((0,0,1100,146))
image.save(root/'Assets/HeaderSurface.png')
print('HeaderSurface.png: 1100 x 146 RGB, no text or icons; opaque tML/FNA sampling.')
