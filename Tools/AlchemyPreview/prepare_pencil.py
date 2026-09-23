"""Premultiply active ExampleAssets artwork for tML's rawimg/AlphaBlend pipeline.

Keep original straight-alpha PNGs unchanged: HTML continues to use them directly.
Only referenced notebook artwork is exported; item Pixel textures are not touched.
"""
from pathlib import Path
import json
from PIL import Image

repo = Path(__file__).resolve().parents[2]
plan = json.loads((repo / 'Tools/AlchemyPlan/plan.json').read_text(encoding='utf-8'))
source = repo / 'ElainaModAlchemy/item/ExampleAssets'
target = repo / 'ElainaModAlchemy/UI/Assets/Pencil'
names = {entry['art'] for entry in plan['items']}
names.update(material.get('art') or 'Materials/' + material['id'] for material in plan['materials'])
for name in sorted(names):
    original = source / (name + '_Pencil.png')
    image = Image.open(original).convert('RGBA')
    image.putdata([((r*a+127)//255, (g*a+127)//255, (b*a+127)//255, a)
                   for r, g, b, a in image.get_flattened_data()])
    destination = target / (name + '_Pencil.png')
    destination.parent.mkdir(parents=True, exist_ok=True)
    image.save(destination)
print(f'Prepared {len(names)} referenced pencil textures; original ExampleAssets unchanged.')
