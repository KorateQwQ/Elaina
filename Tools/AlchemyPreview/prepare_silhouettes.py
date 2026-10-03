"""Export neutral, premultiplied alpha silhouettes for undiscovered notebook entries.

The question mark and unlock conditions remain live UI text. Source item art and
its transparent margins stay unchanged, so both states use the same placement.
"""
from pathlib import Path
import re
from PIL import Image

repo = Path(__file__).resolve().parents[2]
catalog = (repo / 'ElainaModAlchemy/UI/AlchemyCatalog.cs').read_text(encoding='utf-8')
names = sorted(set(re.findall(r'Category = "[^"]+", Art = "([^"]+)"', catalog)))
if not names:
    raise ValueError('No recipe artwork found in AlchemyCatalog.cs.')
source = repo / 'ElainaModAlchemy/UI/Assets/Pencil'
target = repo / 'ElainaModAlchemy/UI/Assets/Silhouettes'
target.mkdir(parents=True, exist_ok=True)
for name in names:
    with Image.open(source / (name + '_Pencil.png')) as original:
        alpha = original.convert('RGBA').getchannel('A')
        # White with RGB=A is premultiplied and can be tinted by SpriteBatch.
        silhouette = Image.merge('RGBA', (alpha, alpha, alpha, alpha))
        silhouette.save(target / (name + '_Silhouette.png'))
print(f'Prepared {len(names)} recipe silhouettes from production notebook artwork.')
