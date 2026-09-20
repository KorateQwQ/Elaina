"""Pin the installed Noto Serif SC variable font to Regular and describe real BMP coverage."""
import argparse
import hashlib
import json
import unicodedata
from pathlib import Path
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

parser = argparse.ArgumentParser()
parser.add_argument('--font', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
args.output.mkdir(parents=True, exist_ok=True)
font = TTFont(args.font)
original_version = font['name'].getDebugName(5)
font = instantiateVariableFont(font, {'wght': 400}, inplace=True)
# A private family avoids collisions with the installed variable font in the old XNA/GDI pipeline.
family = 'KL Noto Serif SC'
for record in font['name'].names:
    value = {1: family, 2: 'Regular', 3: 'KLNotoSerifSC-Regular', 4: family + ' Regular',
             6: 'KLNotoSerifSC-Regular', 16: family, 17: 'Regular'}.get(record.nameID)
    if value is not None:
        record.string = value.encode(record.getEncoding())
font.save(args.output / 'NotoSerifSC-Regular.ttf')
characters = sorted(c for c in font.getBestCmap()
                    if 0x20 <= c < 0xFFFF and unicodedata.category(chr(c)) not in ('Cc', 'Cf', 'Cs'))
ranges = []
for char in characters:
    if ranges and char == ranges[-1][1] + 1:
        ranges[-1][1] = char
    else:
        ranges.append([char, char])
regions = '\n'.join(f'      <CharacterRegion><Start>&#x{a:X};</Start><End>&#x{b:X};</End></CharacterRegion>'
                    for a, b in ranges)
xml = f'''<?xml version="1.0" encoding="utf-8"?>
<XnaContent xmlns:Graphics="ReLogic.Content.Pipeline">
  <Asset Type="Graphics:DynamicFontDescription">
    <FontName>{family}</FontName>
    <Size>36</Size>
    <Spacing>0</Spacing>
    <UseKerning>true</UseKerning>
    <Style>Regular</Style>
    <DefaultCharacter>?</DefaultCharacter>
    <VerticalOffset>DefaultFontAscent</VerticalOffset>
    <CharacterRegions>
{regions}
    </CharacterRegions>
  </Asset>
</XnaContent>
'''
(args.output / 'NotoSerifSC.dynamicfont').write_text(xml, encoding='utf-8')
(args.output / 'source.json').write_text(json.dumps({
    'family': 'Noto Serif SC', 'version': original_version, 'weight': 400,
    'source_sha256': hashlib.sha256(args.font.read_bytes()).hexdigest(),
    'size_points': 36, 'nominal_pixels': 48, 'glyphs': len(characters),
    'coverage': 'All printable BMP characters present in the source cmap; no supplementary-plane support.'
}, indent=2), encoding='utf-8')
print(f'Prepared weight 400, {len(characters)} characters, {len(ranges)} regions.')
