"""Check the filled stroke interior, not its moving tip or the background."""
import math
import statistics
import sys
from pathlib import Path
from PIL import Image

output = Path(sys.argv[1])
spend = [Image.open(output / f'spend-{i:02d}.png').convert('RGB') for i in range(29)]
bright = [sum(b > 221 and r > 70 for r, g, b in frame.getdata()) for frame in spend]
assert bright[0] > bright[9] > bright[17] > bright[18] == 0, bright
spend[0].save(output/'spend.gif',save_all=True,append_images=spend[1:],duration=[30,30,40]*9+[30,350],loop=0)
bends = Image.open(output / 'curvatures.png').convert('RGB')
for row, scale in enumerate((1, 1.25, 2)):
    for col, bend in enumerate((1, .65, 0)):
        cx, cy = 68 + 160 * col, 60 + 120 * row
        for fraction in (.02, .25, .40, .60, .75, .98):
            distance = math.radians(130) * 20 * (fraction - .5)
            theta = bend * distance / 20
            x = 20 + ((math.cos(theta) - 1) * 20 / bend if bend else 0)
            y = math.sin(theta) * 20 / bend if bend else distance
            pixel = bends.getpixel((round(cx + x * scale - .5), round(cy + y * scale - .5)))
            assert (pixel[2] > 190 if fraction < .5 else pixel[2] < 135), (bend, scale, fraction, pixel)

frames = [Image.open(output / f'flow-{i:02d}.png').convert('RGB') for i in range(120)]
repeat = Image.open(output / 'flow-120.png').convert('RGB')
assert max(abs(a-b) for a,b in zip(frames[0].tobytes(), repeat.tobytes())) <= 2, 'Foam jumps when the phase wraps'
interior = [(x, y) for x in range(40, 64) for y in range(12, 68)
            if abs(math.hypot(x + .5 - 36, y + .5 - 40) - 25) < 1.9]
values = [[frame.getpixel(point)[1] for point in interior] for frame in frames]
contrast = statistics.median(max(row) - min(row) for row in values)
movement = statistics.mean(abs(a - b) for a, b in zip(values[0], values[6]))
assert contrast > 65 and movement > 12, (contrast, movement)
frames[0].save(output / 'flow.gif', save_all=True, append_images=frames[1:], duration=50, loop=0)
print(f'Filled stroke contrast {contrast:.1f}, mean quarter-cycle change {movement:.1f}.')

# Inspect both fixed-fill silhouettes, including the faint outer antialiasing.
def silhouette(frame):
    return bytes(pixel != (65, 145, 140) for pixel in frame.getdata())

outline = silhouette(frames[0])
assert all(silhouette(frame) == outline for frame in frames[1:]), 'The water band changes shape'

# Sample the empty half of the left ring and the corresponding filled portion of
# the right ring. Check both edges separately from the centre of the trough.
def arc_samples(frame, cx, radius):
    return [frame.getpixel((round(cx + radius * math.sin(a)), round(39.5 - radius * math.cos(a))))
            for a in [math.pi + i * math.pi / 60 for i in range(5, 56)]]

for frame in frames:
    empty = arc_samples(frame, 35.5, 25)
    assert all(pixel == empty[0] for pixel in empty) and empty[0][1] < 115, 'Missing continuous track'
    assert min(p[1] for p in arc_samples(frame, 107.5, 25)) > empty[0][1] + 65, 'Unreadable fill'
    for r in [25 - 3.8, 25 + 3.8]:
        assert statistics.median(p[1] for p in arc_samples(frame, 35.5, r)) > empty[0][1] + 65, 'Missing border'

# Include the filled arc and empty guide in the opacity comparison.
fade = [Image.open(output / f'fade-{i:02d}.png').convert('RGB') for i in range(21)]
energy = [sum(sum(abs(a-b) for a,b in zip(pixel,(30,42,56))) for pixel in frame.getdata()) for frame in fade]
assert energy[0] == energy[-1] == 0
assert all(a<b for a,b in zip(energy[:10],energy[1:11])) and energy == energy[::-1]
fade[0].save(output/'fade.gif',save_all=True,append_images=fade[1:],duration=[250]+[16]*9+[450]+[22]*9+[250],loop=0)
print('Passed real-time fade, reversal, focus, and rendered opacity checks.')
