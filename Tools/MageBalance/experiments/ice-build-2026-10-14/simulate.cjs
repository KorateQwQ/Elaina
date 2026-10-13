'use strict';
// Standalone design experiment. Does not write index.html or the live mod config.
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '../..');
const html = fs.readFileSync(path.join(root, 'index.html'), 'utf8');
const ctx = {module: {exports: {}}};
vm.runInNewContext(html.match(/<script id="balance-engine">([\s\S]*?)<\/script>/)[1], ctx);
const B = ctx.module.exports;
vm.runInNewContext(html.match(/<script id="combat-engine">([\s\S]*?)<\/script>/)[1], ctx);
const C = ctx.module.exports;
const input = fs.readFileSync(path.join(__dirname, 'source-config.json'), 'utf8');
const source = JSON.parse(input.replace(/^\uFEFF/, '')).configuration;
const ids = {water: 'WaterLaserSKill', ice: 'IceConeSkill', ball: 'WaterBallSkill', lock: 'IceShardLockSkill', thunder: 'prototype.Lightning'};
const activeIds = [ids.ball, ids.lock, ids.thunder];
const clone = x => JSON.parse(JSON.stringify(x));
const EPS = 1e-7;
const frames = seconds => Math.max(1, Math.ceil(seconds * 60 - EPS));

function setup(options = {}) {
  const s = clone(source);
  if (options.hat) s.e.items[3].id = 'elainaHat';
  if (options.prefix === 'damage') B.applyDamagePrefixes(s, 'e');
  else for (const i of B.equippedSlots(s, 'e')) s.e.items[i].prefix = 'arcane';
  Object.assign(s.combat, {duration: options.duration || 300, hurtMode: 'uniform', hurtTotal: 0, hurtEvents: '', startPercent: 100, reserve: 0});
  if (options.potions !== undefined) s.combat.potions = options.potions;
  if (options.regenPercent !== undefined) s.combat.regenPercent = options.regenPercent;
  if (options.potionPercent !== undefined) s.combat.potionPercent = options.potionPercent;
  s.skills.forEach(x => {x.enabled = activeIds.includes(x.id);});
  for (const [key, patch] of Object.entries(options.tune || {})) Object.assign(s.skills.find(x => x.id === ids[key]), patch);
  if (options.fullCombo !== false) s.skills.find(x => x.id === ids.thunder).hit = options.thunderHit ?? 100;
  return s;
}

