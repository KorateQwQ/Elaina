// Revised notebook: presentation and reversible crafting demonstration only.
const $a=id=>document.getElementById(id);
const pad=n=>String(n).padStart(2,'0');
const escapeHtml=value=>String(value).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const RECIPES=ALCHEMY_PLAN.items,MATERIALS=ALCHEMY_PLAN.materials,CATEGORIES=ALCHEMY_PLAN.categories;
const RECIPE=Object.fromEntries(RECIPES.map(r=>[r.id,r])),MAT=Object.fromEntries(MATERIALS.map(m=>[m.id,m]));
const CAT_NAME=Object.fromEntries(CATEGORIES.map(([id,name])=>[id,name]));
const ASSET_ROOT='../../../ElainaModAlchemy/item/ExampleAssets/';
const STORE='elaina-alchemy-plan-v2-'+PAGE_VARIANT,MAX_BREW=99;
const STATE_NAMES={ready:'可炼制',short:'缺素材',acquire:'可取得'};
let st=fresh(),selected='mana',category='potion',stateFilter='all',qty=1,artStyle=DEFAULT_ART;
let brewing=false,toastTimer=null,brewTimer=null,helpReturn=null;
function fresh(){return {materials:Object.fromEntries(MATERIALS.map(m=>[m.id,m.initial||0])),products:{mana:3,painkiller:3,bread:2}}}
function load(){
 let raw;try{raw=JSON.parse(localStorage.getItem(STORE)||'null')}catch{return}
 if(raw?.version!==2)return;
 for(const id of Object.keys(st.materials)){const n=raw.materials?.[id];if(Number.isInteger(n)&&n>=0)st.materials[id]=Math.min(9999,n)}
 for(const r of RECIPES){const n=raw.products?.[r.id];if(Number.isInteger(n)&&n>=0)st.products[r.id]=Math.min(9999,n)}
}
function save(){try{localStorage.setItem(STORE,JSON.stringify({version:2,...st}))}catch{}}
const reducedMotion=()=>window.matchMedia('(prefers-reduced-motion: reduce)').matches;
const choices=row=>Array.isArray(row[0])?row[0]:[row[0]];
const available=row=>choices(row).reduce((sum,id)=>sum+(st.materials[id]||0),0);
const maxBrew=r=>r.mats?.length?Math.min(MAX_BREW,...r.mats.map(row=>Math.floor(available(row)/row[1]))):0;
const stateOf=r=>r.acquisition?'acquire':maxBrew(r)>0?'ready':'short';
const owned=r=>(r.outputMaterial||r.material)?st.materials[r.outputMaterial||r.material]||0:st.products[r.id]||0;
const visibleItems=()=>RECIPES.filter(r=>r.cat===category&&(stateFilter==='all'||stateOf(r)===stateFilter));
const cardEl=id=>$a('alc-shelves').querySelector(`[data-recipe="${id}"]`);
function consume(r,count){
 if(!Number.isInteger(count)||count<1||count>maxBrew(r))return false;
 for(const row of r.mats){let need=row[1]*count;for(const id of choices(row)){const take=Math.min(st.materials[id]||0,need);st.materials[id]-=take;need-=take}}
 const bag=r.outputMaterial?st.materials:st.products,key=r.outputMaterial||r.id;
 bag[key]=Math.min(9999,(bag[key]||0)+r.yield*count);
 return true;
}
function artHtml(r){
 if(artStyle==='pixel'){
  const [w,h]=r.pixelSize;
  return `<span class="icon-art pixel-art" style="--pixel-width:${w*2}px;--pixel-height:${h*2}px"><img src="${ASSET_ROOT+r.art}_Pixel.png" width="${w*2}" height="${h*2}" alt="" draggable="false"></span>`;
 }
 return `<span class="icon-art"><img src="${ASSET_ROOT+r.art+(artStyle==='pencil'?'_Pencil.png':'.svg')}" alt="" draggable="false"></span>`;
}
function materialArt(m){
 const src=ASSET_ROOT+(m.art?m.art+'_Pencil.png':'Materials/'+m.id+'_Pencil.png');
 return `<img src="${src}" alt="" draggable="false">`;
}
function offsetWithin(el,ancestor){let x=0,y=0;while(el&&el!==ancestor){x+=el.offsetLeft;y+=el.offsetTop;el=el.offsetParent}return {x,y}}
function renderRank(){
 $a('alc-count-products').textContent=String(RECIPES.filter(r=>r.cat!=='material').length);
 $a('alc-count-materials').textContent=String(RECIPES.filter(r=>r.cat==='material').length);
}
function renderFilters(){
 const counts={all:RECIPES.length,ready:0,short:0,acquire:0};RECIPES.forEach(r=>counts[stateOf(r)]++);
 $a('alc-filters').querySelectorAll('[data-state-filter]').forEach(button=>{
  const key=button.dataset.stateFilter,active=key===stateFilter;
  button.classList.toggle('active',active);button.setAttribute('aria-pressed',String(active));
  button.disabled=key!=='all'&&counts[key]===0;
  button.querySelector('.filter-count').textContent=pad(counts[key]);
 });
 $a('alc-progress').textContent=pad(visibleItems().length);
 $a('alc-total').textContent='/ '+pad(RECIPES.filter(r=>r.cat===category).length);
 $a('alc-art-controls').querySelectorAll('[data-art-mode]').forEach(b=>{const active=b.dataset.artMode===artStyle;b.classList.toggle('active',active);b.setAttribute('aria-pressed',String(active))});
 $a('alchemy-panel').dataset.artMode=artStyle;
}
function renderCategories(){
 const focus=document.activeElement?.dataset?.category;
 $a('alc-categories').innerHTML=CATEGORIES.map(([id,name])=>`<button type="button" data-category="${id}" aria-pressed="${id===category}" class="${id===category?'active':''}">${name}<small>${pad(RECIPES.filter(r=>r.cat===id).length)}</small></button>`).join('');
 if(focus)$a('alc-categories').querySelector(`[data-category="${focus}"]`)?.focus({preventScroll:true});
}
const PAGE_COPY={
 potion:['随身的魔药','为旅途补充魔力，为战斗调配一点勇气。','“药瓶上系着的丝带，是我认出它们的小记号。”'],
 curio:['旅途中的奇物','盛住微光，唤来细雨，也让暗处的危险显形。','“所谓炼金，大概就是替寻常事物发现另一种可能。”'],
 food:['魔女的厨房','热汤与甜点，让漫长的旅途也有值得期待的一餐。','“我擅长炖菜，不过甜点总是更容易让我动心。”'],
 material:['行囊与素材','基础药液在锅中调制，日常食材向商人购入。','“把每一种材料的来处记清楚，下一次就不用翻遍行囊了。”']
};
function cardHtml(r){
 const state=stateOf(r),active=r.id===selected,no=pad(RECIPES.indexOf(r)+1),name=escapeHtml(r.name);
 const info=r.acquisition?`<span>${escapeHtml(r.stage)}</span><span>持有<b>${owned(r)}</b></span>`:`<span>每批<b>×${r.yield}</b></span><span class="can">可制<b>${maxBrew(r)}</b></span>`;
 return `<button class="alc-card ${state}${active?' selected':''}" type="button" data-recipe="${r.id}" aria-pressed="${active}" aria-label="${name}，${STATE_NAMES[state]}" title="${name} · ${escapeHtml(r.stage)}"><span class="alc-no">No.${no}</span><i class="alc-glyph ${state}" aria-hidden="true"></i><span class="alc-frame">${artHtml(r)}</span><span class="alc-name">${name}</span><span class="alc-meta">${info}</span></button>`;
}
function renderCatalog(){
 const focus=document.activeElement?.closest?.('.alc-card')?.dataset.recipe,items=visibleItems(),copy=PAGE_COPY[category];
 $a('alc-page-title').textContent=copy[0];$a('alc-page-description').textContent=copy[1];
 $a('alc-shelves').innerHTML=items.length?`<div class="alc-row">${items.map(cardHtml).join('')}</div><div class="plan-page-note"><svg aria-hidden="true"><use href="#notebook-quill"/></svg><p>${copy[2]}</p><small>— 伊蕾娜的手记</small></div>`:'<div class="plan-empty">这一页暂时没有符合条件的条目。<small>试试切换分类，或查看全部。</small></div>';
 if(focus)cardEl(focus)?.focus({preventScroll:true});
 placeSelectionQuill();
}
function placeSelectionQuill(){
 const marker=$a('alc-selection-quill'),card=cardEl(selected),scroller=$a('alc-shelves');
 marker.hidden=!card;if(!card)return;
 const height=marker.offsetHeight||42,width=marker.offsetWidth||22;
 const {x,y}=offsetWithin(card,$a('alc-catalog')),top=y-scroller.scrollTop+card.offsetHeight-height-8;
 marker.hidden=top<scroller.offsetTop-4||top+height>scroller.offsetTop+scroller.clientHeight+4;
 marker.style.transform=`translate(${x+card.offsetWidth-Math.round(width/3)}px,${top}px)`;
}
function effectRows(r){
 const rows=r.effects.map(([name,value,tone])=>`<li class="${tone||''}"><span>${escapeHtml(name)}</span><b>${escapeHtml(value)}</b></li>`);
 if(r.live==='starpower'){
  const mp=typeof combat!=='undefined'?combat.mpMax:100,bonus=Number((mp*.05).toFixed(2));
  rows.push(`<li class="live"><span>当前魔力上限 ${mp}</span><b>魔法伤害 +${bonus}%</b></li>`);
 }
 return rows.join('');
}
function matRows(r){
 return (r.mats||[]).map(row=>{
  const ids=choices(row),name=ids.map(id=>MAT[id].name).join(' / '),have=available(row),need=row[1]*qty,lack=have<need;
  const icons=ids.map(id=>`<span class="alc-mat-icon">${materialArt(MAT[id])}</span>`).join('');
  const source=ids.length>1?'二者可合计使用':MAT[ids[0]].source,entry=ids.length===1&&MAT[ids[0]].entry;
  const heading=entry?`<button type="button" class="plan-mat-link" data-open-entry="${entry}">${escapeHtml(name)}<em>查看</em></button>`:escapeHtml(name);
  return `<li class="alc-mat${lack?' short':''}"><span class="plan-mat-art${ids.length>1?' pair':''}">${icons}</span><span class="alc-mat-name">${heading}<small title="${escapeHtml(source)}">${escapeHtml(source)}</small></span><span class="alc-mat-count"><b>${have}</b> / ${need}</span></li>`;
 }).join('');
}
function renderDetail(){
 const r=RECIPE[selected],state=stateOf(r);
 $a('alc-chapter').textContent=CAT_NAME[r.cat]+' · No.'+pad(RECIPES.indexOf(r)+1);
 $a('alc-status').textContent=STATE_NAMES[state];$a('alc-status').dataset.state=state;
 $a('alc-icon').innerHTML=artHtml(r);$a('alc-name').textContent=r.name;$a('alc-type').textContent=r.kind;
 $a('alc-desc').textContent=r.desc;$a('alc-desc').classList.remove('riddle');
 $a('alc-effects').hidden=false;$a('alc-effects').innerHTML=effectRows(r);
 $a('alc-unlock').hidden=false;
 $a('alc-unlock').innerHTML=`<h3>${r.acquisition?'获取方式':'获取阶段'}</h3><p class="alc-unlock-main">${escapeHtml(r.acquisition||r.stage)}</p>${r.acquisition?`<small class="plan-source-note">${escapeHtml(r.stage)} · 持有 ${owned(r)}</small>`:''}`;
 $a('alc-material-section').hidden=!!r.acquisition;
 $a('alc-mats-title').textContent='所需素材';
 renderBrew();
}
function renderBrew(){
 const r=RECIPE[selected],max=maxBrew(r),sourced=!!r.acquisition;
 qty=Math.max(1,Math.min(qty,Math.max(1,max)));
 $a('alc-mats-note').textContent=`× ${qty} 批`;$a('alc-mats').innerHTML=matRows(r);
 $a('alc-qty-row').hidden=sourced;
 const input=$a('alc-qty');if(document.activeElement!==input)input.value=String(qty);
 input.disabled=max<1||brewing;
 $a('alc-minus').disabled=max<1||qty<=1||brewing;$a('alc-plus').disabled=max<1||qty>=max||brewing;$a('alc-max').disabled=max<1||qty>=max||brewing;
 $a('alc-yield').innerHTML=sourced?'':`<small>产出</small>× ${r.yield*qty}`;
 const craft=$a('alc-craft');craft.disabled=sourced||max<1||brewing;
 craft.innerHTML=brewing?'炼制中…':sourced?(r.stage==='商人'?'商人售卖':r.stage==='旅商'?'旅商固定售卖':'以太滴管取得'):max<1?'素材不足':`${r.cat==='food'?'制作':'炼制'} <span class="cost">× ${r.yield*qty}</span>`;
 const lacking=(r.mats||[]).filter(row=>available(row)<row[1]).map(row=>choices(row).map(id=>MAT[id].name).join(' / '));
 $a('alc-hint').textContent=sourced?'可在素材页查看食材与药液的获取方式':brewing?'材料正在慢慢交融…':max<1?'还缺 '+lacking.join('、'):`持有 ${owned(r)} · 每批产出 ${r.yield} · 最多可制 ${max} 批`;
 renderBag(r);
}
function renderBag(r){
 const ids=[...new Set((r.mats||[]).flatMap(choices))];
 if(!ids.length&&r.material)ids.push(r.material);
 $a('alc-bag').classList.toggle('focused',ids.length>0);
 $a('alc-bag').innerHTML=ids.map(id=>{const m=MAT[id],have=st.materials[id]||0,entry=m.entry;
  return `<${entry?'button':'span'} ${entry?'type="button"':''} class="alc-chip need" data-mat="${id}"${entry?` data-open-entry="${entry}"`:''} title="${escapeHtml(m.name+' · 持有 '+have+' · '+m.source)}" aria-label="${escapeHtml(m.name)}，持有 ${have}">${materialArt(m)}<b>${have>999?'999+':have}</b></${entry?'button':'span'}>`;
 }).join('');
}
function render(){
 if(stateFilter!=='all'&&!RECIPES.some(r=>stateOf(r)===stateFilter))stateFilter='all';
 renderRank();renderCategories();renderFilters();renderCatalog();renderDetail();
}
function notify(message){clearTimeout(toastTimer);const toast=$a('alc-toast');toast.textContent=message;toast.classList.add('show');toastTimer=setTimeout(()=>toast.classList.remove('show'),3000)}
function select(id,{focus=false}={}){
 if(!RECIPE[id])return;
 const moved=selected!==id;selected=id;category=RECIPE[id].cat;
 if(stateFilter!=='all'&&stateOf(RECIPE[id])!==stateFilter)stateFilter='all';
 if(moved){qty=1;$a('alc-scroll').scrollTop=0}
 render();
 if(focus){cardEl(id)?.focus({preventScroll:true});cardEl(id)?.scrollIntoView({block:'nearest',inline:'nearest'})}
 $a('alc-announce').textContent=RECIPE[id].name+'，'+STATE_NAMES[stateOf(RECIPE[id])];
}
function setCategory(id){
 if(!CAT_NAME[id])return;category=id;stateFilter='all';
 const entries=visibleItems();if(entries.length&&!entries.some(r=>r.id===selected))selected=entries[0].id;
 qty=1;$a('alc-scroll').scrollTop=0;$a('alc-shelves').scrollTop=0;render();
}
function setFilter(key){
 stateFilter=key;
 if(key!=='all'&&!visibleItems().length){const first=RECIPES.find(r=>stateOf(r)===key);if(first){category=first.cat;selected=first.id;qty=1}}
 const entries=visibleItems();if(entries.length&&!entries.some(r=>r.id===selected)){selected=entries[0].id;qty=1}
 render();
}
function setQty(value){qty=Math.max(1,Math.min(Number.isFinite(value)?Math.round(value):1,Math.max(1,maxBrew(RECIPE[selected]))));renderBrew()}
function craft(){
 const r=RECIPE[selected];if(brewing||r.acquisition)return;
 const count=Math.min(qty,maxBrew(r));if(!consume(r,count))return;
 save();brewing=true;$a('alc-detail').classList.add('brewing');renderBrew();
 clearTimeout(brewTimer);brewTimer=setTimeout(()=>{
  brewing=false;$a('alc-detail').classList.remove('brewing');render();
  cardEl(r.id)?.classList.add('crafted');
  notify(`${r.cat==='food'?'做好了':'炼成'}「${r.name}」× ${r.yield*count}`);
 },reducedMotion()?0:650);
}
function restock(){
 for(const m of MATERIALS)st.materials[m.id]=Math.min(9999,(st.materials[m.id]||0)+Math.max(3,m.initial||3));
 save();render();notify('行囊中的展示素材已补充');
}
function resetNotebook(){
 clearTimeout(brewTimer);brewing=false;$a('alc-detail').classList.remove('brewing');
 st=fresh();selected='mana';category='potion';stateFilter='all';qty=1;save();render();notify('炼金手记已重置');
}
function openAlcHelp(){helpReturn=document.activeElement;$a('alc-help-layer').hidden=false;$a('alc-help-close').focus()}
function closeAlcHelp(){$a('alc-help-layer').hidden=true;if(helpReturn?.isConnected)helpReturn.focus({preventScroll:true})}
function moveSelection(key,from){
 const items=visibleItems(),index=items.findIndex(r=>r.id===from);if(index<0)return;
 const offset=key==='ArrowLeft'?-1:key==='ArrowRight'?1:key==='ArrowUp'?-4:4;
 select(items[Math.max(0,Math.min(items.length-1,index+offset))].id,{focus:true});
}

