from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
out = root / '.vissandbox/water-charge-preview/out-flight'
frames = [Image.open(out / f'flight-{i:03}.png').convert('RGB') for i in range(60)]
# Show the settled flight shape in the rotation loop; the moving clip includes the launch squash.
orbit = [frame.crop((170, 350, 1170, 850)).resize((800, 400)) for frame in frames[8:]]
orbit[0].save(out / 'water-ball-spiral.gif', save_all=True, append_images=orbit[1:],
               duration=([20, 20, 10] * 20)[:len(orbit)], loop=0, disposal=2)
orbit[0].save(out / 'water-ball-spiral-slow.gif', save_all=True, append_images=orbit[1:],
               duration=50, loop=0, disposal=2)
moving = [Image.open(out / f'moving-{i:03}.png').convert('RGB').crop((0, 380, 1440, 820)).resize((1008, 308)) for i in range(24)]
moving[0].save(out / 'water-ball-moving.gif', save_all=True, append_images=moving[1:], duration=[20, 20, 10] * 8, loop=0, disposal=2)
sheet = Image.new('RGB', (1500, 660), (25, 39, 47))
draw = ImageDraw.Draw(sheet)
for slot, tick in enumerate((1, 4, 10, 18, 28, 40)):
    x, y = slot % 3 * 500, slot // 3 * 330
    crop = (170, 150, 1170, 1050) if tick < 8 else (170, 300, 1170, 900)
    tile = frames[tick].crop(crop)
    tile.thumbnail((500, 300))
    sheet.paste(tile, (x + (500 - tile.width) // 2, y + 30 + (300 - tile.height) // 2))
    draw.text((x + 12, y + 8), f'Flight tick {tick} / 60 Hz', fill=(225, 241, 250))
sheet.save(out / 'flight-sheet.png')
print('Flight GIFs and sheet:', out)
