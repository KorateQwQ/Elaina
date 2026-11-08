'use strict';
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const { pathToFileURL } = require('node:url');
const htmlPath = path.join(__dirname, 'index.html');
const html = fs.readFileSync(htmlPath, 'utf8');
const code = html.match(/<script id="balance-engine">([\s\S]*?)<\/script>/)[1];
const ui = html.match(/<script id="balance-ui">([\s\S]*?)<\/script>/)[1];
new vm.Script(code); new vm.Script(ui);
const ctx = { module: { exports: {} } };
vm.runInNewContext(code, ctx);
const B = ctx.module.exports;
let count = 0;
const near = (a,b) => assert.ok(Math.abs(a-b)<1e-7, `${a} != ${b}`);
function test(name, fn) { if(process.env.BALANCE_TEST_FILTER&&!new RegExp(process.env.BALANCE_TEST_FILTER,'i').test(name))return; fn(); count++; console.log('PASS', name); }
// A stage loadout with no armor or accessories; gear() only sees what the test puts in it.
function emptyLoadout(armor='none'){return {stage:1,name:'测试',armor,slots:7,items:Array.from({length:7},()=>({id:'empty',prefix:'none',vPrefix:'none'})),baseMana:20,maxLife:100};}
const gearOf=(l,side='e',nebula)=>B.gear(B.loadoutProbe(l,side,nebula),side);
const configPath=path.join(__dirname,'../../ElainaModSkills/Data/SkillBalance.json');
const savedConfig=()=>JSON.parse(fs.readFileSync(configPath,'utf8').replace(/^\uFEFF/,'')).configuration;

