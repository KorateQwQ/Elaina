'use strict';
// Finisher cost/damage sensitivity under the intended long-CD ice/water design.
// No gameplay edits. Preserve previous experiments and their results.
const fs = require('node:fs'), path = require('node:path'), vm = require('node:vm');
const crypto = require('node:crypto'), assert = require('node:assert/strict');
const adapter = fs.readFileSync(path.join(__dirname, 'long-cd-water-cycle.cjs'), 'utf8').split('const candidates = {')[0]
  + '\nglobalThis.adapter = { code };';
const outer = { require, __dirname, console };
vm.runInNewContext(adapter, outer);
let code = outer.adapter.code;
const ballLine = 'const wantBall = options.noBall !== true && sk.ball.available && ballCD <= 0 && canPay(sk.ball.cost);';
assert(code.includes(ballLine));
code = code.replace(ballLine, `
    // Once an earned finisher is ready or nearly ready, do not spend its remaining budget on a ball.
    const preservingLock = options.saveForLock && sk.lock.available && qualified && lockCD < 10 * 60;
    const ballAffordableWithReserve = !preservingLock || mana + potions * maxMana * .5 >= sk.ball.cost + sk.lock.cost;
    const wantBall = options.noBall !== true && sk.ball.available && ballCD <= 0 && canPay(sk.ball.cost) && ballAffordableWithReserve;`);
const iceLine = 'const wantIce = options.alwaysIce === true || (!prioritizeWater && freezeCD <= 0 && (t >= frozenUntil || options.iceWhileFrozen === true));';
assert(code.includes(iceLine));
code = code.replace(iceLine, `
    const fundingLock = options.saveForLock && sk.lock.available && qualified && lockCD <= 1e-6 && !canPay(sk.lock.cost);
    const wantIce = !fundingLock && (options.alwaysIce === true || (!prioritizeWater && freezeCD <= 0 && (t >= frozenUntil || options.iceWhileFrozen === true)));`);
const ctx = { require, __dirname, console };
vm.runInNewContext(code, ctx);
const { simulate, config } = ctx.model;
const configPath = path.resolve(__dirname, '../../../../ElainaModSkills/Data/SkillBalance.json');
const hash = () => crypto.createHash('sha256').update(fs.readFileSync(configPath)).digest('hex');
const sourceSHA256 = hash();
const lock = config.skills.find(x => x.id === 'IceShardLockSkill');
const savedLock = { ...lock };
const rules = { mode: 'gated', iceCost: 22.5, buildup: 5.6, beamFreeze: .5, beamLock: .3, ballFreeze: 4, ballLock: 3 };
const candidates = [
  { name: 'baseline100', cost: 100, damageB: 21.2 },
  { name: 'cost150', cost: 150, damageB: 21.2 },
  { name: 'cost200-fixed', cost: 200, damageB: 21.2 },
  { name: 'cost250-fixed', cost: 250, damageB: 21.2 },
  { name: 'cost300-fixed', cost: 300, damageB: 21.2 },
  { name: 'cost200-boost25', cost: 200, damageB: 26.5 },
  { name: 'cost250-boost50', cost: 250, damageB: 31.8 },
  { name: 'cost300-boost50', cost: 300, damageB: 31.8 },
  { name: 'cost250-unchanged-k', cost: 250, damageB: 53 },
];
const policies = { greedyBall: {}, saveForLock: { saveForLock: true }, noBall: { noBall: true, saveForLock: true } };
const rows = [];
for (const candidate of candidates) {
  lock.cost = candidate.cost;
  lock.k = candidate.damageB / ((config.rate / 100 / 20) * candidate.cost * lock.cd * lock.cdRatio);
  candidate.k = lock.k;
  for (const stage of [8, 10, 13]) for (const duration of [120, 300]) {
    const row = { candidate: candidate.name, stage, duration, policies: {} };
    for (const [name, options] of Object.entries(policies)) {
      const r = simulate(stage, rules, { ...options, duration });
      row.policies[name] = { dps: r.dps, shares: r.shares, casts: r.casts,
        maxMana: r.build.maxMana, regen: r.build.recovery,
        lockDamage: r.build.skills.lock.damage, lockExpectedDamage: r.build.skills.lock.damage * (1 + r.build.gear.crit / 100),
        mana: r.endMana, minMana: r.minMana, potions: r.ledger.usedPotions, firstPotion: r.firstPotion,
        lockInterval: r.meanLockInterval, lockTimes: r.lockTimes, freezeTimes: r.freezeTimes,
        spendPerSecond: r.ledger.spent / duration, idleSeconds: r.ledger.idleTicks / 60 };
    }
    rows.push(row);
  }
}
assert.equal(hash(), sourceSHA256);
const result = { date: '2026-10-19', sourceSHA256, savedLock, rules, candidates,
  assumptions: [
    'Saved ice cost is22.5; saved lock cost is100, not150. Saved freeze buildup2.8 and beam lock refund0.15 are not the proposed values modeled here.',
    'All scenarios assume proposed freeze permission plus60s skill cooldown, target freeze cooldown60s, freeze4s, ice5.6 buildup, beam lock refund0.30.',
    'Ice damage1.2B; ball mana150, k1.5, formal15s cooldown. No gameplay files changed.',
    'Stage8/10/13 levels40/50/65 and saved gear; full starting mana, five50%-mana elixirs; no life conversion, incoming damage, buffs or armor procs.',
    'Single zero-defense persistent freezable target, full hits, expected standard crit. Not actual boss DPS.',
    'greedyBall always casts affordable ball after lock priority. saveForLock reserves ready/nearly-ready earned lock budget and uses water to recover when needed.',
    'noBall isolates three-skill core. These fixed policies are not globally optimized.',
    'Damage budgetB is independent scenario input; k is adjusted from the actual pricing formula. Keeping k unchanged while raising cost scales damage proportionally.'
  ], rows };
