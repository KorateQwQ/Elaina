"""Assemble actual FNA frames; requires Pillow. Run after run.ps1."""
from pathlib import Path
from PIL import Image, ImageDraw

output = Path(__file__).resolve().parents[2] / '.vissandbox/water-charge-preview/out'
names = ['frame-006', 'frame-010', 'frame-014', 'frame-018', 'frame-022',
         'frame-026', 'day-zoom', 'alternate-small', 'ball-only']
sheet = Image.new('RGB', (1080, 960), (13, 21, 31))
draw = ImageDraw.Draw(sheet)
for i, name in enumerate(names):
    frame = Image.open(output / (name + '.png')).convert('RGB').resize((360, 300))
    x, y = i % 3 * 360, i // 3 * 320
    sheet.paste(frame, (x, y))
    draw.text((x + 10, y + 302), name, fill='white')
sheet.save(output / 'contact-sheet.png')
frames = [Image.open(output / f'frame-{i:03}.png').convert('RGB').resize((600, 500)) for i in range(90)]
frames[0].save(output / 'water-charge.gif', save_all=True, append_images=frames[1:], duration=33, loop=0)

# Preserve aspect ratio and crop the actual zoomed FNA rendering around the sphere.
wrap_frames = [Image.open(output / f'wrap-{i:03}.png').convert('RGB')
               .crop((420, 300, 1020, 900)) for i in range(54)]
wrap_frames[0].save(output / 'water-wrap-slow.gif', save_all=True,
                    append_images=wrap_frames[1:], duration=50, loop=0)
wrap_names = ['wrap-020', 'wrap-026', 'wrap-032', 'wrap-036',
              'wrap-040', 'wrap-044', 'wrap-day', 'wrap-alternate']
wrap_sheet = Image.new('RGB', (1200, 640), (13, 21, 31))
wrap_draw = ImageDraw.Draw(wrap_sheet)
for i, name in enumerate(wrap_names):
    crop = Image.open(output / (name + '.png')).convert('RGB').crop((510, 390, 930, 810))
    x, y = i % 4 * 300, i // 4 * 320
    wrap_sheet.paste(crop.resize((300, 300), Image.Resampling.LANCZOS), (x, y))
    label = f'{int(name[5:]) / 60:.3f} s' if name[5:].isdigit() else name
    wrap_draw.text((x + 8, y + 303), label, fill='white')
wrap_sheet.save(output / 'water-wrap-sheet.png')

# Crop actual rendered pixels; do not substitute a separate material implementation.
material_names = ['sphere-night', 'sphere-day', 'sphere-pattern',
                  'sphere-left', 'sphere-up', 'sphere-stretched']
material_sheet = Image.new('RGB', (1320, 940), (13, 21, 31))
material_draw = ImageDraw.Draw(material_sheet)
for i, name in enumerate(material_names):
    crop = Image.open(output / (name + '.png')).convert('RGB').crop((500, 380, 940, 820))
    x, y = i % 3 * 440, i // 3 * 470
    material_sheet.paste(crop, (x, y))
    material_draw.text((x + 12, y + 446), name, fill='white')
material_sheet.save(output / 'sphere-material-sheet.png')

sphere_frames = [Image.open(output / f'frame-{i:03}.png').convert('RGB')
                 .crop((620, 500, 820, 700)).resize((400, 400), Image.Resampling.NEAREST)
                 for i in range(30, 90)]
sphere_frames[0].save(output / 'water-sphere.gif', save_all=True,
                      append_images=sphere_frames[1:], duration=33, loop=0)

before = output.parent / 'before' / 'ball-only.png'
if before.exists():
    comparison = Image.new('RGB', (800, 438), (13, 21, 31))
    comparison_draw = ImageDraw.Draw(comparison)
    for i, path in enumerate([before, output / 'ball-only.png']):
        crop = Image.open(path).convert('RGB').crop((620, 500, 820, 700))
        comparison.paste(crop.resize((400, 400), Image.Resampling.NEAREST), (i * 400, 0))
        comparison_draw.text((i * 400 + 16, 411), ['BEFORE', 'AFTER'][i], fill='white')
    comparison.save(output / 'sphere-before-after.png')