// Sequential basic pattern: 1 second of water and 4 seconds of ice per five
// seconds of BASIC hand time, not five seconds of wall time. Actives interrupt
// this sequence. Wet/freeze states are an explicit ideal-combo assumption;
// they are not reconstructed from the partially migrated game implementation.
function run(options = {}) {
  const s = setup(options), T = s.combat.duration, endTick = Math.round(T * 60);
  const b = B.calculate(s), M = b.maxMana;
  const regen = s.combat.regenMode === 'mod' ? b.recovery : s.combat.regen;
  const restore = M * s.combat.potionPercent / 100;
  const water = s.basicAttacks.find(x => x.id === ids.water), ice = s.basicAttacks.find(x => x.id === ids.ice);
  const pattern = options.basic === 'water' ? [water] : options.basic === 'ice' ? [ice]
    : [...Array(options.waterActions ?? 5).fill(water), ...Array(options.iceActions ?? 8).fill(ice)];
  const basicTime = pattern.reduce((sum, x) => sum + frames(x.interval) / 60, 0);
  const basicRate = pattern.reduce((sum, x) => sum + x.cost, 0) / basicTime;
  const basicDps = pattern.reduce((sum, x) => sum + B.basicBudget(s, x).total * b.e.multiplier, 0) / basicTime;
  const order = (options.order || ['lock', 'thunder', 'ball']).map(x => ids[x]);
  const skills = s.skills.filter(x => x.enabled).map(x => ({...x, next: Math.round(x.first * 60), damage: B.skillDamageBudget(s, x, b) * x.hit / 100}));
  skills.sort((a, c) => order.indexOf(a.id) - order.indexOf(c.id));
  const policy = options.policy || 'hold';
  // This local greedy policy is simpler than the page's efficiency policy:
  // it does not reserve mana for higher-value skills that are still cooling down.
  if (options.lockDamageFactor !== undefined) skills.find(x => x.id === ids.lock).damage *= options.lockDamageFactor;
  let mana = M, potions = s.combat.potions, potionMana = 0, wasted = 0, regened = 0;
  let tick = 0, patternIndex = 0, occupiedTicks = 0, idleTicks = 0, minMana = M, shortages = 0;
  let minManaAfter60 = M, firstWait = null, hurtPaid = 0;
  const records = Object.fromEntries([...activeIds, ids.water, ids.ice].map(id => [id, {count: 0, damage: 0, spent: 0, handSeconds: 0, times: []}]));
  const actions = [];
  const canPay = cost => cost <= M + EPS && mana + potions * restore + EPS >= cost;
  function pay(cost) {
    const needed = mana + EPS >= cost ? 0 : restore > 0 ? Math.ceil((cost - mana - EPS) / restore) : Infinity;
    assert(needed <= potions && cost <= M + EPS);
    mana += needed * restore - cost; potionMana += needed * restore; potions -= needed;
    if (mana > M) {wasted += mana - M; mana = M;}
    mana = Math.max(0, mana); minMana = Math.min(minMana, mana);
    if (tick >= 3600) minManaAfter60 = Math.min(minManaAfter60, mana);
  }
  function advance(dt) {
    dt = Math.min(dt, endTick - tick);
    const incoming = regen * dt / 60;
    const gain = Math.min(M - mana, incoming);
    regened += gain; wasted += incoming - gain; mana += gain;
    if (options.hurtPerSecond) {const loss = Math.min(mana, options.hurtPerSecond * dt / 60); mana -= loss; hurtPaid += loss;}
    tick += dt;
  }
  while (tick < endTick) {
    let chosen = null, wait = false;
    const ready = skills.filter(x => x.next <= tick && x.cost <= M + EPS && tick / 60 + x.cast + x.delay < T - EPS);
    if (policy === 'greedy') ready.sort((a, c) => (c.damage - basicDps*c.cast)/c.cost - (a.damage - basicDps*a.cast)/a.cost);
    for (const x of ready) {
      if (policy === 'greedy') {
        const remaining = (endTick - tick) / 60;
        const reserve = Math.max(pattern[patternIndex].cost, (basicRate - regen) * remaining);
        if (x.damage <= basicDps*x.cast || mana + potions*restore - reserve + EPS < x.cost) continue;
      }
      if (canPay(x.cost)) {chosen = x; break;}
      if (policy === 'hold' || policy === 'value' && x.damage/x.cost > basicDps/basicRate) {wait = true; break;}
    }
    if (chosen) {
      const t = tick / 60, x = chosen, hand = frames(x.cast), start = t + hand/60 + x.delay;
      pay(x.cost);
      const fraction = x.duration > 0 ? Math.max(0, Math.min(1, (T - start)/x.duration)) : Number(start <= T + EPS);
      const r = records[x.id]; r.count++; r.damage += x.damage*fraction; r.spent += x.cost; r.handSeconds += Math.min(hand,endTick-tick)/60; r.times.push(t);
      actions.push({id:x.id, start:t, end:(tick+hand)/60, cost:x.cost, creditedDamage:x.damage*fraction});
      x.next = tick + frames(x.cd); occupiedTicks += Math.min(hand,endTick-tick); advance(hand);
    } else if (!wait && canPay(pattern[patternIndex].cost)) {
      const x = pattern[patternIndex], hand = frames(x.interval), t = tick/60;
      pay(x.cost);
      const damage = tick + hand <= endTick ? B.basicBudget(s,x).total * b.e.multiplier : 0;
      const r = records[x.id]; r.count++; r.damage += damage; r.spent += x.cost; r.handSeconds += Math.min(hand,endTick-tick)/60; r.times.push(t);
      actions.push({id:x.id, start:t, end:(tick+hand)/60, cost:x.cost, creditedDamage:damage});
      patternIndex = (patternIndex+1)%pattern.length; occupiedTicks += Math.min(hand,endTick-tick); advance(hand);
    } else {
      shortages++; if(firstWait === null) firstWait=tick/60;
      idleTicks++; advance(1);
    }
  }
  const damage = Object.values(records).reduce((sum,x)=>sum+x.damage,0);
  const spent = Object.values(records).reduce((sum,x)=>sum+x.spent,0);
  // regened is effective natural regen; potionMana is gross, potion overflow is
  // impossible here (one payment needs the smallest number of <M restorations).
  const ledgerError = M+regened+potionMana-spent-hurtPaid-mana;
  assert(Math.abs(ledgerError) < 1e-5, `mana ledger ${ledgerError}`);
  assert(occupiedTicks + idleTicks === endTick);
  assert(actions.every((a,i)=>!i || a.start+EPS>=actions[i-1].end));
  assert(mana>=-EPS && mana<=M+EPS);
  return {dps:damage/T, ratio:damage/T/b.vanilla, vanilla:b.vanilla, maxMana:M, regen, endMana:mana, minMana, minManaAfter60,
    usedPotions:s.combat.potions-potions, basicRate, basicDps, idleSeconds:idleTicks/60, firstWait, spent,
    ledger:{initial:M, natural:regened, potion:potionMana, spent, hurtPaid, end:mana, error:ledgerError},
    records, ...(options.trace?{actions}:{}), policy, order:options.order || ['lock','thunder','ball']};
}