fs.writeFileSync(path.join(__dirname, 'finisher-mana-results.json'), JSON.stringify(result, null, 2) + '\n');
const f = (v, n = 0) => v === null ? '—' : v.toFixed(n);
const names = { 8: '肉后初期', 10: '双子后', 13: '花后' };
let report = '# 冰凌锁提高耗蓝的资源与伤害预算\n\n2026-10-19。只试算，未修改正式配置。当前文件冰锥耗蓝22.5，冰凌锁耗蓝100，水球耗蓝150；冰锥积蓄仍为2.8、水流冰凌锁返还仍为0.15，冻结资格机制尚未接入。\n\n';
report += '本报告均以前面讨论的完整新循环为前提：冰锥5.6积蓄、水流0.30技能CD返还、冰凌锁需要冻结资格；两项基础冷却仍60秒。不能将结果当作只改当前cost后游戏就会出现的效果。\n\n';
report += '## 预算联动\n\n当前主动技能伤害与耗蓝相乘，cd60、rate4、cdRatio1下，伤害预算=B×0.12×cost×k。\n\n| 耗蓝 | 单次预算 | k | 含义 |\n|---|---:|---:|---|\n';
report += '| 100 | 21.2B | 1.7666666667 | 当前基准 |\n| 200 | 21.2B | 0.8833333333 | 只加消耗 |\n| 250 | 21.2B | 0.7066666667 | 只加消耗 |\n| 200 | 26.5B | 1.1041666667 | 消耗提高，单次伤害增加25% |\n| 250 | 31.8B | 1.06 | 消耗提高，单次伤害增加50% |\n| 250 | 53B | 1.7666666667 | 只改cost不改k的结果，伤害为当前2.5倍 |\n\n';
report += '250魔力占肉后/双子630魔力池的39.7%、花后640魔力池的39.1%。六轮冰锥135魔力，加250魔力冰凌锁，共支付385魔力；这是毛支出，施法过程仍有自然回复。再加一发水球，毛支出535魔力。\n\n';
report += '## 肉后两分钟比较\n\n| 方案 | DPS | 冰凌锁占比 | 冰凌锁次数 | 水球次数 | 补魔次数 |\n|---|---:|---:|---:|---:|---:|\n';
for (const candidate of ['baseline100', 'cost150', 'cost200-fixed', 'cost250-fixed', 'cost300-fixed', 'cost200-boost25', 'cost250-boost50']) {
  const s = rows.find(r => r.candidate === candidate && r.stage === 8 && r.duration === 120).policies.saveForLock;
  report += `| ${candidate} | ${f(s.dps)} | ${f(s.shares.lock,1)}% | ${s.casts.lock} | ${s.casts.ball} | ${s.potions} |\n`;
}
report += '\n## 250耗蓝、31.8B的阶段效果\n\n| 阶段 | 时长 | DPS | 冰凌锁次数 | 水球次数 | 冰凌锁平均间隔 | 补魔次数 |\n|---|---:|---:|---:|---:|---:|---:|\n';
for (const r of rows.filter(r => r.candidate === 'cost250-boost50')) {
  const s = r.policies.saveForLock;
  report += `| ${names[r.stage]} | ${r.duration}秒 | ${f(s.dps)} | ${s.casts.lock} | ${s.casts.ball} | ${f(s.lockInterval,1)}秒 | ${s.potions} |\n`;
}
report += '\n## 资源取舍\n\n250耗蓝会让水球从固定每15秒必放转为需要选择资源窗口。肉后五分钟为冰凌锁留蓝的策略约1298 DPS、9次冰凌锁、14次水球；继续贪每个可支付水球约1173 DPS、6次冰凌锁、18次水球。两者都耗尽5次补魔。\n\n';
report += '该参数下完全不用水球的三技能核心，在肉后五分钟约1297 DPS、11次冰凌锁，只用2次补魔。这意味着水球在长战中的资源竞争已很明显，不能声称四技能每CD释放仍是最优。短战水球仍增加爆发；更精细的择机水球策略未做最优化搜索。\n\n';
report += '只提高耗蓝而保持21.2B，肉后五分钟留蓝策略约1097 DPS，低于100耗蓝基准约1282 DPS；250耗蓝+50%单次增伤则约1298 DPS。两分钟时后者却提高约21%总DPS，因此它是更集中的短战爆发，不能简单称为全时长平衡不变。\n\n';
report += '300耗蓝会让完整两分钟循环使用全部5次补魔，长战更依赖停用水球，第一版不建议直接采用。\n\n';
report += '## 限制与复现\n\n' + result.assumptions.map(x => '- ' + x).join('\n') + '\n\n';
report += '采用同阶段满技能、满初始魔力、最多5次补魔、单体零防御全中模型，不计生命转魔力、受伤、护盾治疗与装备追击，非游戏Boss实测。运行 `node Tools/MageBalance/experiments/freeze-charge-stages-2026-10-19/finisher-mana.cjs` 可复现。\n';
fs.writeFileSync(path.join(__dirname, 'FINISHER-MANA.md'), report);
console.log(JSON.stringify(rows.filter(r => r.stage === 8).map(r => ({ candidate: r.candidate, duration: r.duration,
  policies: Object.fromEntries(Object.entries(r.policies).map(([n, s]) => [n, { dps: Math.round(s.dps), lockShare: s.shares.lock,
    casts: s.casts.lock, balls: s.casts.ball, gap: s.lockInterval, potions: s.potions, endMana: s.mana }])) })), null, 2));
