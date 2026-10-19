'use strict';
// Intended design: long player/target cooldowns; ice earns permission, water accelerates both.
// All gameplay files are read-only. The adapter reuses the established analytical model.
const fs = require('node:fs'), path = require('node:path'), vm = require('node:vm');
const assert = require('node:assert/strict'), crypto = require('node:crypto');
let code = fs.readFileSync(path.join(__dirname, 'run.cjs'), 'utf8').split('const rows = [];')[0];
function replace(before, after) {
  assert(code.includes(before), 'Review changed model adapter: ' + before.slice(0, 70));
  code = code.replace(before, after);
}
replace("if (key === 'ice' && variant === 'proposed') { row.cost = 22.5; row.freezeBuildup = 5.6; }", `
    if (key === 'ice') { row.cost = variant.iceCost; row.freezeBuildup = variant.buildup; }`);
replace('let ballCD = 0, frozenUntil = -1, freezeCD = 0, buildup = 0, lockCharge = 0;',
  'let ballCD = 0, lockCD = 0, frozenUntil = -1, freezeCD = 0, buildup = 0, lockCharge = 0, nextFreezeReward = 0, recoverySinceCast = 0;');
replace('const ledger = { spent: 0, regen: 0, potion: 0, overflow: 0, usedPotions: 0, idleTicks: 0 };', `
  const ledger = { spent: 0, regen: 0, potion: 0, overflow: 0, usedPotions: 0, idleTicks: 0,
    beamLockRefund: 0, ballLockRefund: 0, freezeLockRefund: 0, frozenTicks: 0, controlTicks: 0 };
  function refundLock(seconds, kind) {
    const amount = Math.min(lockCD, seconds * 60);
    lockCD -= amount; recoverySinceCast += amount / 60; ledger[kind] += amount / 60;
  }`);
replace('if (sk.lock.available && lockCharge === 0) { lockCharge = 1; chargeGrants.push(t / 60); }', `
        if (sk.lock.available && lockCharge === 0) { lockCharge = 1; chargeGrants.push(t / 60); }
        if (variant.mode === 'runtime' && t >= nextFreezeReward) {
          nextFreezeReward = t + 900; refundLock(18, 'freezeLockRefund');
        }`);
replace("freezeCD = Math.max(0, freezeCD - cycleConstant(key === 'ball' ? 'BallFreezeRefund' : 'BeamFreezeRefund') * 60);", `
      freezeCD = Math.max(0, freezeCD - (key === 'ball' ? variant.ballFreeze : variant.beamFreeze) * 60);
      refundLock(key === 'ball' ? variant.ballLock : variant.beamLock, key === 'ball' ? 'ballLockRefund' : 'beamLockRefund');`);
replace('ballCD = Math.max(0, ballCD - 1);', `
    ballCD = Math.max(0, ballCD - 1);
    const naturalRecovery = Math.min(1, lockCD);
    lockCD -= naturalRecovery; recoverySinceCast += naturalRecovery / 60;`);
replace('if (t % 60 === 0) manaSamples.push({ time: t / 60, mana });', `
    if (t < frozenUntil) ledger.frozenTicks++;
    if (t < frozenUntil || lockTimes.some(x => t >= Math.round(x * 60) + 29 && t < Math.round(x * 60) + 110)) ledger.controlTicks++;
    if (t % 60 === 0) manaSamples.push({ time: t / 60, mana });`);
replace('const wantLock = lockCharge > 0 && canPay(sk.lock.cost);', `
    const qualified = variant.mode === 'runtime' || lockCharge > 0;
    const wantLock = options.noLock !== true && sk.lock.available && qualified && lockCD <= 1e-6 && canPay(sk.lock.cost);`);
replace('const wantIce = options.alwaysIce === true || (freezeCD <= 0 && (t >= frozenUntil || options.iceWhileFrozen === true));', `
    // Banked permission: water can still accelerate the player cooldown; prioritize it if requested.
    const prioritizeWater = options.waterUntilLockReady && lockCharge > 0 && lockCD > 0;
    const wantIce = options.alwaysIce === true || (!prioritizeWater && freezeCD <= 0 && (t >= frozenUntil || options.iceWhileFrozen === true));`);
