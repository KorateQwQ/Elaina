"""Build the three reviewed notebook variants from one plan and interface source."""
import json
import re
from pathlib import Path
from urllib.parse import quote

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
EXAMPLES=ROOT/'ElainaModSkills/ElainaSkillUI/NewUIExample'
SHELL=HERE/'notebook-shell.html'


def prepare_shell():
    page=(EXAMPLES/'elaina-battle-eight-skills-alchemy-all-pencil.html').read_text(encoding='utf-8')
    scope=page.rindex("(()=>{'use strict';")
    start=page.index('const $a=',scope);end=page.index("/* The skill book's flight",start)
    page=page[:start]+'__ALCHEMY_PLAN_SCRIPT__\n'+page[end:]
    page=re.sub(r'<title>.*?</title>','<title>__PAGE_TITLE__</title>',page,count=1)
    page=re.sub(r'<h1>炼金手记(?:<small>.*?</small>)?</h1>','<h1>炼金手记<small>魔药与造物</small></h1>',page,count=1)
    page=page.replace('<span>全部 22 件 · 紫色笔触与淡彩</span>','<span>配方、奇物与旅途中的一餐</span>',1)
    start=page.index('<div class="alc-rank" id="alc-rank">');end=page.index('<button class="close" id="alc-close"',start)
    page=page[:start]+'''<div class="plan-counter" id="alc-rank"><div><b id="alc-count-products">15</b><span>造物</span></div><i></i><div><b id="alc-count-materials">8</b><span>素材</span></div></div>'''+page[end:]
    filters=''.join(f'<button class="filter{ " active" if key=="all" else ""}" type="button" data-state-filter="{key}" aria-pressed="{str(key=="all").lower()}"><i class="state-glyph glyph-{key}" aria-hidden="true"></i>{label}<span class="filter-count">00</span></button>' for key,label in [('all','全部'),('ready','可炼制'),('short','缺素材'),('acquire','可取得')])
    styles=''.join(f'<button type="button" data-art-mode="{key}" aria-pressed="false">{label}</button>' for key,label in [('pencil','彩铅'),('pixel','像素'),('original','原画')])
    nav='<nav class="navigation" aria-label="炼制状态"><div class="filters" id="alc-filters">'+filters+'</div><div class="plan-art-controls" id="alc-art-controls" role="group" aria-label="图标画风">'+styles+'</div><div class="progress" title="本页显示 / 本页全部"><b id="alc-progress">08</b><em id="alc-total">/ 08</em></div></nav>'
    page,n=re.subn(r'<nav class="navigation" aria-label="炼制状态">[\s\S]*?</nav>',lambda _:nav,page,count=1);assert n==1
    page=page.replace('<div class="alc-shelves" id="alc-shelves"></div>','<div class="plan-categories" id="alc-categories" role="group" aria-label="造物分类"></div><div class="plan-page-heading"><h2 id="alc-page-title"></h2><p id="alc-page-description"></p></div><div class="alc-shelves" id="alc-shelves"></div>',1)
    legend='<div class="alc-legend" aria-hidden="true"><span><i class="alc-glyph ready"></i>可炼制</span><span><i class="alc-glyph short"></i>缺素材</span><span><i class="alc-glyph acquire"></i>购买或采集</span></div>'
    page=re.sub(r'<div class="alc-legend"[\s\S]*?</div>',lambda _:legend,page,count=1)
    page=page.replace('<section class="alc-materials">','<section class="alc-materials" id="alc-material-section">',1)
    page=page.replace('<span>炼制份数</span>','<span>制作批数</span>',1)
    page=page.replace('<div class="alc-bag-caption">行囊<small>炼金素材</small></div>','<div class="alc-bag-caption">行囊<small>本次相关</small></div>',1)
    help_markup='''<div class="help-layer" id="alc-help-layer" hidden><section class="help-box" role="dialog" aria-modal="true" aria-labelledby="alc-help-title"><h2 id="alc-help-title">炼金手记</h2><p>在魔药、奇物、料理与素材之间翻页。选中条目，右页会写下它的效果、获取阶段和所需素材。</p><dl><dt>彩铅 / 像素 / 原画</dt><dd>切换整页的物品图标。原版素材始终以简洁的彩铅符号表示。</dd><dt>配方与材料</dt><dd>椎骨与腐肉、铁锭与铅锭可以合计使用。点击月露、树脂等素材名称，可查看它们的来处。</dd><dt>演示操作</dt><dd>炼制会扣除展示素材并增加产物；「补充素材」补充行囊，「重置手记」恢复初始数量。</dd><dt>快捷键</dt><dd>P 打开 / 合上手记 · Esc 返回<br>方向键选择条目 · C 切换魔女手札</dd></dl><button class="awaken-button" id="alc-help-close" type="button">返回</button></section></div>'''
    page,n=re.subn(r'<div class="help-layer" id="alc-help-layer"[\s\S]*?</section></div>',lambda _:help_markup,page,count=1);assert n==1
    index=page.rindex('</style>');page=page[:index]+'\n__ALCHEMY_PLAN_CSS__\n'+page[index:]
    page=page.replace('-all-pencil-preview','-plan-v2-__VARIANT__')
    SHELL.write_text(page,encoding='utf-8',newline='\n')
    return page


def main():
    shell=SHELL.read_text(encoding='utf-8') if SHELL.exists() else prepare_shell()
    plan=json.loads((HERE/'plan.json').read_text(encoding='utf-8'))
    logic=(HERE/'notebook.js').read_text(encoding='utf-8');css=(HERE/'notebook.css').read_text(encoding='utf-8')
    quill=(HERE/'ui/selection-quill.svg').read_text(encoding='utf-8')
    css=css.replace('__SELECTION_QUILL__','data:image/svg+xml,'+quote(quill,safe=''))
    variants=[('elaina-battle-eight-skills-alchemy.html','main','pencil','炼金手记 · 魔药与造物'),('elaina-battle-eight-skills-alchemy-all-pencil.html','pencil','pencil','炼金手记 · 彩铅版'),('elaina-battle-eight-skills-alchemy-all-pixel.html','pixel','pixel','炼金手记 · 像素版')]
    for filename,variant,style,title in variants:
        data='const ALCHEMY_PLAN='+json.dumps(plan,ensure_ascii=False,separators=(',',':')).replace('</','<\\/')+';\n'
        data+='const DEFAULT_ART='+json.dumps(style)+',PAGE_VARIANT='+json.dumps(variant)+';\n'
        page=shell.replace('__ALCHEMY_PLAN_SCRIPT__',data+logic).replace('__ALCHEMY_PLAN_CSS__',css).replace('__VARIANT__',variant).replace('__PAGE_TITLE__',title)
        assert not re.search(r'__(?:ALCHEMY|VARIANT|PAGE_TITLE)',page)
        (EXAMPLES/filename).write_text(page,encoding='utf-8',newline='\n')
    print('Updated main, pencil, and pixel notebooks from one plan: 15 creations + 8 material entries.')

if __name__=='__main__':main()
