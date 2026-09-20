"""Capture the current HTML and bake only its text-free detail background."""
from pathlib import Path
import html as html_module
import json
import re
import subprocess
from PIL import Image

repo = Path(__file__).resolve().parents[2]
out = repo / '.vissandbox/constellation-skill-panel/reference'
out.mkdir(parents=True, exist_ok=True)
source = (repo / 'ElainaModSkills/ElainaSkillUI/NewUIExample/elaina-battle-eight-skills.html').read_text(encoding='utf-8')
chrome = r'C:\Program Files\Google\Chrome\Application\chrome.exe'

def capture(name, content, width=1600, height=1000, dump=False):
    page = out / (name + '.html')
    page.write_text(content, encoding='utf-8')
    args = [chrome, '--headless', '--disable-gpu', '--no-first-run', '--hide-scrollbars',
            '--force-device-scale-factor=1', f'--window-size={width},{height}',
            '--virtual-time-budget=1800', '--user-data-dir=' + str(out/'chrome-profile'),
            '--screenshot=' + str(out/(name+'.png'))]
    if dump: args.append('--dump-dom')
    result = subprocess.run(args + [page.as_uri()], capture_output=True, timeout=45,
                            creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0), check=True)
    return result.stdout.decode('utf-8', errors='replace')

setup = """
progression.reset();progression.learn('moon');selected='moon';filter='learned';targetSlot=0;
openPanel();render();
setTimeout(()=>{
const panel=document.querySelector('#panel').getBoundingClientRect();
const selectors=['.detail','.detail-hero','.detail-icon-wrap','.detail-scroll','.description','.stats','.stat','.growth-heading','.level-track','.growth-preview','.upgrade-button','.effect-requirements','.detail-actions','.awaken-button'];
const data={};for(const selector of selectors){
 const e=document.querySelector(selector),r=e.getBoundingClientRect(),s=getComputedStyle(e);
 data[selector]={x:r.x-panel.x,y:r.y-panel.y,width:r.width,height:r.height,color:s.color,background:s.background,font:s.font,lineHeight:s.lineHeight,gap:s.gap};
}
const pre=document.createElement('pre');pre.id='metrics';pre.hidden=true;pre.textContent=JSON.stringify(data);document.body.append(pre);
},400);
"""
document = source.replace('</head>', '<style>*,*:before,*:after{animation:none!important;transition:none!important}</style></head>')
dom = capture('moon', document.replace('</body>', '<script>'+setup+'</script></body>'), dump=True)
match = re.search(r'<pre id="metrics"[^>]*>(.*?)</pre>', dom, re.S)
if not match: raise RuntimeError('Reference metrics missing')
metrics = json.loads(html_module.unescape(match.group(1)))
(out/'metrics.json').write_text(json.dumps(metrics, ensure_ascii=False, indent=2), encoding='utf-8')

def background(selector):
    rule = re.search(re.escape(selector)+r'\{([^}]+)\}', source).group(1)
    return re.search(r'(?:^|;)background:([^;]+)', rule).group(1)

# The detail surface includes the full-page gradient beneath it, with identical CSS
# gradient domains and no text, icons, borders, or world imagery. Opaque RGB avoids
# premultiplication differences between Chromium PNG and the tML rawimg loader.
grimoire = re.search(r'\.grimoire\{([^}]+)\}', source).group(1)
shadow = re.search(r'inset 0 0 85px #[0-9a-f]+', grimoire).group(0)
surface = f'''<!doctype html><style>
html,body{{margin:0;background:#14151c;overflow:hidden}}
.page{{position:relative;width:1100px;height:800px;background:{background('.grimoire')};isolation:isolate}}
.inset{{position:absolute;inset:0;box-shadow:{shadow};pointer-events:none}}
.chart{{position:absolute;left:26px;top:146px;width:727px;height:578px;background:{background('.chart:before')}}}
.viewport{{position:absolute;left:0;right:0;top:62px;bottom:43px;overflow:hidden}}
.galaxy{{position:absolute;inset:-150px;background:{background('.galaxy-field').split(',radial-gradient(circle at 35px')[0]}}}
.detail{{position:absolute;left:754px;top:146px;width:320px;height:578px;background:{background('.detail')}}}
</style><div class="page"><div class="inset"></div><div class="chart"><div class="viewport"><div class="galaxy"></div></div></div><div class="detail"></div></div>'''
capture('surface', surface, 1100, 900)
asset = repo/'ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets/NotebookSurface.png'
asset.parent.mkdir(parents=True, exist_ok=True)
Image.open(out/'surface.png').convert('RGB').crop((0,0,1100,800)).save(asset)
print('Reference and CSS metrics:', out)
print('NotebookSurface.png: 1100 x 800 RGB, opaque; approx. 3.36 MiB RGBA GPU storage')