replace('if (wantLock && pay(sk.lock.cost, t)) {', `
    if (options.reserveLock && qualified && sk.lock.available && lockCD <= 1e-6 && !canPay(sk.lock.cost)) {
      ledger.idleTicks++; continue;
    }
    if (wantLock && pay(sk.lock.cost, t)) {`);
replace("lockCharge = 0; casts.lock++; lockTimes.push(t / 60); occupy('lock', 45, t);", `
      if (casts.lock > 0) assert(recoverySinceCast >= 60 - 1e-6, 'Every recast requires60 seconds of actual cooldown progress');
      lockCharge = 0; lockCD = 3600; recoverySinceCast = 0;
      casts.lock++; lockTimes.push(t / 60); occupy('lock', 45, t);`);
replace('assert(casts.lock <= chargeGrants.length && chargeGrants.length <= freezeTimes.length);', `
  if (variant.mode !== 'runtime') assert(casts.lock <= chargeGrants.length && chargeGrants.length <= freezeTimes.length);
  if (variant.mode !== 'runtime') assert.equal(ledger.freezeLockRefund, 0);`);
replace('assert(casts.lock <= freezeTimes.length && (lockTimes.length === 0 || lockTimes[0] >= freezeTimes[0]));',
  "if (variant.mode !== 'runtime') assert(casts.lock <= freezeTimes.length && (lockTimes.length === 0 || lockTimes[0] >= freezeTimes[0]));");
