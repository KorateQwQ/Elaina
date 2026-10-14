'use strict';
const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const assert = require('node:assert/strict');
const {dummy: S, moon: M} = require('./simulate-haste.cjs');
const original = require('./simulate-v3.cjs');
const originalMoon = require('../moon-lord-master-2026-10-14/simulate.cjs');
const p = JSON.parse(fs.readFileSync(path.join(__dirname, 'candidate-parameters-v5.json'), 'utf8')).parameters;
const protectedFiles = ['../../index.html', '../../../../ElainaModSkills/Data/SkillBalance.json',
  '../../../../ElainaAttribute/ElainaMagicAttributes.cs', 'candidate-parameters-v5.json', 'REPORT-v5.md',
  'results-v5.json', 'simulate-v3.cjs', '../moon-lord-master-2026-10-14/simulate.cjs'];
const hash = f => crypto.createHash('sha256').update(fs.readFileSync(path.resolve(__dirname, f))).digest('hex');
const before = protectedFiles.map(hash);
assert(/CombatMaxMagicPointRecoveryPercentPerSecond\s*=>\s*0\.02f/.test(
  fs.readFileSync(path.resolve(__dirname, protectedFiles[2]), 'utf8')));
const base = {...p, filler: 'water', regenPercent: 2, trace: true};
const keys = ['water', 'ice', 'ball', 'lock', 'thunder'];
const mean = values => values.length ? values.reduce((a,b) => a+b, 0) / values.length : null;
const near = (a,b) => assert(Math.abs(a-b) < 1e-7, `${a} != ${b}`);
function compact(r) {
  const basics = r.actions.filter(a => a.key === 'ice' || a.key === 'water');
  const total = r.dps * r.options.duration;
  return {dps:r.dps, ratio:r.ratio, maxMana:r.M, regen:r.regen, idle:r.idleSeconds,
    potions:r.usedPotions, endMana:r.endMana, ledger:r.ledger,
    freezes:r.freezeCount, firstFreeze:r.freezes[0]?.time ?? null,
    freezeInterval:mean(r.freezeIntervals), freezeTimes:r.freezes.map(f=>f.time),
    iceShare:r.records.ice.handSeconds/(r.records.ice.handSeconds+r.records.water.handSeconds),
    switches:basics.slice(1).reduce((n,a,i)=>n+Number(a.key!==basics[i].key),0),
    thunderPaidSeconds:r.thunderPaidSeconds, thunderManaStops:r.clouds.filter(c=>c.reason==='mana').length,
    casts:Object.fromEntries(keys.map(k=>[k,r.records[k].count])),
    share:Object.fromEntries(keys.map(k=>[k,r.records[k].damage/total])), records:r.records};
}
function verifyDamage(r) {
  for (const e of r.damageEvents) {
    const value=e.damage/r.scale;
    if(e.key==='ice') near(value,.45);
    if(e.key==='water') assert(Math.abs(value-.18)<1e-8||Math.abs(value-.14)<1e-8);
    if(e.key==='ball') near(value,8.1);
    if(e.key==='thunder') assert(Math.abs(value-48/30/60)<1e-8||Math.abs(value-48/30/60*.5)<1e-8);
  }
  for(const d of r.detonations) near(d.damageB,3.2+2.5*d.remaining);
  assert(r.freezes.every(f=>f.iceSeconds>=14-1e-7)); // 28 attacks, never 28/haste attacks.
  near(r.ledger.error,0);
}
const rows=[];
for(const hasteBasics of ['fixed','cooldown']) for(const hat of [false,true])
for(const prefix of ['arcane','damage']) for(const thunder of [false,true])
for(const haste of [0,25,50,75,100,150,200,300,500]) {
  const o={...base,hasteBasics,hat,prefix,haste,omit:thunder?[]:['thunder']};
  const r=S.run(o);verifyDamage(r);
  if(haste===0) assert.deepEqual(compact(r),compact(original.run(o)),'zero haste reproduces existing model');
  rows.push({hasteBasics,hat,prefix,thunder,haste,...compact(r)});
}
// Reproduce the historical 2.5% v5 results before separating the recovery correction.
const legacy=JSON.parse(fs.readFileSync(path.join(__dirname,'results-v5.json'),'utf8'));
for(const hat of [false,true]) for(const prefix of ['arcane','damage']) {
  const r=S.run({...base,hat,prefix,regenPercent:2.5});
  near(r.dps,legacy[hat?'hat':'baseline'][prefix].dps);
}
// Unlimited mana is a diagnostic for supply bottlenecks, never a playable loadout.
const unlimited=[];
for(const hasteBasics of ['fixed','cooldown']) for(const haste of [0,50,100,500]) {
  const r=S.run({...base,hat:true,prefix:'arcane',hasteBasics,haste,unlimitedMana:true});
  verifyDamage(r);unlimited.push({hasteBasics,haste,...compact(r)});
}
// Does a modest change in scheduling help? This is a finite comparison, not an optimizer.
const policies=[];
for(const hasteBasics of ['fixed','cooldown']) for(const haste of [0,50,100])
for(const prefix of ['arcane','damage']) for(const thunder of [false,true]) {
  const candidates=[];
  for(const policy of ['reserve','flow','hold']) for(const order of [['thunder','ball'],['ball','thunder']]) {
    const r=S.run({...base,hat:true,prefix,hasteBasics,haste,omit:thunder?[]:['thunder'],policy,order});
    verifyDamage(r);candidates.push({policy,order,...compact(r)});
  }
  candidates.sort((a,b)=>b.dps-a.dps);
  policies.push({hasteBasics,haste,prefix,thunder,best:candidates[0],tested:candidates.length});
}
const boss=[];
for(const hasteBasics of ['fixed','cooldown']) for(const haste of [0,50,100])
for(const stage of [18,19]) for(const prefix of ['arcane','damage']) {
  const opts={stage,slots:7,hat:true,prefix,hasteBasics,haste,profile:'normal',regenPercent:2,
    allowThunder:stage===19,preBuild:stage===19?['thunder','ball']:['ball'],
    healthRegen:stage===19?9:2,healthPotionHeal:stage===19?200:150,
    lockPolicy:'adaptive',buildPolicy:'spread'};
  const runs=Array.from({length:24},(_,i)=>M.run({...opts,seed:i+1}));
  if(haste===0) assert.deepEqual(runs[0],originalMoon.run({...opts,seed:1}));
  const wins=runs.filter(r=>r.killed);
  boss.push({hasteBasics,haste,stage,prefix,completed:wins.length,samples:24,
    seconds:mean(wins.map(r=>r.time)),minSeconds:wins.length?Math.min(...wins.map(r=>r.time)):null,
    maxSeconds:wins.length?Math.max(...wins.map(r=>r.time)):null,
    freezes:mean(wins.map(r=>r.freezeEvents.length)),locks:mean(wins.map(r=>r.records.lock.count)),
    share:Object.fromEntries(keys.map(k=>[k,mean(wins.map(r=>r.records[k].share))])),
    runs:runs.map(r=>({time:r.time,killed:r.killed,playerDead:r.playerDead,usedPotions:r.usedPotions,
      endMana:r.endMana,records:r.records,freezes:r.freezeEvents,locks:r.lockEvents}))});
}
// KL clamps to [0,500]. Neither a negative value nor an overflow changes the boundary result.
// Ready sooner never forces a cast: waiting until the original schedule preserves it.
for(const hasteBasics of ['fixed','cooldown']) for(const prefix of ['arcane','damage']) {
  assert.deepEqual(compact(S.run({...base,hat:true,prefix,hasteBasics,haste:100,waitForBaseCadence:true})),
    compact(S.run({...base,hat:true,prefix,hasteBasics,haste:0})));
}
for(const [input,bound] of [[-20,0],[600,500]]) {
  near(S.run({...base,haste:input,hasteBasics:'cooldown'}).dps,
    S.run({...base,haste:bound,hasteBasics:'cooldown'}).dps);
}
assert(protectedFiles.every((f,i)=>hash(f)===before[i]));
const results={date:'2026-10-15',experimentOnly:true,parameters:p,regenPercent:2,
  assumptions:{haste:'Constant free added stat; KL clamp 0..500, multiplier 100/(100+H), snapshot at cooldown start.',
    fixed:'Basic hand intervals 0.2s/0.5s stay fixed; only ball, lock and thunder cooldowns shorten.',
    cooldown:'Conditional no-animation-gate model: basic intervals also shorten, fixed damage/cost per attack and fixed 28 attacks for freeze.',
    unchanged:'Damage budgets, active cast time, impact delays, freeze60s/duration4s, buildup84 hits, wet duration, per-hit discounts, mana regen.',
    thunder:'One cloud at a time, 30s duration, 15 mana/s,48B; cannot refresh/stack before expiry.',
    dummy:'300s graduation6slots, five30% potions, no injury, full nebula damage stacks.',
    boss:'Normal hit/dodge/hurt profile,24 matched seeds,7slots/hat,shared freeze,adaptive cross-target lock/spread buildup. Model only.',
    limits:'No price paid for haste. No game measurement, actual attack gating/flight/rounding calibration or global policy optimization.'},
  checks:['historical v5 2.5% baseline reproduced for four loadouts','zero haste matches original models',
    'per-hit damage and fixed per-hit buildup preserved','mana/hand-time/freeze invariants pass',
    'haste clamps match KL','voluntary wait at100 haste exactly reproduces zero-haste schedule',
    'existing candidate/config/simulators unchanged'],
  rows,unlimited,policies,boss,inputHashes:Object.fromEntries(protectedFiles.map((f,i)=>[f,before[i]]))};