test('Page keeps only skill, growth and loadout configuration',()=>{
 assert.deepEqual(Object.keys(B.defaults()).sort(),['basicAttacks','loadoutRevision','loadouts','occupyRatio','progression','rate','selectedBasicAttackId','skills','version']);
 assert.equal(/<script id="combat-engine">/.test(html),false,'Combat simulation engine is removed');
 for(const id of ['labPage','preset','baseDps','vArmor','eArmor','combatPanel','manaTimeline','ladderRows','basicCurve','rotationResults','followDps','sourcePath'])assert.equal(html.includes(`id="${id}"`),false,id);
 for(const name of ['calculate','skillDamageBudget','basicBudget','applyLoadout','applyStageUnlocks','normalizeBaseDps','dpsLevel'])assert.equal(B[name],undefined,name);
 for(const row of [B.newSkill('X','X'),...B.defaults().skills])for(const k of ['enabled','delay','duration','first','priority','hit'])assert.equal(Object.hasOwn(row,k),false,k);
});
test('Published config only stores game parameters and matches the page format',()=>{
 const c=savedConfig();
 assert.deepEqual(Object.keys(c).sort(),Object.keys(B.defaults()).sort());
 for(const row of c.skills)for(const k of ['enabled','delay','duration','first','priority','hit'])assert.equal(Object.hasOwn(row,k),false,row.id+'.'+k);
 assert.ok(B.validBasics(c));assert.ok(B.validLoadouts(c.loadouts));
});
test('MagicMissileSkill follows its level curve down to 60% at Lv50 and is not an active skill',()=>{
 const s=B.defaults(),r=s.basicAttacks[0];near(r.ratio,.6);
 assert.deepEqual(Array.from(r.ratioCurve,p=>Array.from(p)),[[1,1],[10,.9],[20,.8],[35,.7],[50,.6]]);
 near(B.progressionBudget(s,r,30).ratio,.8-.1/15*10);near(B.progressionBudget(s,r,50).ratio,.6);
 assert.equal(s.skills.some(x=>x.id==='MagicMissileSkill'),false);
});
test('Ratio curve interpolates linearly and clamps at both ends',()=>{const r=B.defaults().basicAttacks[0];near(B.basicRatio(r,0),1);near(B.basicRatio(r,1),1);near(B.basicRatio(r,5.5),.95);near(B.basicRatio(r,30),.8-.1/15*10);near(B.basicRatio(r,50),.6);near(B.basicRatio(r,95),.6);r.ratioCurve=[];near(B.basicRatio(r,1),.6);delete r.ratioCurve;near(B.basicRatio(r,1),.6);});
test('Curve text parses loosely and round-trips; invalid curves are rejected',()=>{assert.deepEqual(Array.from(B.parseCurve('1:1, 10：0.9；20 : .8'),p=>Array.from(p)),[[1,1],[10,.9],[20,.8]]);assert.deepEqual(Array.from(B.parseCurve('')),[]);const c=[[1,1],[50,.6]];assert.deepEqual(Array.from(B.parseCurve(B.formatCurve(c)),p=>Array.from(p)),c);for(const t of ['10:1, 5:.9','1:1, 1:.9','0:1','1001:1','1:6','1.5:1','abc','1:-1'])assert.equal(B.parseCurve(t),null,t);assert.equal(B.validCurve(Array.from({length:17},(_,i)=>[i+1,1])),false);const s=B.defaults();s.basicAttacks[0].ratioCurve=[[10,1],[5,1]];assert.equal(B.validBasics(s),false);s.basicAttacks[0].ratioCurve=[[1,1]];assert.ok(B.validBasics(s));});
test('Basic attack damage is B × ratio × interval, split across shots and independent of mana cost',()=>{
 const s=B.defaults(),ice={...B.newBasic('IceConeBasic','冰锥'),ratio:.8,cost:10,interval:.5,shots:3};s.basicAttacks.push(ice);
 let r=B.progressionBudget(s,ice,10);near(r.baseline,50);near(r.total,20);near(r.perShot,20/3);near(r.dps,40);assert.equal(r.integerHit,6);
 ice.shots=6;r=B.progressionBudget(s,ice,10);near(r.total,20);near(r.perShot,20/6);
 ice.cost=30;near(B.progressionBudget(s,ice,10).total,20);
});
test('Active skill damage has no occupancy compensation by default; occupyRatio prices cast time in B',()=>{
 const s=B.defaults(),x=s.skills[0];Object.assign(x,{cost:100,k:1,cd:10,cdRatio:1,cast:1});near(s.occupyRatio,0);
 let r=B.progressionBudget(s,x,10);near(r.total,100);near(r.dps,10);assert.equal(r.ratio,null);
 s.occupyRatio=.5;near(B.progressionBudget(s,x,10).total,125);
 x.cast=0;near(B.progressionBudget(s,x,10).total,100+50*.5/60);
});
test('Cooldown damage ratio reduces only the cooldown-priced skill damage',()=>{
 const s=B.defaults(),x=s.skills[0];Object.assign(x,{cost:100,k:1,cd:10,cdRatio:1,cast:1});s.occupyRatio=1;
 near(B.progressionBudget(s,x,10).total,150);x.cdRatio=.5;near(B.progressionBudget(s,x,10).total,100);
});
test('Explicit DPS ratio prices the full cooldown plus cast cycle',()=>{
 const s=B.defaults(),x={...s.skills[0],ratio:.2,cd:10,cast:1,k:99};const r=B.progressionBudget(s,x,10);
 near(r.total,50*.2*11);near(r.dps,10);near(r.ratio,.2);
});
test('Active skill curves replace their own coefficient: DPS ratio for ratio skills, K otherwise',()=>{
 const s=B.defaults(),k={...s.skills[0],cost:100,cd:10,cdRatio:1,cast:1,k:1,ratioCurve:[[1,2],[11,4]]};s.skills[0]=k;
 near(B.progressionBudget(s,k,1).k,2);near(B.progressionBudget(s,k,6).k,3);near(B.progressionBudget(s,k,50).k,4);
 near(B.progressionBudget(s,k,6).total,B.levelDps(6)*(.04/20*100*10*3));near(B.progressionBudget(s,k,6).coef,3);
 const r={...s.skills[1],ratio:.2,cd:10,cast:1,ratioCurve:[[1,.1],[21,.3]]};s.skills[1]=r;
 near(B.progressionBudget(s,r,11).ratio,.2);near(B.progressionBudget(s,r,11).total,B.levelDps(11)*.2*11);assert.equal(B.progressionBudget(s,r,11).k,null);
 assert.equal(B.curveLimit(k),200);assert.equal(B.curveLimit(r),5);assert.equal(B.curveLimit(s.basicAttacks[0]),5);
 assert.ok(B.validCurve([[1,150]],200));assert.equal(B.validCurve([[1,150]]),false);assert.equal(B.validCurve([[1,201]],200),false);
 assert.deepEqual(Array.from(B.parseCurve('1:150',200),p=>Array.from(p)),[[1,150]]);assert.equal(B.parseCurve('1:150'),null);
 for(const row of [B.newSkill('X','X'),...B.defaults().skills])assert.deepEqual(Array.from(row.ratioCurve),[]);
});
test('Fractional mana costs are valid for basic attacks and active skills',()=>{
 const s=B.defaults();s.basicAttacks[0].cost=2.5;s.skills[0].cost=12.5;assert.ok(B.validBasics(s));
 near(B.progressionBudget(s,s.skills[0],10).total*2,B.progressionBudget(s,{...s.skills[0],cost:25},10).total);
});
test('Armor aggregation',()=>{
 for(const [armor,damage,crit,mana] of [['jungle',6,16,80],['adamantite',20,23,80],['hallowed',19,23,100],['spectre',25,21,60]]){const g=gearOf(emptyLoadout(armor));near(g.damage,damage);near(g.crit,crit);near(g.mana,mana);}
});
test('Single Menacing adds 4 percentage points, not a separate multiplier',()=>{const l=emptyLoadout('hallowed');l.items[0]={id:'sorcerer',prefix:'menacing',vPrefix:'none'};near(gearOf(l).damage,38);});
test('Empty and keep slots cannot invent prefix benefits',()=>{
 const l=emptyLoadout();l.items[0]={id:'empty',prefix:'arcane',vPrefix:'none'};l.items[1]={id:'utility',prefix:'menacing',vPrefix:'none'};
 const g=gearOf(l);near(g.mana,0);near(g.damage,0);assert.equal(B.equippedSlots(B.loadoutProbe(l),'e').length,0);
});
test('Slots beyond the chosen cap are inactive',()=>{const l=emptyLoadout();l.slots=5;l.items[6]={id:'sorcerer',prefix:'arcane',vPrefix:'none'};near(gearOf(l).damage,0);near(gearOf(l).mana,0);});
test('Crit clamps between 0 and 100%',()=>{const p=B.loadoutProbe(emptyLoadout());p.e.crit=100;near(B.gear(p,'e').crit,100);near(B.gear(p,'e').multiplier,2/1.04);p.e.crit=-100;near(B.gear(p,'e').crit,0);});
test('Elaina hat adds damage, crit and special mana only on the Elaina side',()=>{
 const l=emptyLoadout();l.items[0]={id:'elainaHat',prefix:'arcane',vPrefix:'menacing'};
 const e=gearOf(l),v=gearOf(l,'v');near(e.damage,10);near(e.crit,9);near(e.mana,20);near(e.magic,20);
 near(v.damage,0);near(v.crit,4);near(v.magic,0);assert.equal(B.sideItem('elainaHat','v'),'empty');assert.equal(B.sideItem('elainaHat','e'),'elainaHat');
});
test('Ashen Witch converts the halved extra life into special mana',()=>{for(const [life,mana] of [[100,0],[160,30],[305,103],[400,150],[500,200],[50,0]])near(B.ashenWitchMana(life),mana);});
test('Nebula armor counts static stats by default and adds buff stacks only when asked',()=>{
 const l=emptyLoadout('nebula');let g=gearOf(l);near(g.damage,26);near(g.crit,20);near(g.mana,60);near(g.armorDefense,46);near(g.manaReduction,15);near(g.moveSpeed,10);
 g=gearOf(l,'e',{damage:3,mana:3,life:3});near(g.damage,71);near(g.nebulaDamage,45);near(g.nebulaManaRegen,30);near(g.nebulaLifeRegen,9);
});
test('Damage prefix picks menacing or lucky from current damage and capped crit',()=>{
 const s=B.loadoutProbe(emptyLoadout());s.e.items[0]={id:'regenLife',prefix:'none'};
 s.e.damage=50;assert.equal(B.damagePrefixStep(s,'e').prefix,'lucky');near(B.damagePrefixStep(s,'e').ratio,1.08/1.04);
 s.e.damage=0;assert.equal(B.damagePrefixStep(s,'e').prefix,'menacing');near(B.damagePrefixStep(s,'e').ratio,1.04);
 s.e.damage=0;s.e.crit=0;s.baseCrit=0;assert.equal(B.damagePrefixStep(s,'e').prefix,'menacing','A tie keeps menacing');
 s.e.damage=200;s.e.crit=96;s.baseCrit=4;assert.equal(B.damagePrefixStep(s,'e').prefix,'menacing','Crit capped at 100 gains nothing from lucky');
 s.e.damage=300;s.e.crit=94;assert.equal(B.damagePrefixStep(s,'e').prefix,'lucky','Lucky is clipped at the crit cap');near(B.damagePrefixStep(s,'e').ratio,2/1.98);
});
test('All-damage assignment matches the best menacing/lucky mix and ignores empty or hidden slots',()=>{
 for(const [armor,crit,items] of [['hallowed',0,['sorcerer','avenger','celestial','regenLife','regenLife','regenLife']],['spectre',0,['regenLife','regenLife','regenLife','regenLife','regenLife','regenLife']],['meteor',0,['sorcerer','avenger','regenLife','regenLife','regenLife','regenLife']],['none',80,['regenLife','regenLife','regenLife','regenLife','regenLife','regenLife']]]){
  const l=emptyLoadout(armor);l.slots=6;items.forEach((id,i)=>{l.items[i]={id,prefix:'arcane',vPrefix:'none'};});l.items[2].id='empty';l.items[6]={id:'regenLife',prefix:'warding',vPrefix:'none'};
  const s=B.loadoutProbe(l);s.e.crit=crit;
  const picks=B.applyDamagePrefixes(s,'e'),slots=B.equippedSlots(s,'e');
  assert.equal(picks.length,5);assert.equal(s.e.items[2].prefix,'arcane');assert.equal(s.e.items[6].prefix,'warding');
  let best=0;for(let mask=0;mask<1<<slots.length;mask++){const c=B.clone(s);slots.forEach((i,n)=>{c.e.items[i].prefix=mask>>n&1?'lucky':'menacing';});best=Math.max(best,B.gear(c,'e').multiplier);}
  near(B.gear(s,'e').multiplier,best);
 }
 const l=emptyLoadout();l.items.forEach(x=>{x.id='regenLife';});const mixed=B.loadoutProbe(l);mixed.e.crit=10;const picks=B.applyDamagePrefixes(mixed,'e');
 assert.ok(picks.includes('menacing')&&picks.includes('lucky'),'Balanced damage and crit mix both prefixes');
});
test('All recommended slots use mapped accessories and Spectre stays a full damage set',()=>{
 const mapping=fs.readFileSync(path.join(__dirname,'../../ElainaModSkills/Loadout/LoadoutItems.cs'),'utf8');
 const saved=savedConfig();
 assert.equal(JSON.stringify(saved.loadouts),JSON.stringify(B.loadoutDefaults()),'Published game loadouts must match browser defaults');
 for(const l of saved.loadouts){
  for(const x of l.items){assert.notEqual(x.id,'utility');if(x.id!=='empty'&&x.id!=='elainaHat')assert.ok(mapping.includes('["'+x.id+'"] = ItemID.'),x.id);}
  if(l.stage>=3)assert.ok(l.items.slice(0,l.slots).every(x=>x.id!=='empty'));
  if(l.stage>=13&&l.stage<=17)assert.equal(l.armor,'spectre');
 }
 assert.ok(mapping.includes('["spectre"] = [ItemID.SpectreMask, ItemID.SpectreRobe, ItemID.SpectrePants]'));
});
test('Defaults use available mage equipment with no artificial ore-tier delay',()=>{
 const l=B.loadoutDefaults();
 assert.equal(l[0].armor,'amethystWood');
 for(let i=1;i<7;i++)assert.equal(l[i].armor,'jungle');
 for(let i=7;i<9;i++)assert.equal(l[i].armor,'titanium');
 for(let i=9;i<12;i++)assert.equal(l[i].armor,'hallowed');
 const first={sorcerer:8,avenger:12,celestial:12,destroyer:14,eyeGolem:14,celestialStone:14,celestialShell:14,wormScarf:4,brain:4,obsidianShield:6,ankhShield:8,frozenShield:13,charmMyths:8,regen:4};
 for(const row of l)for(const x of row.items)assert.ok(row.stage>=(first[x.id]??1),`${row.stage}: ${x.id} is not yet available`);
 for(let i=12;i<18;i++)assert.ok(l[i].items.some(x=>x.id==='frozenShield'));
 const map=fs.readFileSync(path.join(__dirname,'../../ElainaModSkills/Loadout/LoadoutItems.cs'),'utf8');
 assert.ok(!map.includes('ItemID.AdamantiteMask'));
});
test('Revision 1 migrates only untouched default loadouts',()=>{
 const oldCtx={module:{exports:{}}};vm.runInNewContext(code.replace('return {levelDps,','return {old:Object.values(legacyPresetsV1).map(p=>loadoutDefault(p.stage,p)),levelDps,'),oldCtx);
 const s=B.defaults();s.loadoutRevision=1;s.loadouts=JSON.parse(JSON.stringify(oldCtx.module.exports.old));
 const custom=s.loadouts[2];custom.maxLife=220;custom.items[1].prefix='warding';
 B.migrateLoadouts(s);
 assert.equal(s.loadoutRevision,2);assert.equal(s.loadouts[9].armor,'hallowed');
 assert.equal(s.loadouts[2],custom);assert.equal(custom.maxLife,220);assert.equal(custom.items[1].prefix,'warding');
 const once=JSON.stringify(s);B.migrateLoadouts(s);assert.equal(JSON.stringify(s),once);
});
test('Legacy migration fills placeholders once, preserves custom gear and replaces Spectre mixes',()=>{
 const s=B.defaults();delete s.loadoutRevision;const l=s.loadouts[15];l.armor='spectreDarkArtist';l.baseMana=160;l.maxLife=420;
 l.items[0]={id:'sorcerer',prefix:'warding',vPrefix:'lucky'};l.items[1]={id:'utility',prefix:'arcane',vPrefix:'menacing'};
 B.migrateLoadouts(s);assert.equal(l.armor,'spectre');assert.equal(l.baseMana,160);assert.equal(l.maxLife,420);
 assert.equal(l.items[0].id,'sorcerer');assert.equal(l.items[0].prefix,'warding');assert.equal(l.items[1].id,'elainaHat');
 assert.equal(l.items[1].prefix,'arcane');assert.equal(l.items[1].vPrefix,'none');
 const active=l.items.slice(0,l.slots).map(x=>x.id);assert.equal(new Set(active).size,active.length);
 l.items[1].id='utility';B.migrateLoadouts(s);assert.equal(l.items[1].id,'utility','Explicit new keep-slot choices survive reload');
});
test('Legacy 19-stage layouts drop the Betsy stage and shift later unlocks',()=>{
 const old=B.defaults(),extra=B.clone(old.loadouts[15]);extra.name='旧拜月前';
 old.loadouts.splice(16,0,extra);old.loadouts.forEach((l,i)=>l.stage=i+1);old.stage=19;old.progression.previewLevel=95;
 Object.assign(old.skills[0],{unlockStageCap:95,unlockLevel:91});Object.assign(old.skills[1],{unlockStageCap:85,unlockLevel:81});Object.assign(old.skills[2],{unlockStageCap:80,unlockLevel:76});
 B.migrateStageLayout(old);B.migrateStageLayout(old);
 assert.ok(B.validLoadouts(old.loadouts));assert.ok(old.loadouts.every(l=>l.name!=='旧拜月前'));assert.equal(old.stage,18);near(old.progression.previewLevel,90);
 assert.equal(JSON.stringify(old.skills.slice(0,3).map(r=>[r.unlockStageCap,r.unlockLevel])),'[[90,86],[80,76],[80,76]]');
});
test('Default loadouts cover all 18 stages, mirror the presets and validate',()=>{
 const list=B.loadoutDefaults();assert.equal(list.length,18);assert.ok(B.validLoadouts(list));
 for(const p of Object.values(B.presets)){const l=list[p.stage-1],ids=Array.from({length:7},(_,i)=>p.items[i]||'empty');assert.equal(l.stage,p.stage);assert.equal(l.armor,p.armor);assert.equal(l.slots,p.slots);assert.deepEqual(Array.from(l.items,x=>x.id),ids,'Defaults list explicit accessories including the hat');}
 near(list[0].baseMana,100);near(list[1].baseMana,200);near(list[2].maxLife,200);assert.ok(list.every((l,i)=>i===0||l.maxLife>=list[i-1].maxLife));near(list[17].maxLife,500);
});
test('King Slime stage has one reforge before the Goblin Tinkerer',()=>{
 const l=B.loadoutDefault(2);
 assert.deepEqual(Array.from(l.items.slice(0,5),x=>x.id),['elainaHat','band','regenLife','shackle','empty']);
 assert.deepEqual(Array.from(l.items.slice(0,5),x=>x.prefix),['arcane','none','none','none','none']);
 assert.deepEqual(Array.from(l.items.slice(0,5),x=>x.vPrefix),['none','none','none','none','none']);
});
test('Mana and life rounding follow crystals and life fruit',()=>{
 for(const [a,b] of [[10,20],[35,40],[200,200],[999,200]])near(B.roundMana(a),b);
 for(const [a,b] of [[0,100],[150,160],[399,400],[403,405],[412,410],[999,500]])near(B.roundLife(a),b);
});
test('Invalid loadouts are rejected',()=>{
 const bad=[l=>l.pop(),l=>{l[3].stage=5;},l=>{l[0].baseMana=30;},l=>{l[0].maxLife=390;},l=>{l[0].maxLife=520;},l=>{l[0].armor='constructor';},l=>{l[0].items[0].id='toString';},l=>{l[0].items[0].prefix='godly';},l=>{l[0].items[0].vPrefix='godly';},l=>{delete l[0].items[0].vPrefix;},l=>{l[0].items.pop();},l=>{l[0].slots=8;},l=>{l[0].name=' ';},l=>{l[0].name='x'.repeat(65);}];
 for(const f of bad){const l=B.loadoutDefaults();f(l);assert.equal(B.validLoadouts(l),false,f.toString());}
 const l=B.loadoutDefaults();l[0].maxLife=405;l[0].baseMana=20;assert.ok(B.validLoadouts(l));
});
test('Presets cover every vanilla period in order with KL stage DPS and valid gear',()=>{
 const list=Object.values(B.presets);assert.equal(JSON.stringify(list.map(p=>p.stage)),JSON.stringify(B.periods.map(p=>p.stage)));
 for(const p of list){assert.ok(Object.hasOwn(B.armors,p.armor),p.name);assert.ok(p.items.length===7&&p.items.every(id=>Object.hasOwn(B.accessories,id)),p.name);const real=p.items.filter(id=>!['utility','empty'].includes(id));assert.equal(new Set(real).size,real.length,p.name);}
 near(B.presets.start.base,30);near(B.presets.kingSlime.base,50);near(B.presets.jungle.base,200);near(B.presets.adamantite.base,450);near(B.presets.hallowed.base,900);near(B.presets.spectre.base,1800);near(B.presets.nebula.base,3500);
});
test('Default prefixes ramp up with the Goblin Tinkerer; both prefix keys reforge the same slots',()=>{
 const counts=Array.from(B.loadoutDefaults(),l=>{
  const active=l.items.slice(0,l.slots).filter(x=>x.id!=='empty'),good=active.filter(x=>x.prefix==='arcane');
  for(const x of l.items){assert.ok(x.prefix==='arcane'||x.prefix==='none',l.name);assert.equal(x.vPrefix==='none',x.id==='elainaHat'||x.prefix==='none',l.name);assert.ok(['none','menacing','lucky'].includes(x.vPrefix),l.name);}
  assert.ok(l.items.every(x=>x.id!=='empty'||x.prefix==='none'),'Empty slots never carry a prefix');
  return [good.length,active.length];
 });
 assert.deepEqual(counts.slice(0,6),[[0,3],[1,4],[2,5],[3,5],[4,5],[5,5]],'Start has no damage prefix, then one more reforge per stage');
 for(const [good,active] of counts.slice(5))assert.equal(good,active,'From Skeletron on every equipped slot is reforged');
 assert.equal(B.loadoutDefault(8).slots,6,'Expert Demon Heart arrives with the Wall of Flesh');
 const probe=B.loadoutProbe(B.loadoutDefault(18),'v',{damage:3,mana:3,life:3}),c=B.clone(probe);B.applyDamagePrefixes(c,'v');
 assert.deepEqual(Array.from(probe.v.items,x=>x.prefix),Array.from(c.v.items,x=>x.prefix),'vPrefix defaults are the best menacing/lucky mix for that gear');
});
test('Unlock period maps to stage cap 5k and first level 5k-4; custom values are period 0',()=>{
 const row=B.newSkill('X','X');assert.ok(B.setPeriod(row,2));assert.equal(row.unlockStageCap,10);assert.equal(row.unlockLevel,6);assert.equal(B.periodOf(row),2);
 row.unlockStageCap=12;assert.equal(B.periodOf(row),0);assert.equal(B.setPeriod(row,19),false);assert.equal(row.unlockStageCap,12);
});
test('Unlock check needs both the stage cap and the unlock level',()=>{
 const row=B.newSkill('X','X');B.setPeriod(row,8);
 assert.equal(B.unlockedAt(row,35),false);assert.equal(B.unlockedAt(row,39),false);assert.equal(B.unlockedAt(row,40),true);
 row.unlockStageCap=10;row.unlockLevel=12;assert.equal(B.unlockedAt(row,10),false);assert.equal(B.unlockedAt(row,12),true);
});
test('Missile progression has two upgrades per stage excluding learned level',()=>{const r=B.newBasic();assert.deepEqual(Array.from(B.progressionNodes(r,5,15)),[1,3,5,8,10,13,15]);near(B.skillLevelAt(r,5),3);near(B.skillLevelAt(r,10),5);});
test('Ice phase cap and first level are distinct',()=>{const r={...B.newBasic(),unlockStageCap:10,unlockLevel:6};assert.deepEqual(Array.from(B.progressionNodes(r,5,15)),[6,8,10,13,15]);near(B.skillLevelAt(r,5),0);near(B.skillLevelAt(r,8),2);r.unlockLevel=10;assert.deepEqual(Array.from(B.progressionNodes(r,5,15)),[10,13,15]);});
test('Progression does not duplicate integer gates or truncate at 64 skills',()=>{const r={...B.newBasic(),levelsPerPhase:10};const nodes=B.progressionNodes(r,1,1000);assert.equal(new Set(nodes).size,nodes.length);near(B.skillLevelAt(r,1000,1),1000);});
test('Level curve and rounded damage expose weak early upgrades',()=>{const s=B.defaults(),r=s.basicAttacks[0];near(B.levelDps(1),22);near(B.levelDps(3),26);near(B.levelDps(5),30);near(B.levelDps(10),50);near(B.levelDps(1000),3500);assert.deepEqual([1,3,5].map(l=>B.progressionBudget(s,r,l).integerHit),[6,7,8]);near(B.progressionBudget(s,r,1).ratio,1);near(B.progressionBudget(s,r,50).ratio,.6);assert.equal(B.progressionBudget(s,s.skills[0],1).ratio,null);});
test('Fixed intervals map every ten or seven character levels from the unlock level',()=>{
 for(const interval of [7,10]){
  const row={...B.newBasic(),growthMode:'interval',levelInterval:interval};
  assert.deepEqual(Array.from(B.progressionNodes(row,5,35)),interval===10?[1,11,21,31]:[1,8,15,22,29]);
  near(B.skillLevelAt(row,interval),1);near(B.skillLevelAt(row,interval+1),2);
 }
});
test('Fixed progression is per skill, ignores phase settings, and respects delayed unlock and limits',()=>{
 const row={...B.newBasic(),growthMode:'interval',levelInterval:7,unlockLevel:6,unlockStageCap:42,levelsPerPhase:10};
 assert.deepEqual(Array.from(B.progressionNodes(row,5,30)),[6,13,20,27]);
 assert.deepEqual(Array.from(B.progressionNodes(row,20,30)),[6,13,20,27]);
 assert.equal(B.progressionNodes(row,5,5).length,0);
 near(B.skillLevelAt(row,12),1);near(B.skillLevelAt(row,13),2);
 row.unlockLevel=1000;assert.deepEqual(Array.from(B.progressionNodes(row)),[1000]);
 const s=B.defaults();s.basicAttacks[0].growthMode='interval';s.basicAttacks[0].levelInterval=10;
 assert.deepEqual(Array.from(B.progressionNodes(s.skills[0],5,15)),[1,5,10,15]);
 near(B.progressionBudget(s,s.basicAttacks[0],11).baseline,B.levelDps(11));
});
test('New growth fields validate strictly while old v2 entries retain phase progression',()=>{
 const old=B.newBasic();delete old.growthMode;delete old.levelInterval;
 assert.equal(B.validGrowth(old),true);assert.deepEqual(Array.from(B.progressionNodes(old,5,10)),[1,3,5,8,10]);
 const interval={...old,growthMode:'interval'};assert.deepEqual(Array.from(B.progressionNodes(interval,5,11)),[1,6,11]);
 for(const growthMode of ['bad','',null,1,{}])assert.equal(B.validGrowth({...old,growthMode}),false);
 for(const levelInterval of [0,-1,1.5,1001,'7',null,true,Infinity])assert.equal(B.validGrowth({...old,levelInterval}),false);
 for(const levelInterval of [1,7,10,1000])assert.equal(B.validGrowth({...old,levelInterval}),true);
});
test('Fixed level caps stop both growth modes; zero and legacy omission preserve growth',()=>{
 for(const growthMode of ['phase','interval'])for(const maxLevel of [1,8,1000]){
  const row={...B.newBasic(),growthMode,maxLevel,unlockLevel:6,unlockStageCap:10,levelsPerPhase:1,levelInterval:5};
  const unlimited={...row,maxLevel:0},nodes=Array.from(B.progressionNodes(unlimited));
  assert.deepEqual(Array.from(B.progressionNodes(row)),nodes.slice(0,maxLevel));
  assert.equal(B.skillLevelAt(row,5),0);
  assert.equal(B.skillLevelAt(row,1000),Math.min(nodes.length,maxLevel));
  delete unlimited.maxLevel;assert.deepEqual(Array.from(B.progressionNodes(unlimited)),nodes);
 }
 for(const maxLevel of [-1,1.5,1001,'8',null,true,Infinity])assert.equal(B.validGrowth({...B.newBasic(),maxLevel}),false);
 for(const maxLevel of [0,1,8,1000])assert.equal(B.validGrowth({...B.newBasic(),maxLevel}),true);
 const alchemy=savedConfig().skills.find(x=>x.id==='AlchemySkill');
 assert.equal(alchemy.maxLevel,8);
 assert.deepEqual(Array.from(B.progressionNodes(alchemy)),[6,10,15,20,25,30,35,40]);
 for(const level of [40,45,95,1000])assert.equal(B.skillLevelAt(alchemy,level),8);
});
test('Progression pricing does not mutate configuration',()=>{const s=B.defaults(),before=JSON.stringify(s);for(const row of [...s.basicAttacks,...s.skills])B.progressionBudget(s,row,37);B.gear(B.loadoutProbe(s.loadouts[12]),'e');assert.equal(JSON.stringify(s),before);});
function iceDesign() {const s=B.defaults();const ice={...B.newBasic('IceConeBasic','冰锥'),ratio:.8,cost:10,interval:.5,shots:3};s.basicAttacks.push(ice);s.selectedBasicAttackId=ice.id;return s;}
test('Basic IDs are unique across all skill categories',()=>{const s=iceDesign();assert.ok(B.validBasics(s));s.skills[0].id='IceConeBasic';assert.equal(B.validBasics(s),false);});
test('Invalid basic counts, bounds and selection are rejected',()=>{for(const value of [0,1.5,101,NaN]){const s=iceDesign();s.basicAttacks[1].shots=value;assert.equal(B.validBasics(s),false);}const s=iceDesign();s.selectedBasicAttackId='missing';assert.equal(B.validBasics(s),false);});
test('Static IDs are unique',()=>{const ids=[...html.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);assert.equal(ids.length,new Set(ids).size);});
test('Standalone has no remote script or stylesheet dependency; fixed saving uses a relative loopback endpoint',()=>{assert.ok(!/<script[^>]+src=|<link[^>]+href=/.test(html));assert.ok(html.includes("fetch('/api/skill-balance'"));});
console.log(`\n${count} model/static tests passed.`);

