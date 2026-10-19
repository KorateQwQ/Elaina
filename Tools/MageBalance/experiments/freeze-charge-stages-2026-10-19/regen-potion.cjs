'use strict';
// Hypothetical +0.5% maximum magic per second; no gameplay or HTML edits.
const fs = require('node:fs'), path = require('node:path'), vm = require('node:vm');
const assert = require('node:assert/strict'), crypto = require('node:crypto');
const source = fs.readFileSync(path.join(__dirname, 'finisher-mana.cjs'), 'utf8').split('const candidates = [')[0]
  + '\nglobalThis.experiment = { code, config, rules };';
const outer = { require, __dirname, console };
vm.runInNewContext(source, outer);
let code = outer.experiment.code;
const original = 'recovery: 5 + .02 * maxMana';
assert(code.includes(original));
code = code.replace(original, 'recovery: 5 + (.02 + (variant.regenBonus ?? 0)) * maxMana');
const ctx = { require, __dirname, console };
vm.runInNewContext(code, ctx);
const { simulate, config } = ctx.model;
const file = path.resolve(__dirname, '../../../../ElainaModSkills/Data/SkillBalance.json');
const digest = () => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const sourceSHA256 = digest();
const ice = config.basicAttacks.find(x => x.id === 'IceConeSkill');
const cycleText = fs.readFileSync(path.resolve(__dirname, '../../../../ElainaModSkills/Skills/Ice/IceCycleBalance.cs'), 'utf8');
const constant = name => Number(cycleText.match(new RegExp(name + ' = ([\\d.]+)f?;'))[1]);
const rules = { mode: 'gated', iceCost: ice.cost, buildup: ice.freezeBuildup,
  beamFreeze: constant('BeamFreezeRefund'), beamLock: constant('BeamLockRefund'),
  ballFreeze: constant('BallFreezeRefund'), ballLock: constant('BallLockRefund') };
const policies = {
  mixed: { saveForLock: true },
  pureIce: { alwaysIce: true, noBall: true, reserveLock: true },
  coreNoBall: { noBall: true, saveForLock: true },
  mixedNoElixirs: { saveForLock: true, potions: 0 },
};
const rows = [];
for (const stage of [7, 8, 10, 13]) for (const duration of [120, 300]) for (const bonus of [0, .005]) {
  const row = { stage, duration, bonus, policies: {} };
  for (const [name, options] of Object.entries(policies)) {
    const r = simulate(stage, { ...rules, regenBonus: bonus }, { ...options, duration });
    row.policies[name] = { dps: r.dps, shares: r.shares, casts: r.casts,
      maxMana: r.build.maxMana, recovery: r.build.recovery, mana: r.endMana,
      potions: r.ledger.usedPotions, firstPotion: r.firstPotion,
      interval: r.meanLockInterval, locks: r.lockTimes,
      spentPerSecond: r.ledger.spent / duration, overflow: r.ledger.overflow, idleSeconds: r.ledger.idleTicks / 60 };
  }
  rows.push(row);
}
assert.equal(digest(), sourceSHA256);
const assumptions = [
  'Buff active for the whole120/300second fight; adds0.005*maximum mana per second without increasing maximum mana. Fixed5/sec retained.',
  'Current saved skills and gear; stage7/8/10/13 levels35/40/50/65. Planned unlock excludes lock before hardmode.',
  'Same single-target model as finisher-mana: zero defense, full hits, expected crit; no spectral/titanium procs, incoming damage, healing/shield or life conversion.',
  'Full opening mana and five50%-mana elixirs except explicitly no-elixirs policy. Regen potion has no modeled cast/item cost or stacking.',
  'WaterBall formal15second cooldown, ignoring current temporary test override. Freeze permission and60second cooldown both required.',
  'Fixed policies, not global optimization or actual boss measurement; extra regen can change timings and end-of-window cast counts.',
  'Mixed reserves earned upcoming lock budget and uses beam while funding ready lock. Pure ice can pause to save for lock.',
];
fs.writeFileSync(path.join(__dirname, 'regen-potion-results.json'), JSON.stringify({ sourceSHA256, rules, assumptions, rows }, null, 2) + '\n');
const names = { 7: '巨鹿后', 8: '肉后初期', 10: '双子后', 13: '花后' };
const f = (v, d=0) => v == null ? '—' : v.toFixed(d);
let report = '# 每秒额外0.5%最大魔力回复的影响\n\n按整场持续、不可叠加、回复最大魔力的0.5%计算：战斗回蓝=5+最大魔力×2.5%。只试算，不实现药剂。\n\n';
report += '## 基础回复\n\n| 阶段 | 魔力上限 | 原回复/秒 | 药后回复/秒 | 每27秒额外回复 |\n|---|---:|---:|---:|---:|\n';
for (const stage of [7,8,10,13]) {
  const s=rows.find(r=>r.stage===stage&&r.duration===120&&r.bonus===0).policies.mixed;
  report += `| ${names[stage]} | ${s.maxMana} | ${f(s.recovery,2)} | ${f(s.recovery+s.maxMana*.005,2)} | ${f(s.maxMana*.005*27,1)} |\n`;
}
for (const duration of [120,300]) {
  report += `\n## ${duration}秒完整循环\n\n| 阶段 | 原DPS→药后 | 增幅 | 冰凌锁次数 | 水球次数 | 补魔次数 | 冰凌锁间隔 |\n|---|---:|---:|---|---|---|---|\n`;
  for (const stage of [7,8,10,13]) {
    const a=rows.find(r=>r.stage===stage&&r.duration===duration&&r.bonus===0).policies.mixed;
    const b=rows.find(r=>r.stage===stage&&r.duration===duration&&r.bonus>0).policies.mixed;
    report += `| ${names[stage]} | ${f(a.dps)}→${f(b.dps)} | ${f((b.dps/a.dps-1)*100,1)}% | ${a.casts.lock}→${b.casts.lock} | ${a.casts.ball}→${b.casts.ball} | ${a.potions}→${b.potions} | ${f(a.interval,1)}→${f(b.interval,1)} |\n`;
  }
}
report += '\n## 五分钟纯冰对照\n\n| 阶段 | 原纯冰DPS | 药后纯冰DPS | 药后完整循环DPS |\n|---|---:|---:|---:|\n';
for (const stage of [7,8,10,13]) {
  const a=rows.find(r=>r.stage===stage&&r.duration===300&&r.bonus===0).policies;
  const b=rows.find(r=>r.stage===stage&&r.duration===300&&r.bonus>0).policies;
  report += `| ${names[stage]} | ${f(a.pureIce.dps)} | ${f(b.pureIce.dps)} | ${f(b.mixed.dps)} |\n`;
}
report += '\n冰锥仍消耗30/秒。药后自然回复追平连续冰锥的最大魔力阈值为1000，药前1250；这只覆盖普攻，不含终结技、水球和防御支出。每100秒理论额外回复50%最大魔力，相当于当前一份储存补魔的额度，但满蓝溢出会降低实际收益。\n\n';
report += assumptions.map(a=>'- '+a).join('\n')+'\n\n复现：`node Tools/MageBalance/experiments/freeze-charge-stages-2026-10-19/regen-potion.cjs`。\n';
fs.writeFileSync(path.join(__dirname, 'REGEN-POTION.md'), report);
console.log(report);
