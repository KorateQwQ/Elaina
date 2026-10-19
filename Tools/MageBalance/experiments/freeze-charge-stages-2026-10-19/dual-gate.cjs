'use strict';
// Numerical experiment only: cooldown plus one freeze-earned permission.
// Reuses the prior model without changing gameplay source or saved balance data.
const fs = require('node:fs'), path = require('node:path'), vm = require('node:vm');
const assert = require('node:assert/strict'), crypto = require('node:crypto');
const originalModel = fs.readFileSync(path.join(__dirname, 'run.cjs'), 'utf8');
let code = originalModel.split('const rows = [];')[0];
function replace(before, after) {
  assert(code.includes(before), 'Model adapter needs review: ' + before.slice(0, 80));
  code = code.replace(before, after);
}
replace("if (key === 'ice' && variant === 'proposed') { row.cost = 22.5; row.freezeBuildup = 5.6; }", `
    if (key === 'ice') {
      row.cost = stage < 8 ? variant.preIceCost : variant.iceCost;
      row.freezeBuildup = variant.buildup;
    }
    if (key === 'lock') {
      // Preserve the current per-cast budget even when the recovery duration changes.
      const originalCooldown = row.cd;
      row.cd = variant.lockCD > 0 ? variant.lockCD : originalCooldown;
      row.k *= originalCooldown / row.cd;
    }`);
replace('let ballCD = 0, frozenUntil = -1, freezeCD = 0, buildup = 0, lockCharge = 0;',
  'let ballCD = 0, lockCD = 0, frozenUntil = -1, freezeCD = 0, buildup = 0, lockCharge = 0;');
replace('const ledger = { spent: 0, regen: 0, potion: 0, overflow: 0, usedPotions: 0, idleTicks: 0 };',
  'const ledger = { spent: 0, regen: 0, potion: 0, overflow: 0, usedPotions: 0, idleTicks: 0, frozenTicks: 0, controlTicks: 0 };');
replace("freezeCD = Math.max(0, freezeCD - cycleConstant(key === 'ball' ? 'BallFreezeRefund' : 'BeamFreezeRefund') * 60);",
  "freezeCD = Math.max(0, freezeCD - (key === 'ball' ? variant.ballRefund : variant.beamRefund) * 60);");
replace('ballCD = Math.max(0, ballCD - 1);', `
    ballCD = Math.max(0, ballCD - 1);
    lockCD = Math.max(0, lockCD - 1);`);
replace('if (t === frozenUntil) freezeCD = 3600;', 'if (t === frozenUntil) freezeCD = variant.freezeCD * 60;');
replace('if (t % 60 === 0) manaSamples.push({ time: t / 60, mana });', `
    if (t < frozenUntil) ledger.frozenTicks++;
    if (t < frozenUntil || lockTimes.some(x => t >= Math.round(x * 60) + 29 && t < Math.round(x * 60) + 110)) ledger.controlTicks++;
    if (t % 60 === 0) manaSamples.push({ time: t / 60, mana });`);
replace('const wantLock = lockCharge > 0 && canPay(sk.lock.cost);',
  'const wantLock = options.noLock !== true && lockCharge > 0 && lockCD <= 0 && canPay(sk.lock.cost);');
replace('const wantIce = options.alwaysIce === true || (freezeCD <= 0 && (t >= frozenUntil || options.iceWhileFrozen === true));', `
    const permissionReady = options.iceWhenPermissionReady && lockCharge > 0 && lockCD > 0;
    const wantIce = options.alwaysIce === true || permissionReady || (freezeCD <= 0 && (t >= frozenUntil || options.iceWhileFrozen === true));`);
replace("lockCharge = 0; casts.lock++; lockTimes.push(t / 60); occupy('lock', 45, t);",
  "lockCharge = 0; lockCD = Math.round(variant.lockCD * 60); casts.lock++; lockTimes.push(t / 60); occupy('lock', 45, t);");
replace('if (wantLock && pay(sk.lock.cost, t)) {', `
    if (options.reserveLock && lockCharge > 0 && lockCD <= 0 && !canPay(sk.lock.cost)) {
      ledger.idleTicks++; continue;
    }
    if (wantLock && pay(sk.lock.cost, t)) {`);
