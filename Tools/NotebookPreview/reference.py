"""Open a copied, otherwise unchanged reference in headless Chrome for visual comparison."""
from pathlib import Path
import subprocess
here=Path(__file__).resolve().parent
repo=here.parents[1]
out=repo/'.vissandbox/notebook-preview'
source=(repo/'ElainaModSkills/ElainaSkillUI/NewUIExample/elaina-battle-eight-skills.html').read_text(encoding='utf-8')
for scene in ['initial','moon']:
    setup="progression.reset();selected='origin';filter='learned';combat.slots=['ember',null,null,null,null,null,null,null];"
    if scene=='moon':
        setup+="progression.learn('moon');progression.upgrade('moon');selected='moon';targetSlot=7;combat.slots[7]='moon';"
    setup+="openPanel();render();"
    html=out/f'reference-{scene}.html'
    html.write_text(source.replace('</body>',f'<script>{setup}</script></body>'),encoding='utf-8')
    subprocess.run([r'C:\Program Files\Google\Chrome\Application\chrome.exe','--headless','--disable-gpu',
        '--no-first-run','--hide-scrollbars','--force-device-scale-factor=1','--window-size=1600,1000',
        '--virtual-time-budget=1500','--user-data-dir='+str(out/'chrome-profile'),
        '--screenshot='+str(out/'output'/f'reference-{scene}.png'),html.as_uri()],
        stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,timeout=45,creationflags=0x08000000,check=True)
    print(scene)
