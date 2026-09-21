"""Capture deterministic HTML book poses; never modifies the reference or production assets."""
from pathlib import Path
import argparse
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--chrome', type=Path, default=Path(r'C:\Program Files\Google\Chrome\Application\chrome.exe'))
args = parser.parse_args()
repo = Path(__file__).resolve().parents[2]
reference = repo/'ElainaModSkills/ElainaSkillUI/NewUIExample/elaina-battle-eight-skills.html'
source = reference.read_text(encoding='utf-8')
out = repo/'.vissandbox/constellation-skill-panel/book-reference'
out.mkdir(parents=True, exist_ok=True)
# Relative artwork in the copied document still resolves against the actual reference directory.
source = source.replace('<head>', '<head><base href="'+reference.parent.as_uri()+'/">', 1)
for name, progress, direction in [('opening-048', .48, 1), ('opening-070', .70, 1),
                                  ('opening-084', .84, 1), ('closing-070', .70, -1), ('closing-048', .48, -1)]:
    setup = f'''
    requestBookOpen(true);
    if(bookMotion.frame!==null)cancelAnimationFrame(bookMotion.frame);
    bookMotion.frame=null;bookMotion.progress={progress};bookMotion.curlDirection={direction};
    bookMotion.curlVelocity=0;measureBookDock();drawBookMotion();
    '''
    document = source.replace('</body>', '<script>'+setup+'</script></body>')
    page = out/(name+'.html'); page.write_text(document, encoding='utf-8')
    result = subprocess.run([str(args.chrome), '--headless', '--no-first-run', '--hide-scrollbars',
        '--force-device-scale-factor=1', '--window-size=1600,1000', '--virtual-time-budget=1000',
        '--user-data-dir='+str(out/'chrome-profile'), '--screenshot='+str(out/(name+'.png')), page.as_uri()],
        capture_output=True, timeout=45, creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
    if result.returncode or not (out/(name+'.png')).exists():
        raise RuntimeError(result.stderr.decode(errors='replace'))
    print(name)
print(out)