function pair(options) {
  const arcane=run({...options,prefix:'arcane'}), damage=run({...options,prefix:'damage'});
  return {arcane,damage,arcaneLead:arcane.dps/damage.dps-1};
}
function compact(p) {
  return Object.fromEntries(['arcane','damage'].map(side=>[side,{dps:p[side].dps,ratio:p[side].ratio,M:p[side].maxMana,regen:p[side].regen,idle:p[side].idleSeconds,casts:Object.fromEntries(activeIds.map(id=>[id,p[side].records[id].count])),potions:p[side].usedPotions,end:p[side].endMana}]).concat([['arcaneLead',p.arcaneLead]]));
}

const recommended = {ball:{k:1.8,cast:1.35,first:1},lock:{k:3.2,cd:20,first:4},thunder:{cost:300,cd:60,k:.75,first:5}};
const orders = [['lock','thunder','ball'],['lock','ball','thunder'],['ball','lock','thunder'],['ball','thunder','lock'],['thunder','lock','ball'],['thunder','ball','lock']];
const policies = ['hold','flow','value','greedy'];
function search(options = {}) {
  const all = {};
  for (const prefix of ['arcane','damage']) {
    all[prefix] = [];
    for (const order of orders) for (const policy of policies) all[prefix].push(run({...options,order,policy,prefix}));
    all[prefix].sort((a,b)=>b.dps-a.dps);
  }
  return {arcane:all.arcane[0],damage:all.damage[0],arcaneLead:all.arcane[0].dps/all.damage[0].dps-1,all};
}
function verify() {
  // Independent cross-checks with the original page engine where policies and
  // a single basic have identical semantics, including actual casts and mana.
  for (const [basic,policy] of [['water','flow'],['ice','hold']]) {
    const options={basic,policy,fullCombo:false,order:['lock','ball','thunder']};
    const ours=run(options),s=setup(options);
    s.selectedBasicAttackId=basic==='water'?ids.water:ids.ice;
    s.combat.policy=policy==='hold'?'save':'flow';
    const reference=C.run(s);
    assert(Math.abs(ours.dps-reference.dps)<1e-5);
    assert(Math.abs(ours.endMana-reference.endMana)<1e-5);
    for(const x of s.skills.filter(x=>x.enabled)) assert.equal(ours.records[x.id].count,reference.skills[s.skills.indexOf(x)].count);
    assert.equal(ours.records[s.selectedBasicAttackId].count,reference.autoCount);
  }
  // Pricing sanity: no extra synergy multiplier is secretly added to K.
  const s=setup({tune:recommended}),b=B.calculate(s);
  for(const [id,totalB] of [[ids.ball,8.1],[ids.lock,12.8],[ids.thunder,27]]) {
    assert(Math.abs(B.skillDamageBudget(s,s.skills.find(x=>x.id===id),b)/s.baseDps/b.skillMultiplier-totalB)<1e-9);
  }
  const base=run({tune:recommended,trace:true});
  const double=clone(recommended);double.lock.k*=2;
  const changed=run({tune:double});
  const extraDps=base.records[ids.lock].damage/300;
  assert(Math.abs(changed.dps-base.dps-extraDps)<1e-6);
  assert.equal(changed.spent,base.spent);
  assert.equal(base.records[ids.thunder].count,5);
  assert(base.records[ids.water].count>0 && base.records[ids.ice].count>0);
  assert(base.idleSeconds>0); // A full rotation really competes for the same pool.
  return ['Original engine parity: water/flow and ice/save (DPS, casts, end mana)',
    'Budgets: water ball 8.1B, ice lock 12.8B, thunder 27B',
    'Fixed-rotation damage scales linearly with K; resource consumption unchanged',
    'All runs assert mana conservation, bounded mana, and exclusive hand time'];
}

