"""Check player UI key coverage, English text, and matching format arguments."""
from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[2]


def read(culture):
    values = {}
    parents = []
    for line in (ROOT / 'Localization' / f'{culture}_Mods.伊蕾娜.UI.hjson').read_text(encoding='utf-8-sig').splitlines():
        line = line.strip()
        if not line or line.startswith('//'):
            continue
        if line == '}':
            parents.pop()
            continue
        key, value = line.split(':', 1)
        value = value.strip()
        if value == '{':
            parents.append(key)
            continue
        key = '.'.join(parents + [key])
        assert key not in values, f'Duplicate key: {key}'
        values[key] = json.loads(value) if value.startswith('"') else value
    return values


zh, en = read('zh-Hans'), read('en-US')
assert zh.keys() == en.keys(), 'Chinese and English keys differ'
for key in zh:
    assert re.findall(r'\{\d+(?:[^}]*)\}', zh[key]) == re.findall(r'\{\d+(?:[^}]*)\}', en[key]), key
    # Resource paths are identifiers, not translated prose; keep checking the visible color-tag text.
    icon_pattern = r'\[klicon/[^:\]]+:[^\]]+\]'
    assert re.findall(icon_pattern, zh[key]) == re.findall(icon_pattern, en[key]), f'Icon paths differ: {key}'
    visible_english = re.sub(icon_pattern, '', en[key])
    assert not re.search(r'[\u4e00-\u9fff]', visible_english), f'Chinese in English value: {key}'

used = set()
for folder in ('ElainaModSkills', 'ElainaModAlchemy', '炼金'):
    for file in (ROOT / folder).rglob('*.cs'):
        source = file.read_text(encoding='utf-8-sig')
        used.update(re.findall(r'LocalizeUI\("([\w.]+)"', source))
        used.update(re.findall(r'"(Catalog\.[\w.]+)"', source))
assert not used - en.keys(), f'Missing UI keys: {used - en.keys()}'

print(f'PASS: {len(en)} translation pairs, matching placeholders, {len(used)} referenced UI keys, no Chinese English values.')