// WIRE: browser interaction begins here; pure recipe helpers above also support offline checks.
$a('alc-shelves').addEventListener('click',event=>{const card=event.target.closest('[data-recipe]');if(card)select(card.dataset.recipe)});
$a('alc-shelves').addEventListener('scroll',placeSelectionQuill,{passive:true});
$a('alc-categories').addEventListener('click',event=>{const button=event.target.closest('[data-category]');if(button)setCategory(button.dataset.category)});
$a('alc-filters').addEventListener('click',event=>{const button=event.target.closest('[data-state-filter]');if(button)setFilter(button.dataset.stateFilter)});
$a('alc-art-controls').addEventListener('click',event=>{const b=event.target.closest('[data-art-mode]');if(!b)return;artStyle=b.dataset.artMode;render();$a('alc-announce').textContent='图标已切换为'+({pencil:'彩铅',pixel:'像素',original:'原画'}[artStyle])});
for(const container of ['alc-mats','alc-bag'])$a(container).addEventListener('click',event=>{const button=event.target.closest('[data-open-entry]');if(button)select(button.dataset.openEntry)});
$a('alc-minus').addEventListener('click',()=>setQty(qty-1));$a('alc-plus').addEventListener('click',()=>setQty(qty+1));$a('alc-max').addEventListener('click',()=>setQty(maxBrew(RECIPE[selected])));
$a('alc-qty').addEventListener('input',event=>{const input=event.target,digits=input.value.replace(/\D/g,'').slice(0,2);input.value=digits;if(digits){setQty(Number(digits));input.value=String(qty)}});
$a('alc-qty').addEventListener('change',event=>{setQty(Number(event.target.value)||1);event.target.value=String(qty)});
$a('alc-qty').addEventListener('blur',event=>{event.target.value=String(qty)});
$a('alc-craft').addEventListener('click',craft);$a('alc-restock').addEventListener('click',restock);$a('alc-reset').addEventListener('click',resetNotebook);
$a('alc-help').addEventListener('click',openAlcHelp);$a('alc-help-close').addEventListener('click',closeAlcHelp);
$a('alc-help-layer').addEventListener('click',event=>{if(event.target===$a('alc-help-layer'))closeAlcHelp()});