module.exports = {B,C,source,ids,setup,run,pair,compact,recommended,search,verify};
if (require.main === module) {
  const checks=verify();
  const baselineS=setup({fullCombo:false});baselineS.combat.policy='efficiency';
  const originalPage=C.prefixDuel(baselineS);
  const result={date:'2026-10-14',duration:300,sourceSha256:crypto.createHash('sha256').update(input).digest('hex'),engineSha256:crypto.createHash('sha256').update(html).digest('hex'),checks,
    originalPage:{selectedBasic:baselineS.selectedBasicAttackId,arcane:originalPage.arcane.dps,damage:originalPage.damage.dps,vanilla:B.calculate(baselineS).vanilla},
    recommended,fullRotation:pair({tune:recommended}),hatRotation:pair({tune:recommended,hat:true}),
    strategySearch:search({tune:recommended}),hatStrategySearch:search({tune:recommended,hat:true}),
    sensitivity:[
      {label:'水流占普攻动作时间约4.8%',options:{waterActions:1,iceActions:8}},
      {label:'水流占普攻动作时间40%',options:{waterActions:10,iceActions:6}},
      {label:'无药剂',options:{potions:0}},
      {label:'天雷只兑现90%伤害',options:{thunderHit:90}},
      {label:'冰凌锁只兑现80%满联动伤害',options:{lockDamageFactor:.8}},
      {label:'水球占手2秒',options:{tune:{...recommended,ball:{...recommended.ball,cast:2}}}},
    ].map(x=>({label:x.label,options:x.options,result:pair({tune:recommended,...x.options})})),
    candidates:[
      {name:'只下调K、保留原CD与蓝耗',tune:{ball:{k:1.8,cast:1.35,first:1},lock:{k:2,first:4},thunder:{k:.5,first:5}}},
      {name:'较保守的20秒冰凌锁、60秒天雷',tune:{ball:{k:1.8,cast:1.35,first:1},lock:{k:3,cd:20,first:4},thunder:{cost:300,cd:60,k:.7,first:5}}},
      {name:'推荐',tune:recommended},
      {name:'更高输出',tune:{ball:{k:2,cast:1.35,first:1},lock:{k:3.5,cd:20,first:4},thunder:{cost:300,cd:60,k:.9,first:5}}},
    ].map(x=>({...x,result:pair({tune:x.tune})})),
    assumptions:{injuries:0,potions:5,potionRestorePercent:30,regen:'5 + maxMana * 2.5% / s',basicPattern:'5 water casts (1 second), then 8 ice casts (4 seconds); repeat in available basic hand time',fullCombo:'Ice lock budget is already full-freeze damage; thunder gets 100% realized hits. No extra damage multiplier. Wet/freeze uptime not simulated.',policy:'lock > thunder > ball; wait for an affordable skill that is ready but lacks mana; basics fill other windows',opening:'water ball first available at 1s, ice lock at 4s, thunder at 5s; availability does not guarantee actual cast time',scope:'Theoretical fixed-combo model; not in-game measurement, not proof of a global best loadout or policy.'}};
  fs.writeFileSync(path.join(__dirname,'results.json'),JSON.stringify(result,(key,value)=>key==='times'?undefined:value,2)+'\n');
  const candidate={experimentOnly:true,description:'Do not replace the live config with this file. Full-combo expected budgets; wet/freeze mechanics are not encoded here.',activePatches:recommended,basics:[ids.water,ids.ice].map(id=>source.basicAttacks.find(x=>x.id===id)),assumptions:result.assumptions};
  fs.writeFileSync(path.join(__dirname,'candidate-parameters.json'),JSON.stringify(candidate,null,2)+'\n');
  console.log(JSON.stringify({checks,originalPage:result.originalPage,fullRotation:compact(result.fullRotation),hatRotation:compact(result.hatRotation),bestTested:compact(result.strategySearch),hatBestTested:compact(result.hatStrategySearch),sensitivity:result.sensitivity.map(x=>({label:x.label,result:compact(x.result)})),candidates:result.candidates.map(x=>({name:x.name,result:compact(x.result)}))},null,2));
}
