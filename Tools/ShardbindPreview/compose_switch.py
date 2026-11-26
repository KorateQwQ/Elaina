"""Compact animation of the FNA target handoff, including confirmation and the first volley."""
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[2] / '.vissandbox/shardbind-preview/html-v2'
frames = []
for tick in range(0, 334, 3):
    with Image.open(root/'switch/normal'/f'{tick:03}.png') as source:
        frames.append(source.convert('RGB').crop((280,350,1640,1115)).resize((680,382),Image.Resampling.LANCZOS))
samples = Image.new('RGB',(680*8,382))
for i, frame in enumerate(frames[::14]):
    samples.paste(frame,(680*i,0))
palette = samples.quantize(colors=256)
images = [frame.quantize(palette=palette,dither=Image.Dither.NONE) for frame in frames]
images[0].save(root/'viewer/target-switch.gif',save_all=True,append_images=images[1:],
    duration=[50]*(len(images)-1)+[550],loop=0,disposal=2,optimize=False)
print(root/'viewer/target-switch.gif')
