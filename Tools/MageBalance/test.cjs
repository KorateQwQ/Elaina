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
const combatCode=html.match(/<script id="combat-engine">([\s\S]*?)<\/script>/)[1];
new vm.Script(combatCode);vm.runInNewContext(combatCode,ctx);const C=ctx.module.exports;
let count = 0;
const near = (a,b) => assert.ok(Math.abs(a-b)<1e-7, `${a} != ${b}`);
function test(name, fn) { fn(); count++; console.log('PASS', name); }
function naked() {
 const s = B.defaults(); s.baseDps=1000; s.basicAttacks[0].ratio=1;s.basicAttacks[0].ratioCurve=[];s.ashenWitch=false; // isolate legacy balance arithmetic from the level curve and Ashen Witch
 for(const side of ['v','e']) { s[side].armor='none'; s[side].items.forEach(x=>{x.id='empty';x.prefix='none';}); }
 return s;
}
test('MagicMissileSkill follows its level curve down to 60% at Lv50 and is not an active skill',()=>{const s=B.defaults(),r=s.basicAttacks[0];near(r.ratio,.6);assert.deepEqual(Array.from(r.ratioCurve,p=>Array.from(p)),[[1,1],[10,.9],[20,.8],[35,.7],[50,.6]]);near(B.calculate(s).auto/s.baseDps/B.gear(s,'e').multiplier,.6);near(B.basicBudget(B.defaults('jungle')).ratio,.8-.1/15*10);assert.equal(s.skills.some(x=>x.id==='MagicMissileSkill'),false);});
test('Ratio curve interpolates linearly and clamps at both ends',()=>{const r=B.defaults().basicAttacks[0];near(B.basicRatio(r,0),1);near(B.basicRatio(r,1),1);near(B.basicRatio(r,5.5),.95);near(B.basicRatio(r,30),.8-.1/15*10);near(B.basicRatio(r,50),.6);near(B.basicRatio(r,95),.6);r.ratioCurve=[];near(B.basicRatio(r,1),.6);delete r.ratioCurve;near(B.basicRatio(r,1),.6);});
test('dpsLevel inverts the KL level table',()=>{for(const l of [1,3,5,12,30,50,65,94])near(B.dpsLevel(B.levelDps(l)),l);near(B.dpsLevel(20),0);near(B.dpsLevel(200),30);near(B.dpsLevel(450),50);near(B.dpsLevel(99999),95);});
test('Curve text parses loosely and round-trips; invalid curves are rejected',()=>{assert.deepEqual(Array.from(B.parseCurve('1:1, 10：0.9；20 : .8'),p=>Array.from(p)),[[1,1],[10,.9],[20,.8]]);assert.deepEqual(Array.from(B.parseCurve('')),[]);const c=[[1,1],[50,.6]];assert.deepEqual(Array.from(B.parseCurve(B.formatCurve(c)),p=>Array.from(p)),c);for(const t of ['10:1, 5:.9','1:1, 1:.9','0:1','1001:1','1:6','1.5:1','abc','1:-1'])assert.equal(B.parseCurve(t),null,t);assert.equal(B.validCurve(Array.from({length:17},(_,i)=>[i+1,1])),false);const s=B.defaults();s.basicAttacks[0].ratioCurve=[[10,1],[5,1]];assert.equal(B.validBasics(s),false);s.basicAttacks[0].ratioCurve=[[1,1]];assert.ok(B.validBasics(s));});
test('Active skill damage has no occupancy compensation by default; occupyRatio prices cast time in B',()=>{const s=naked(),x=s.skills[0];Object.assign(x,{cost:100,k:1,cd:10,cdRatio:1,cast:1});near(s.occupyRatio,0);near(B.skillDamageBudget(s,x),2000);s.occupyRatio=.5;near(B.skillDamageBudget(s,x),2500);x.cast=0;near(B.skillDamageBudget(s,x),2000+500/60);s.occupyRatio=0;s.basicAttacks[0].ratio=3;near(B.skillDamageBudget(s,x),2000);});
test('Both naked outputs equal B, including baseline crit normalization',()=>{const r=B.calculate(naked());near(r.vanilla,1000);near(r.auto,1000);near(r.maxMana,200);});
test('Single Menacing adds 4 percentage points, not a separate multiplier',()=>{const s=naked();s.v.damage=50;s.v.items[0]={id:'utility',prefix:'menacing'};near(B.calculate(s).vanilla,1540);});
test('Single Arcane buys 4% in the explicit budget model',()=>{const s=naked();s.e.items[0]={id:'utility',prefix:'arcane'};const r=B.calculate(s);near(r.maxMana,220);});
test('Damage gain does not compound additively stacked gear',()=>{const s=naked();s.v.armor='hallowed';s.v.items[0]={id:'sorcerer',prefix:'menacing'};near(B.gear(s,'v').damage,38);});
test('Jungle armor aggregation',()=>{const s=naked();s.e.armor='jungle';const g=B.gear(s,'e');near(g.damage,6);near(g.crit,16);near(g.mana,80);});
test('Adamantite armor aggregation',()=>{const s=naked();s.e.armor='adamantite';const g=B.gear(s,'e');near(g.damage,20);near(g.crit,23);near(g.mana,80);});
test('Hallowed armor aggregation',()=>{const s=naked();s.e.armor='hallowed';const g=B.gear(s,'e');near(g.damage,19);near(g.crit,23);near(g.mana,100);});
test('Spectre passive only, no invented proc',()=>{const s=naked();s.e.armor='spectre';near(B.gear(s,'e').damage,25);near(B.gear(s,'e').crit,21);near(s.e.proc,0);});
test('Crit clamps at 100%',()=>{const s=naked();s.e.crit=100;near(B.gear(s,'e').crit,100);near(B.gear(s,'e').multiplier,2/1.04);});
test('Negative crit cannot drop below zero',()=>{const s=naked();s.e.crit=-100;near(B.gear(s,'e').crit,0);});
test('Empty accessories cannot receive prefix bonuses',()=>{const s=naked();s.e.items[0].prefix='arcane';near(B.calculate(s).maxMana,200);});
test('Slots beyond chosen cap are inactive',()=>{const s=naked();s.slots=5;s.e.items[6]={id:'sorcerer',prefix:'arcane'};near(B.calculate(s).auto,1000);near(B.calculate(s).maxMana,200);});
test('Common stats on skills can be disabled',()=>{const s=naked();s.e.damage=50;s.skillStats=false;near(B.calculate(s).skillMultiplier,1);s.skillStats=true;near(B.calculate(s).skillMultiplier,1.5);});
test('Cooldown damage ratio reduces only the cooldown-priced skill damage',()=>{const s=naked();const skill=s.skills[0];const full=B.skillDamageBudget(s,skill);skill.cdRatio=.5;const half=B.skillDamageBudget(s,skill);const occupy=0;near(half,occupy+(full-occupy)*.5);s.occupyRatio=1;skill.cdRatio=1;const full1=B.skillDamageBudget(s,skill);skill.cdRatio=.5;const occupy1=s.baseDps*Math.max(1/60,skill.cast);near(B.skillDamageBudget(s,skill),occupy1+(full1-occupy1)*.5);});
test('No cooldown or mana-reduction axis is present',()=>{const s=B.defaults();assert.equal(s.cooldown,undefined);assert.equal(s.manaReduction,undefined);});
test('Custom proc damage enters vanilla output once',()=>{const s=naked();s.v.proc=300;near(B.calculate(s).vanilla,1300);});
test('Displayed recovery uses local mod parameters, not DPS bonus',()=>{const s=naked();near(B.calculate(s).recovery,9);near(B.calculate(s).auto,1000);});
test('Changing B scales outputs without changing mana',()=>{const a=B.defaults(),b=B.clone(a);b.baseDps=a.baseDps*3;const x=B.calculate(a),y=B.calculate(b);near(y.vanilla,x.vanilla*3);near(y.auto,x.auto*3);near(y.maxMana,x.maxMana);});
test('Calculation does not mutate configuration',()=>{const s=B.defaults(),before=JSON.stringify(s);B.calculate(s);B.skillDamageBudget(s,s.skills[0]);assert.equal(JSON.stringify(s),before);});
test('All default presets produce finite results',()=>{for(const key of Object.keys(B.presets)){const r=B.calculate(B.defaults(key));assert.ok(Number.isFinite(r.vanilla)&&Number.isFinite(r.auto)&&Number.isFinite(r.maxMana));}});
test('Default loadouts cover all 19 stages, mirror the presets and validate',()=>{
 const list=B.loadoutDefaults();assert.equal(list.length,19);assert.ok(B.validLoadouts(list));
 for(const p of Object.values(B.presets)){const l=list[p.stage-1],ids=Array.from({length:7},(_,i)=>p.items[i]||'utility');ids[ids.slice(0,p.slots).indexOf('utility')]='elainaHat';assert.equal(l.stage,p.stage);assert.equal(l.armor,p.armor);assert.equal(l.slots,p.slots);assert.deepEqual(Array.from(l.items,x=>x.id),ids,'Elaina hat takes the first active utility slot');}
 near(list[0].baseMana,100);near(list[1].baseMana,200);near(list[2].maxLife,200);assert.ok(list.every((l,i)=>i===0||l.maxLife>=list[i-1].maxLife));near(list[18].maxLife,500);
});
test('Elaina hat adds damage, crit and special mana only on the Elaina side',()=>{
 const s=naked();s.e.items[0]={id:'elainaHat',prefix:'arcane'};s.v.items[0]={id:'elainaHat',prefix:'menacing'};
 const e=B.gear(s,'e'),v=B.gear(s,'v');near(e.damage,10);near(e.crit,9);near(e.mana,20);near(e.magic,20);
 near(v.damage,4);near(v.crit,4);near(v.magic,0);near(B.calculate(s).maxMana,240);assert.equal(B.sideItem('elainaHat','v'),'utility');assert.equal(B.sideItem('elainaHat','e'),'elainaHat');
});
test('Ashen Witch converts the halved extra life into special mana and can be switched off',()=>{
 for(const [life,mana] of [[100,0],[160,30],[305,103],[400,150],[500,200],[50,0]])near(B.ashenWitchMana(life),mana);
 const s=naked();s.maxLife=500;assert.equal(B.defaults().ashenWitch,true);s.ashenWitch=true;let r=B.calculate(s);near(r.maxMana,400);near(r.witchMana,200);near(r.recovery,5+400*.02);
 s.ashenWitch=false;r=B.calculate(s);near(r.maxMana,200);near(r.witchMana,0);
 const d=B.defaults(3);near(d.maxLife,200);near(B.calculate(d).witchMana,50);B.applyLoadout(d,19,false);near(d.maxLife,500);
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
test('Stage defaults read from loadouts; string preset keys still map to stages',()=>{
 const s=B.defaults(2);assert.equal(s.stage,2);near(s.baseDps,50);near(s.baseMana,200);assert.equal(s.e.armor,'magicTopaz');assert.equal(s.loadouts.length,19);
 assert.equal(B.defaults('jungle').stage,6);assert.equal(B.defaults().stage,13);
 assert.deepEqual(Array.from(s.e.items.slice(0,5),x=>x.prefix),['arcane','none','none','none','none'],'King Slime stage: one lucky prefix before the Goblin Tinkerer');
 assert.deepEqual(Array.from(s.v.items.slice(0,5),x=>x.prefix),['menacing','none','none','none','none']);
 assert.deepEqual(Array.from(s.e.items.slice(0,5),x=>x.id),['band','elainaHat','utility','utility','empty']);
 assert.deepEqual(Array.from(s.v.items.slice(0,5),x=>x.id),['band','utility','utility','utility','empty'],'Vanilla side sees the hat slot as a utility placeholder');
});
test('Applying a loadout swaps gear, both side prefixes and mana, and unlocks skills',()=>{
 const s=B.defaults(13);s.loadouts[5]={...s.loadouts[5],armor:'meteor',slots:6,baseMana:160,items:s.loadouts[5].items.map((x,i)=>({id:i?x.id:'celestialCuffs',prefix:'lucky',vPrefix:'menacing'}))};
 s.v.items.forEach(x=>x.prefix='warding');B.setPeriod(s.skills[0],8);
 const r=B.applyLoadout(s,6);
 assert.equal(s.stage,6);near(s.baseDps,200);near(s.baseMana,160);near(s.slots,6);assert.equal(s.v.armor,'meteor');assert.equal(s.e.armor,'meteor');
 assert.equal(s.v.items[0].id,'celestialCuffs');assert.equal(s.e.items[0].id,'celestialCuffs');assert.ok(s.e.items.every(x=>x.prefix==='lucky'));assert.ok(s.v.items.every(x=>x.prefix==='menacing'),'Vanilla prefixes come from vPrefix');
 assert.equal(s.skills[0].enabled,false);near(r.level,30);
 s.skills[0].enabled=true;assert.equal(B.applyLoadout(s,6,false),null);assert.equal(s.skills[0].enabled,true,'Editing without unlock keeps manual skill toggles');
});
test('Presets cover every vanilla period in order with KL stage DPS and valid gear',()=>{
 const list=Object.values(B.presets);assert.equal(JSON.stringify(list.map(p=>p.stage)),JSON.stringify(B.periods.map(p=>p.stage)));
 for(const p of list){assert.ok(Object.hasOwn(B.armors,p.armor),p.name);assert.ok(p.items.length<=p.slots&&p.items.every(id=>Object.hasOwn(B.accessories,id)),p.name);const real=p.items.filter(id=>!['utility','empty'].includes(id));assert.equal(new Set(real).size,real.length,p.name);}
 near(B.presets.start.base,30);near(B.presets.kingSlime.base,50);near(B.presets.jungle.base,200);near(B.presets.adamantite.base,450);near(B.presets.hallowed.base,900);near(B.presets.spectre.base,1800);near(B.presets.nebula.base,3500);
});
test('Default prefixes ramp up with the Goblin Tinkerer; both sides reforge the same slots',()=>{
 const counts=Array.from(B.loadoutDefaults(),l=>{
  const active=l.items.slice(0,l.slots).filter(x=>x.id!=='empty'),good=active.filter(x=>x.prefix==='arcane');
  for(const x of l.items){assert.ok(x.prefix==='arcane'||x.prefix==='none',l.name);assert.equal(x.vPrefix==='none',x.prefix==='none',l.name+': vanilla and Elaina share reforged slots');assert.ok(['none','menacing','lucky'].includes(x.vPrefix),l.name);}
  assert.ok(l.items.every(x=>x.id!=='empty'||x.prefix==='none'),'Empty slots never carry a prefix');
  return [good.length,active.length];
 });
 assert.deepEqual(counts.slice(0,6),[[0,3],[1,4],[2,5],[3,5],[4,5],[5,5]],'Start has no damage prefix, then one more reforge per stage');
 for(const [good,active] of counts.slice(5))assert.equal(good,active,'From Skeletron on every equipped slot is reforged');
 assert.equal(B.loadoutDefault(8).slots,6,'Expert Demon Heart arrives with the Wall of Flesh');
 const s=B.defaults(19);B.applyLoadout(s,19);const c=B.clone(s);B.applyDamagePrefixes(c,'v');
 assert.deepEqual(Array.from(s.v.items,x=>x.prefix),Array.from(c.v.items,x=>x.prefix),'Vanilla defaults are the best menacing/lucky mix for that gear');
});
test('Unlock period maps to stage cap 5k and first level 5k-4; custom values are period 0',()=>{
 const row=B.newSkill('X','X');assert.ok(B.setPeriod(row,2));assert.equal(row.unlockStageCap,10);assert.equal(row.unlockLevel,6);assert.equal(B.periodOf(row),2);
 row.unlockStageCap=12;assert.equal(B.periodOf(row),0);assert.equal(B.setPeriod(row,20),false);assert.equal(row.unlockStageCap,12);
});
test('Stage unlocks enable exactly the skills reachable at that period and fall back the basic attack',()=>{
 const s=B.defaults();B.setPeriod(s.skills[1],8);B.setPeriod(s.skills[2],13);s.skills[0].enabled=false;
 s.basicAttacks.push({...B.newBasic('IceBasic','冰锥'),unlockStageCap:30,unlockLevel:26});s.selectedBasicAttackId='IceBasic';
 let r=B.applyStageUnlocks(s,6);
 assert.equal(JSON.stringify(s.skills.map(x=>x.enabled)),JSON.stringify(s.skills.map((x,i)=>i!==1&&i!==2)));assert.equal(s.progression.previewLevel,30);assert.equal(s.selectedBasicAttackId,'IceBasic');assert.equal(r.level,30);
 r=B.applyStageUnlocks(s,5);assert.equal(s.selectedBasicAttackId,'MagicMissileSkill');
 B.applyStageUnlocks(s,13);assert.ok(s.skills.every(x=>x.enabled));
});
test('Fractional mana costs are valid for basic attacks and active skills',()=>{
 const s=B.defaults();s.basicAttacks[0].cost=2.5;s.skills[0].cost=12.5;assert.ok(B.validBasics(s));
 near(B.basicBudget(s).manaPerSecond,2.5/s.basicAttacks[0].interval);assert.ok(Number.isFinite(B.calculate(s).auto));
 near(s.occupyRatio,0);near(B.skillDamageBudget(s,s.skills[0])*2,B.skillDamageBudget(s,{...s.skills[0],cost:25}));
});
test('Static IDs are unique',()=>{const ids=[...html.matchAll(/\bid="([^"]+)"/g)].map(x=>x[1]);assert.equal(ids.length,new Set(ids).size);});
test('Standalone has no remote script or stylesheet dependency; fixed saving uses a relative loopback endpoint',()=>{assert.ok(!/<script[^>]+src=|<link[^>]+href=/.test(html));assert.ok(html.includes("fetch('/api/skill-balance'"));});

function setCombat(s,values){
 const {autoCost,autoInterval,...combat}=values;Object.assign(s.combat,combat);
 if(autoCost!==undefined)s.basicAttacks[0].cost=autoCost;
 if(autoInterval!==undefined)s.basicAttacks[0].interval=autoInterval;
}
function combatBase(){const s=naked();s.skills.forEach(x=>x.enabled=false);setCombat(s,{duration:10,potions:0,regenMode:'manual',regen:0,autoCost:0,autoInterval:.5});return s;}
function onlySkill(s){const x=s.skills[0];Object.assign(x,{enabled:true,cost:100,k:1,cd:10,cast:1,delay:0,duration:0,maxCharges:1,initialCharges:1,first:0,priority:1,hit:100});return x;}
function ledger(r){near(r.initialMana+r.regenEffective+r.potionEffective-r.autoMana-r.skillMana-r.hurtPaid,r.endMana);assert.ok(r.endMana>=-1e-7&&r.endMana<=r.maxMana+1e-7);}
test('Combat naked free auto reproduces base DPS',()=>{const r=C.run(combatBase());near(r.dps,1000);near(r.autoCount,20);ledger(r);});
test('Combat fixed mana limits attack count',()=>{const s=combatBase();s.basicAttacks[0].cost=50;const r=C.run(s);near(r.autoCount,4);near(r.endMana,0);near(r.firstShortage,2);ledger(r);});
test('Combat zero opening mana means no free paid attacks',()=>{const s=combatBase();s.combat.startPercent=0;s.basicAttacks[0].cost=3;near(C.run(s).autoDamage,0);});
test('Combat five potions add exactly 2.5 pools when fully spent',()=>{const s=combatBase();setCombat(s,{duration:20,potions:5,autoCost:50});const r=C.run(s);near(r.usedPotions,5);near(r.autoCount,14);near(r.potionEffective,500);near(r.fifthPotionTime,6);near(r.lastPotionTime,6);ledger(r);});
test('Atomic payment does not waste insufficient potion charges',()=>{const s=combatBase();setCombat(s,{startPercent:0,potions:1,autoCost:200});const r=C.run(s);near(r.usedPotions,0);near(r.potionsLeft,1);near(r.autoCount,0);});
test('Atomic payment can use several bottles at once',()=>{const s=combatBase();setCombat(s,{duration:1,startPercent:0,potions:5,autoCost:200});const r=C.run(s);near(r.autoCount,2);near(r.usedPotions,4);ledger(r);});
test('Cost above maximum is never cast in resource simulation',()=>{const s=combatBase();onlySkill(s).cost=201;const r=C.run(s);near(r.skills[0].count,0);assert.equal(r.invalid.length,1);});
test('Unrestricted reference still shows impossible skill potential',()=>{const s=combatBase();onlySkill(s).cost=201;near(C.run(s,true).skills[0].count,1);});
test('WaterBall proposal: cost100 cd10 K0.8 means 1440 at B900, plus 720 only with occupyRatio 0.8',()=>{const s=combatBase();s.baseDps=900;const skill=onlySkill(s);skill.k=.8;near(B.skillDamageBudget(s,skill),1440);near(C.run(s).skillDamage,1440);s.occupyRatio=.8;near(B.skillDamageBudget(s,skill),2160);near(C.run(s).skillDamage,2160);});
test('Cast time costs autos and is compensated only through occupyRatio',()=>{const s=combatBase();onlySkill(s);let r=C.run(s);near(r.autoDamage,9000);near(r.skillDamage,2000);near(r.dps,1100);s.occupyRatio=1;r=C.run(s);near(r.autoDamage,9000);near(r.skillDamage,3000);near(r.dps,1200);});
test('Cooldown bounds cast count and excludes an action at the end',()=>{const s=combatBase();s.combat.duration=30;onlySkill(s);near(C.run(s,true).skills[0].count,3);});
test('Initial empty charges recover after a full cooldown',()=>{const s=combatBase();s.combat.duration=20;onlySkill(s).initialCharges=0;const r=C.run(s,true);near(r.skills[0].count,1);near(r.events.find(e=>e.type==='cast').time,10);});
test('Five stored charges refill serially, not five per cooldown',()=>{const s=combatBase();s.combat.duration=25;Object.assign(onlySkill(s),{cast:.1,maxCharges:5,initialCharges:5});near(C.run(s,true).skills[0].count,7);});
test('First release time is respected',()=>{const s=combatBase();s.combat.duration=30;onlySkill(s).first=15;const r=C.run(s,true);near(r.skills[0].count,2);near(r.events.find(e=>e.type==='cast').time,15);});
test('Damage after battle end is not credited',()=>{const s=combatBase();Object.assign(onlySkill(s),{cast:1,delay:20});const r=C.run(s);near(r.skills[0].count,1);near(r.skillDamage,0);});
test('DoT is clipped at battle end',()=>{const s=combatBase();Object.assign(onlySkill(s),{cast:1,delay:0,duration:20});near(C.run(s).skillDamage,2000*9/20);});
test('Hit rate affects damage but not cost',()=>{const s=combatBase();onlySkill(s).hit=50;const r=C.run(s);near(r.skillDamage,1000);near(r.skillMana,100);});
test('Uniform injuries integrate their full requested total',()=>{const s=combatBase();s.combat.hurtTotal=100;const r=C.run(s);near(r.hurtRequested,100);near(r.hurtPaid,100);near(r.endMana,100);ledger(r);});
test('Insufficient defensive mana never goes negative',()=>{const s=combatBase();s.combat.hurtTotal=500;const r=C.run(s);near(r.hurtPaid,200);near(r.uncoveredHurt,300);near(r.endMana,0);});
test('Periodic injuries occur at intervals, not at time zero or T',()=>{const s=combatBase();setCombat(s,{duration:30,hurtMode:'periodic',hurtInterval:10,hurtPerHit:40});near(C.run(s).hurtRequested,80);});
test('Explicit injury event at zero precedes casting',()=>{const s=combatBase();setCombat(s,{hurtMode:'events',hurtEvents:'0:200',autoCost:3});const r=C.run(s);near(r.hurtPaid,200);near(r.autoCount,0);});
test('Injury in the final subframe is not lost',()=>{const s=combatBase();s.combat.hurtMode='events';s.combat.hurtEvents='9.999:40';near(C.run(s).hurtRequested,40);});
test('Malformed and out-of-battle injury lines are reported',()=>{const s=combatBase();s.combat.hurtMode='events';s.combat.hurtEvents='bad\n12:40\n5:20';const r=C.run(s);assert.equal(r.injuryErrors.length,2);near(r.hurtRequested,20);});
test('Full mana regeneration is counted as waste',()=>{const s=combatBase();s.combat.regen=10;const r=C.run(s);near(r.regenWaste,100);near(r.regenEffective,0);ledger(r);});
test('Active reserve prevents skill spending but permits ordinary attack',()=>{const s=combatBase();onlySkill(s);s.combat.reserve=150;const r=C.run(s);near(r.skillMana,0);near(r.dps,1000);});
test('Save strategy can stop autos to accumulate for a ready skill',()=>{const s=combatBase();onlySkill(s);setCombat(s,{startPercent:0,regen:10,autoCost:3,duration:20,policy:'save'});const save=C.run(s);s.combat.policy='flow';const flow=C.run(s);assert.ok(save.skills[0].count>flow.skills[0].count);ledger(save);ledger(flow);});
test('Skill priority chooses the first action deterministically',()=>{const s=combatBase();onlySkill(s);s.skills[2].enabled=true;s.skills[2].priority=1;s.skills[0].priority=2;assert.match(C.run(s,true).events.find(e=>e.type==='cast').text,/炎爆/);});
test('Efficiency strategy ignores priority and chooses higher damage per mana',()=>{const s=combatBase();s.basicAttacks[0].cost=100;s.basicAttacks[0].interval=1;s.skills.forEach(x=>x.enabled=false);const a=s.skills[0],b=s.skills[1];Object.assign(a,{enabled:true,cost:100,k:.2,cd:10,cdRatio:1,priority:1});Object.assign(b,{enabled:true,cost:50,k:1,cd:30,cdRatio:1,priority:9});s.combat.policy='efficiency';const first=C.run(s,true).events.find(e=>e.type==='cast');assert.match(first.text,/风龙卷/);});
test('Efficiency strategy keeps ordinary attacks while a better skill waits for mana',()=>{const s=combatBase();s.basicAttacks[0].cost=5;s.basicAttacks[0].interval=.5;s.skills.forEach(x=>x.enabled=false);const high=s.skills[0];Object.assign(high,{enabled:true,cost:100,k:3,cd:20,cdRatio:1,priority:9});setCombat(s,{startPercent:25,regen:15,duration:14,policy:'efficiency'});const r=C.run(s);assert.ok(r.autoCount>0);assert.ok(r.skills[0].count>0);});
test('Efficiency strategy still casts skills weaker per mana than sustainable autos',()=>{const s=combatBase();const x=onlySkill(s);x.k=1;setCombat(s,{autoCost:1,regen:10,policy:'efficiency'});const r=C.run(s);assert.ok(r.skills[0].count>0);near(r.autoCount,18);assert.equal(r.events.some(e=>e.text==='普攻缺蓝，等待回复'),false);ledger(r);});
test('Efficiency strategy protects autos until the end before funding skills',()=>{const s=combatBase();onlySkill(s);setCombat(s,{autoCost:10,policy:'efficiency'});const eff=C.run(s);assert.equal(eff.skills[0].count,0);near(eff.autoCount,20);s.combat.policy='flow';const flow=C.run(s);assert.ok(flow.skills[0].count>0);assert.ok(eff.dps>flow.dps);ledger(eff);});
test('Efficiency strategy spends only the surplus beyond the auto reserve',()=>{const s=combatBase();onlySkill(s);setCombat(s,{autoCost:10,duration:5,policy:'efficiency'});const r=C.run(s);near(r.skills[0].count,1);near(r.autoCount,8);near(r.endMana,20);ledger(r);});
test('Efficiency strategy skips skills with no net gain over autos',()=>{const s=combatBase();const x=onlySkill(s);x.k=0;s.combat.policy='efficiency';const r=C.run(s,true);assert.equal(r.skills[0].count,0);near(r.autoCount,20);});
test('Adding an unpayable skill does not invent extra damage',()=>{const s=combatBase();s.combat.startPercent=0;s.basicAttacks[0].cost=0;const a=C.run(s);onlySkill(s);near(C.run(s).dps,a.dps);});
test('Regen and potion settings do not alter unlimited reference output',()=>{const s=B.defaults();const a=C.run(s,true);s.combat.potions=0;s.combat.regenMode='manual';s.combat.regen=0;near(C.run(s,true).dps,a.dps);});
test('Total damage equals its contributors',()=>{const s=B.defaults();s.e.proc=123;const r=C.run(s);near(r.totalDamage,r.autoDamage+r.skillDamage+r.procDamage);near(r.dps*r.T,r.totalDamage);ledger(r);});
test('Simulation does not mutate input',()=>{const s=B.defaults(),before=JSON.stringify(s);C.compare(s);assert.equal(JSON.stringify(s),before);});
test('Deterministic resource conservation across varied configurations',()=>{for(let n=0;n<16;n++){const s=B.defaults();s.combat.duration=30+n*3;s.combat.potions=n%6;s.combat.hurtTotal=n*35;s.extraMana=n*20;s.combat.startPercent=20+n*5;s.combat.policy=n%2?'flow':'save';s.skills[n%7].enabled=true;const r=C.run(s);ledger(r);assert.ok(r.usedPotions<=s.combat.potions);assert.ok(r.samples.every(x=>x.mana>=-1e-7&&x.mana<=r.maxMana+1e-7));}});

test('Nebula unbuffed armor has 26 damage,16 crit,60 mana and 46 defense',()=>{const s=naked();s.e.armor='nebula';s.e.nebula={damage:0,mana:0,life:0};const g=B.gear(s,'e');near(g.damage,26);near(g.crit,20);near(g.mana,60);near(g.armorDefense,46);near(g.manaReduction,15);near(g.moveSpeed,10);});
test('Full Nebula buffs add 45 damage,30 mana/s and 9 life/s',()=>{const s=naked();s.e.armor='nebula';const g=B.gear(s,'e');near(g.damage,71);near(g.nebulaDamage,45);near(g.nebulaManaRegen,30);near(g.nebulaLifeRegen,9);near(g.crit,20);});
test('Nebula damage is additive to equipment, not a separate 45% multiplier',()=>{const s=naked();s.v.armor='nebula';s.v.damage=20;const g=B.gear(s,'v');near(g.damage,91);near(g.multiplier,1.91*1.2/1.04);});
test('Independent Nebula stack counts apply per buff and per player',()=>{const s=naked();s.v.armor='nebula';s.e.armor='nebula';s.e.nebula={damage:1,mana:2,life:0};const e=B.gear(s,'e'),v=B.gear(s,'v');near(e.damage,41);near(e.nebulaManaRegen,20);near(e.nebulaLifeRegen,0);near(v.damage,71);});
test('Removing Nebula armor disables its stored buffs and mana bridge',()=>{const s=B.defaults('nebula');s.nebulaManaBridge=true;s.e.armor='none';const r=C.run(s);near(B.gear(s,'e').nebulaDamage,0);near(r.nebulaRegen,0);near(r.regen,5+r.maxMana*.02);});
test('Current-code preset does not silently restore special mana',()=>{const s=B.defaults('nebula');assert.equal(s.nebulaManaBridge,false);const r=C.run(s);near(r.nebulaRegen,0);near(r.regen,5+r.maxMana*.02);});
test('Optional Nebula bridge adds exactly 30/s to special regeneration',()=>{const s=B.defaults('nebula'),a=C.run(s);s.nebulaManaBridge=true;const r=C.run(s);near(r.regen,a.regen+30);near(r.nebulaRegen,30);near(B.calculate(s).recovery,r.regen);near(r.maxMana,a.maxMana);ledger(r);});
test('Manual base regen plus Nebula is added exactly once',()=>{const s=B.defaults('nebula');s.nebulaManaBridge=true;s.combat.regenMode='manual';s.combat.regen=7;const r=C.run(s);near(r.baseRegen,7);near(r.regen,37);});
test('Nebula life buff does not create damage, mana or absorb injuries',()=>{const s=B.defaults('nebula');s.combat.hurtTotal=200;const a=C.run(s);s.e.nebula.life=0;const r=C.run(s);near(r.totalDamage,a.totalDamage);near(r.hurtPaid,a.hurtPaid);near(r.endMana,a.endMana);});
test('Nebula 15% vanilla reduction never discounts special skill or auto cost',()=>{const s=combatBase();s.e.armor='nebula';onlySkill(s);s.basicAttacks[0].cost=2;const r=C.run(s);near(r.skillMana,r.skills[0].count*100);near(r.autoMana,r.autoCount*2);});
test('Graduate preset uses KL 3500 base and full buffs on both sides',()=>{const s=B.defaults('nebula');near(s.baseDps,3500);assert.equal(s.v.armor,'nebula');assert.equal(s.e.armor,'nebula');near(B.gear(s,'v').nebulaDamage,45);near(B.gear(s,'e').nebulaLifeRegen,9);});
test('Zero mana stacks cannot grant bridge recovery',()=>{const s=B.defaults('nebula');s.nebulaManaBridge=true;s.e.nebula.mana=0;near(C.run(s).nebulaRegen,0);});
console.log('Default result:', JSON.stringify(B.calculate(B.defaults()),null,2));

function iceDesign() {const s=B.defaults();s.baseDps=1000;const ice={...B.newBasic('IceConeBasic','冰锥'),ratio:.8,cost:10,interval:.5,shots:3};s.basicAttacks.push(ice);s.selectedBasicAttackId=ice.id;return s;}
test('Ice basic: 0.5 seconds / 3 shots / 10 mana distributes one DPS budget',()=>{const s=iceDesign(),r=B.basicBudget(s);near(r.dps,800);near(r.total,400);near(r.perShot,400/3);near(r.manaPerSecond,20);});
test('More shots never multiply the basic attack total damage',()=>{const s=iceDesign();s.basicAttacks[1].shots=6;near(B.basicBudget(s).total,400);near(B.basicBudget(s).perShot,400/6);});
test('Basic mana cost is independent of DPS pricing',()=>{const s=iceDesign();s.basicAttacks[1].cost=30;near(B.basicBudget(s).dps,800);near(B.basicBudget(s).manaPerSecond,60);});
test('Independent basic DPS and interval edits',()=>{const s=iceDesign();Object.assign(s.basicAttacks[1],{ratio:.6,interval:1});near(B.basicBudget(s).dps,600);near(B.basicBudget(s).total,600);near(s.basicAttacks[0].ratio,.6);near(s.basicAttacks[0].cost,3);near(s.basicAttacks[0].interval,.3);});
test('Selected basic drives simulation once and preserves MagicMissile config',()=>{const s=iceDesign();s.skills=[];setCombat(s,{duration:10,regenMode:'manual',regen:0,startPercent:100,potions:0});s.baseMana=1000;const r=C.run(s);near(r.autoCount,20);near(r.autoMana,200);near(r.autoDamage,800*B.gear(s,'e').multiplier*10);near(s.basicAttacks[0].cost,3);});
test('Basic IDs are unique across all skill categories',()=>{const s=iceDesign();assert.ok(B.validBasics(s));s.skills[0].id='IceConeBasic';assert.equal(B.validBasics(s),false);});
test('Invalid basic counts, bounds and selection are rejected',()=>{for(const value of [0,1.5,101,NaN]){const s=iceDesign();s.basicAttacks[1].shots=value;assert.equal(B.validBasics(s),false);}const s=iceDesign();s.selectedBasicAttackId='missing';assert.equal(B.validBasics(s),false);});
test('Generic basic entries are the sole runtime source',()=>{const s=B.defaults();s.basicAttacks[0].ratioCurve=[];s.basicAttacks[0].ratio=.6;s.basicAttacks[0].cost=7;s.basicAttacks[0].interval=.4;near(B.basicBudget(s).dps,s.baseDps*.6);near(B.selectedBasic(s).cost,7);near(B.selectedBasic(s).interval,.4);});
test('Missile progression has two upgrades per stage excluding learned level',()=>{const r=B.newBasic();assert.deepEqual(Array.from(B.progressionNodes(r,5,15)),[1,3,5,8,10,13,15]);near(B.skillLevelAt(r,5),3);near(B.skillLevelAt(r,10),5);});
test('Ice phase cap and first level are distinct',()=>{const r={...B.newBasic(),unlockStageCap:10,unlockLevel:6};assert.deepEqual(Array.from(B.progressionNodes(r,5,15)),[6,8,10,13,15]);near(B.skillLevelAt(r,5),0);near(B.skillLevelAt(r,8),2);r.unlockLevel=10;assert.deepEqual(Array.from(B.progressionNodes(r,5,15)),[10,13,15]);});
test('Progression does not duplicate integer gates or truncate at 64 skills',()=>{const r={...B.newBasic(),levelsPerPhase:10};const nodes=B.progressionNodes(r,1,1000);assert.equal(new Set(nodes).size,nodes.length);near(B.skillLevelAt(r,1000,1),1000);});
test('Level curve and rounded damage expose weak early upgrades',()=>{const s=B.defaults(),r=s.basicAttacks[0];near(B.levelDps(1),22);near(B.levelDps(3),26);near(B.levelDps(5),30);near(B.levelDps(10),50);near(B.levelDps(1000),3500);assert.deepEqual([1,3,5].map(l=>B.progressionBudget(s,r,l).integerHit),[6,7,8]);near(B.progressionBudget(s,r,1).ratio,1);near(B.progressionBudget(s,r,50).ratio,.6);assert.equal(B.progressionBudget(s,s.skills[0],1).ratio,null);});
test('Progression preview does not alter current pricing or runtime simulation',()=>{const s=B.defaults(),before=B.basicBudget(s).dps;B.progressionBudget(s,s.basicAttacks[0],1);near(B.basicBudget(s).dps,before);});
function ladderBase(regen){
 const s=combatBase();setCombat(s,{duration:100,startPercent:0,regen});
 [[100,10],[100,20],[60,2]].forEach(([cost,cd],i)=>Object.assign(s.skills[i],{enabled:true,cost,cd,k:1,cdRatio:1,cast:.1,delay:0,duration:0,maxCharges:1,initialCharges:1,first:0,priority:i+1,hit:100}));
 return s;
}
test('Mana ladder pours skill mana by value per mana and stops at the marginal skill',()=>{const L=C.ladder(ladderBase(30));assert.deepEqual(Array.from(L.rows,x=>x.value),[20,10,2]);near(L.income,30);near(L.demand,45);assert.deepEqual(Array.from(L.rows,x=>x.fill),[1,1,.5]);assert.equal(L.marginal,L.rows[2]);near(L.overflow,0);});
test('Mana ladder reports overflow when every skill is fully funded',()=>{const L=C.ladder(ladderBase(50));assert.equal(L.marginal,null);near(L.overflow,5);});
test('Mana ladder income subtracts autos and injuries and spreads the opening pool and potions',()=>{const s=ladderBase(30);setCombat(s,{startPercent:100,potions:2,potionPercent:50,autoCost:5,autoInterval:.5,hurtMode:'uniform',hurtTotal:200});const L=C.ladder(s);near(L.income,30-10-2+(200+200)/100);});
test('Mana ladder skips unaffordable or non-positive skills when pouring',()=>{const s=ladderBase(30);s.skills[1].cost=201;s.skills[0].hit=0;const L=C.ladder(s);assert.equal(L.rows.find(x=>x.index===1).usable,false);assert.equal(L.rows.find(x=>x.index===0).positive,false);near(L.rows.find(x=>x.index===2).fill,1);});
test('Break-even line separates arcane wins from damage-prefix wins for a never-saturating sink',()=>{
 // Ashen Witch off: V is a one-swap linearisation at k=10, and the extra 200 mana pool widens its gap from the true break-even.
 const sink=(v)=>{const s=B.defaults();s.ashenWitch=false;s.skills.forEach(x=>x.enabled=false);Object.assign(s.skills[0],{enabled:true,cost:20,cd:1,k:v,cdRatio:1,cast:.1,delay:0,duration:0,maxCharges:1,initialCharges:1,first:0,priority:1,hit:100});setCombat(s,{duration:300,potions:0,regenMode:'mod',regenPercent:2,policy:'efficiency',hurtMode:'uniform',hurtTotal:0});return s;};
 const ratio=(v)=>{const a=sink(v),m=B.clone(a);B.applyDamagePrefixes(m,'e',B.equippedSlots(m,'e').filter(i=>m.e.items[i].prefix==='arcane'));return C.run(a).dps/C.run(m).dps;};
 const V=C.ladder(sink(10)).threshold;assert.ok(Number.isFinite(V)&&V>0);assert.ok(ratio(V*.5)<1);assert.ok(ratio(V*1.5)>1);
 const all=(s,prefix)=>{const c=B.clone(s);if(prefix==='damage')B.applyDamagePrefixes(c,'e');else c.e.items.slice(0,c.slots).forEach(x=>{if(x.id!=='empty')x.prefix=prefix;});return C.run(c).dps;};
 const D=C.prefixDuel(sink(V*1.5)),slots=sink(1).e.items.slice(0,sink(1).slots).filter(x=>x.id!=='empty').length;
 assert.equal(D.slots,slots);assert.equal(D.menacingCount+D.luckyCount,slots);near(D.lead,all(sink(V*1.5),'arcane')/all(sink(V*1.5),'damage')-1);near(D.diff,D.arcane.dps-D.damage.dps);assert.ok(C.prefixDuel(sink(V*.5)).lead<0);
});
test('Damage prefix picks menacing or lucky from current damage and capped crit',()=>{
 const s=naked();s.e.items[0]={id:'utility',prefix:'none'};
 s.e.damage=50;assert.equal(B.damagePrefixStep(s,'e').prefix,'lucky');near(B.damagePrefixStep(s,'e').ratio,1.08/1.04);
 s.e.damage=0;assert.equal(B.damagePrefixStep(s,'e').prefix,'menacing');near(B.damagePrefixStep(s,'e').ratio,1.04);
 s.e.damage=0;s.e.crit=0;s.baseCrit=0;assert.equal(B.damagePrefixStep(s,'e').prefix,'menacing','A tie keeps menacing');
 s.e.damage=200;s.e.crit=96;s.baseCrit=4;assert.equal(B.damagePrefixStep(s,'e').prefix,'menacing','Crit capped at 100 gains nothing from lucky');
 s.e.damage=300;s.e.crit=94;assert.equal(B.damagePrefixStep(s,'e').prefix,'lucky','Lucky is clipped at the crit cap');near(B.damagePrefixStep(s,'e').ratio,2/1.98);
});
test('All-damage assignment matches the best menacing/lucky mix and ignores empty or hidden slots',()=>{
 for(const [armor,damage,crit,items] of [['hallowed',0,0,['sorcerer','avenger','celestial','utility','utility','utility']],['spectreDarkArtist',0,0,['utility','utility','utility','utility','utility','utility']],['meteor',0,0,['sorcerer','avenger','utility','utility','utility','utility']],['none',0,80,['utility','utility','utility','utility','utility','utility']]]){
  const s=naked();s.slots=6;s.e.armor=armor;s.e.damage=damage;s.e.crit=crit;items.forEach((id,i)=>{s.e.items[i]={id,prefix:'arcane'};});s.e.items[2].id='empty';s.e.items[6]={id:'utility',prefix:'warding'};
  const picks=B.applyDamagePrefixes(s,'e'),slots=B.equippedSlots(s,'e');
  assert.equal(picks.length,5);assert.equal(s.e.items[2].prefix,'arcane');assert.equal(s.e.items[6].prefix,'warding');
  let best=0;for(let mask=0;mask<1<<slots.length;mask++){const c=B.clone(s);slots.forEach((i,n)=>{c.e.items[i].prefix=mask>>n&1?'lucky':'menacing';});best=Math.max(best,B.gear(c,'e').multiplier);}
  near(B.gear(s,'e').multiplier,best);
 }
 const mixed=naked();mixed.e.crit=10;mixed.e.items.forEach(x=>{x.id='utility';});const picks=B.applyDamagePrefixes(mixed,'e');
 assert.ok(picks.includes('menacing')&&picks.includes('lucky'),'Balanced damage and crit mix both prefixes');
});
test('Ladder break-even uses the damage prefix that the current gear prefers',()=>{
 const s=ladderBase(30),L=C.ladder(s),step=B.damagePrefixStep(s,'e');
 assert.equal(L.damagePrefix,step.prefix);const a=C.run(s);near(L.damageGain,(a.autoDamage+a.skillDamage)/a.T*(step.ratio-1));
});
test('Prefix duel leaves the page loadout untouched and skips empty slots',()=>{
 const s=ladderBase(30);s.e.items[0].id='empty';s.e.items[1].prefix='warding';const before=JSON.stringify(s);
 const D=C.prefixDuel(s);assert.equal(JSON.stringify(s),before);assert.equal(D.slots,s.e.items.slice(0,s.slots).filter(x=>x.id!=='empty').length);
});
console.log(`\n${count} model/static tests passed.`);
async function browserTests(){
 // Isolated disposable browser for this local HTML; no user browser profile or session is used.
 const runtime = process.env.CODEX_NODE_MODULES || path.join(require('node:os').homedir(),'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
 const {chromium}=require(path.join(runtime,'playwright'));
 const browser=await chromium.launch({headless:true, channel:process.env.BALANCE_BROWSER_CHANNEL || 'chrome'});
 try {
  fs.mkdirSync(path.join(__dirname,'test-output'),{recursive:true});
  const page=await browser.newPage({viewport:{width:1440,height:1100},deviceScaleFactor:1});
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  const resetPage=async(accept=true)=>{
   await page.locator('#moreActions summary').click();
   page.once('dialog',dialog=>accept?dialog.accept():dialog.dismiss());
   await page.locator('#reset').click();
   assert.equal(await page.locator('#moreActions').evaluate(el=>el.open),false);
  };
  await page.goto(pathToFileURL(htmlPath).href);
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
  await page.locator('#baseDps').fill('1234');
  await resetPage(false);
  assert.equal(await page.locator('#baseDps').inputValue(),'1234','Cancel reset preserves edits');
  await page.screenshot({path:path.join(__dirname,'test-output','toolbar-desktop.png'),clip:{x:0,y:0,width:1440,height:260}});
  await page.setViewportSize({width:390,height:844});
  await page.locator('#moreActions summary').click();
  await page.screenshot({path:path.join(__dirname,'test-output','toolbar-mobile.png')});
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
  await page.keyboard.press('Escape');
  await page.setViewportSize({width:1440,height:1100});
  await resetPage();

  const progressionPanel=page.locator('#progressionPanel');
  const progressionToggle=progressionPanel.locator('summary');
  assert.equal(await progressionPanel.evaluate(el=>el.open),false,'Progression preview starts collapsed');
  assert.equal(await page.locator('#progressionPhaseSize').isVisible(),false);
  const collapsedHeight=(await progressionPanel.boundingBox()).height;
  await progressionPanel.screenshot({path:path.join(__dirname,'test-output','progression-collapsed.png')});
  await progressionToggle.click();
  assert.equal(await progressionPanel.evaluate(el=>el.open),true);
  assert.ok((await progressionPanel.boundingBox()).height>collapsedHeight+200,'Collapsing must reclaim preview space');

  // Configurable skill progression: two upgrades per five-level phase for small attacks.
  await page.getByRole('button',{name:'添加冰锥示例',exact:true}).click();
  assert.equal(await page.locator('#progressionRows tr').count(),B.defaults().skills.length+2);
  assert.equal(await page.getByRole('spinbutton',{name:'冰锥普攻首级角色等级',exact:true}).inputValue(),'6');
  assert.equal(await page.getByRole('spinbutton',{name:'冰锥普攻每阶段新增技能等级',exact:true}).inputValue(),'2');
  const progressionText=await page.locator('#progressionRows tr').filter({hasText:'冰锥普攻'}).innerText();
  assert.match(progressionText,/Lv1→6、Lv2→8、Lv3→10/);
  assert.match(progressionText,/Lv3（角色 10）/);
  await page.getByRole('spinbutton',{name:'魔法飞弹首级角色等级',exact:true}).fill('1');
  await page.getByRole('spinbutton',{name:'魔法飞弹每阶段新增技能等级',exact:true}).fill('2');
  const missileProgression=await page.locator('#progressionRows tr').filter({hasText:'魔法飞弹'}).innerText();
  assert.match(missileProgression,/Lv1→1、Lv2→3、Lv3→5/);
  assert.match(missileProgression,/Lv5（角色 10）/);
  // Unlock period shortcut writes cap/first level; switching preset enables only unlocked skills.
  assert.equal(await page.getByRole('combobox',{name:'冰锥普攻解锁时期',exact:true}).inputValue(),'2');
  await page.getByRole('combobox',{name:'水球魔法解锁时期',exact:true}).selectOption('8');
  assert.equal(await page.getByRole('spinbutton',{name:'水球魔法解锁阶段上限',exact:true}).inputValue(),'40');
  assert.equal(await page.getByRole('spinbutton',{name:'水球魔法首级角色等级',exact:true}).inputValue(),'36');
  await page.getByRole('spinbutton',{name:'水球魔法解锁阶段上限',exact:true}).fill('42');
  assert.equal(await page.getByRole('combobox',{name:'水球魔法解锁时期',exact:true}).inputValue(),'0');
  await page.getByRole('combobox',{name:'水球魔法解锁时期',exact:true}).selectOption('8');
  const enabledSkills=()=>page.locator('#rotationRows input[data-skill-field="enabled"]').evaluateAll(xs=>xs.map(x=>x.checked));
  await page.locator('#preset').selectOption('6');
  assert.equal(JSON.stringify(await enabledSkills()),JSON.stringify([false,true,true,true,true,true,true]));
  assert.equal(await page.locator('#progressionPreviewLevel').inputValue(),'30');
  assert.match(await page.locator('#status').innerText(),/角色 30 级.*停用未解锁技能（水球魔法）/);
  await page.locator('#preset').selectOption('8');
  assert.ok((await enabledSkills()).every(Boolean));
  await page.getByRole('combobox',{name:'水球魔法解锁时期',exact:true}).selectOption('1');
  await page.locator('#progressionPanel').screenshot({path:path.join(__dirname,'test-output','progression-design.png')});
  assert.equal(await progressionPanel.evaluate(el=>el.open),true,'Editing must not close the preview');
  await progressionToggle.focus();
  await page.keyboard.press('Enter');
  assert.equal(await page.locator('#progressionPhaseSize').isVisible(),false);
  await page.setViewportSize({width:390,height:844});
  await progressionPanel.screenshot({path:path.join(__dirname,'test-output','progression-collapsed-mobile.png')});
  await progressionToggle.click();
  assert.equal(await page.locator('#progressionPhaseSize').isVisible(),true);
  assert.equal(await page.getByRole('spinbutton',{name:'魔法飞弹每阶段新增技能等级',exact:true}).inputValue(),'2');
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>window.innerWidth),false);
  await progressionPanel.screenshot({path:path.join(__dirname,'test-output','progression-expanded-mobile.png')});
  await progressionToggle.click();
  await page.setViewportSize({width:1440,height:1100});
  await page.reload();
  assert.equal(await progressionPanel.evaluate(el=>el.open),false,'Reload returns to compact default');
  await resetPage();

  // Independent ordinary-attack design, persistence, deletion and backward migration.
  await page.locator('#baseDps').fill('1000');
  await page.getByRole('button',{name:'添加冰锥示例',exact:true}).click();
  assert.equal(await page.locator('#basicRows tr').count(),2);
  assert.equal(await page.locator('#basicDps1').innerText(),'800');
  assert.equal(await page.locator('#basicTotal1').innerText(),'400');
  assert.equal(await page.locator('#basicHit1').innerText(),'133.33');
  assert.equal(await page.locator('#basicMana1').innerText(),'20');
  await page.getByRole('spinbutton',{name:'冰锥普攻整次耗蓝',exact:true}).fill('4.5');
  assert.equal(await page.locator('#basicMana1').innerText(),'9','Basic attack cost keeps decimals');
  await page.getByRole('spinbutton',{name:'冰锥普攻整次耗蓝',exact:true}).fill('10');
  await page.getByRole('spinbutton',{name:'冰锥普攻目标DPS倍率',exact:true}).fill('0.6');
  assert.equal(await page.locator('#basicDps1').innerText(),'600');
  assert.equal(await page.locator('#basicHit1').innerText(),'100');
  const basicSaved=await page.evaluate(()=>JSON.parse(localStorage.getItem('elaina-mage-balance-v2')));
  near(basicSaved.basicAttacks[0].ratio,.6);assert.equal(basicSaved.basicAttacks[0].ratioCurve.length,5);near(basicSaved.basicAttacks[0].cost,3);near(basicSaved.basicAttacks[0].interval,.3);
  await page.locator('#basicTable').evaluate(el=>el.closest('section').scrollIntoView());
  await page.locator('[aria-label="普通攻击设计"]').screenshot({path:path.join(__dirname,'test-output','basic-design.png')});
  await resetPage();
  await page.evaluate(value=>localStorage.setItem('elaina-mage-balance-v2',JSON.stringify(value)),basicSaved);
  await page.reload();
  assert.equal(await page.locator('#basicHit1').innerText(),'100');
  await page.locator('#preset').selectOption('6');
  assert.equal(await page.locator('#basicRows tr').count(),2,'Changing stage must preserve ordinary attack drafts');
  await page.locator('[data-prefix-side="v"][data-prefix-value="arcane"]').click();
  const duration=await page.locator('#battleDuration').inputValue();
  await page.locator('#battleDuration').fill(String(Number(duration)+7));
  await page.locator('#preset').selectOption('10');
  const vPrefixes=await page.locator('#vSlots select[data-prop="prefix"]').evaluateAll(xs=>xs.map(x=>x.value));
  const ePrefixes=await page.locator('#eSlots select[data-prop="prefix"]').evaluateAll(xs=>xs.map(x=>x.value));
  assert.equal(vPrefixes.length,6);assert.deepEqual(vPrefixes,Array.from(B.loadoutDefault(10).items.slice(0,6),x=>x.vPrefix),'Vanilla prefixes come from the stage loadout');
  assert.deepEqual(ePrefixes,Array.from(B.loadoutDefault(10).items.slice(0,6),x=>x.prefix),'Elaina prefixes come from the stage loadout');
  assert.equal(await page.locator('#eArmor').inputValue(),'adamantite');
  assert.equal(await page.locator('#battleDuration').inputValue(),String(Number(duration)+7),'Changing gear preset must keep combat settings');
  assert.equal(await page.locator('#baseDps').inputValue(),'450');
  await page.locator('#preset').selectOption('6');
  await page.locator('#baseDps').fill('1000');

  await page.reload();
  assert.equal(await page.locator('#basicHit1').innerText(),'100');
  assert.equal(await page.getByRole('radio',{name:'使用冰锥普攻作为参考普攻'}).isChecked(),true);
  await page.getByRole('button',{name:'添加普通攻击',exact:true}).click();
  assert.equal(await page.locator('#basicRows tr').count(),3);
  await page.getByRole('button',{name:'删除普攻新普攻 1',exact:true}).click();
  assert.equal(await page.getByRole('radio',{name:'使用魔法飞弹作为参考普攻'}).isChecked(),true);
  await page.getByRole('button',{name:'删除普攻冰锥普攻',exact:true}).click();
  await resetPage();

  // Missile ratio curve: editable text, fixed ratio disabled while a curve exists, chart drawn, legacy saves migrate.
  await page.locator('#baseDps').fill('900');
  const curveBox=page.getByRole('textbox',{name:'魔法飞弹倍率曲线',exact:true});
  const missileRatio=page.getByRole('spinbutton',{name:'魔法飞弹目标DPS倍率',exact:true});
  assert.equal(await curveBox.inputValue(),'1:1, 10:0.9, 20:0.8, 35:0.7, 50:0.6');
  assert.equal(await missileRatio.isDisabled(),true);
  assert.equal(await page.locator('#basicDps0').innerText(),'540');
  assert.match(await page.locator('#basicSummary').innerText(),/Lv65/);
  assert.ok(await page.locator('#basicCurve path, #basicCurve polyline').count()>0,'Curve chart is drawn');
  await curveBox.fill('1:1，100：0.5');await curveBox.press('Tab');
  near(Number(await page.locator('#basicDps0').innerText()),Math.round(900*(1-.5*64/99)*100)/100);
  assert.equal(await curveBox.inputValue(),'1:1, 100:0.5');
  await curveBox.fill('100:1, 5:.5');await curveBox.press('Tab');
  assert.equal(await curveBox.inputValue(),'1:1, 100:0.5','Invalid curve text restores the previous curve');
  assert.match(await page.locator('#status').innerText(),/曲线/);
  await curveBox.fill('');await curveBox.press('Tab');
  assert.equal(await missileRatio.isDisabled(),false);
  assert.equal(await page.locator('#basicDps0').innerText(),'540');
  assert.equal(errors.length,0,errors.join(' | '));
  const legacy=await page.evaluate(()=>JSON.parse(localStorage.getItem('elaina-mage-balance-v2')));
  delete legacy.occupyRatio;delete legacy.ashenWitch;delete legacy.maxLife;legacy.basicAttacks.forEach(r=>delete r.ratioCurve);legacy.baseDps=777;
  await page.evaluate(value=>localStorage.setItem('elaina-mage-balance-v2',JSON.stringify(value)),legacy);
  await page.reload();
  assert.equal(await page.locator('#baseDps').inputValue(),'777','Saves without curve or occupyRatio still load');
  assert.equal(await page.locator('#occupyRatio').inputValue(),'0');
  assert.equal(await page.locator('#ashenWitch').isChecked(),true,'Saves without the Ashen Witch switch default to on');
  assert.equal(await page.locator('#maxLife').inputValue(),String(legacy.loadouts[legacy.stage-1].maxLife),'Missing max life falls back to the stage loadout');
  assert.equal(await page.getByRole('textbox',{name:'魔法飞弹倍率曲线',exact:true}).inputValue(),'');
  await page.locator('#occupyRatio').fill('1');
  near((await page.evaluate(()=>JSON.parse(localStorage.getItem('elaina-mage-balance-v2')))).occupyRatio,1);
  await resetPage();

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
  assert.equal(savedDirect.configuration.autoRatio,undefined);
  assert.equal(savedDirect.configuration.loadouts.length,19,'Loadouts are saved with the mod JSON');
  assert.equal(savedDirect.configuration.preset,undefined);assert.equal(savedDirect.configuration.hurtMana,undefined);
  assert.deepEqual(Object.keys(savedDirect).sort(),['configuration','schema'],'Save contains parameters, not calculated logs');
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
  await page.locator('#baseDps').fill('1234');
  await page.getByRole('button',{name:'重新读取',exact:true}).click();
  await page.waitForFunction(()=>document.getElementById('baseDps').value!=='1234');
  assert.equal(await page.locator('#baseDps').inputValue(),String(savedDirect.configuration.baseDps));
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
  await page.evaluate(()=>{window.testFile.fail=false;});

  await page.evaluate(()=>{window.testFile.denied=false;window.testFile.name='wrong.json';});
  await page.getByRole('button',{name:'更换…',exact:true}).click();
  await page.waitForFunction(()=>document.getElementById('fileStatus').textContent.includes('请选择模组'));
  assert.match(await page.locator('#fileStatus').innerText(),/请选择模组/);
  await page.evaluate(()=>{window.showOpenFilePicker=async()=>{throw new DOMException('cancel','AbortError');};});
  await page.getByRole('button',{name:'更换…',exact:true}).click();
  assert.match(await page.locator('#fileStatus').innerText(),/已取消/);

  // Bulk prefixes change only the requested side's active, occupied slots.
  const values=async(side,prop)=>page.locator('#'+side+'Slots select[data-prop="'+prop+'"]').evaluateAll(els=>els.map(el=>el.value));
  const initialVItems=await values('v','id'),initialEItems=await values('e','id');
  const initialEPrefixes=await values('e','prefix');
  const isDamage=v=>v==='menacing'||v==='lucky';
  // Stage defaults already give vanilla full damage prefixes late game, so start from arcane to see the change.
  await page.getByRole('button',{name:'原版法师全奥秘',exact:true}).click();
  const beforePrefixDps=await page.locator('#simVanilla').innerText();
  await page.getByRole('button',{name:'原版法师全增伤',exact:true}).click();
  assert.ok((await values('v','prefix')).every(isDamage));
  assert.match(await page.locator('#vPrefixStatus').innerText(),/增伤（险恶 \d+ 个、幸运 \d+ 个/);
  assert.deepEqual(await values('e','prefix'),initialEPrefixes);
  assert.notEqual(await page.locator('#simVanilla').innerText(),beforePrefixDps);
  await page.getByRole('button',{name:'原版法师全奥秘',exact:true}).click();
  assert.ok((await values('v','prefix')).every(v=>v==='arcane'));
  await page.getByRole('button',{name:'伊蕾娜全增伤',exact:true}).click();
  assert.ok((await values('e','prefix')).every(isDamage));
  const beforeArcane=await page.locator('#simCapacity').innerText();
  await page.getByRole('button',{name:'伊蕾娜全奥秘',exact:true}).click();
  assert.ok((await values('e','prefix')).every(v=>v==='arcane'));
  assert.notEqual(await page.locator('#simCapacity').innerText(),beforeArcane);
  assert.deepEqual(await values('v','id'),initialVItems);
  assert.deepEqual(await values('e','id'),initialEItems);
  await page.reload();
  assert.ok((await values('v','prefix')).every(v=>v==='arcane'));
  assert.ok((await values('e','prefix')).every(v=>v==='arcane'));
  await page.locator('#slots').selectOption('7');
  await page.locator('#eSlots select[data-prop="prefix"]').nth(6).selectOption('lucky');
  await page.locator('#slots').selectOption('5');
  await page.locator('#eSlots select[data-prop="id"]').first().selectOption('empty');
  await page.locator('#eSlots select[data-prop="prefix"]').first().selectOption('warding');
  await page.getByRole('button',{name:'伊蕾娜全增伤',exact:true}).click();
  const damagePicks=await values('e','prefix');
  assert.equal(damagePicks[0],'warding');assert.ok(damagePicks.slice(1).every(isDamage));
  assert.match(await page.locator('#ePrefixStatus').innerText(),/4 个/);
  await page.locator('#slots').selectOption('7');
  assert.deepEqual((await values('e','prefix')).slice(0,5),damagePicks,'Opening slots keeps the visible damage prefixes');
  assert.ok((await values('e','prefix')).slice(5).every(isDamage),'All-damage visible prefixes extend to newly opened slots');
  await page.locator('#eSlots select[data-prop="prefix"]').nth(6).selectOption('lucky');
  await page.locator('#slots').selectOption('6');
  await page.locator('#eSlots select[data-prop="prefix"]').nth(1).selectOption('warding');
  await page.locator('#slots').selectOption('7');
  assert.equal((await values('e','prefix'))[6],'lucky','Mixed visible prefixes keep the stored prefix of reopened slots');
  await resetPage();

  // Graduate preset, three independent buff stacks and explicit special-mana adaptation.
  await page.locator('#preset').selectOption('19');
  assert.equal(await page.locator('#baseDps').inputValue(),'3500');
  assert.ok(await page.locator('#vNebula').isVisible());
  assert.ok(await page.locator('#eNebula').isVisible());
  assert.equal(await page.locator('#vNebulaDamage').inputValue(),'3');
  assert.equal(await page.locator('#eNebulaMana').inputValue(),'3');
  assert.equal(await page.locator('#nebulaManaBridge').isChecked(),false);
  assert.match(await page.locator('#eNebulaInfo').innerText(),/\+45% 加算伤害、\+30 原版魔力\/秒、\+9 生命\/秒/);
  const fullNebulaDps=await page.locator('#simVanilla').innerText();
  await page.locator('#vNebulaDamage').selectOption('0');
  assert.notEqual(await page.locator('#simVanilla').innerText(),fullNebulaDps);
  assert.equal(await page.locator('#eNebulaDamage').inputValue(),'3');
  await page.getByRole('button',{name:'原版法师星云强化全满',exact:true}).click();
  assert.equal(await page.locator('#simVanilla').innerText(),fullNebulaDps);
  const beforeBridge=await page.locator('#simCapacity').innerText();
  await page.locator('#nebulaManaBridge').check();
  assert.notEqual(await page.locator('#simCapacity').innerText(),beforeBridge);
  assert.match(await page.locator('#simCapacity').innerText(),/星云 30/);
  await page.locator('#eNebulaMana').selectOption('1');
  assert.match(await page.locator('#simCapacity').innerText(),/星云 10/);
  await page.reload();
  assert.equal(await page.locator('#nebulaManaBridge').isChecked(),true);
  assert.equal(await page.locator('#eNebulaMana').inputValue(),'1');
  await page.getByRole('button',{name:'伊蕾娜星云强化全满',exact:true}).click();
  await page.locator('#eNebula').screenshot({path:path.join(__dirname,'test-output','nebula-buffs.png')});
  await page.setViewportSize({width:390,height:844});
  await page.locator('#eNebula').screenshot({path:path.join(__dirname,'test-output','nebula-mobile.png')});
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>window.innerWidth),false);
  await page.setViewportSize({width:1440,height:1100});
  await page.locator('#eArmor').selectOption('hallowed');
  assert.equal(await page.locator('#eNebula').isVisible(),false);
  assert.doesNotMatch(await page.locator('#simCapacity').innerText(),/星云/);
  await page.locator('#vNebulaLife').selectOption('1');
  await page.getByRole('button',{name:'复制左侧装备 · 前缀转奥秘'}).click();
  assert.equal(await page.locator('#eNebulaLife').inputValue(),'1');
  await resetPage();
  // Exercise the new resource-limited simulation before the legacy-budget regression suite.
  const initialSim=await page.locator('#simActual').innerText();
  const initialIdeal=await page.locator('#simIdeal').innerText();
  // Stage 13 has 500 max life: Ashen Witch adds 200 special mana, so the default fight only becomes potion-bound without it.
  assert.equal(await page.locator('#ashenWitch').isChecked(),true);
  assert.match(await page.locator('#eStageInfo').innerText(),/帽子 20 \+ 灰之魔女 200（最大生命 500 → 300）/);
  await page.locator('#ashenWitch').uncheck();
  assert.match(await page.locator('#eStageInfo').innerText(),/灰之魔女未开启/);
  await page.locator('#battlePotions').fill('0');
  assert.notEqual(await page.locator('#simActual').innerText(),initialSim);
  assert.equal(await page.locator('#simIdeal').innerText(),initialIdeal);
  await page.locator('#ashenWitch').check();
  assert.equal(await page.locator('#simActual').innerText(),initialSim,'Ashen Witch mana covers the missing potions');
  await page.locator('#ashenWitch').uncheck();
  assert.equal(await page.locator('#simPotionEnd').innerText(),'未携带');
  await page.getByRole('button',{name:'长战 180 秒',exact:true}).click();
  assert.equal(await page.locator('#battleDuration').inputValue(),'180');
  await page.locator('#battleRegenMode').selectOption('manual');
  await page.locator('#battleRegen').fill('0');
  await page.locator('#battleStart').fill('0');
  assert.equal(await page.locator('#simActual').innerText(),'0');
  await page.locator('#battlePolicy').selectOption('save');
  await resetPage();
  await page.getByText('受伤损蓝：均匀损失、周期受击或指定时间',{exact:true}).click();
  // Large enough to bind even with the all-arcane stage-13 default.
  await page.locator('#battleHurtTotal').fill('5000');
  assert.notEqual(await page.locator('#simActual').innerText(),initialSim);
  await page.locator('#battleHurtMode').selectOption('events');
  await page.locator('#battleHurtEvents').fill('0:380\nbad\n150:20');
  assert.match(await page.locator('#simWarnings').innerText(),/格式错误/);
  await page.locator('#battleHurtMode').selectOption('periodic');
  assert.ok(await page.locator('#battleHurtInterval').isVisible());
  await page.locator('#battleHurtPerHit').fill('50');
  await page.locator('#battleHurtInterval').fill('20');
  await page.getByRole('checkbox',{name:'模拟启用冰凌锁',exact:true}).check();
  await page.getByRole('spinbutton',{name:'模拟冰凌锁蓝耗',exact:true}).fill('900');
  assert.match(await page.locator('#simWarnings').innerText(),/无法支付/);
  assert.match(await page.locator('#ladderRows').innerText(),/冰凌锁[\s\S]*无法释放/);
  assert.match(await page.locator('#ladderSummary').innerText(),/技能可用蓝/);
  assert.match(await page.locator('#ladderSummary').innerText(),/奥秘 vs 增伤[\s\S]*全增伤（险恶 \d+、幸运 \d+）[\s\S]*奥秘(领先|落后) [\d.]+%/);
  await page.getByRole('spinbutton',{name:'模拟水球魔法CD',exact:true}).fill('30');
  await page.getByRole('spinbutton',{name:'模拟水球魔法命中',exact:true}).fill('50');
  await page.reload();
  assert.equal(await page.getByRole('spinbutton',{name:'模拟水球魔法CD',exact:true}).inputValue(),'30');
  assert.equal(await page.locator('#battleHurtMode').inputValue(),'periodic');
  await resetPage();
  await page.locator('#slots').selectOption('7');
  assert.equal(await page.locator('#eSlots .slot').count(),7);
  await page.getByRole('button',{name:'复制左侧装备 · 前缀转奥秘'}).click();
  assert.equal(await page.locator('#eSlots select[data-prop="prefix"]').first().inputValue(),'arcane');
  await page.reload();
  assert.equal(await page.locator('#slots').inputValue(),'7');
  await page.locator('#preset').selectOption('18');
  assert.equal(await page.locator('#baseDps').inputValue(),'1800');
  assert.match(await page.locator('#vSummary').innerText(),/追击未自动计入/);
  assert.equal(await page.locator('#vSlots option[value="elainaHat"]').count(),0,'Vanilla slots cannot pick the Elaina-only hat');
  assert.equal(await page.locator('#eSlots select[data-prop="id"]').first().locator('option[value="elainaHat"]').count(),1);
  assert.equal(await page.locator('#eSlots select[data-prop="id"]').nth(3).inputValue(),'elainaHat');
  assert.equal(await page.locator('#vSlots select[data-prop="id"]').nth(3).inputValue(),'utility');
  await page.locator('#vSlots select[data-prop="id"]').nth(1).selectOption('sorcerer');
  assert.match(await page.locator('#vSummary').innerText(),/重复装备/);
  await resetPage();
  const out=path.join(__dirname,'test-output');fs.mkdirSync(out,{recursive:true});

  // Loadout & attribute page: per-stage editing, lab sync, persistence and keyboard tabs.
  await page.getByRole('tab',{name:'配装与属性配置'}).click();
  assert.equal(await page.locator('#loadoutPage').isVisible(),true);
  assert.equal(await page.locator('#labPage').isVisible(),false);
  assert.equal(await page.evaluate(()=>location.hash),'#loadouts');
  assert.equal(await page.locator('#loadoutList [data-loadout-stage]').count(),19);
  assert.equal(await page.locator('[data-loadout-stage="13"]').getAttribute('aria-current'),'true');
  assert.match(await page.locator('[data-loadout-stage="13"]').innerText(),/实验室/);
  await page.locator('[data-loadout-stage="3"]').click();
  assert.match(await page.locator('#loadoutEditor h2').innerText(),/克苏鲁之眼后/);
  assert.equal(await page.locator('#loadoutLife').inputValue(),'200','Eye of Cthulhu stage assumes 200 life for invasion events');
  assert.equal(await page.locator('#loadoutMana').inputValue(),'200');
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
  await page.getByRole('button',{name:'本阶段伊蕾娜全增伤',exact:true}).click();
  const stageDamage=(()=>{const p=B.defaults();Object.assign(p.e,{armor:'jungle'});p.slots=5;p.e.items=B.loadoutDefault(3).items;p.e.items[0].id='sorcerer';p.e.items[1].id='celestialCuffs';return Array.from(B.applyDamagePrefixes(p,'e'));})();
  assert.ok(stageDamage.includes('lucky'),'Jungle stage gear prefers lucky for some slots');
  assert.deepEqual(await page.locator('[data-loadout-prop="prefix"]').evaluateAll(els=>els.slice(0,5).map(el=>el.value)),stageDamage,'Stage all-damage uses this stage gear plus lab extras');
  assert.match(await page.locator('#loadoutList [data-loadout-stage="3"]').innerText(),/克苏鲁之眼后/);
  assert.equal(await page.locator('#eArmor').inputValue(),'hallowed','Editing another stage does not touch the lab');
  await page.getByRole('button',{name:'在实验室中查看',exact:true}).click();
  assert.equal(await page.locator('#labPage').isVisible(),true);
  assert.equal(await page.locator('#preset').inputValue(),'3');
  assert.equal(await page.locator('#eArmor').inputValue(),'jungle');
  assert.equal(await page.locator('#vArmor').inputValue(),'jungle');
  assert.equal(await page.locator('#baseMana').inputValue(),'160');
  assert.equal(await page.locator('#baseDps').inputValue(),'80');
  assert.deepEqual(await values('e','prefix'),stageDamage);
  assert.match(await page.locator('#eStageInfo').innerText(),/基础魔力 160、最大生命 410/);
  await page.getByRole('tab',{name:'配装与属性配置'}).click();
  assert.match(await page.locator('#loadoutEditor h2').innerText(),/克苏鲁之眼后/);
  await page.locator('#loadoutArmor').selectOption('meteor');
  assert.equal(await page.locator('#eArmor').inputValue(),'meteor','Editing the lab stage syncs the lab immediately');
  await page.locator('[data-loadout-stage="4"]').click();
  await page.getByRole('button',{name:'复制上一阶段',exact:true}).click();
  assert.equal(await page.locator('#loadoutArmor').inputValue(),'meteor');
  assert.equal(await page.locator('#loadoutMana').inputValue(),'160');
  assert.match(await page.locator('#loadoutName').inputValue(),/巫师帽钻石长袍/,'Copy keeps the stage name');
  await page.getByRole('button',{name:'恢复本阶段默认',exact:true}).click();
  assert.equal(await page.locator('#loadoutArmor').inputValue(),'wizardDiamond');
  await page.locator('#loadoutName').fill('');await page.locator('#loadoutName').press('Tab');
  assert.match(await page.locator('#loadoutName').inputValue(),/巫师帽钻石长袍/,'Empty names are rejected');
  await page.locator('#loadoutName').fill('<b>测试</b>');await page.locator('#loadoutName').press('Tab');
  assert.equal(await page.locator('#loadoutList b b').count(),0,'Loadout names must be escaped');
  await page.screenshot({path:path.join(out,'loadouts-desktop.png'),fullPage:true});
  await page.setViewportSize({width:390,height:844});
  await page.screenshot({path:path.join(out,'loadouts-mobile.png'),fullPage:true});
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>window.innerWidth),false,'Loadout page must not overflow on mobile');
  await page.setViewportSize({width:1440,height:1100});
  await page.reload();
  assert.equal(await page.locator('#loadoutPage').isVisible(),true,'#loadouts hash reopens the loadout page');
  const savedLoadouts=await page.evaluate(()=>JSON.parse(localStorage.getItem('elaina-mage-balance-v2')));
  assert.equal(savedLoadouts.stage,3);near(savedLoadouts.loadouts[2].baseMana,160);near(savedLoadouts.loadouts[2].maxLife,410);assert.equal(savedLoadouts.loadouts[3].name,'<b>测试</b>');
  await page.locator('#tabLoadouts').focus();await page.keyboard.press('ArrowLeft');
  assert.equal(await page.locator('#tabLab').getAttribute('aria-selected'),'true');
  assert.equal(await page.locator('#tabLab').evaluate(el=>el===document.activeElement),true);
  assert.equal(await page.locator('#labPage').isVisible(),true);
  await resetPage();
  assert.equal(await page.locator('#preset').inputValue(),'13');
  assert.equal((await page.evaluate(()=>JSON.parse(localStorage.getItem('elaina-mage-balance-v2')))).loadouts[2].baseMana,200,'Reset restores default loadouts');
  await page.getByText('受伤损蓝：均匀损失、周期受击或指定时间',{exact:true}).evaluate(el=>el.parentElement.open=true);
  await page.locator('#battleHurtMode').selectOption('uniform');
  await page.locator('#battleHurtTotal').fill('240');
  await page.screenshot({path:path.join(out,'desktop.png'),fullPage:true});
  await page.setViewportSize({width:390,height:844});
  await page.screenshot({path:path.join(out,'mobile.png'),fullPage:true});
  const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>window.innerWidth);
  assert.equal(overflow,false,'Mobile document must not overflow horizontally');
  await page.setViewportSize({width:1440,height:1100});
  await resetPage();
  const exported={schema:'elaina-mage-balance-v2',configuration:B.defaults()};
  assert.ok(await page.getByText('魔力转换率 K',{exact:true}).count());
  await page.getByRole('button',{name:'添加主动技能'}).click();
  const added=page.locator('#rotationRows tr').last();
  await added.getByRole('textbox',{name:/技能名称/}).fill('<试炼&飞弹>');
  await added.getByRole('textbox',{name:/技能 ID/}).fill('ExperimentalSkill');
  await added.getByRole('checkbox').check();
  await added.getByRole('spinbutton',{name:'模拟<试炼&飞弹>蓝耗'}).fill('100');
  assert.equal(await page.locator('#rotationRows tr').count(),B.defaults().skills.length+1);
  assert.equal(await page.locator('#rotationRows img').count(),0,'User-provided skill names must be escaped');
  await page.reload();
  assert.equal(await page.locator('#rotationRows tr').count(),B.defaults().skills.length+1);
  assert.equal(await page.locator('#rotationRows tr').last().getByRole('textbox',{name:/技能 ID/}).inputValue(),'ExperimentalSkill');
  const extraExport={schema:'elaina-mage-balance-v2',configuration:await page.evaluate(()=>JSON.parse(localStorage.getItem('elaina-mage-balance-v2')))};
  assert.equal(extraExport.configuration.skills.at(-1).name,'<试炼&飞弹>');
  await resetPage();


  // Exercise the remaining file-reload path using an isolated in-memory handle.
  const loadPayload=async(value)=>{
   const payload=value&&typeof value==='object'&&!Array.isArray(value)&&!Object.hasOwn(value,'schema')?{schema:'elaina-mage-balance-v2',configuration:value}:value;
   await page.evaluate(text=>{
    window.showOpenFilePicker=async()=>[{name:'SkillBalance.json',queryPermission:async()=> 'granted',requestPermission:async()=> 'granted',getFile:async()=>({text:async()=>text})}];
    document.getElementById('fileStatus').textContent='';
   },typeof payload==='string'?payload:JSON.stringify(payload));
   await page.locator('#linkMod').click();
   await page.waitForFunction(()=>document.getElementById('fileStatus').textContent.includes('已关联'));
   await page.locator('#fileStatus').evaluate(el=>el.textContent='');
   await page.locator('#loadMod').click();
   await page.waitForFunction(()=>document.getElementById('fileStatus').textContent!=='');
  };
  await page.locator('#extraMana').fill('900');
  const duplicate=B.clone(extraExport.configuration);duplicate.skills.at(-1).id=duplicate.skills[0].id;
  await loadPayload(duplicate);assert.match(await page.locator('#fileStatus').innerText(),/读取失败/);
  const malicious=B.clone(extraExport.configuration);malicious.skills.at(-1).name='<img src=x onerror=window.injected=1>';
  await loadPayload(malicious);
  assert.equal(await page.locator('#rotationRows img,#rotationResults img,#combatEvents img,#progressionRows img,#loadoutList img').count(),0,'Names must not be interpreted as HTML');
  assert.equal(await page.evaluate(()=>window.injected),undefined);
  const empty=B.clone(extraExport.configuration);empty.skills=[];await loadPayload(empty);
  assert.equal(await page.locator('#rotationRows tr').count(),0,'Zero active skills is a valid auto-only plan');
  await loadPayload(extraExport);
  assert.equal(await page.locator('#rotationRows tr').count(),B.defaults().skills.length+1,'Extra skills must survive file reload');
  await page.locator('#rotationRows tr').last().getByRole('button',{name:/删除/}).click();
  assert.equal(await page.locator('#rotationRows tr').count(),B.defaults().skills.length);
  await loadPayload(exported);
  assert.equal(await page.locator('#extraMana').inputValue(),'0');
  assert.match(await page.locator('#fileStatus').innerText(),/已读取/);
  near(Number((await page.locator('#simActual').innerText()).replaceAll(',','')),Math.round(C.run(exported.configuration).dps*10)/10);
  const customized=B.defaults('nebula');customized.extraMana=240;customized.nebulaManaBridge=true;customized.combat.duration=180;customized.e.nebula.life=1;customized.skills[0].cost=130;
  await loadPayload({schema:'elaina-mage-balance-v2',configuration:customized,result:{elaina:-123},combat:{actual:{dps:-123}}});
  assert.equal(await page.locator('#preset').inputValue(),'19');
  assert.equal(await page.locator('#battleDuration').inputValue(),'180');
  assert.equal(await page.locator('#eNebulaLife').inputValue(),'1');
  assert.equal(await page.getByRole('spinbutton',{name:'模拟水球魔法蓝耗',exact:true}).inputValue(),'130');
  assert.equal(await page.locator('#nebulaManaBridge').isChecked(),true);
  await page.reload();
  assert.equal(await page.locator('#extraMana').inputValue(),'240');
  const retained=await page.locator('#simActual').innerText();
  for(const bad of ['{bad json',null,[],{schema:'future',configuration:customized},{version:2}, {...customized,baseDps:1000001},{...customized,stage:'toString'},{...customized,stage:20},{...customized,loadouts:customized.loadouts.slice(1)},{...customized,skills:[null]}]){
   await loadPayload(bad===null?'null':bad);
   assert.match(await page.locator('#fileStatus').innerText(),/读取失败，当前配置未改变/);
   assert.equal(await page.locator('#extraMana').inputValue(),'240');
   assert.equal(await page.locator('#simActual').innerText(),retained);
  }
  const invalidArmor=B.clone(customized);invalidArmor.e.armor='constructor';await loadPayload(invalidArmor);
  assert.match(await page.locator('#fileStatus').innerText(),/读取失败/);
  const legacyImport=B.clone(customized);legacyImport.version=1;
  await loadPayload({schema:'elaina-mage-balance-v1',configuration:legacyImport});
  assert.match(await page.locator('#fileStatus').innerText(),/读取失败/);
  // Re-selecting the same file remains possible; unknown payload keys are not stored.
  await loadPayload({...customized,unexpected:'discard me'});
  await loadPayload({...customized,unexpected:'discard me'});
  assert.equal(await page.evaluate(()=>Object.hasOwn(JSON.parse(localStorage.getItem('elaina-mage-balance-v2')),'unexpected')),false);
  await resetPage();
  await page.locator('#extraMana').fill('120');
  await loadPayload(JSON.parse(fs.readFileSync(path.join(__dirname,'elaina-mage-balance.json'),'utf8')));
  assert.match(await page.locator('#fileStatus').innerText(),/已读取/,'Existing saved JSON must remain readable');
  assert.deepEqual(errors,[]);
  console.log('Browser smoke passed: stage loadouts page, lab sync, presets, slot count, copy, persistence, duplicate warning, compact toolbar, file reload, desktop/mobile layout; zero JS errors.');
 } finally {await browser.close();}
}
if(process.argv.includes('--browser'))browserTests().catch(e=>{console.error(e);process.exitCode=1;});