replace('assert(casts.lock <= chargeGrants.length && chargeGrants.length <= freezeTimes.length);', `
  assert(casts.lock <= chargeGrants.length && chargeGrants.length <= freezeTimes.length);
  assert(lockTimes.every((time, i) => i === 0 || time - lockTimes[i - 1] >= variant.lockCD - 1e-6), 'Independent lock cooldown cap');`);
code += '\nglobalThis.model = { simulate, config };';
const ctx = { require, __dirname, console };
vm.runInNewContext(code, ctx);
const { simulate, config } = ctx.model;
const configPath = path.resolve(__dirname, '../../../../ElainaModSkills/Data/SkillBalance.json');
const sourceSHA256 = crypto.createHash('sha256').update(fs.readFileSync(configPath)).digest('hex');
const candidates = {
  previous: { preIceCost: 22.5, iceCost: 22.5, buildup: 5.6, lockCD: 0, freezeCD: 60, beamRefund: .5, ballRefund: 4 },
  cd24Freeze30Cost18: { preIceCost: 15, iceCost: 18, buildup: 5.6, lockCD: 24, freezeCD: 30, beamRefund: .5, ballRefund: 4 },
  recommended: { preIceCost: 18, iceCost: 18, buildup: 5.6, lockCD: 24, freezeCD: 30, beamRefund: .5, ballRefund: 4 },
  cd24Freeze30Cost20: { preIceCost: 15, iceCost: 20, buildup: 5.6, lockCD: 24, freezeCD: 30, beamRefund: .5, ballRefund: 4 },
  cd24Freeze30Cost22: { preIceCost: 15, iceCost: 22.5, buildup: 5.6, lockCD: 24, freezeCD: 30, beamRefund: .5, ballRefund: 4 },
  cd24Freeze25Cost18: { preIceCost: 15, iceCost: 18, buildup: 5.6, lockCD: 24, freezeCD: 25, beamRefund: .5, ballRefund: 4 },
  cd24Freeze25Cost20: { preIceCost: 15, iceCost: 20, buildup: 5.6, lockCD: 24, freezeCD: 25, beamRefund: .5, ballRefund: 4 },
  cd24Freeze30Beam075: { preIceCost: 15, iceCost: 18, buildup: 5.6, lockCD: 24, freezeCD: 30, beamRefund: .75, ballRefund: 4 },
  cd24Freeze35Cost18: { preIceCost: 15, iceCost: 18, buildup: 5.6, lockCD: 24, freezeCD: 35, beamRefund: .5, ballRefund: 4 },
};
const strategies = {
  mixed: {}, mixedFrozenIce: { iceWhileFrozen: true }, mixedBankedIce: { iceWhileFrozen: true, iceWhenPermissionReady: true },
  pureIce: { alwaysIce: true, noBall: true, reserveLock: true }, iceBall: { alwaysIce: true, reserveLock: true },
};
const rows = [];
for (const [candidate, rules] of Object.entries(candidates)) for (const stage of [7, 8, 13]) for (const duration of [120, 300]) {
  const row = { candidate, rules, stage, duration, strategies: {} };
  for (const [name, options] of Object.entries(strategies)) {
    const r = simulate(stage, rules, { ...options, duration });
    row.strategies[name] = { dps: r.dps, shares: r.shares, casts: r.casts,
      maxMana: r.build.maxMana, recovery: r.build.recovery,
      mana: r.endMana, minMana: r.minMana, potions: r.ledger.usedPotions,
      idleSeconds: r.ledger.idleTicks / 60, firstPotion: r.firstPotion,
      firstLock: r.firstLock, lockInterval: r.meanLockInterval, lockTimes: r.lockTimes,
      freezeInterval: r.meanFreezeInterval, freezeTimes: r.freezeTimes,
      controlPercent: 100 * r.ledger.controlTicks / (duration * 60),
      freezePercent: 100 * r.ledger.frozenTicks / (duration * 60),
      spendPerSecond: r.ledger.spent / duration,
      levels: Object.fromEntries(Object.entries(r.build.skills).map(([key, value]) => [key, value.skillLevel])),
      lockDamage: r.build.skills.lock.damage, lockK: r.build.skills.lock.k };
  }
  rows.push(row);
}
assert.equal(crypto.createHash('sha256').update(fs.readFileSync(configPath)).digest('hex'), sourceSHA256);
const result = { date: '2026-10-19', sourceSHA256, candidates,
  assumptions: [
    'Hypothetical independent player cooldown plus max-one freeze permission; no opening permission. No direct cooldown refunds.',
    'Lock per-cast damage preserved at21.2B by compensating k when cd changes. Cost100, freeze duration4s retained.',
    'No haste; cooldown24 is an actual minimum in these scenarios. With haste the cap follows the adjusted cooldown.',
    'Same stages7/8/13 loadouts and levels35/40/65. Lock only available after planned hardmode blizzard unlock.',
    'WaterBall uses saved k1.5 and formal15s CD. Five50%-mana elixirs, no life conversion, incoming damage, healing/shield cost or extra buffs.',
    'Zero-defense single persistent target, all hits land, expected crit, no spectre/titanium procs; not a real boss fight.',
    'Strategies are fixed representative policies, not globally optimal. Pure ice includes earned IceShardLock but no water skills.',
    'Pure-ice and no-beam policies reserve mana for a ready earned lock after elixirs run out, instead of spending each recovered point on filler forever.',
    'mixed uses water after freeze; mixedFrozenIce keeps attacking with ice during freeze; mixedBankedIce also uses ice while permission is banked and lock cooldown remains.',
    'Control uptime counts union of4-second elemental freeze and lock hold from29th to110th frame; late-period target control is truncated.',
    'Source stage/deer equipment has five equipped accessories in six slots. See earlier reports for exact snapshots and motion assumptions.'
  ], rows };
