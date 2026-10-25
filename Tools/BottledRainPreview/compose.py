"""Compose the directly rendered FNA frames; never redraw the effect in Python."""
from pathlib import Path
import sys
from PIL import Image

root = Path(sys.argv[1])
frames = [Image.open(p).convert('RGB') for p in sorted(root.glob('motion-*.png'))]
frames[0].save(root / 'bottled-rain.gif', save_all=True, append_images=frames[1:], duration=50, loop=0, disposal=2)
print(root / 'bottled-rain.gif')
