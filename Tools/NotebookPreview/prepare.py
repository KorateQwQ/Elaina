"""Link production rows/data/drawing and refresh the actual notebook draw methods."""
from pathlib import Path
import argparse
import re
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser()
parser.add_argument('--tml', required=True, type=Path)
args = parser.parse_args()
here = Path(__file__).resolve().parent
repo = here.parents[1]
out = repo / '.vissandbox/notebook-preview'
out.mkdir(parents=True, exist_ok=True)
ui = repo / 'ElainaModSkills/ElainaSkillUI/ConstellationPreview'
source = (ui / 'ConstellationPreviewUI.cs').read_text(encoding='utf-8')

def method(name):
    match = re.search(r'^    private [^\n]*\b' + name + r'\(', source, re.M)
    if not match: raise ValueError('Missing method: ' + name)
    start = match.start()
    brace, semi = source.index('{', start), source.index(';', start)
    if '=>' in source[start:brace] and semi < brace: return source[start:semi + 1]
    depth, end = 1, brace + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]

constants = '\n'.join(re.findall(r'^    private const (?:int|float) (?:DesignWidth|MapX|WorldWidth|SlotLeft)[^\n]*', source, re.M))
prefix = (here / 'Scene.cs.txt').read_text(encoding='utf-8').replace('// LIVE_CONSTANTS', constants)
methods = ['MapPoint', 'DrawChrome', 'DrawFilter', 'DrawCloseButton', 'CameraControl', 'DrawMap', 'DrawEdge', 'DrawNode',
           'DrawDetails', 'DrawActionButton', 'DrawSlot', 'DrawLoadoutCaption', 'DrawClearSlot', 'DrawModal']
(out / 'Scene.cs').write_text(prefix + '\n'.join(map(method, methods)) + '\n}', encoding='utf-8')
for name in ['Program', 'Checks']:
    (out / (name + '.cs')).write_text((here / (name + '.cs.txt')).read_text(encoding='utf-8'), encoding='utf-8')
project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
props = ET.SubElement(project, 'PropertyGroup')
for k, v in [('TargetFramework','net8.0'), ('OutputType','Exe'), ('LangVersion','latest')]: ET.SubElement(props,k).text=v
items = ET.SubElement(project, 'ItemGroup')
for lib in ['FNA','ReLogic']:
    ET.SubElement(items, 'Reference', Include=str(args.tml/'Libraries'/lib/'1.0.0'/f'{lib}.dll'))
for name in ['PreviewData.cs', 'PreviewDrawing.cs', 'PreviewDetail.cs']:
    ET.SubElement(items, 'Compile', Include=str(ui/name))
ET.SubElement(items, 'None', Include=str(args.tml/'Libraries/Native/Windows/*.dll'), Link='%(Filename)%(Extension)', CopyToOutputDirectory='PreserveNewest')
ET.ElementTree(project).write(out/'Preview.csproj', encoding='utf-8')
print(out/'Preview.csproj')
