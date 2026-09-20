"""Compose actual FNA output frames; does not redraw the skill bar."""
from pathlib import Path
from PIL import Image, ImageDraw

output = Path(__file__).resolve().parents[2] / ".vissandbox/skillbar-preview/output"
frames = [Image.open(output / f"animation-{i:03d}-1.5.png").convert("RGB") for i in range(60)]
palette_source = Image.new("RGB", (frames[0].width, frames[0].height * 3))
for row, index in enumerate((0, 2, 32)):
    palette_source.paste(frames[index], (0, frames[0].height * row))
palette = palette_source.quantize(colors=256)
indexed = [frame.quantize(palette=palette, dither=Image.Dither.NONE) for frame in frames]
indexed[0].save(output / "selection-arrival.gif", save_all=True, append_images=indexed[1:],
                duration=[30, 30, 40] * 20, loop=0, disposal=2, optimize=False)

times = ("0.000", "0.055", "0.120", "0.240", "0.450", "0.700")
sheet = Image.new("RGB", (160 * len(times), 180), (19, 20, 28))
draw = ImageDraw.Draw(sheet)
for index, seconds in enumerate(times):
    with Image.open(output / f"arrival-{seconds}-1.5.png") as frame:
        sheet.paste(frame.crop((287, 35, 447, 190)), (160 * index, 25))
    draw.text((160 * index + 12, 6), f"{float(seconds) * 1000:.0f} ms", fill=(235, 229, 248))
sheet.save(output / "selection-arrival-timeline.png")

# The pulse must end without changing the established selected appearance.
with Image.open(output / "selected-1.5.png") as steady:
    with Image.open(output / "arrival-0.700-1.5.png") as settled:
        assert steady.tobytes() == settled.tobytes(), "Settled selection differs from steady state"
assert frames[2].tobytes() != frames[20].tobytes(), "Animation is blank or static"
print(output / "selection-arrival.gif")
print(output / "selection-arrival-timeline.png")
