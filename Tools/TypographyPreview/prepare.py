"""Refresh the preview from live panel drawing methods, then link the actual text helper."""
from pathlib import Path
import re
import argparse
import xml.etree.ElementTree as ET

p = argparse.ArgumentParser()
p.add_argument('--tml', required=True, type=Path)
args = p.parse_args()
repo = Path(__file__).resolve().parents[2]
out = repo / '.vissandbox/typography-preview'
out.mkdir(parents=True, exist_ok=True)
ui = repo / 'ElainaModSkills/ElainaSkillUI/ConstellationPreview'
source = (ui / 'ConstellationPreviewUI.cs').read_text(encoding='utf-8')

def method(name):
    match = re.search(r'^    private [^\n]*\b' + name + r'\(', source, re.M)
    start = match.start()
    brace = source.index('{', start)
    semi = source.index(';', start)
    if '=>' in source[start:brace] and semi < brace:
        return source[start:semi+1]
    depth, end = 1, brace+1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]

prefix = '''using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationPreview.PreviewDrawing;
namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationPreview;
internal sealed class PanelReplay
{
    private readonly PreviewState _state = new(new Terraria.ModLoader.Mod());
    private readonly PreviewDrawing _draw = new(new Terraria.ModLoader.Mod());
    private const int MapX=26, MapY=236, MapWidth=727, MapHeight=445, WorldWidth=727, WorldHeight=550;
    private const float DesignWidth=1100, DesignHeight=800;
    private static readonly string[] SlotKeys = PreviewState.SlotLabels;
    // LOADOUT_LAYOUT
    private const string ToggleKeyLabel="C";
    private float _zoom=.84f, _flash;
    private Vector2 _pan;
    private string _hoverNode, _modal, _tooltip;
    private HashSet<string> _ancestors;
    internal void Render(SpriteBatch batch, float scale, bool modal, string scenario = "initial")
    {
        VerifySlots();
        if (scenario != "initial")
        {
            _state.Selected="vortex";
            if (!_state.Unlock()) throw new Exception("Cannot unlock demo vortex.");
            _state.TargetSlot=2; _state.Equip();
            _state.Selected="fireflower";
            if (!_state.Unlock()) throw new Exception("Cannot unlock demo fireflower.");
            _state.Slots[0]=null;
            _state.TargetSlot=1; _state.Equip();
            _state.TargetSlot=scenario=="eighth" ? 7 : scenario=="occupied" ? 1 : 0;
            if(scenario=="eighth") _state.Equip();
        }
        _modal=modal ? "help" : null;
        _pan=(new Vector2(MapWidth,MapHeight)-new Vector2(WorldWidth,WorldHeight)*_zoom)/2;
        _ancestors=_state.Ancestors();
        _draw.Batch=batch; _draw.Scale=scale; _draw.Origin=Vector2.Zero;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        DrawChrome(); DrawDetails(); DrawActionButton(false);
        for(int i=0;i<SlotKeys.Length;i++) DrawSlot(i,scenario=="hover" && i==7);
        DrawClearSlot(scenario=="occupied");
        string[] labels=["全部星轨","月之咒术","自然之息","未竟秘仪"];
        for(int i=0;i<4;i++) _draw.Text(labels[i],245+i*88,133,12,Muted,.5f);
        batch.End();
        using var clip=new RasterizerState {ScissorTestEnable=true,CullMode=CullMode.None};
        batch.GraphicsDevice.ScissorRectangle=new Rectangle((int)(MapX*scale),(int)(MapY*scale),(int)(MapWidth*scale),(int)(MapHeight*scale));
        batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,clip);
        DrawMap(); batch.End();
        batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,RasterizerState.CullNone);
        DrawModal(); batch.End();
    }
    private void VerifySlots()
    {
        if (_state.Slots.Length!=8) throw new Exception("Expected eight slots.");
        _state.Selected="ember"; _state.TargetSlot=7;
        if (!_state.Equip() || _state.Slots[7]!="ember" || _state.Slots[0]!=null || _state.CanEquip)
            throw new Exception("Moving equipment into slot 08 failed.");
        _state.Slots[7]=null;
        if (!_state.CanEquip || !_state.Equip()) throw new Exception("Re-equipping a cleared slot failed.");
        _state.Reset();
        if (_state.TargetSlot!=0 || _state.Slots[0]!="ember" || _state.Slots.Skip(1).Any(s=>s!=null))
            throw new Exception("Eight-slot reset failed.");
    }
'''
layout = re.search(r'^    private const int SlotLeft[^\n]+', source, re.M).group(0)
prefix = prefix.replace('    // LOADOUT_LAYOUT', layout)
methods=['MapPoint','DrawChrome','DrawMap','DrawEdge','DrawNode','DrawDetails','RequirementRow','DrawActionButton','DrawSlot','DrawLoadoutCaption','DrawClearSlot','DrawModal']
(out/'PanelReplay.cs').write_text(prefix+'\n'.join(map(method,methods))+'\n}',encoding='utf-8')
(out/'Program.cs').write_text((Path(__file__).parent/'Preview.cs.txt').read_text(encoding='utf-8'),encoding='utf-8')
project=ET.Element('Project',Sdk='Microsoft.NET.Sdk')
props=ET.SubElement(project,'PropertyGroup')
for k,v in [('TargetFramework','net8.0'),('OutputType','Exe'),('LangVersion','latest')]: ET.SubElement(props,k).text=v
items=ET.SubElement(project,'ItemGroup')
for lib in ['FNA','ReLogic']: ET.SubElement(items,'Reference',Include=str(args.tml/'Libraries'/lib/'1.0.0'/f'{lib}.dll'))
for file in ['PreviewDrawing.cs','PreviewData.cs']: ET.SubElement(items,'Compile',Include=str(ui/file))
ET.SubElement(items,'None',Include=str(args.tml/'Libraries/Native/Windows/*.dll'),Link='%(Filename)%(Extension)',CopyToOutputDirectory='PreserveNewest')
ET.ElementTree(project).write(out/'Preview.csproj',encoding='utf-8')
print(out/'Preview.csproj')
