from pathlib import Path
import re
import sys

repo, output = map(Path, sys.argv[1:3])
source = (repo / 'ElainaModSkills/Skills/Ice/IceConeTargetSystem.cs').read_text(encoding='utf-8-sig')
def method(name):
    match = re.search(r'    private static [^\n]+\b' + name + r'\([^\n]*\)\s*\{', source)
    assert match, name
    end = source.index('{', match.start()) + 1
    depth = 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[match.start():end]

fields = '\n'.join(re.findall(r'    private (?:const|static readonly) [^\n]+\b(?:ReticleExtent|NoiseRange|DeepColor|MidColor)\s*=.*?;', source))
methods = '\n'.join(method(n) for n in ['SetReticleParameters', 'EaseOutBack', 'SmoothStep'])
methods = methods.replace('Main.GlobalTimeWrappedHourly', 'clock').replace('Main.GameViewMatrix.Zoom.X', 'zoom')
result = '''using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
internal static class ReticleReplay {
    private static float clock, zoom, lockProgress, age, alpha, reticleRadius;
    internal static void Apply(Effect effect, float time, float scale, float progress, float ageSeconds) {
        clock = time; zoom = scale; lockProgress = progress; age = ageSeconds; alpha = 1; reticleRadius = 52;
        SetReticleParameters(effect);
    }
''' + fields + '\n' + methods + '\n}\n'
(output / 'ReticleReplay.cs').write_text(result, encoding='utf-8-sig')
