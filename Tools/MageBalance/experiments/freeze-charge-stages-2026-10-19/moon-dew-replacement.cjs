'use strict';
// Read-only gameplay evaluation: replace instant charges with a timed regeneration buff.
const fs = require('node:fs'), path = require('node:path'), vm = require('node:vm');
const assert = require('node:assert/strict'), crypto = require('node:crypto');
const source = fs.readFileSync(path.join(__dirname, 'finisher-mana.cjs'), 'utf8').split('const candidates = [')[0]
  + '\nglobalThis.experiment = { code };';
const outer = { require, __dirname, console };
vm.runInNewContext(source, outer);
let code = outer.experiment.code;
const original = 'const regen = recovery / 60; ledger.regen += regen;';
assert(code.includes(original));
code = code.replace(original, `const regen = (recovery + (t < (options.buffSeconds ?? 0) * 60 ? maxMana * .01 : 0)) / 60; ledger.regen += regen;`);
const ctx = { require, __dirname, console };
vm.runInNewContext(code, ctx);
const { simulate, config } = ctx.model;
const balancePath = path.resolve(__dirname, '../../../../ElainaModSkills/Data/SkillBalance.json');
const hash = () => crypto.createHash('sha256').update(fs.readFileSync(balancePath)).digest('hex');
const sourceSHA256 = hash();
const ice = config.basicAttacks.find(x => x.id === 'IceConeSkill');
const cycleText = fs.readFileSync(path.resolve(__dirname, '../../../../ElainaModSkills/Skills/Ice/IceCycleBalance.cs'), 'utf8');
const constant = name => Number(cycleText.match(new RegExp(name + ' = ([\\d.]+)f?;'))[1]);
const rules = { mode: 'gated', iceCost: ice.cost, buildup: ice.freezeBuildup,
  beamFreeze: constant('BeamFreezeRefund'), beamLock: constant('BeamLockRefund'),
  ballFreeze: constant('BallFreezeRefund'), ballLock: constant('BallLockRefund') };
const scenarios = {
  oldFiveCharges: { potions: 5 },
  oldOneCharge: { potions: 1 },
  newOneDose: { potions: 0, buffSeconds: 120 },
  newRefreshed: { potions: 0, buffSeconds: 300 },
};
const policies = {
  mixed: { saveForLock: true },
  pureIce: { alwaysIce: true, noBall: true, reserveLock: true },
  coreNoBall: { noBall: true, saveForLock: true },
};
const rows = [];
for (const stage of [7, 8, 10, 13]) for (const duration of [120, 300]) {
  for (const [scenario, supply] of Object.entries(scenarios)) {
    const row = { stage, duration, scenario, policies: {} };
    for (const [name, policy] of Object.entries(policies)) {
      const r = simulate(stage, rules, { ...policy, ...supply, duration });
      row.policies[name] = { dps: r.dps, shares: r.shares, casts: r.casts,
        maxMana: r.build.maxMana, baseRegen: r.build.recovery, endMana: r.endMana,
        usedCharges: r.ledger.usedPotions, interval: r.meanLockInterval,
        lockTimes: r.lockTimes, handTicks: r.handTicks, regen: r.ledger.regen,
        overflow: r.ledger.overflow, spent: r.ledger.spent, idleSeconds: r.ledger.idleTicks / 60 };
    }
    rows.push(row);
  }
}
assert.equal(hash(), sourceSHA256);
const assumptions = [
  'Replacement only: no old instant charges in new scenarios, and no separate0.5% regeneration potion.',
  'A dose grants an additional1% maximum mana per second for120seconds. Reuse refreshes duration without stacking strength.',
  'newOneDose is active only in the first120seconds. newRefreshed covers a300second fight with three doses; item use animation/cost not modeled.',
  'oldFiveCharges is the present full combat reserve, representing up to five consumed items. oldOneCharge compares a single item.',
  'Saved gear and skill values, capped progression; stages7/8/10/13 at35/40/50/65; lock250mana, k1.06. Formal ball15second cooldown.',
  'One persistent zero-defense target, all hits, expected crit; full opening mana; no incoming damage, life conversion, defense/healing spells or armor procs.',
  'Fixed representative casting policies, not globally optimal or an actual boss test. Output changes include cast timing near the end of the time window.',
];
fs.writeFileSync(path.join(__dirname, 'moon-dew-replacement-results.json'), JSON.stringify({ sourceSHA256, rules, scenarios, assumptions, rows }, null, 2) + '\n');
const names = { 7: '巨鹿后', 8: '肉后初期', 10: '双子后', 13: '花后' };
const labels = { oldFiveCharges: '旧制5份储备', oldOneCharge: '旧制1份', newOneDose: '新制1瓶/120秒', newRefreshed: '新制续杯全程' };
const f = (n, d = 0) => n == null ? '—' : n.toFixed(d);
let report = '# 月露合剂：瞬间补魔替换为持续回蓝\n\n只评估替换方案，不加入另外0.5%回蓝药，不修改游戏代码和HTML。\n\n';
report += '同一瓶药：旧制回复50%上限，新制120秒累计最多回复120%上限。旧制允许携带5份战斗储备，总计250%上限；新制取消这些瞬间储备。新制重复使用仅刷新时长。\n\n';
for (const duration of [120, 300]) {
  report += `## ${duration}秒：完整冰水循环\n\n| 阶段 | 方案 | DPS | 冰凌锁 | 水球 | 锁平均间隔 |\n|---|---|---:|---:|---:|---:|\n`;
  for (const stage of [7,8,10,13]) for (const scenario of Object.keys(scenarios)) {
    if (duration === 120 && scenario === 'newRefreshed') continue;
    const s = rows.find(r => r.stage === stage && r.duration === duration && r.scenario === scenario).policies.mixed;
    report += `| ${names[stage]} | ${labels[scenario]} | ${f(s.dps)} | ${s.casts.lock} | ${s.casts.ball} | ${f(s.interval,1)}秒 |\n`;
  }
  report += '\n';
}
report += '## 新制持续覆盖300秒：纯冰与完整循环\n\n| 阶段 | 纯冰DPS | 完整循环DPS | 不用水球DPS |\n|---|---:|---:|---:|\n';
for (const stage of [7,8,10,13]) {
  const p = rows.find(r => r.stage === stage && r.duration === 300 && r.scenario === 'newRefreshed').policies;
  report += `| ${names[stage]} | ${f(p.pureIce.dps)} | ${f(p.mixed.dps)} | ${f(p.coreNoBall.dps)} |\n`;
}
report += '\n' + assumptions.map(s => '- ' + s).join('\n') + '\n';
fs.writeFileSync(path.join(__dirname, 'MOON-DEW-REPLACEMENT.md'), report);
console.log(report);
