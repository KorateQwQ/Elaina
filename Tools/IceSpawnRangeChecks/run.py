"""Rebuild the isolated FNA range check from the current action source."""
import re
import subprocess
import argparse
from pathlib import Path
from xml.sax.saxutils import escape

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--live', action='store_true', help='Open the native live range editor')
parser.add_argument('--live-check', action='store_true', help='Check the live controls in a hidden window')
args = parser.parse_args()
work = root / '.vissandbox/ice-spawn-aim-preview'
work.mkdir(parents=True, exist_ok=True)
targets = (root.parent / 'tModLoader.targets').read_text(encoding='utf-8-sig')
tml = Path(re.search(r'<Import Project="([^"]+)"', targets)[1]).parent
source = (root / 'ElainaActions/Action_IceCone.cs').read_text(encoding='utf-8-sig')

def method(name):
    start = source.index('    internal static ', source.index(name) - 40)
    opening = source.index('{', start)
    end, depth = opening + 1, 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]

properties = []
defaults = []
for name in ['SpawnBackMin', 'SpawnBackMax', 'SpawnSideRange', 'SpawnArcDegrees', 'AimDistance']:
    match = re.search(r'internal static float ' + name + r' => ([^;]+);', source)
    if not match:
        raise ValueError(f'Missing parameter property: {name}')
    defaults.append(f'public static float {name} = {match[1]};')
    properties.append(f'internal static float {name} => Fixture.{name};')

constants = re.findall(r'    private const float (?:MouseAimInfluence|MaxMouseAimCorrection) = [^;]+;', source)
if len(constants) != 2:
    raise ValueError('Missing aiming constants')
velocity = re.search(r'_ => (GetShotDirection\([\s\S]+?\) \* 35f),\s*projectile =>', source)[1]
velocity = velocity.replace('worldAimRotation.ToRotationVector2()', 'forward').replace('GetSpawnPosition()', 'spawn').replace('owner.MountedCenter', 'center').replace('Main.MouseWorld', 'cursor')
generated = ('using System; using Microsoft.Xna.Framework; using Microsoft.Xna.Framework.Graphics;\n'
             'static class Fixture { ' + '\n'.join(defaults) + ' }\n'
             'static class Range {\n' + '\n'.join(properties + constants) + '\n'
             + method('GetSpawnOffset(float') + '\n' + method('GetShotDirection(Vector2') + '\n' + method('DrawSpawnRange(Vector2')
             + '\ninternal static Vector2 LaunchVelocity(Vector2 center, Vector2 forward, Vector2 spawn, Vector2 cursor) => ' + velocity + ';\n}\n')
(work / 'Range.cs').write_text(generated, encoding='utf-8')
(work / 'Preview.cs').write_text((Path(__file__).parent / 'Preview.cs.txt').read_text(encoding='utf-8'), encoding='utf-8')
(work / 'Live.cs').write_text((Path(__file__).parent / 'Live.cs.txt').read_text(encoding='utf-8'), encoding='utf-8')
(work / 'Preview.csproj').write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWindowsForms>true</UseWindowsForms><LangVersion>latest</LangVersion></PropertyGroup>
  <ItemGroup>
    <Reference Include="FNA" HintPath="{escape(str(tml / 'Libraries/FNA/1.0.0/FNA.dll'))}" />
    <None Include="{escape(str(tml / 'Libraries/Native/Windows'))}/*.dll" Link="%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>''', encoding='utf-8')
command = ['dotnet', 'run', '--project', str(work / 'Preview.csproj'), '--', str(work / 'out')]
if args.live or args.live_check:
    command += ['--live-check' if args.live_check else '--live', str(root)]
if args.live:
    subprocess.run(['dotnet', 'build', str(work / 'Preview.csproj'), '-v:q'], check=True, cwd=root)
    log = open(work / 'live.log', 'w', encoding='utf-8')
    process = subprocess.Popen(['dotnet', str(work / 'bin/Debug/net8.0-windows/Preview.dll'), str(work / 'out'), '--live', str(root)],
                               cwd=root, stdout=log, stderr=subprocess.STDOUT, creationflags=subprocess.CREATE_NO_WINDOW)
    (work / 'live.pid').write_text(str(process.pid), encoding='ascii')
    print(f'Live range editor PID: {process.pid}')
else:
    subprocess.run(command, check=True, cwd=root)
