'use strict';
// 2026-10-20: compare 0/1/2 water balls per lock-to-lock interval. No gameplay edits.
const fs = require('node:fs'), path = require('node:path'), vm = require('node:vm');
const assert = require('node:assert/strict'), crypto = require('node:crypto');
const source = fs.readFileSync(path.join(__dirname, 'prefix-comparison.cjs'), 'utf8').split(/\r?\nconst scenarios = \{/)[0]
  + '\nglobalThis.adapter = { code, rules, sourceSHA256 };';
const outer = { require, __dirname, console };
vm.runInNewContext(source, outer);
let { code, rules, sourceSHA256 } = outer.adapter;
function replace(before, after) { assert(code.includes(before), before); code = code.replace(before, after); }
replace('const damage = { ice: 0, water: 0, ball: 0, lock: 0 };', `
  let ballsThisCycle = 0, cycleThaw = -1;
  const cycleSnapshots = [];
  const damage = { ice: 0, water: 0, ball: 0, lock: 0 };`);
replace('function canPay(cost) { return mana + potions * maxMana * .5 + 1e-6 >= cost; }',
  'function canPay(cost) { return options.freeMana || mana + potions * maxMana * .5 + 1e-6 >= cost; }');
replace('function pay(cost, t) {', `function pay(cost, t) {
    if (options.freeMana) { const grant = Math.max(0, cost - mana); mana += grant; ledger.potion += grant; }`);
replace('const ballAffordableWithReserve = !preservingLock ||', 'const ballAffordableWithReserve = options.freeMana || !preservingLock ||');
replace('&& mana + potions * maxMana * .5 >= sk.ball.cost + (options.ballReserve ?? 0)',
  '&& (options.freeMana || mana + potions * maxMana * .5 >= sk.ball.cost + (options.ballReserve ?? 0))');
replace('const prioritizeWater = options.waterUntilLockReady && lockCharge > 0 && lockCD > 0;',
  'const prioritizeWater = options.waterUntilLockReady && lockCharge > 0;');
replace('const wantLock = options.noLock !== true && sk.lock.available && qualified && lockCD <= 1e-6 && canPay(sk.lock.cost);',
  `const wantLock = options.noLock !== true && sk.lock.available && qualified && lockCD <= 1e-6 && canPay(sk.lock.cost)
      && (casts.lock === 0 || options.strictQuota === false || ballsThisCycle >= (options.ballsPerLock ?? 0));`);
const wantBall = code.match(/const wantBall = [\s\S]*?;/)[0];
replace(wantBall, wantBall.slice(0, -1) + `
      && casts.lock > 0 && ballsThisCycle < (options.ballsPerLock ?? 0)
      && t + 70 >= cycleThaw
      && t >= (lockTimes.at(-1) + (options.ballDelay ?? 0)) * 60;`);
replace('lockCharge = 0; lockCD = 3600; recoverySinceCast = 0;', `
      if (casts.lock > 0 && options.strictQuota !== false) assert.equal(ballsThisCycle, options.ballsPerLock ?? 0);
      cycleSnapshots.push({ time: t / 60, mana, spent: ledger.spent, regen: ledger.regen, overflow: ledger.overflow,
        ballsBeforeLock: ballsThisCycle, damage: { ...damage }, casts: { ...casts }, handTicks: { ...handTicks } });
      ballsThisCycle = 0; cycleThaw = frozenUntil;
      lockCharge = 0; lockCD = 3600; recoverySinceCast = 0;`);
replace('casts.ball++; ballTimes.push(t / 60);', 'casts.ball++; ballsThisCycle++; ballTimes.push(t / 60);');
replace('return { build, options, duration, dps:', 'return { cycleSnapshots, build, options, duration, dps:');
const ctx = { require, __dirname, console };
vm.runInNewContext(code, ctx);
const { simulate } = ctx.model;
const mean = xs => xs.length ? xs.reduce((a,b) => a+b,0) / xs.length : null;
const scenarios = {
  replacement: { potions: 0, buffSeconds: 600 },
  oldCharges: { potions: 5, buffSeconds: 0 },
  freeMana: { potions: 0, buffSeconds: 600, freeMana: true },
};
const rows = [];
for (const stage of [8,10,13]) for (const prefix of ['damage','arcane']) {
  for (const [scenario, supply] of Object.entries(scenarios)) for (const duration of [120,300,600]) {
    if (scenario === 'freeMana' && duration !== 120 && duration !== 600) continue;
    if (scenario === 'oldCharges' && duration !== 300) continue;
    for (const ballsPerLock of [0,1,2]) {
      const options = { ...supply, duration, ballsPerLock, saveForLock: true, waterUntilLockReady: true };
      const r = simulate(stage, { ...rules, arcaneCount: prefix === 'arcane' ? 7 : 0 }, options);
      const gaps = r.lockTimes.slice(1).map((t,i) => t-r.lockTimes[i]);
      rows.push({ stage, prefix, scenario, duration, ballsPerLock, dps: r.dps,
        damage: r.damage, shares: r.shares, casts: r.casts, maxMana: r.build.maxMana,
        recovery: r.build.recovery + (supply.buffSeconds ? .01*r.build.maxMana : 0),
        averageGap: mean(gaps), lateGap: mean(gaps.slice(-5)), firstGap: gaps[0],
        lockTimes: r.lockTimes, ballTimes: r.ballTimes, freezeTimes: r.freezeTimes,
        cycleSnapshots: r.cycleSnapshots, ledger: r.ledger, endMana: r.endMana });
    }
  }
}
// Timing sensitivity: avoid assuming the earliest cast time is universally optimal.
const timing = [];
for (const stage of [8,13]) for (const prefix of ['damage','arcane']) for (const ballsPerLock of [1,2]) {
  for (const ballDelay of [0,4,8,12,16]) for (const strictQuota of [true,false]) {
    const r = simulate(stage, { ...rules, arcaneCount: prefix === 'arcane' ? 7 : 0 },
      { ...scenarios.replacement, duration: 300, ballsPerLock, ballDelay, strictQuota, saveForLock: true, waterUntilLockReady: true });
    timing.push({ stage, prefix, ballsPerLock, ballDelay, strictQuota, dps: r.dps,
      casts: r.casts, gap: r.meanLockInterval,
      completedQuotas: r.cycleSnapshots.slice(1).map(x => x.ballsBeforeLock) });
  }
}
const configPath = path.resolve(__dirname, '../../../../ElainaModSkills/Data/SkillBalance.json');
assert.equal(crypto.createHash('sha256').update(fs.readFileSync(configPath)).digest('hex'), sourceSHA256);
const assumptions = [
  'Same ice opening without a pre-opening ball. Each round starts on one lock cast and ends on the next; final incomplete round excluded from interval counts.',
  'Strict quota schedules exactly0/1/2balls in every completed interval. First ball is timed so its modeled hit occurs after the original freeze thaws; subsequent ball respects15second CD.',
  'Bank freeze permission; use water to advance cooldown. Reserve lock funds near readiness. Strict quota can delay the next lock to fund a required ball; optional quota sensitivity never delays a ready payable lock just to satisfy the quota.',
  'Replacement Moon Dew supplies1% max mana/sec for120seconds and is refreshed throughout, replacing all instant charges. No additional0.5% potion.',
  'freeMana only grants missing mana without changing skill cost/damage or action durations. It diagnoses cooldown/action limits, not playable output.',
  'Ball minimum action82frames, water exit20frames, fixed ball hit70frames after casting. Real range/charge/action timing changes these approximations.',
  'One zero-defense freezable target, full hits, expected crit, stage-capped skills. No armor procs, life conversion, incoming damage, shields or healing.',
  'AOE estimates add ball damage to extra targets outside all other skill areas, with no overkill and no extra cooldown refunds. They are marginal damage scenarios, not actual multitarget boss tests.',
];
fs.writeFileSync(path.join(__dirname, 'balls-per-lock-results.json'), JSON.stringify({ date: '2026-10-20', sourceSHA256, rules, assumptions, rows, timing }, null, 2)+'\n');
const f=(v,d=0)=>v==null?'—':v.toFixed(d), stageNames={8:'肉后初期',10:'双子后',13:'花后'};
let report='# 每轮冰凌锁使用0/1/2个水球\n\n2026-10-20。只做模型分析，正式配置和HTML未改。\n\n';
for (const scenario of ['replacement','freeMana','oldCharges']) {
  const duration=scenario==='freeMana'?120:300;
  report+=`## ${scenario}，${duration}秒\n\n| 阶段 | 前缀 | 每轮水球 | DPS | 锁/水球次数 | 锁平均间隔 | 后5轮间隔 | 首轮间隔 |\n|---|---|---:|---:|---|---:|---:|---:|\n`;
  for(const r of rows.filter(r=>r.scenario===scenario&&r.duration===duration))
    report+=`| ${stageNames[r.stage]} | ${r.prefix} | ${r.ballsPerLock} | ${f(r.dps)} | ${r.casts.lock}/${r.casts.ball} | ${f(r.averageGap,2)} | ${f(r.lateGap,2)} | ${f(r.firstGap,2)} |\n`;
  report+='\n';
}
report+='## 水球独占额外目标时，300秒总伤害相对不用水球\n\n目标数包含主目标。其余技能仅命中主目标，水球对额外目标伤害完整兑现；没有额外减CD、击杀或换怪冻结收益。\n\n| 阶段 | 前缀 | 每轮水球 | 单体变化 | 平均有效目标打平线 | 命中2目标时变化 | 命中3目标时变化 |\n|---|---|---:|---:|---:|---:|---:|\n';
for(const r of rows.filter(r=>r.scenario==='replacement'&&r.duration===300&&r.ballsPerLock>0)) {
 const base=rows.find(b=>b.scenario===r.scenario&&b.duration===r.duration&&b.stage===r.stage&&b.prefix===r.prefix&&b.ballsPerLock===0);
 const ballDps=r.damage.ball/r.duration, breakEven=1+Math.max(0,base.dps-r.dps)/ballDps;
 report+=`| ${stageNames[r.stage]} | ${r.prefix} | ${r.ballsPerLock} | ${f((r.dps/base.dps-1)*100,1)}% | ${f(breakEven,2)} | ${f(((r.dps+ballDps)/base.dps-1)*100,1)}% | ${f(((r.dps+2*ballDps)/base.dps-1)*100,1)}% |\n`;
}
report+='\n'+assumptions.map(a=>'- '+a).join('\n')+'\n';
report+='\n## 解释与边界\n\n水流每秒5次命中，额外推进1.5秒技能CD和2.5秒冻结CD。水球自身动作约1.37秒，从水流转入还需约0.33秒；其替代的约1.7秒水流本能推进约2.55秒技能CD和4.25秒冻结CD，因此水球当前3秒/4秒返还在单体持续站桩下净加速很小。命中脉冲相位和冻结开始时刻会改变小数结果。\n\n';
report+='水球基础CD15秒，若坚持每个完整循环都放2个，长期平均每轮至少30秒，初始技能可用和有限观测窗口只能暂时低于这一平均值。现有无水球循环约27.5秒，因此每轮2球不可能在此CD下长期更快。\n\n';
report+='严格每轮2球与“最多2球，锁就绪便放”不是同一种策略：后一种试算会自动降为多数轮1球。额外目标增加的是AOE伤害；IceWaterHitBatch对拥有者的锁CD每个水球只返还一次3秒，各独立元素锚点的冻结CD各减4秒，不能按目标数重复计算锁返还。\n\n';
fs.writeFileSync(path.join(__dirname,'BALLS-PER-LOCK.md'),report);
console.log(report);
