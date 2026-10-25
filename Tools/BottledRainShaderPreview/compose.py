"""Only compose rendered shader frames; the cloud itself comes from FNA/HLSL."""
from pathlib import Path
import sys
from PIL import Image

root = Path(sys.argv[1])
frames = [Image.open(root / f'motion-{i:03}.png').convert('RGB') for i in range(120)]
frames[0].save(root / 'shader-cloud.gif', save_all=True, append_images=frames[1:], duration=50, loop=0, disposal=2)
frames[0].save(root / 'plant-growth-indicators.gif', save_all=True, append_images=frames[1:], duration=50, loop=0, disposal=2)
print('Shader animation written.')
reveal = [Image.open(root / f'reveal-{i:03}.png').convert('RGB') for i in range(150)]
reveal[0].save(root / 'dissolve-reveal-soft.gif', save_all=True, append_images=reveal[1:],
               duration=[30, 30, 40] * 50, loop=0, disposal=2)
print('Dissolve appearance animation written.')
sheet = Image.new('RGB', (1200, 640), (27, 31, 52))
for index, frame_index in enumerate(range(0, 72, 6)):
    crop = reveal[frame_index].crop((120, 30, 520, 190))
    sheet.paste(crop, (index % 3 * 400, index // 3 * 160))
sheet.save(root / 'reveal-timing.png')
