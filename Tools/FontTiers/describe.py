"""Writes trimmed font descriptions (Name_px.dynamicfont) from a 48px master description.

Usage: python -I describe.py <master.dynamicfont> <out dir> <Name> <mod|items> <px> [<px> ...]

Fonts only hold characters the UI can show, intersected with the master's coverage: basic Latin and CJK
punctuation, this mod's and KL's localization, both mods' C# string literals and, with `items`, vanilla item
names in every language (requirement rows, alchemy). Size copies fall back to the scaled master for other
text; a trimmed master shows its default character instead, so it uses `items`. Rebake after adding text.
"""
import glob
import json
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
VANILLA = r'D:\泰拉原版贴图与代码与其他工具\tModLoader\Terraria\Localization\Content'
SKIPPED = re.compile(r'[\\/](Tools|\.vissandbox|bin|obj)[\\/]')
LITERAL = re.compile(r'"(?:[^"\\\n]|\\.)*"')  # C# string literals, not comments
# ASCII, Latin-1, general punctuation, arrows, CJK symbols and punctuation, full-width ASCII.
BASIC = [(0x20, 0x7E), (0xA0, 0xFF), (0x2010, 0x205E), (0x2190, 0x2193), (0x3000, 0x303F), (0xFF01, 0xFF5E)]


def read(path):
    with open(path, encoding='utf-8-sig') as f:
        return f.read()


def needed(kind):
    text = []
    for pattern in ('Localization/*.hjson', '../KL/Localization/*.hjson'):
        text += [read(p) for p in glob.glob(os.path.join(ROOT, pattern))]
    for pattern in ('**/*.cs', '../KL/**/*.cs'):
        text += [''.join(LITERAL.findall(read(p))) for p in glob.glob(os.path.join(ROOT, pattern), recursive=True)
                 if not SKIPPED.search(p)]
    if kind == 'items':
        items = glob.glob(os.path.join(VANILLA, '*', 'Items.json'))
        if len(items) < 9:
            raise SystemExit(f'Missing vanilla item names: {VANILLA}')
        # Vanilla files carry trailing commas.
        text += [''.join(json.loads(re.sub(r',(\s*[}\]])', r'\1', read(p))).get('ItemName', {}).values()) for p in items]
    codes = {ord(c) for c in ''.join(text)}
    for start, end in BASIC:
        codes.update(range(start, end + 1))
    return codes


def code(text):
    text = text.strip()
    return int(text[3:-1], 16) if text.startswith('&#x') else ord(text)


def runs(codes):
    codes = sorted(codes)
    start = prev = codes[0]
    for c in codes[1:]:
        if c != prev + 1:
            yield start, prev
            start = c
        prev = c
    yield start, prev


def main(master_path, out_dir, name, kind, pixels):
    master = read(master_path)
    regions = re.search(r'<CharacterRegions>.*</CharacterRegions>', master, re.S)
    covered = set()
    for start, end in re.findall(r'<Start>(.*?)</Start>\s*<End>(.*?)</End>', regions.group(0), re.S):
        covered.update(range(code(start), code(end) + 1))
    codes = covered & needed(kind)
    codes.add(code(re.search(r'<DefaultCharacter>(.*?)</DefaultCharacter>', master).group(1)))
    body = ''.join(f'\t\t\t<CharacterRegion><Start>&#x{a:X};</Start><End>&#x{b:X};</End></CharacterRegion>\n'
                   for a, b in runs(codes))
    described = master[:regions.start()] + '<CharacterRegions>\n' + body + '\t\t</CharacterRegions>' + master[regions.end():]
    if '<Size>36</Size>' not in described:
        raise SystemExit(f'{master_path} is not baked at 36pt (48px).')
    os.makedirs(out_dir, exist_ok=True)
    for px in pixels:
        text = described.replace('<Size>36</Size>', f'<Size>{px * 3 / 4:g}</Size>')
        with open(os.path.join(out_dir, f'{name}_{px}.dynamicfont'), 'w', encoding='utf-8') as f:
            f.write(text)
    print(f'{name}: {len(codes)} characters, sizes {", ".join(map(str, pixels))}')


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4], [int(p) for p in sys.argv[5:]])