fs.writeFileSync(path.join(__dirname,'results-haste.json'),JSON.stringify(results,null,2)+'\n');
const f=(x,d=3)=>x===null?'—':x.toFixed(d),pct=x=>f(x*100,1)+'%';
const get=(mode,h,prefix,thunder=true,hat=true)=>rows.find(r=>r.hasteBasics===mode&&r.haste===h&&r.prefix===prefix&&r.thunder===thunder&&r.hat===hat);
function table(mode,thunder) {
  return '| 急速 | 奥秘DPS/原版 | 增伤DPS/原版 | 奥秘较0急速 | 增伤较0急速 | 奥秘冻结/水球/天雷次数 | 奥秘空闲秒 | 增伤空闲秒 |\n|---:|---:|---:|---:|---:|---|---:|---:|\n'+
    [0,25,50,75,100,150,200,300,500].map(h=>{
      const a=get(mode,h,'arcane',thunder),d=get(mode,h,'damage',thunder);
      return `| ${h} | ${f(a.ratio)} | ${f(d.ratio)} | ${pct(a.dps/get(mode,0,'arcane',thunder).dps-1)} | ${pct(d.dps/get(mode,0,'damage',thunder).dps-1)} | ${a.freezes}/${a.casts.ball}/${a.casts.thunder} | ${f(a.idle,1)} | ${f(d.idle,1)} |`;
    }).join('\n');
}
let report=`# 最终v5冰水流派：KL技能急速敏感性验证

2026-10-15。独立数值实验，正式配置及此前报告均未修改。急速只改变冷却调度，所有单次基础伤害保持不变。本次新增急速不占用装备预算，结果是属性敏感性，不是可获得配装强度。

KL规则：H裁剪到0～500，CD实值=基础CD×100/(100+H)，每轮冷却开始时读取，随后按真实秒倒计时。100急速使水球15→7.5秒、冰凌锁20→10秒、天雷120→60秒；不会将水球8.1B降成4.05B，也不会额外乘伤害。

冻结60秒是目标的触发间隔，保持60秒；水流每次有效命中扣0.5秒、水球扣4秒，均扣实际秒数，不再乘急速。冻结持续4秒、潮湿5/15秒、冰凌锁0.2秒施法+1秒延迟、天雷30秒持续与每秒15蓝不变。天雷沿用单云不叠加限制。

冰锥积累保持28次完整攻击/84发有效命中。在普攻间隔固定时为14秒；若普攻CD成为唯一频率门槛，则时间缩短，但每次命中积累不变。不会同时缩短命中门槛。

## 代码范围及两种口径

KL Skill.StartCooldown没有普攻豁免；伊蕾娜ElainaSkillModPlayer.UseSkill传入Attributes，伤害公式使用SkillBalanceEntry.BaseCooldown而非缩短后的CurrentCD。但KL不改动作速度，ActionModPlayer会按动作TotalFrame锁定itemTime，当前冰锥Action_SimpleSlash为45帧；水流类也尚无完整候选攻击实现，冰凌锁还保留测试用CD/耗蓝覆盖。因此不能把候选模型当作当前游戏实测。

- 固定普攻间隔：保留v5水流0.2秒、冰锥0.5秒的占手门槛，只缩短三个主动技能CD。这是“急速不改变普攻动作/引导频率”口径。
- 普攻受CD限制：假设上述0.2/0.5秒完全由可加速CD决定，按急速缩短，但每次伤害、耗蓝、冻结积累不变。这是额外设计假设，不是KL自动加快动画的结论。

计算按60Hz向上取整新的CD/普攻间隔；原模型的命中时刻仍是攻击末尾。真实KL浮点及帧更新顺序、真实投射物飞行没有校准。

## 回蓝基线

当前代码战斗回蓝为5+魔力上限×2%/秒。原REPORT-v5使用2.5%，本次先复现其四种配装，再统一改为当前2%。所以旧v5戴帽1.953/1.956倍不能直接充当本次0急速基线。当前戴帽有天雷为${f(get('fixed',0,'arcane').ratio)}/${f(get('fixed',0,'damage').ratio)}倍；无天雷为${f(get('fixed',0,'arcane',false).ratio)}/${f(get('fixed',0,'damage',false).ratio)}倍。

## 如何理解结果

1. 普攻间隔固定时，急速主要增加主动技能的可用机会。冻结本身仍需要60秒目标CD和14秒有效冰锥；冰凌锁通常早已转好，缩短它的CD不会额外创造冻结。积累阶段又优先持续冰锥，所以水球CD缩短并不等于水球施放次数同比增加。无天雷100急速长期收益只有奥秘约1.3%、增伤约0%。
2. 如果让普攻实际频率也提高，急速会同时提高普攻每秒伤害、每秒耗蓝、水流减冻结CD速度和冰锥积累速度。100急速时理想冰锥积累14→7秒、水流0.2→0.1秒，水流持续命中时冻结CD每秒推进从1+5×0.5=3.5秒变为1+10×0.5=6秒（未计水球/占手）。目标60秒CD本身不变，不能再额外减半。无天雷木桩100急速提升奥秘约30.0%、增伤约23.7%，已超过原先1.5～2倍目标上沿。
3. 带天雷时，固定策略更早追加每轮450蓝的持续支出，会挤掉水球、普攻和冻结。固定普攻间隔下，奥秘100急速天雷3→5次、水球15→12次、冻结9→7次，空闲21.1→73.3秒，整场DPS约下降5.3%。这是该自动调度变差，不是急速降低单次伤害。
4. 更短CD不会强迫玩家释放。已验证100急速主动等待到原始节奏时，能逐项复现0急速的输出、耗蓝和冻结记录。因此在没有装备机会成本且允许延迟操作的条件下，最优可达输出不会仅因CD缩短而降低；表中的负数只能用于诊断原策略，不是急速属性的负收益结论。
5. 普攻也加快但不扩充魔力时，高急速逐渐转为“短时间打完、长时间回蓝”。戴帽奥秘无天雷100→500急速仅从${f(get('cooldown',100,'arcane',false).ratio)}→${f(get('cooldown',500,'arcane',false).ratio)}倍，空闲从${f(get('cooldown',100,'arcane',false).idle,1)}→${f(get('cooldown',500,'arcane',false).idle,1)}秒。实际配装若为急速牺牲增伤/魔力，还需要另算机会成本。

## 戴帽六槽300秒：固定普攻间隔，有天雷

${table('fixed',true)}

## 戴帽六槽300秒：固定普攻间隔，无天雷

${table('fixed',false)}

## 戴帽六槽300秒：普攻也按CD加快，有天雷

${table('cooldown',true)}

## 戴帽六槽300秒：普攻也按CD加快，无天雷

${table('cooldown',false)}

以上沿用v5优先完成冰锥积累、冻结后及时引爆、水流阶段补水球/天雷的固定策略。空闲包含资源不足及为冰凌锁保留魔力造成的停手。具体施放次数、技能占比、耗蓝、冻结时刻、无帽结果见JSON。

## 100急速的节奏和资源（戴帽奥秘，有天雷）

| 口径/急速 | 首次冻结秒 | 后续平均冻结间隔秒 | 普攻占手中冰锥比例 | 水冰切换次数 | 总耗蓝 | 自然回蓝 | 天雷付费秒 | 天雷缺蓝提前结束次数 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
${['fixed','cooldown'].flatMap(mode=>[0,100].map(h=>{const r=get(mode,h,'arcane');return `| ${mode}/${h} | ${f(r.firstFreeze,2)} | ${f(r.freezeInterval,2)} | ${pct(r.iceShare)} | ${r.switches} | ${f(r.ledger.spent,1)} | ${f(r.ledger.natural,1)} | ${f(r.thunderPaidSeconds,1)} | ${r.thunderManaStops} |`;})).join('\n')}

## 魔力无限诊断（戴帽奥秘，有天雷；不能当成实际强度）

| 口径 | 急速 | DPS/原版 | 外部补蓝 | 冻结次数 |
|---|---:|---:|---:|---:|
${unlimited.map(r=>`| ${r.hasteBasics} | ${r.haste} | ${f(r.ratio)} | ${f(r.ledger.externalMana,1)} | ${r.freezes} |`).join('\n')}

## 六种有限策略对照（戴帽，有天雷）

只比较reserve/flow/hold与天雷/水球优先顺序，不穷举玩家操作。最高值也不是全局最优。

| 口径 | 急速 | 奥秘固定/所测最高 | 增伤固定/所测最高 |
|---|---:|---:|---:|
${['fixed','cooldown'].flatMap(mode=>[0,50,100].map(h=>`| ${mode} | ${h} | ${['arcane','damage'].map(prefix=>f(get(mode,h,prefix).ratio)+'/'+f(policies.find(r=>r.hasteBasics===mode&&r.haste===h&&r.prefix===prefix&&r.thunder).best.ratio)).join(' | ')} |`)).join('\n')}

## 大师月总短战：普通操作，每项24个种子

月前stage18无天雷，毕业stage19有天雷；均七槽戴帽、2%回蓝、全身共享冻结，允许跨部位引爆与分散积累。仍沿用旧模型的命中、躲避、生存假设，样本完成率不能当成游戏胜率。

| 口径 | 阶段 | 急速 | 奥秘平均秒/完成数 | 增伤平均秒/完成数 |
|---|---|---:|---:|---:|
${['fixed','cooldown'].flatMap(mode=>[18,19].flatMap(stage=>[0,50,100].map(h=>`| ${mode} | ${stage===18?'月前无天雷':'毕业有天雷'} | ${h} | ${['arcane','damage'].map(prefix=>{const r=boss.find(r=>r.hasteBasics===mode&&r.haste===h&&r.stage===stage&&r.prefix===prefix);return f(r.seconds,1)+' / '+r.completed;}).join(' | ')} |`))).join('\n')}

## 验证和复跑

已验证：${results.checks.join('；')}。

运行：\`node Tools/MageBalance/experiments/ice-build-2026-10-14/study-haste.cjs\`。

simulate-haste.cjs只在内存中对旧模拟器插入冷却调度，替换点数量有断言；study-haste.cjs记录输入哈希。没有改动正式C#、配置、旧模拟器和v5结论。
`;
fs.writeFileSync(path.join(__dirname,'REPORT-haste.md'),report);
console.log(JSON.stringify({checks:results.checks,summary:rows.filter(r=>r.hat&&[0,50,100].includes(r.haste)).map(r=>({mode:r.hasteBasics,haste:r.haste,prefix:r.prefix,thunder:r.thunder,ratio:r.ratio,idle:r.idle,freezes:r.freezes,casts:r.casts})),boss:boss.map(({runs,...r})=>r)},null,2));