// Same rule as the page's stage damage table: [skill level, value] cell texts, or dashes when locked.
function expectedStageCell(cfg,row,stage,metric='hit',multiplier=1){
 const p=B.periods[stage-1];if(!B.unlockedAt(row,p.cap))return ['—','—'];
 const nodes=B.progressionNodes(row,cfg.progression.phaseSize,p.cap);if(!nodes.length)return ['—','—'];
 const r=B.progressionBudget(cfg,row,nodes.at(-1));
 const value=metric==='total'?r.total*multiplier:metric==='dps'?r.dps*multiplier:Math.max(1,Math.floor(r.perShot*multiplier));
 return ['Lv'+nodes.length,value.toLocaleString('zh-CN',{maximumFractionDigits:metric==='hit'?0:1})];
}
async function browserTests(){
 // Isolated disposable browser for this local HTML; no user browser profile or session is used.
 const runtime = process.env.CODEX_NODE_MODULES || path.join(require('node:os').homedir(),'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
 const {chromium}=require(path.join(runtime,'playwright'));
 const browser=await chromium.launch({headless:true, channel:process.env.BALANCE_BROWSER_CHANNEL || 'chrome'});
 try {
  const out=path.join(__dirname,'test-output');fs.mkdirSync(out,{recursive:true});
  const page=await browser.newPage({viewport:{width:1440,height:1100},deviceScaleFactor:1});
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  const stored=()=>page.evaluate(()=>JSON.parse(localStorage.getItem('elaina-mage-balance-v2')));
  const noOverflow=async label=>assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>window.innerWidth),false,label);
  const resetPage=async(accept=true)=>{
   await page.locator('#moreActions summary').click();
   page.once('dialog',dialog=>accept?dialog.accept():dialog.dismiss());
   await page.locator('#reset').click();
   assert.equal(await page.locator('#moreActions').evaluate(el=>el.open),false);
  };
  await page.goto(pathToFileURL(htmlPath).href);

  // Compact toolbar.
  assert.equal(await page.locator('#import,#importFile,#export').count(),0,'Obsolete import/export UI is removed');
  assert.equal(await page.locator('.toolbar .actions > button:visible').count(),2,'Only save and reload are primary buttons');
  assert.equal(await page.locator('#reset').isVisible(),false);
  await page.locator('#moreActions summary').focus();
  await page.keyboard.press('Enter');
  assert.equal(await page.locator('#reset').isVisible(),true);
  await page.keyboard.press('Escape');
  assert.equal(await page.locator('#reset').isVisible(),false);
  assert.equal(await page.locator('#moreActions summary').evaluate(el=>el===document.activeElement),true);
  await page.locator('#moreActions summary').click();
  await page.locator('h1').click();
  assert.equal(await page.locator('#reset').isVisible(),false);
  await page.locator('#rate').fill('5');
  await resetPage(false);
  assert.equal(await page.locator('#rate').inputValue(),'5','Cancel reset preserves edits');
  await resetPage();
  assert.equal(await page.locator('#rate').inputValue(),'4');
  await page.screenshot({path:path.join(out,'toolbar-desktop.png'),clip:{x:0,y:0,width:1440,height:260}});

  // Only configuration remains: three tabs, no lab, comparison, combat or ladder UI.
  assert.deepEqual(await page.locator('[role="tab"]').allInnerTexts(),['技能配置','成长与阶段伤害','配装与属性配置']);
  assert.equal(await page.locator('#skillsPage').isVisible(),true);
  for(const selector of ['#labPage','#preset','#baseDps','#vArmor','#combatPanel','#manaTimeline','#ladderRows','#basicCurve','#rotationResults','#simActual','#followDps'])assert.equal(await page.locator(selector).count(),0,selector);
  assert.equal(await page.locator('#rotationRows input[type=checkbox]').count(),0,'No simulation enable toggles');
  const skillHeaders=(await page.locator('.rotationtable thead th').allInnerTexts()).join('|');
  for(const removed of ['优先级','命中 %','首次释放','离手延迟','伤害持续','单次伤害预算'])assert.ok(!skillHeaders.includes(removed),removed);
  const basicHeaders=(await page.locator('#basicTable thead th').allInnerTexts()).join('|');
  for(const removed of ['参考普攻','每蓝价值','每蓝伤害','裸装 DPS'])assert.ok(!basicHeaders.includes(removed),removed);

  // Ordinary attacks: curve editing, decimals, freeze build-up, add/delete and ID validation.
  const curveBox=page.getByRole('textbox',{name:'魔法飞弹倍率曲线',exact:true});
  const missileRatio=page.getByRole('spinbutton',{name:'魔法飞弹DPS倍率',exact:true});
  assert.equal(await curveBox.inputValue(),'1:1, 10:0.9, 20:0.8, 35:0.7, 50:0.6');
  assert.equal(await missileRatio.isDisabled(),true);
  await curveBox.fill('100:1, 5:.5');await curveBox.press('Tab');
  assert.equal(await curveBox.inputValue(),'1:1, 10:0.9, 20:0.8, 35:0.7, 50:0.6','Invalid curve text restores the previous curve');
  assert.match(await page.locator('#status').innerText(),/曲线/);
  await curveBox.fill('');await curveBox.press('Tab');
  assert.equal(await missileRatio.isDisabled(),false);
  await curveBox.fill('1:1，50：0.75');await curveBox.press('Tab');
  assert.equal(await curveBox.inputValue(),'1:1, 50:0.75');
  await page.getByRole('spinbutton',{name:'魔法飞弹整次耗蓝',exact:true}).fill('2.5');
  await page.getByRole('spinbutton',{name:'魔法飞弹冻结积蓄',exact:true}).fill('5.6');
  let saved=await stored();
  assert.deepEqual(saved.basicAttacks[0].ratioCurve,[[1,1],[50,.75]]);near(saved.basicAttacks[0].cost,2.5);near(saved.basicAttacks[0].freezeBuildup,5.6);
  assert.equal(await page.getByRole('button',{name:'删除普攻魔法飞弹',exact:true}).isDisabled(),true,'At least one basic attack stays');
  await page.getByRole('button',{name:'添加普通攻击',exact:true}).click();
  assert.equal(await page.locator('#basicRows tr').count(),2);
  assert.equal(await page.getByRole('textbox',{name:'普攻名称 2',exact:true}).evaluate(el=>el===document.activeElement),true);
  const basicId=page.getByRole('textbox',{name:'普攻 ID 2',exact:true});
  await basicId.fill('WaterBallSkill');await basicId.press('Tab');
  assert.equal(await basicId.inputValue(),'CustomBasic1','Basic IDs cannot reuse active skill IDs');
  assert.match(await page.locator('#status').innerText(),/不能与普攻或主动技能重复/);
  await page.getByRole('textbox',{name:'普攻名称 2',exact:true}).fill('冰锥普攻');await page.getByRole('textbox',{name:'普攻名称 2',exact:true}).press('Tab');
  await page.getByRole('spinbutton',{name:'冰锥普攻每次发数',exact:true}).fill('3');
  assert.equal(await page.locator('#progressionRows tr').count(),B.defaults().skills.length+2,'Progression rows follow the skill tables');
  await page.locator('[aria-label="普通攻击"]').screenshot({path:path.join(out,'basic-config.png')});

  // Active skills: only game parameters, escaped names, charges clamp, persistence.
  await page.getByRole('button',{name:'添加主动技能',exact:true}).click();
  const added=page.locator('#rotationRows tr').last();
  await added.getByRole('textbox',{name:/技能名称/}).fill('<img src=x onerror=window.injected=1>');await added.getByRole('textbox',{name:/技能名称/}).press('Tab');
  await added.getByRole('textbox',{name:/技能 ID/}).fill('ExperimentalSkill');await added.getByRole('textbox',{name:/技能 ID/}).press('Tab');
  const addedName='<img src=x onerror=window.injected=1>';
  await page.getByRole('spinbutton',{name:addedName+'蓝耗',exact:true}).fill('12.5');
  await page.getByRole('spinbutton',{name:addedName+'充能上限',exact:true}).fill('3');
  await page.getByRole('spinbutton',{name:addedName+'初始充能',exact:true}).fill('5');
  saved=await stored();const addedRow=saved.skills.at(-1);
  assert.equal(addedRow.id,'ExperimentalSkill');near(addedRow.cost,12.5);near(addedRow.maxCharges,3);near(addedRow.initialCharges,3,'Initial charges never exceed the charge cap');
  for(const k of ['enabled','priority','hit','delay','duration','first'])assert.equal(Object.hasOwn(addedRow,k),false,k);
  assert.equal(await page.locator('#rotationRows img,#progressionRows img,#stageDamageSkill img,#stageDamageCompare img,#stageDamageChart img').count(),0,'User-provided names must be escaped');
  assert.equal(await page.evaluate(()=>window.injected),undefined);
  const skillId=page.getByRole('textbox',{name:'技能 ID 1',exact:true});
  await skillId.fill('MagicMissileSkill');await skillId.press('Tab');
  assert.equal(await skillId.inputValue(),'WaterBallSkill','Active IDs cannot reuse basic attack IDs');
  await page.reload();
  assert.equal(await page.locator('#rotationRows tr').count(),B.defaults().skills.length+1);
  assert.equal(await page.locator('#rotationRows tr').last().getByRole('textbox',{name:/技能 ID/}).inputValue(),'ExperimentalSkill');
  await page.getByRole('button',{name:'删除'+addedName,exact:true}).click();
  assert.equal(await page.locator('#rotationRows tr').count(),B.defaults().skills.length);
  await page.locator('#occupyRatio').fill('0.5');near((await stored()).occupyRatio,.5);
  const waterCurve=page.getByRole('textbox',{name:'水球魔法系数曲线',exact:true}),waterK=page.getByRole('spinbutton',{name:'水球魔法魔力转换率',exact:true});
  assert.equal(await waterCurve.inputValue(),'');assert.equal(await waterK.isDisabled(),false);
  await waterCurve.fill('1:250');await waterCurve.press('Tab');
  assert.equal(await waterCurve.inputValue(),'','K curves stop at 200');assert.match(await page.locator('#status').innerText(),/系数 0–200/);
  await waterCurve.fill('1:0.5，50：150');await waterCurve.press('Tab');
  assert.equal(await waterCurve.inputValue(),'1:0.5, 50:150');assert.equal(await page.getByRole('spinbutton',{name:'水球魔法魔力转换率',exact:true}).isDisabled(),true);
  assert.deepEqual((await stored()).skills[0].ratioCurve,[[1,.5],[50,150]]);
  await page.locator('#skillPanel').screenshot({path:path.join(out,'skill-config.png')});
  await page.setViewportSize({width:390,height:844});
  await noOverflow('Skill configuration must not overflow on mobile');
  for(const tab of await page.locator('[role="tab"]').all()){const box=await tab.boundingBox();assert.ok(box.x>=0&&box.x+box.width<=390,'All tabs fit the mobile viewport');}
  await page.screenshot({path:path.join(out,'skills-mobile.png'),fullPage:true});
  await page.setViewportSize({width:1440,height:1100});

  // Stage damage: one row per period, values follow the configured growth and B curve.
  await page.getByRole('tab',{name:'成长与阶段伤害',exact:true}).click();
  assert.equal(await page.locator('#progressionPage').isVisible(),true);
  assert.equal(await page.locator('#skillsPage').isVisible(),false);
  assert.equal(await page.evaluate(()=>location.hash),'#progression');
  assert.equal(await page.evaluate(()=>window.scrollY),0,'Opening a subpage starts at the top');
  // One selected skill: an 18-period table plus a curve with one point per unlocked period.
  const checkStageSkill=async(id,metric,withGear)=>{
   const cfg=await stored(),row=[...cfg.basicAttacks,...cfg.skills].find(r=>r.id===id);
   await page.locator('#stageDamageSkill').selectOption(id);await page.locator('#stageDamageCompare').selectOption('');
   const head=await page.locator('#stageDamageHead th').allTextContents(),offset=withGear?4:3;
   assert.equal(head.length,offset+2);assert.equal(head[offset+1],{hit:'每发伤害',total:'整次伤害',dps:'基础周期 DPS'}[metric]);
   assert.equal(await page.locator('#stageDamageRows tr').count(),18);
   let unlocked=0;
   for(let stage=1;stage<=18;stage++){
    const multiplier=withGear?1+B.gear(B.loadoutProbe(cfg.loadouts[stage-1]),'e').damage/100:1,expected=expectedStageCell(cfg,row,stage,metric,multiplier);
    if(expected[0]!=='—')unlocked++;
    if(![1,6,8,13,18].includes(stage))continue;
    const cells=await page.locator('#stageDamageRows tr').nth(stage-1).locator('td').allTextContents();
    near(Number(cells[2].replaceAll(',','')),Math.round(B.levelDps(5*stage)*10)/10);
    assert.deepEqual(cells.slice(offset),expected,`${row.name} @ stage ${stage}`);
   }
   assert.equal(await page.locator('#stageDamageChart circle').count(),unlocked,'One chart point per unlocked period');
   assert.equal(await page.locator('#stageDamageChart path').count(),unlocked?1:0);
   assert.ok((await page.locator('#stageDamageChart').textContent()).includes(row.name),'Chart is titled with the selected skill');
  };
  const checkStageTable=async(metric='hit',withGear=false)=>{
   const cfg=await stored(),rows=[...cfg.basicAttacks,...cfg.skills].filter(row=>B.progressionBudget(cfg,row,1).total>0);
   assert.deepEqual(await page.locator('#stageDamageSkill option').evaluateAll(xs=>xs.map(x=>x.value)),rows.map(r=>r.id),'Only damage skills are selectable');
   const selected=await page.locator('#stageDamageSkill').inputValue();
   for(const id of ['MagicMissileSkill','WaterBallSkill','CustomBasic1'])if(rows.some(r=>r.id===id))await checkStageSkill(id,metric,withGear);
   await page.locator('#stageDamageSkill').selectOption(selected);
  };
  await checkStageTable();
  await page.locator('#stageDamageMetric').selectOption('total');await checkStageTable('total');
  await page.locator('#stageDamageMetric').selectOption('dps');await checkStageTable('dps');
  await page.locator('#stageDamageGear').check();await checkStageTable('dps',true);
  await page.locator('#stageDamageMetric').selectOption('hit');await checkStageTable('hit',true);
  await page.locator('#stageDamageGear').uncheck();
  // Compare: a second series in the table and chart; the main skill is never offered as its own comparison.
  await page.locator('#stageDamageSkill').selectOption('WaterBallSkill');
  assert.equal(await page.locator('#stageDamageCompare').inputValue(),'');
  assert.ok(!(await page.locator('#stageDamageCompare option').evaluateAll(xs=>xs.map(x=>x.value))).includes('WaterBallSkill'));
  await page.locator('#stageDamageCompare').selectOption('MagicMissileSkill');
  {
   const cfg=await stored(),main=cfg.skills.find(r=>r.id==='WaterBallSkill'),other=cfg.basicAttacks.find(r=>r.id==='MagicMissileSkill');
   const head=await page.locator('#stageDamageHead th').allTextContents();
   assert.deepEqual(head.slice(3),['技能等级','每发伤害','对比：魔法飞弹','比值']);
   for(const stage of [1,9,18]){
    const cells=await page.locator('#stageDamageRows tr').nth(stage-1).locator('td').allTextContents(),a=expectedStageCell(cfg,main,stage),o=expectedStageCell(cfg,other,stage);
    assert.deepEqual(cells.slice(3,5),a);assert.equal(cells[5],o[1]+o[0]);
    const ratio=Number(a[1].replaceAll(',',''))/Number(o[1].replaceAll(',',''));
    assert.equal(cells[6],ratio.toLocaleString('zh-CN',{maximumFractionDigits:2})+'×');
   }
   assert.equal(await page.locator('#stageDamageChart path').count(),2,'Two curves when comparing');
   assert.equal(await page.locator('#stageDamageChart path[stroke-dasharray]').count(),1,'The comparison curve is dashed');
   const legend=await page.locator('#stageDamageChart').textContent();assert.ok(legend.includes('水球魔法')&&legend.includes('魔法飞弹'));
  }
  await page.locator('#stageDamageMetric').selectOption('dps');
  assert.match(await page.locator('#stageDamageRows tr').nth(17).locator('td').nth(6).textContent(),/×$/);
  await page.locator('#stageDamageMetric').selectOption('hit');
  await page.locator('#stageDamagePanel').screenshot({path:path.join(out,'stage-damage-compare.png')});
  await page.locator('#stageDamageSkill').selectOption('MagicMissileSkill');
  assert.equal(await page.locator('#stageDamageCompare').inputValue(),'','Picking the compared skill as main clears the comparison');
  assert.equal(await page.locator('#stageDamageChart path').count(),1);
  const box=async(sel)=>page.locator(sel).boundingBox();
  const tableBox=await box('#stageDamagePanel .tablewrap'),chartBox=await box('#stageDamageChart');
  assert.ok(chartBox.x>tableBox.x+tableBox.width-1&&chartBox.width>300&&chartBox.height>300,'Chart sits to the right of the table on desktop');
  await page.locator('#stageDamageSkill').selectOption('WaterBallSkill');
  await page.getByRole('combobox',{name:'水球魔法解锁时期',exact:true}).selectOption('8');
  assert.equal(await page.getByRole('spinbutton',{name:'水球魔法解锁阶段上限',exact:true}).inputValue(),'40');
  assert.equal(await page.getByRole('spinbutton',{name:'水球魔法解锁角色等级',exact:true}).inputValue(),'36');
  assert.equal(await page.locator('#stageDamageSkill').inputValue(),'WaterBallSkill','Editing keeps the selected skill');
  for(const stage of [1,7])assert.deepEqual(await page.locator('#stageDamageRows tr').nth(stage-1).locator('td').allTextContents().then(x=>x.slice(3)),['—','—'],'Locked periods show a dash');
  assert.match(await page.locator('#stageDamageRows tr').nth(7).locator('td').nth(3).textContent(),/^Lv\d+$/);
  assert.equal(await page.locator('#stageDamageChart circle').count(),11,'Locked periods are not plotted');
  await checkStageTable();
  await page.getByRole('spinbutton',{name:'水球魔法解锁阶段上限',exact:true}).fill('42');
  assert.equal(await page.getByRole('combobox',{name:'水球魔法解锁时期',exact:true}).inputValue(),'0');
  await page.getByRole('combobox',{name:'水球魔法解锁时期',exact:true}).selectOption('1');
  await page.locator('#stageDamagePanel').screenshot({path:path.join(out,'stage-damage.png')});

  // Growth configuration and per-level preview.
  await page.getByRole('combobox',{name:'冰锥普攻解锁时期',exact:true}).selectOption('2');
  const iceProgression=await page.locator('#progressionRows tr').filter({hasText:'冰锥普攻'}).innerText();
  assert.match(iceProgression,/Lv1→6、Lv2→8、Lv3→10/);assert.match(iceProgression,/Lv3（角色 10）/);
  const missileProgression=await page.locator('#progressionRows tr').filter({hasText:'魔法飞弹'}).innerText();
  assert.match(missileProgression,/Lv1→1、Lv2→3、Lv3→5/);assert.match(missileProgression,/Lv5（角色 10）/);
  const maxInput=page.getByRole('spinbutton',{name:'魔法飞弹最高技能等级（0为不封顶）',exact:true});
  assert.equal(await maxInput.inputValue(),'0');
  await maxInput.fill('2');
  assert.match(await page.locator('#progressionRows tr').filter({hasText:'魔法飞弹'}).innerText(),/Lv2（角色 10）/);
  await page.locator('#progressionDetailSkill').selectOption('MagicMissileSkill');
  assert.equal(await page.locator('#progressionDetailRows tr').count(),2);
  assert.match(await page.locator('#progressionNotice').innerText(),/最高技能等级 Lv2/);
  await checkStageTable();
  await page.reload();
  assert.equal(await page.locator('#progressionPage').isVisible(),true,'#progression reopens the progression subpage');
  assert.equal(await maxInput.inputValue(),'2','Fixed level cap survives local storage round trip');
  await maxInput.fill('0');
  assert.equal(await page.locator('#progressionDetailRows tr').count(),20);
  const missileMode=page.getByRole('combobox',{name:'魔法飞弹成长方式',exact:true});
  const missileInterval=page.getByRole('spinbutton',{name:'魔法飞弹每级角色等级间隔',exact:true});
  const missilePerPhase=page.getByRole('spinbutton',{name:'魔法飞弹每阶段新增技能等级',exact:true});
  assert.equal(await missileMode.inputValue(),'phase');assert.equal(await missileInterval.isDisabled(),true);
  await missileMode.selectOption('interval');await missileInterval.fill('10');
  assert.equal(await missilePerPhase.isDisabled(),true);assert.equal(await missileInterval.isDisabled(),false);
  assert.match(await page.locator('#progressionRows tr').filter({hasText:'魔法飞弹'}).innerText(),/Lv1→1、Lv2→11、Lv3→21/);
  await page.getByRole('combobox',{name:'水球魔法成长方式',exact:true}).selectOption('interval');
  await page.getByRole('spinbutton',{name:'水球魔法每级角色等级间隔',exact:true}).fill('7');
  assert.match(await page.locator('#progressionRows tr').filter({hasText:'水球魔法'}).innerText(),/Lv1→1、Lv2→8、Lv3→15/);
  assert.equal(await page.getByRole('combobox',{name:'风龙卷成长方式',exact:true}).inputValue(),'phase');
  await page.locator('#progressionPhaseSize').fill('7');
  assert.match(await page.locator('#progressionRows tr').filter({hasText:'风龙卷'}).innerText(),/Lv1→1、Lv2→5、Lv3→12/);
  assert.match(await page.locator('#progressionNotice').innerText(),/固定等级间隔.*每 10 个角色等级/);
  assert.equal(await page.locator('#progressionDetailRows tr').nth(1).locator('td').nth(1).innerText(),'11');
  await checkStageTable();
  await missileMode.selectOption('phase');assert.equal(await missilePerPhase.inputValue(),'2');
  await missileMode.selectOption('interval');assert.equal(await missileInterval.inputValue(),'10','Switching modes keeps the inactive parameters');
  await page.reload();
  assert.equal(await missileMode.inputValue(),'interval');assert.equal(await missileInterval.inputValue(),'10');
  await page.screenshot({path:path.join(out,'progression-desktop.png'),fullPage:true});
  await page.setViewportSize({width:390,height:844});
  await noOverflow('Progression page must not overflow on mobile');
  await page.screenshot({path:path.join(out,'progression-mobile.png'),fullPage:true});
  await page.setViewportSize({width:1440,height:1100});

  // Keyboard tab navigation wraps.
  await page.locator('#tabProgression').focus();
  await page.keyboard.press('Home');
  assert.equal(await page.locator('#skillsPage').isVisible(),true);
  assert.equal(await page.locator('#tabSkills').evaluate(el=>el===document.activeElement),true);
  assert.equal(await page.evaluate(()=>location.hash),'','Returning to the first tab clears the hash');
  await page.keyboard.press('End');
  assert.equal(await page.locator('#loadoutPage').isVisible(),true);
  await page.keyboard.press('ArrowRight');
  assert.equal(await page.locator('#skillsPage').isVisible(),true,'Right arrow wraps from the third tab');
  await page.keyboard.press('ArrowLeft');
  assert.equal(await page.locator('#loadoutPage').isVisible(),true,'Left arrow wraps to the third tab');
  await resetPage();

  // Loadout page: per-stage editing, batch prefixes, copy, reset, persistence.
  await page.getByRole('tab',{name:'配装与属性配置'}).click();
  assert.equal(await page.evaluate(()=>location.hash),'#loadouts');
  assert.equal(await page.locator('#loadoutList [data-loadout-stage]').count(),18);
  assert.equal(await page.locator('[data-loadout-prop="vPrefix"]').count(),0,'Vanilla comparison prefixes are not edited');
  assert.equal(await page.getByRole('button',{name:'在实验室中查看'}).count(),0);
  await page.locator('[data-loadout-stage="3"]').click();
  assert.match(await page.locator('#loadoutEditor h2').innerText(),/克苏鲁之眼后/);
  assert.equal(await page.locator('#loadoutLife').inputValue(),'200','Eye of Cthulhu stage assumes 200 life for invasion events');
  await page.locator('#loadoutMana').fill('150');await page.locator('#loadoutMana').press('Tab');
  assert.equal(await page.locator('#loadoutMana').inputValue(),'160','Base mana snaps to mana crystals');
  await page.locator('#loadoutLife').fill('412');await page.locator('#loadoutLife').press('Tab');
  assert.equal(await page.locator('#loadoutLife').inputValue(),'410','Life above 400 snaps to life fruit');
  assert.match(await page.locator('#loadoutStats').innerText(),/魔力水晶\s*7/);
  assert.match(await page.locator('#loadoutStats').innerText(),/生命水晶 15 · 生命果 2/);
  await page.locator('#loadoutArmor').selectOption('jungle');
  await page.locator('[data-loadout-index="0"][data-loadout-prop="id"]').selectOption('sorcerer');
  await page.locator('[data-loadout-index="1"][data-loadout-prop="id"]').selectOption('sorcerer');
  assert.match(await page.locator('#loadoutStats').innerText(),/重复装备/);
  await page.locator('[data-loadout-index="1"][data-loadout-prop="id"]').selectOption('celestialCuffs');
  await page.getByRole('button',{name:'本阶段全增伤',exact:true}).click();
  const l3=(await stored()).loadouts[2],probe=B.loadoutProbe({...l3,items:l3.items.map(x=>({...x,prefix:'none'}))});
  const expectedPrefixes=Array.from(B.applyDamagePrefixes(probe,'e'));
  assert.ok(expectedPrefixes.includes('lucky'),'Jungle stage gear prefers lucky for some slots');
  assert.deepEqual(await page.locator('[data-loadout-prop="prefix"]').evaluateAll(els=>els.slice(0,5).map(el=>el.value)),expectedPrefixes);
  near(Number((await page.locator('#loadoutStats strong').first().innerText()).replace(/[+%]/g,'')),B.gear(B.loadoutProbe((await stored()).loadouts[2]),'e').damage);
  await page.getByRole('tab',{name:'成长与阶段伤害',exact:true}).click();
  await page.locator('#stageDamageGear').check();await checkStageTable('hit',true);
  await page.locator('#stageDamageGear').uncheck();
  await page.getByRole('tab',{name:'配装与属性配置'}).click();
  await page.locator('[data-loadout-stage="4"]').click();
  await page.getByRole('button',{name:'复制上一阶段',exact:true}).click();
  assert.equal(await page.locator('#loadoutArmor').inputValue(),'jungle');
  assert.equal(await page.locator('#loadoutMana').inputValue(),'160');
  assert.equal(await page.locator('#loadoutName').inputValue(),B.loadoutDefault(4).name,'Copy keeps the stage name');
  await page.getByRole('button',{name:'恢复本阶段默认',exact:true}).click();
  assert.equal(await page.locator('#loadoutArmor').inputValue(),B.loadoutDefault(4).armor);
  await page.locator('#loadoutName').fill('');await page.locator('#loadoutName').press('Tab');
  assert.equal(await page.locator('#loadoutName').inputValue(),B.loadoutDefault(4).name,'Empty names are rejected');
  await page.locator('#loadoutName').fill('<b>测试</b>');await page.locator('#loadoutName').press('Tab');
  assert.equal(await page.locator('#loadoutList b b').count(),0,'Loadout names must be escaped');
  await page.screenshot({path:path.join(out,'loadouts-desktop.png'),fullPage:true});
  await page.setViewportSize({width:390,height:844});
  await page.screenshot({path:path.join(out,'loadouts-mobile.png'),fullPage:true});
  await noOverflow('Loadout page must not overflow on mobile');
  await page.setViewportSize({width:1440,height:1100});
  await page.reload();
  assert.equal(await page.locator('#loadoutPage').isVisible(),true,'#loadouts hash reopens the loadout page');
  saved=await stored();near(saved.loadouts[2].baseMana,160);near(saved.loadouts[2].maxLife,410);assert.equal(saved.loadouts[3].name,'<b>测试</b>');
  await resetPage();
  assert.equal((await stored()).loadouts[2].baseMana,200,'Reset restores default loadouts');

  // Old browser caches with lab, combat and simulation fields still load, and those fields are dropped.
  const legacyCache={...B.defaults(),stage:13,baseDpsMode:'manual',baseDps:777,baseCrit:4,slots:6,v:{armor:'none'},e:{armor:'none'},baseMana:200,maxLife:500,ashenWitch:true,extraMana:0,skillStats:true,nebulaManaBridge:false,combat:{duration:120}};
  legacyCache.skills=legacyCache.skills.map(r=>({...r,enabled:true,delay:0,duration:0,first:0,priority:1,hit:100}));legacyCache.rate=6;
  await page.evaluate(value=>localStorage.setItem('elaina-mage-balance-v2',JSON.stringify(value)),legacyCache);
  await page.reload();await page.locator('#tabSkills').click();
  assert.equal(await page.locator('#rate').inputValue(),'6','Legacy cache keeps its skill parameters');
  await page.locator('#rate').fill('6.5');
  saved=await stored();assert.deepEqual(Object.keys(saved).sort(),Object.keys(B.defaults()).sort());assert.ok(saved.skills.every(r=>!Object.hasOwn(r,'priority')&&!Object.hasOwn(r,'enabled')));
  await resetPage();

  // Save to a mocked file handle.
  await page.evaluate(()=>{
   window.testFile={name:'SkillBalance.json',text:'',picks:0,writes:0};
   window.showOpenFilePicker=async()=>{window.testFile.picks++;return [{name:window.testFile.name,
    queryPermission:async()=>window.testFile.denied?'denied':'granted',requestPermission:async()=>window.testFile.denied?'denied':'granted',
    getFile:async()=>({text:async()=>window.testFile.text}),
    createWritable:async()=>({write:async text=>{if(window.testFile.fail)throw new Error('test write failure');window.testFile.pending=text;},close:async()=>{window.testFile.text=window.testFile.pending;window.testFile.writes++;},abort:async()=>{}})
   }];};
  });
  await page.getByRole('button',{name:'保存到模组',exact:true}).click();
  await page.waitForFunction(()=>!document.getElementById('saveMod').disabled);
  assert.match(await page.locator('#fileStatus').innerText(),/已保存/);
  const savedDirect=await page.evaluate(()=>JSON.parse(window.testFile.text));
  assert.equal(savedDirect.schema,'elaina-mage-balance-v2');
  assert.deepEqual(Object.keys(savedDirect).sort(),['configuration','schema'],'Save contains parameters, not calculated results');
  assert.deepEqual(Object.keys(savedDirect.configuration).sort(),Object.keys(B.defaults()).sort(),'Only game configuration is saved');
  assert.equal(savedDirect.configuration.loadouts.length,18,'Loadouts are saved with the mod JSON');
  await page.getByRole('button',{name:'保存到模组',exact:true}).click();
  await page.waitForFunction(()=>!document.getElementById('saveMod').disabled);
  assert.equal(await page.evaluate(()=>window.testFile.picks),1,'Subsequent saves reuse the selected handle');
  assert.equal(await page.evaluate(()=>window.testFile.writes),2);
  await page.keyboard.press('Control+s');
  await page.waitForFunction(()=>window.testFile.writes===3);
  const beforeResetFile=await page.evaluate(()=>window.testFile.text);
  await resetPage();
  assert.equal(await page.evaluate(()=>window.testFile.text),beforeResetFile,'Reset does not overwrite the mod file');
  assert.match(await page.locator('#status').innerText(),/尚未写入模组文件/);
  await page.locator('#rate').fill('7');
  await page.getByRole('button',{name:'重新读取',exact:true}).click();
  await page.waitForFunction(()=>document.getElementById('rate').value!=='7');
  assert.equal(await page.locator('#rate').inputValue(),String(savedDirect.configuration.rate));
  await page.evaluate(()=>{window.testFile.denied=true;});
  await page.getByRole('button',{name:'保存到模组',exact:true}).click();
  await page.waitForFunction(()=>!document.getElementById('saveMod').disabled);
  assert.match(await page.locator('#fileStatus').innerText(),/未获得/);
  assert.equal(await page.evaluate(()=>window.testFile.writes),3,'Denied permission must not write');
  await page.evaluate(()=>{window.testFile.denied=false;window.testFile.fail=true;});
  await page.getByRole('button',{name:'保存到模组',exact:true}).click();
  await page.waitForFunction(()=>!document.getElementById('saveMod').disabled);
  assert.match(await page.locator('#fileStatus').innerText(),/test write failure/);
  assert.equal(await page.evaluate(()=>window.testFile.writes),3,'Failed write must not close or report success');
  await page.evaluate(()=>{window.testFile.fail=false;window.testFile.name='wrong.json';});
  await page.getByRole('button',{name:'更换…',exact:true}).click();
  await page.waitForFunction(()=>document.getElementById('fileStatus').textContent.includes('请选择模组'));
  await page.evaluate(()=>{window.showOpenFilePicker=async()=>{throw new DOMException('cancel','AbortError');};});
  await page.getByRole('button',{name:'更换…',exact:true}).click();
  assert.match(await page.locator('#fileStatus').innerText(),/已取消/);

  // File reload path using an isolated in-memory handle.
  const loadPayload=async(value)=>{
   const payload=value&&typeof value==='object'&&!Array.isArray(value)&&!Object.hasOwn(value,'schema')?{schema:'elaina-mage-balance-v2',configuration:value}:value;
   await page.evaluate(text=>{
    window.showOpenFilePicker=async()=>[{name:'SkillBalance.json',queryPermission:async()=> 'granted',requestPermission:async()=> 'granted',getFile:async()=>({text:async()=>text}),createWritable:async()=>({write:async value=>{window.testSavedText=value;},close:async()=>{}})}];
    document.getElementById('fileStatus').textContent='';
   },typeof payload==='string'?payload:JSON.stringify(payload));
   await page.locator('#linkMod').click();
   await page.waitForFunction(()=>document.getElementById('fileStatus').textContent.includes('已关联'));
   await page.locator('#fileStatus').evaluate(el=>el.textContent='');
   await page.locator('#loadMod').click();
   await page.waitForFunction(()=>document.getElementById('fileStatus').textContent!=='');
  };
  const base=savedConfig();
  await loadPayload(base);
  assert.match(await page.locator('#fileStatus').innerText(),/已读取/,'The published mod JSON loads');
  assert.equal(await page.locator('#rotationRows tr').count(),base.skills.length);
  for(const category of ['basicAttacks','skills']){
   for(const [field,value] of [['growthMode','bad'],['growthMode',null],['levelInterval',0],['levelInterval',1.5],['levelInterval',1001],['freezeBuildup',101]]){
    const invalid=B.clone(base);invalid[category][0][field]=value;
    await loadPayload(invalid);assert.match(await page.locator('#fileStatus').innerText(),/读取失败/,category+'.'+field);
   }
  }
  const intervalPayload=B.clone(base);
  Object.assign(intervalPayload.basicAttacks[0],{growthMode:'interval',levelInterval:10});
  Object.assign(intervalPayload.skills[0],{growthMode:'interval',levelInterval:7});
  await loadPayload(intervalPayload);
  await page.locator('#saveMod').click();
  await page.waitForFunction(()=>typeof window.testSavedText==='string');
  const intervalExport=await page.evaluate(()=>JSON.parse(window.testSavedText).configuration);
  assert.equal(intervalExport.basicAttacks[0].growthMode,'interval');assert.equal(intervalExport.basicAttacks[0].levelInterval,10);
  assert.equal(intervalExport.skills[0].growthMode,'interval');assert.equal(intervalExport.skills[0].levelInterval,7);
  for(const row of base.skills.filter(r=>Number.isFinite(r.ratio)))near(intervalExport.skills.find(r=>r.id===row.id).ratio,row.ratio);
  const duplicate=B.clone(base);duplicate.skills.at(-1).id=duplicate.skills[0].id;
  await loadPayload(duplicate);assert.match(await page.locator('#fileStatus').innerText(),/读取失败/);
  const empty=B.clone(base);empty.skills=[];await loadPayload(empty);
  assert.equal(await page.locator('#rotationRows input').count(),0,'Zero active skills is a valid basic-only plan');
  await loadPayload(base);
  await page.locator('#rate').fill('9');
  const retained=await page.locator('#rate').inputValue();
  for(const bad of ['{bad json',null,[],{schema:'future',configuration:base},{version:2},{...base,rate:21},{...base,loadouts:base.loadouts.slice(1)},{...base,skills:[null]}]){
   await loadPayload(bad===null?'null':bad);
   assert.match(await page.locator('#fileStatus').innerText(),/读取失败，当前配置未改变/);
   assert.equal(await page.locator('#rate').inputValue(),retained);
  }
  await loadPayload({...base,unexpected:'discard me'});
  assert.equal(await page.evaluate(()=>Object.hasOwn(JSON.parse(localStorage.getItem('elaina-mage-balance-v2')),'unexpected')),false);
  await loadPayload(JSON.parse(fs.readFileSync(path.join(__dirname,'elaina-mage-balance.json'),'utf8')));
  assert.match(await page.locator('#fileStatus').innerText(),/已读取/,'Old saved JSON with lab fields remains readable');
  await resetPage();
  assert.deepEqual(errors,[]);
  console.log('Browser smoke passed: skill configuration, stage damage table, growth preview, loadouts, legacy cache cleanup, file save/reload, desktop/mobile layout; zero JS errors.');
 } finally {await browser.close();}
}
if(process.argv.includes('--browser'))browserTests().catch(e=>{console.error(e);process.exitCode=1;});