code += '\nglobalThis.model = { simulate, config };';
const context = { require, __dirname, console };
vm.runInNewContext(code, context);
const { simulate, config } = context.model;
const configPath = path.resolve(__dirname, '../../../../ElainaModSkills/Data/SkillBalance.json');
const sourceSHA256 = crypto.createHash('sha256').update(fs.readFileSync(configPath)).digest('hex');
const candidates = {
  currentRuntime: { mode: 'runtime', iceCost: 15, buildup: 2.8, beamFreeze: .5, beamLock: .15, ballFreeze: 4, ballLock: 3 },
  gatedOnly: { mode: 'gated', iceCost: 15, buildup: 2.8, beamFreeze: .5, beamLock: .15, ballFreeze: 4, ballLock: 3 },
  fastIceOldWater: { mode: 'gated', iceCost: 22.5, buildup: 5.6, beamFreeze: .5, beamLock: .15, ballFreeze: 4, ballLock: 3 },
  beam025: { mode: 'gated', iceCost: 22.5, buildup: 5.6, beamFreeze: .5, beamLock: .25, ballFreeze: 4, ballLock: 3 },
  beam030: { mode: 'gated', iceCost: 22.5, buildup: 5.6, beamFreeze: .5, beamLock: .3, ballFreeze: 4, ballLock: 3 },
  beam035: { mode: 'gated', iceCost: 22.5, buildup: 5.6, beamFreeze: .5, beamLock: .35, ballFreeze: 4, ballLock: 3 },
  bothStronger: { mode: 'gated', iceCost: 22.5, buildup: 5.6, beamFreeze: .75, beamLock: .3, ballFreeze: 4, ballLock: 3 },
  beam030Ball6: { mode: 'gated', iceCost: 22.5, buildup: 5.6, beamFreeze: .5, beamLock: .3, ballFreeze: 4, ballLock: 6 },
  beam030Cost20: { mode: 'gated', iceCost: 20, buildup: 5.6, beamFreeze: .5, beamLock: .3, ballFreeze: 4, ballLock: 3 },
};
const strategies = {
  mixed: {}, mixedFrozenIce: { iceWhileFrozen: true }, waterPriority: { waterUntilLockReady: true },
  pureIce: { alwaysIce: true, noBall: true, reserveLock: true },
  iceBall: { alwaysIce: true, reserveLock: true },
  iceBeam: { noBall: true },
};
const rows = [];
for (const [candidate, rules] of Object.entries(candidates)) for (const stage of [7, 8, 13]) for (const duration of [120, 300]) {
  const row = { candidate, rules, stage, duration, strategies: {} };
  for (const [name, options] of Object.entries(strategies)) {
    const r = simulate(stage, rules, { ...options, duration });
    row.strategies[name] = { dps: r.dps, shares: r.shares, casts: r.casts,
      maxMana: r.build.maxMana, recovery: r.build.recovery,
      mana: r.endMana, minMana: r.minMana, potions: r.ledger.usedPotions, idleSeconds: r.ledger.idleTicks / 60,
      firstPotion: r.firstPotion, firstFreeze: r.firstFreeze, firstLock: r.firstLock,
      lockInterval: r.meanLockInterval, lockTimes: r.lockTimes, freezeInterval: r.meanFreezeInterval, freezeTimes: r.freezeTimes,
      controlPercent: 100 * r.ledger.controlTicks / (duration * 60), spendPerSecond: r.ledger.spent / duration,
      cooldownRefunds: { beam: r.ledger.beamLockRefund, ball: r.ledger.ballLockRefund, freeze: r.ledger.freezeLockRefund },
      levels: Object.fromEntries(Object.entries(r.build.skills).map(([key, value]) => [key, value.skillLevel])),
      lockDamage: r.build.skills.lock.damage, lockK: r.build.skills.lock.k };
  }
  rows.push(row);
}
assert.equal(crypto.createHash('sha256').update(fs.readFileSync(configPath)).digest('hex'), sourceSHA256);
const result = { date: '2026-10-19', sourceSHA256, candidates,
  assumptions: [
    'Player lock base cooldown60s and target post-thaw freeze cooldown60s retained in EVERY scenario; freeze duration4s.',
    'Gated proposals require both freeze-earned permission (cap1, opening0) and recovered cooldown; no freeze18s refund or15s reward limiter.',
    'CurrentRuntime comparison uses original18s freeze refund/15s limiter and no permission requirement, with opening lock ready.',
    'Lock damage21.2B, mana100 and k1.7666666667 retained. WaterBall saved k1.5, formal15s cooldown, mana150.',
    'Saved stages7/8/13 equipment and levels35/40/65; capped skill levels. Planned unlock excludes lock before hardmode in all scenarios.',
    'Single persistent zero-defense freezable target, all hits land, expected crit; no armor procs, phase invulnerability or movement.',
    'Full opening mana, five50%-mana elixirs; no life conversion, incoming damage, shield/healing costs or extra buffs. No haste.',
    'mixed: water after freeze; mixedFrozenIce: ice during frozen duration; waterPriority: banked permission favors water until lock cooldown recovers.',
    'PureIce includes ice and earned lock. IceBall omits beam. PureIce/IceBall wait to reserve mana for a ready lock when needed.',
    'Per-hit water refunds are capped by current remaining cooldown; shared-target/multi-hit duplicate refunds are not modeled as extra gains.',
    'Fixed representative strategies, not global optimization or real boss DPS. See prior model for approximate action/flight timings.'
  ], rows };
