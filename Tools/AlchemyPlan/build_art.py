"""Prepare the revised plan's SVG, coloured-pencil source, and native PNG art."""
import json
import sys
import argparse
from pathlib import Path
from urllib.parse import quote,unquote

sys.dont_write_bytecode=True
from artwork import ART,glyph
from pixels import DRAW
from pencil_style import convert

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
OUT=ROOT/'ElainaModAlchemy/item/ExampleAssets'
WORK=HERE/'generated'

def prepare_existing_pencils(plan):
    """Refresh pencils from saved originals, including retained sample items."""
    profile=plan.get('pencilProfile','hatched')
    overrides={r['art']:r.get('pencilProfile',profile) for r in plan['items']}
    targets=[]
    for png in sorted(OUT.glob('*_Pencil.png')):
        stem=png.stem.removesuffix('_Pencil')
        source=(OUT/(stem+'.svg')).read_text(encoding='utf-8')
        pencil=unquote(convert('data:image/svg+xml,'+quote(source),profile=overrides.get(stem,profile)).split(',',1)[1])
        generated=WORK/(stem+'_Pencil.svg');generated.write_text(pencil,encoding='utf-8')
        targets.append({'source':generated.name,'output':png.name,'size':100})
    for material in plan['materials']:
        if material.get('art'):continue
        source=glyph(material['glyph'],material['color'])
        pencil=unquote(convert('data:image/svg+xml,'+quote(source),profile=profile).split(',',1)[1])
        generated=WORK/('material-'+material['id']+'.svg');generated.write_text(pencil,encoding='utf-8')
        targets.append({'source':generated.name,'output':'Materials/'+material['id']+'_Pencil.png','size':64})
    (WORK/'pencil-refresh.json').write_text(json.dumps(targets,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f'Prepared {len(targets)} pencil-only updates from the saved SVGs and material glyphs.')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--pencils-only',action='store_true',help='Refresh all saved pencil assets without changing SVG or pixel files')
    args=parser.parse_args()
    plan=json.loads((HERE/'plan.json').read_text(encoding='utf-8'))
    WORK.mkdir(parents=True,exist_ok=True);(OUT/'Materials').mkdir(exist_ok=True)
    if args.pencils_only:
        prepare_existing_pencils(plan)
        return
    from PIL import Image
    for item in plan['items']:
        stem=item['art']
        if item.get('reuse'):
            old=item['reuse']
            source=(OUT/(old+'.svg')).read_text(encoding='utf-8')
            if stem!=old:
                (OUT/(stem+'.svg')).write_text(source,encoding='utf-8')
                (OUT/(stem+'_Pixel.png')).write_bytes((OUT/(old+'_Pixel.png')).read_bytes())
        else:
            source=ART[stem]
            (OUT/(stem+'.svg')).write_text(source,encoding='utf-8')
            image=DRAW[stem]();assert set(image.getchannel('A').get_flattened_data())<={0,255}
            image.save(OUT/(stem+'_Pixel.png'))
        pencil=unquote(convert('data:image/svg+xml,'+quote(source),profile=item.get('pencilProfile',plan.get('pencilProfile','hatched'))).split(',',1)[1])
        (WORK/(stem+'_Pencil.svg')).write_text(pencil,encoding='utf-8')
        with Image.open(OUT/(stem+'_Pixel.png')) as image:item['pixelSize']=list(image.size)
    for material in plan['materials']:
        if material.get('art'):continue
        source=glyph(material['glyph'],material['color'])
        pencil=unquote(convert('data:image/svg+xml,'+quote(source),profile=plan.get('pencilProfile','hatched')).split(',',1)[1])
        (WORK/('material-'+material['id']+'.svg')).write_text(pencil,encoding='utf-8')
    (HERE/'plan.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(f"Prepared {len(plan['items'])} item sets and {sum(not m.get('art') for m in plan['materials'])} vanilla material glyphs.")

if __name__=='__main__':main()
