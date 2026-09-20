"""Rasterize the refined Elaina emblem with its individual fine strokes; premultiply once."""
from pathlib import Path
import subprocess
import sys
import tempfile
from PIL import Image

assets = Path(__file__).resolve().parent / 'Assets'
converter = Path.home() / '.codex/skills/svg-to-png/scripts/convert_svg.py'
with tempfile.TemporaryDirectory(prefix='elaina-header-hat-') as folder:
    straight = Path(folder) / 'hat.png'
    subprocess.run([sys.executable, str(converter), str(assets / 'HeaderHat.svg'), str(straight),
                    '--width', '156', '--height', '156', '--color', '#ffffff'], check=True)
    image = Image.open(straight).convert('RGBA')
    image.putdata([(r*a//255, g*a//255, b*a//255, a) for r, g, b, a in image.get_flattened_data()])
    image.save(assets / 'HeaderHat.png')