fs.writeFileSync(path.join(__dirname, 'long-cd-water-results.json'), JSON.stringify(result, null, 2) + '\n');
const selected = rows.filter(r => r.candidate === 'beam030');
const stageLabels = { 7: '巨鹿后', 8: '肉后初期', 13: '花后' };
const f = (value, digits = 0) => value === null ? '—' : value.toFixed(digits);
let report = '# 冰水联动：保留60秒冷却的推荐试调\n\n2026-10-19。本报告取代此前为扶持纯冰而提出的24秒技能CD、30秒冻结抗性方案。用户确认：完整冰水循环是平衡基准，纯冰是低操作、低收益打法，不追求强度接近。这里只试算，未修改正式参数或游戏机制。\n\n';
report += '配置SHA256：`' + sourceSHA256 + '`。\n\n';
report += '## 建议参数\n\n| 项目 | 当前 | 建议 |\n|---|---|---|\n';
report += '| 冰锥伤害系数 | 1.2B | 保持 |\n| 冰锥每轮耗蓝 | 15 | 22.5（每秒30） |\n| 冰锥每发冻结积蓄 | 2.8 | 5.6（单体全中18发/6轮，约4.5秒施法） |\n| 冰凌锁基础CD | 60秒 | 保持 |\n| 冰凌锁使用资格 | 无 | 自己的魔杖触发冻结后获得1次，最多储存1次，与CD同时满足才可用；开场资格0 |\n| 冰凌锁单次预算/耗蓝/k | 21.2B/100/1.7666666667 | 保持，CD未改无需补偿k |\n| 冻结返还冰凌锁CD | 18秒，玩家15秒间隔 | 以冻结资格取代，不再返还18秒；移除这项奖励的15秒限频 |\n| 水流每次有效脉冲削减冰凌锁CD | 0.15秒 | 0.30秒 |\n| 敌人解冻后的冻结冷却 | 60秒 | 保持 |\n| 冻结持续时间 | 4秒 | 保持 |\n| 水流每次削减冻结CD | 0.5秒 | 保持 |\n| 水球削减冰凌锁/冻结CD | 3秒/4秒 | 保持 |\n| 水球伤害/耗蓝/CD | k1.5/150/15秒 | 保持，忽略临时1秒测试覆盖 |\n\n';
report += '自然倒计时仍保留。换怪冻结只能补一个资格，不能增加层数或直接重置技能CD；要加速60秒技能恢复，需要实际使用水系命中。水流每秒5个有效周期，0.30秒返还意味着每秒额外推进1.5秒冰凌锁CD。\n\n';
report += '## 配装与口径\n\n巨鹿后丛林/35级，肉后初期钛金/40级且已完成计划中的暴雪解锁，花后输出幽灵/65级。所有技能升满阶段上限。灰之魔女开启，满初始魔力，最多5次各恢复50%最大魔力的药剂，无额外急速。\n\n';
report += '这是单体零防御、可冻结、持续存活目标的模型；不计幽灵/钛金追击、Boss移动、阶段无敌、空枪、受伤与护盾/治疗支出。不是游戏内实测，也不是某个具体Boss的击杀时间。\n\n';
report += '## 120秒技能本体输出\n\n正确循环：冰锥触发冻结后用冰凌锁，随后转水；水球在可支付时使用。开场优先水球，因此第一次冰凌锁约5.87秒；若先冰锥则约4.5秒。\n\n| 阶段 | 冰水循环DPS | 纯冰DPS | 冰锥＋水球＋锁、无水流DPS | 冰凌锁次数：循环/纯冰 | 补魔次数：循环/纯冰 |\n|---|---:|---:|---:|---|---|\n';
for (const r of selected.filter(r => r.duration === 120)) {
  const s = r.strategies;
  report += `| ${stageLabels[r.stage]} | ${f(s.mixed.dps)} | ${f(s.pureIce.dps)} | ${f(s.iceBall.dps)} | ${s.mixed.casts.lock}/${s.pureIce.casts.lock} | ${s.mixed.potions}/${s.pureIce.potions} |\n`;
}
report += '\n肉后与花后的冰凌锁平均间隔：正确循环约27.15秒，纯冰约68.25秒。冰锥＋水球但不用水流约55.85秒。正确循环两分钟比纯冰高约39%伤害，而且补魔更少；肉前尚无冰凌锁，短战DPS差距较小，收益首先表现为资源效率和控制频率。\n\n';
report += '## 正确循环伤害分配，120秒\n\n| 阶段 | 冰锥 | 水流 | 水球 | 冰凌锁 |\n|---|---:|---:|---:|---:|\n';
for (const r of selected.filter(r => r.duration === 120)) {
  const h = r.strategies.mixed.shares;
  report += `| ${stageLabels[r.stage]} | ${f(h.ice,1)}% | ${f(h.water,1)}% | ${f(h.ball,1)}% | ${f(h.lock,1)}% |\n`;
}
report += '\n冰锥本体占比低于冰凌锁，但它负责取得每次爆发资格；不能只按直接伤害占比认定其弱。\n\n';
report += '## 300秒长战\n\n| 阶段 | 正确循环DPS | 纯冰DPS | 正确循环相对提高 | 循环消耗药剂 |\n|---|---:|---:|---:|---:|\n';
for (const r of selected.filter(r => r.duration === 300)) {
  const s = r.strategies;
  report += `| ${stageLabels[r.stage]} | ${f(s.mixed.dps)} | ${f(s.pureIce.dps)} | ${f((s.mixed.dps/s.pureIce.dps-1)*100,1)}% | ${s.mixed.potions} |\n`;
}
report += '\n五分钟完整循环已使用4～5次补魔，不能把此结果理解为无限续航。纯冰/无水流策略在药剂耗尽且冰凌锁已就绪时会停手攒蓝，不会故意一直打低费普攻而永远释放不了技能。\n\n';
report += '## 与当前机制比较\n\n对照采用相同配装、相同策略、正式水球CD及计划解锁顺序；当前机制包含开场冰凌锁和冻结返还18秒，冰锥15耗蓝/2.8积蓄。\n\n| 阶段 | 当前机制120秒DPS | 新方案120秒DPS | 差异 |\n|---|---:|---:|---:|\n';
for (const r of selected.filter(r => r.duration === 120)) {
  const previous = rows.find(x => x.candidate === 'currentRuntime' && x.stage === r.stage && x.duration === 120).strategies.mixed;
  report += `| ${stageLabels[r.stage]} | ${f(previous.dps)} | ${f(r.strategies.mixed.dps)} | ${f((r.strategies.mixed.dps/previous.dps-1)*100,1)}% |\n`;
}
report += '\n这组目标是保持完整循环的大致输出，同时把收益转移到冰水配合上，不是全局增伤：开场不再白送冰凌锁，必须先冻结。单看两分钟总DPS没有明显提升，机制约束与纯冰代价更明显。\n\n';
report += '## 为什么选择0.30，而非同时提高所有返还\n\n肉后全中120秒相同策略：只接资格、保留旧冰锥时，冰凌锁间隔约36.8秒；换快速冻结后仍用旧0.15水流返还约34.6秒；0.25约29.3秒；0.30约27.2秒；0.35约26.2秒。0.30已接近目标冻结循环，继续提高的收益开始受冻结资格限制。\n\n';
report += '把水流冻结CD返还也提高到0.75时，冰凌锁仍约27.2秒一次，控制覆盖上升但没有明显增加爆发频率；把水球对冰凌锁返还改6秒也只到约26.3秒。因此第一版只加强水流对冰凌锁的返还，保留其他返还。\n\n';
report += '这些是同一固定操作策略下的比较，不是全局最优策略搜索；释放次数和有限战斗结束点可能影响平均DPS。主方案控制覆盖约19%，没有采用此前30秒目标抗性方案带来的约25～27%覆盖。\n\n';
report += '## 复现与检查\n\n运行 `node Tools/MageBalance/experiments/freeze-charge-stages-2026-10-19/long-cd-water-cycle.cjs` 刷新报告与结果。检查魔力守恒、资格来源、每次重放已累计60秒实际冷却进度（自然＋实际水系返还）、取消18秒返还、技能伤害预算和运行期间配置未变化。\n\n' + result.assumptions.map(x => '- ' + x).join('\n') + '\n';
fs.writeFileSync(path.join(__dirname, 'LONG-CD-WATER.md'), report);
console.log(JSON.stringify(rows.filter(r => r.stage === 8 && r.duration === 120).map(r => ({ candidate: r.candidate,
  strategies: Object.fromEntries(Object.entries(r.strategies).map(([name, s]) => [name, { dps: Math.round(s.dps), locks: s.casts.lock,
    potions: s.potions, firstLock: s.firstLock, lockInterval: s.lockInterval, control: s.controlPercent, mana: s.mana }])) })), null, 2));