fs.writeFileSync(path.join(__dirname, 'dual-gate-results.json'), JSON.stringify(result, null, 2) + '\n');
const stageLabels = { 7: '巨鹿后', 8: '肉后初期', 13: '花后' };
const num = (x, digits = 0) => x === null ? '—' : x.toFixed(digits);
const selected = rows.filter(r => r.candidate === 'recommended');
let report = '# 冰凌锁独立冷却＋冻结资格：参数与三阶段试算\n\n2026-10-19。未修改正式配置或游戏机制；以下为待实施的设计参数。配置SHA256：`' + sourceSHA256 + '`。\n\n';
report += '## 第一版建议\n\n| 项目 | 当前正式值 | 建议试值 |\n|---|---|---|\n';
report += '| 冰锥DPS系数 | 1.2B | 保持1.2B |\n| 冰锥每轮耗蓝 | 15 | 18（24/秒） |\n| 冰锥每发冻结积蓄 | 2.8 | 5.6（单体全中18发、6轮） |\n| 冰凌锁自身CD | 60秒，可被返还 | 24秒，不接受本轮讨论的直接返还 |\n| 冰凌锁释放资格 | 无冻结前置 | 自己成功冻结后获得1次，最多储存1次；资格和CD同时满足才可用 |\n| 冰凌锁开场资格 | 无此机制 | 0，先用冰锥触发冻结 |\n| 冰凌锁耗蓝/单次伤害 | 100/21.2B | 保持 |\n| 冰凌锁伤害系数k | 1.7666666667 | 4.4166666667，补偿CD变化以保持21.2B |\n| 目标解冻后冻结抗性 | 60秒 | 30秒 |\n| 冻结持续时间 | 4秒 | 保持 |\n| 水流每次有效脉冲削减目标冻结CD | 0.5秒 | 保持 |\n| 水球 | 15秒CD、150耗蓝、k1.5、削减目标冻结CD4秒 | 保持；不采用1秒测试覆盖 |\n| 直接冰凌锁减CD | 冻结18秒、水流0.15秒、水球3秒 | 取消；15秒冻结返还限频也取消，由资格上限和自身CD限频 |\n\n';
report += '伤害预算当前含cd乘数。仅改60→24而不调整k，会将单次预算从21.2B降至8.48B；本模型同步补偿k，未暗中降低伤害。无急速时24秒是玩家自己的最低释放间隔，换怪冻结不能突破。现有急速会改变实际CD，本轮没有额外急速。\n\n';
report += '## 120秒单体结果\n\n技能升满阶段上限，灰之魔女开启，满初始魔力，允许5次各回复50%最大魔力的药剂。纯冰=冰锥＋取得资格的冰凌锁；巨鹿后尚无冰凌锁。\n\n';
report += '| 阶段 | 配装/等级 | 纯冰DPS | 冰锥＋水球＋锁，不用水流 | 完整循环，冻结期间继续冰锥 | 冰凌锁平均间隔：纯冰/混合 |\n|---|---|---:|---:|---:|---|\n';
const armorNames = { 7: '丛林/35', 8: '钛金/40，已完成暴雪解锁', 13: '输出幽灵/65' };
for (const r of selected.filter(r => r.duration === 120)) {
  const s = r.strategies;
  report += `| ${stageLabels[r.stage]} | ${armorNames[r.stage]} | ${num(s.pureIce.dps)} | ${num(s.iceBall.dps)} | ${num(s.mixedFrozenIce.dps)} | ${num(s.pureIce.lockInterval, 1)}/${num(s.mixedFrozenIce.lockInterval, 1)}秒 |\n`;
}
report += '\n未计目标防御、幽灵/钛金套追击、空枪、多部位、阶段无敌、受伤和护盾/治疗消耗，不是实测Boss DPS。两分钟结束点可能截断最后一次冰凌锁；例如30秒目标抗性下纯冰第四次施法接近120秒，次数不等于四次完整预算。\n\n';
report += '## 完整循环伤害分配与资源，120秒\n\n| 阶段 | 冰锥 | 水流 | 水球 | 冰凌锁 | 用药：纯冰/无水流/完整循环 |\n|---|---:|---:|---:|---:|---|\n';
for (const r of selected.filter(r => r.duration === 120)) {
  const s = r.strategies, h = s.mixedFrozenIce.shares;
  report += `| ${stageLabels[r.stage]} | ${num(h.ice, 1)}% | ${num(h.water, 1)}% | ${num(h.ball, 1)}% | ${num(h.lock, 1)}% | ${s.pureIce.potions}/${s.iceBall.potions}/${s.mixedFrozenIce.potions} |\n`;
}
report += '\n已储存冻结资格且技能仍在CD时，继续使用冰锥的更积极策略，在肉后/花后两分钟约1456/4627 DPS，但各用4次补魔，长期未必有利。\n\n';
report += '## 300秒续航对照\n\n长战完整循环采用冻结后立刻转水的节约策略，以避免持续用冰锥耗尽药剂。\n\n| 阶段 | 纯冰DPS | 不用水流DPS | 冻结后转水DPS |\n|---|---:|---:|---:|\n';
for (const r of selected.filter(r => r.duration === 300)) {
  report += `| ${stageLabels[r.stage]} | ${num(r.strategies.pureIce.dps)} | ${num(r.strategies.iceBall.dps)} | ${num(r.strategies.mixed.dps)} |\n`;
}
report += '\n纯冰与无水流策略在药剂耗尽后，会为已就绪的冰凌锁停手攒蓝，不采用一直打便宜普攻、永远攒不到100魔力的策略。尽管如此，长战水流的续航仍有价值。模型没有求全局最优操作，结果不能视为玩法强度上限。\n\n';
report += '## 为什么不再增强水流\n\n以肉后120秒相同操作为例，30秒冻结抗性下，水流0.5→0.75秒削减对冰凌锁频率提升很小：自身24秒CD已成为限制。它主要提高普通冻结频率或腾出更多冰锥输出时间。因此第一版保留0.5，避免同时大幅增加控制频率。\n\n';
report += '这组改变最大的额外收益是控场：完整混合循环的单体控制覆盖，在模型中由上一版约17%增至约25～27%，取决于操作。实际Boss是否能正常完成关键招式，需要实测。30秒冻结抗性位于KL通用冻结逻辑，正式接入时也会影响其他使用该冻结系统的攻击来源。\n\n';
report += '## 模型边界与复现\n\n' + result.assumptions.map(x => '- ' + x).join('\n');
report += '\n\n运行 `node Tools/MageBalance/experiments/freeze-charge-stages-2026-10-19/dual-gate.cjs`。检查魔力收支守恒、资格来源、独立冷却间隔、技能伤害预算和配置在运行期间未变化。所有正式参数均只读。\n';
fs.writeFileSync(path.join(__dirname, 'DUAL-GATE.md'), report);
console.log(JSON.stringify(rows.filter(r => r.duration === 120 && r.stage === 8).map(r => ({ candidate: r.candidate,
  strategies: Object.fromEntries(Object.entries(r.strategies).map(([name, s]) => [name, { dps: Math.round(s.dps), locks: s.casts.lock,
    potions: s.potions, lockInterval: s.lockInterval, control: s.controlPercent, iceShare: s.shares.ice, mana: s.mana }])) })), null, 2));
