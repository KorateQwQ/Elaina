"""Assemble .vissandbox/fire-laser-preview/out/frames into a cropped real-time GIF (run.ps1 with FIRE_FRAMES=1 first)."""
import glob, os, sys
from PIL import Image

root = os.path.join(os.path.dirname(__file__), '..', '..', '.vissandbox', 'fire-laser-preview', 'out')
files = sorted(glob.glob(os.path.join(root, 'frames', 'day-*.png')))
if not files:
    sys.exit('no frames; run run.ps1 with FIRE_FRAMES=1')
frames = [Image.open(f).crop((120, 200, 1200, 500)).convert('P', palette=Image.ADAPTIVE) for f in files]
out = os.path.join(root, 'fire-laser-day.gif')
frames[0].save(out, save_all=True, append_images=frames[1:], duration=17, loop=0)
print(out)
