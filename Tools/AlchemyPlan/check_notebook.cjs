// Offline script, data, file-reference, and rendering-state checks (not a browser screenshot).
const fs=require('fs'),path=require('path'),vm=require('vm'),assert=require('assert/strict');
const here=__dirname,root=path.resolve(here,'../..'),dir=path.join(root,'ElainaModSkills/ElainaSkillUI/NewUIExample');
const files=['elaina-battle-eight-skills-alchemy.html','elaina-battle-eight-skills-alchemy-all-pencil.html','elaina-battle-eight-skills-alchemy-all-pixel.html'];
for(const file of files){
 const html=fs.readFileSync(path.join(dir,file),'utf8');
 const scripts=[...html.matchAll(/<script(?:\s[^>]*)?>([\s\S]*?)<\/script>/g)].map(m=>m[1]);
 scripts.forEach((s,i)=>new vm.Script(s,{filename:file+':'+i}));
 const s=scripts.find(s=>s.includes('const ALCHEMY_PLAN='));
 assert(s&&!s.includes('const PENCIL_ART=')&&!s.includes('const MARKS='));
 const idList=[...html.slice(0,html.indexOf('<script')).matchAll(/\bid="([^"]+)"/g)].map(m=>m[1]);
 const ids=new Set(idList);assert.equal(ids.size,idList.length,'Duplicate static element ID');
 for(const match of s.matchAll(/\$a\('([^']+)'\)/g))assert(ids.has(match[1]),'Missing static element: '+match[1]);
 const elements=new Map();
 function node(id,dataset={}){
  const classes=new Set();return {id,dataset,style:{},hidden:false,disabled:false,textContent:'',innerHTML:'',value:'1',scrollTop:0,clientHeight:429,offsetTop:0,offsetLeft:0,offsetWidth:id==='alc-selection-quill'?22:150,offsetHeight:id==='alc-selection-quill'?42:132,offsetParent:null,isConnected:true,
   classList:{add:x=>classes.add(x),remove:x=>classes.delete(x),contains:x=>classes.has(x),toggle:(x,on)=>{if(on===undefined)on=!classes.has(x);on?classes.add(x):classes.delete(x);return on}},
   setAttribute(){},focus(){},scrollIntoView(){},querySelector(q){
    if(q==='.filter-count')return this.counter||(this.counter=node(id+'-count'));
    const found=q.match(/\[data-(recipe|category)="([^\"]+)"\]/);
    if(found&&this.innerHTML.includes(`data-${found[1]}="${found[2]}"`)){
     const child=node(found[2]);child.offsetParent=this;
     if(found[1]==='recipe'){
      const entries=[...this.innerHTML.matchAll(/data-recipe="([^"]+)"/g)].map(m=>m[1]),index=entries.indexOf(found[2]);
      child.offsetLeft=(index%4)*162+2;child.offsetTop=Math.floor(index/4)*144+2;child.offsetHeight=132;
     }
     return child;
    }
    return null;
   },querySelectorAll(q){
    if(q==='[data-state-filter]')return ['all','ready','short','acquire'].map(key=>node(key,{stateFilter:key}));
    if(q==='[data-art-mode]')return ['pencil','pixel','original'].map(key=>node(key,{artMode:key}));
    return [];
   }};
 }
 ids.forEach(id=>elements.set(id,node(id)));
 const timers=new Map(),memory=new Map();let nextTimer=0;
 const ctx=vm.createContext({document:{activeElement:null,getElementById:id=>elements.get(id)},window:{matchMedia:()=>({matches:false})},
  localStorage:{getItem:key=>memory.get(key)||null,setItem:(key,val)=>memory.set(key,val)},
  setTimeout:cb=>{timers.set(++nextTimer,cb);return nextTimer},clearTimeout:id=>timers.delete(id),combat:{mpMax:200}});
 vm.runInContext(s.slice(s.indexOf('const ALCHEMY_PLAN='),s.indexOf('// WIRE:')),ctx);
 const result=vm.runInContext(`(()=>{
  if(RECIPES.length!==23||MATERIALS.length!==36)throw Error('Unexpected plan count');
  if(MATERIALS.some(m=>m.id.includes('pollen')))throw Error('Removed material remains');
  if(RECIPE.isolation.name!=='净土露滴'||RECIPE.mana.name!=='月露合剂')throw Error('Names not updated');
  render();
  for(const style of ['pencil','pixel','original']){artStyle=style;for(const r of RECIPES){select(r.id);if($a('alc-name').textContent!==r.name)throw Error('Detail name mismatch');if(!$a('alc-icon').innerHTML.includes(r.art))throw Error('Missing art');if($a('alc-material-section').hidden!==!!r.acquisition)throw Error('Material source view mismatch')}}
  const links=[];for(const style of ['pencil','pixel','original']){artStyle=style;for(const r of RECIPES)links.push(artHtml(r).match(/src="([^"]+)"/)[1])}
  for(const m of MATERIALS)links.push(materialArt(m).match(/src="([^"]+)"/)[1]);
  const empty=()=>{st=fresh();Object.keys(st.materials).forEach(k=>st.materials[k]=0)};
  empty();Object.assign(st.materials,{water:1,deathweed:1,vertebra:1,rottenchunk:2});
  if(maxBrew(RECIPE.bloodlust)!==1||!consume(RECIPE.bloodlust,1)||st.materials.vertebra!==0||st.materials.rottenchunk!==0)throw Error('Vertebra/rotten chunk alternative failed');
  empty();Object.assign(st.materials,{glass:10,iron:2,lead:3,hallowed:3});
  if(maxBrew(RECIPE.dropper)!==1||!consume(RECIPE.dropper,1)||st.products.dropper!==1)throw Error('Metal alternative failed');
  empty();Object.assign(st.materials,{water:3,moonglow:1});
  if(!consume(RECIPE.moondew,1)||st.materials.moondew!==3)throw Error('Base liquid output not in material bag');
  Object.assign(st.materials,{wood:20,gel:2});if(!consume(RECIPE.resin,1)||st.materials.resin!==2)throw Error('Resin yield');
  empty();Object.assign(st.materials,{moondew:3,star:1,glowingmushroom:3});
  const before=st.products.mana;if(!consume(RECIPE.mana,1)||st.products.mana-before!==3)throw Error('Moon dew elixir yield');
  if(consume(RECIPE.mana,1)||Object.values(st.materials).some(v=>v<0))throw Error('Overspending materials');
  empty();Object.assign(st.materials,{shimmer:1,obsidian:1,star:1});if(!consume(RECIPE.mimic,1)||st.products.mimic!==50)throw Error('Dust yield');
  empty();Object.assign(st.materials,{pixiedust:1,blinkroot:1,bone:3});if(!consume(RECIPE.trace,1)||st.products.trace!==3)throw Error('Revealing dust yield');
  for(const id of ['flour','sugar','egg','beef','potato','shimmer'])if(stateOf(RECIPE[id])!=='acquire'||consume(RECIPE[id],1))throw Error('Acquisition entry was craftable');
  st=fresh();select('rain');setQty(999);if(qty!==3)throw Error('Quantity clamp');craft();if(st.products.rain!==3||!brewing)throw Error('Craft button behavior');
  resetNotebook();if(brewing||selected!=='mana'||category!=='potion')throw Error('Reset');
  setFilter('acquire');if(category!=='material')throw Error('Acquisition filter did not navigate');setCategory('food');if(stateFilter!=='all'||category!=='food')throw Error('Category state');
  select('starpower');if(!effectRows(RECIPE.starpower).includes('+10%'))throw Error('Magic scaling display');
  select('mana');const first=$a('alc-selection-quill').style.transform;
  select('painkiller');if($a('alc-selection-quill').style.transform===first||$a('alc-selection-quill').hidden)throw Error('Quill did not move to the next visible slip');
  $a('alc-shelves').scrollTop=1000;placeSelectionQuill();if(!$a('alc-selection-quill').hidden)throw Error('Quill escaped the scrolling area');
  $a('alc-shelves').scrollTop=0;placeSelectionQuill();if($a('alc-selection-quill').hidden)throw Error('Quill did not return with the visible slip');
  return {links};
 })()`,ctx);
 for(const link of result.links)assert(fs.existsSync(path.resolve(dir,link)),'Missing referenced image: '+link);
 console.log(file+': compiled '+scripts.length+' scripts, rendered all 23 entries in 3 modes, checked material links and recipe interactions.');
}
