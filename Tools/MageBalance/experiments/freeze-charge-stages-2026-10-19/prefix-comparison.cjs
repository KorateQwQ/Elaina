'use strict';
// Prefix sensitivity for the current cycle and hypothetical replacement Moon Dew.
const fs = require('node:fs'), path = require('node:path'), vm = require('node:vm');
const assert = require('node:assert/strict'), crypto = require('node:crypto');
const source = fs.readFileSync(path.join(__dirname, 'moon-dew-replacement.cjs'), 'utf8').split('const scenarios = {')[0]
  + '\nglobalThis.adapter = { code, rules, sourceSHA256 };';
const outer = { require, __dirname, console };
vm.runInNewContext(source, outer);
let { code, rules, sourceSHA256 } = outer.adapter;
const before = 'B.applyLoadout(s, stage, false);';
assert(code.includes(before));
code = code.replace(before, `${before}
  const prefixSlots = B.equippedSlots(s, 'e');
  const arcaneCount = variant.arcaneCount ?? prefixSlots.length;
  for (const [i, slot] of prefixSlots.entries()) s.e.items[slot].prefix = i < arcaneCount ? 'arcane' : 'none';
  const damageSlots = prefixSlots.slice(arcaneCount);
  if (variant.menacingOnly) for (const slot of damageSlots) s.e.items[slot].prefix = 'menacing';
  else B.applyDamagePrefixes(s, 'e', damageSlots);
  const prefixAllocation = prefixSlots.map(i => s.e.items[i].prefix);`);
assert(code.includes('return { stage, variant, level,'));
code = code.replace('return { stage, variant, level,', 'return { prefixAllocation, stage, variant, level,');
assert(code.includes('&& ballAffordableWithReserve;'));
code = code.replace('&& ballAffordableWithReserve;', `&& ballAffordableWithReserve
      && mana + potions * maxMana * .5 >= sk.ball.cost + (options.ballReserve ?? 0)
      && t / 60 - (ballTimes.at(-1) ?? -1e9) >= (options.ballGap ?? 0);`);
const ctx = { require, __dirname, console };
vm.runInNewContext(code, ctx);
const { simulate, config } = ctx.model;
const scenarios = {
  old: { potions: 5, buffSeconds: 0 },
  replacement: { potions: 0, buffSeconds: 600 },
};
const rows = [];
for (const stage of [7,8,10,13]) {
  const count = simulate(stage, rules, { duration: 1 }).build.prefixAllocation.length;
  for (const duration of [60,120,300,600]) for (const [scenario, supply] of Object.entries(scenarios)) {
    for (let arcaneCount = 0; arcaneCount <= count; arcaneCount++) {
      for (const policy of ['mixed', 'coreNoBall']) {
        const r = simulate(stage, { ...rules, arcaneCount }, { duration, ...supply, saveForLock: true, noBall: policy === 'coreNoBall' });
        rows.push({ stage, duration, scenario, policy, arcaneCount, prefixCount: count,
          allocation: r.build.prefixAllocation, maxMana: r.build.maxMana,
          recovery: r.build.recovery + (scenario === 'replacement' ? .01 * r.build.maxMana : 0),
          damageBonus: r.build.gear.damage, crit: r.build.gear.crit, dps: r.dps,
          casts: r.casts, shares: r.shares, handTicks: r.handTicks, minMana: r.minMana, endMana: r.endMana,
          spent: r.ledger.spent, regen: r.ledger.regen, overflow: r.ledger.overflow,
          usedCharges: r.ledger.usedPotions, interval: r.meanLockInterval });
      }
    }
    const r = simulate(stage, { ...rules, arcaneCount: 0, menacingOnly: true }, { duration, ...supply, saveForLock: true });
    rows.push({ stage, duration, scenario, policy: 'allMenacing', arcaneCount: 0, prefixCount: count,
      allocation: r.build.prefixAllocation, maxMana: r.build.maxMana, recovery: r.build.recovery + (scenario === 'replacement' ? .01 * r.build.maxMana : 0),
      damageBonus: r.build.gear.damage, crit: r.build.gear.crit, dps: r.dps, casts: r.casts });
  }
}
// Check whether the fixed ball policy hides a stronger damage-prefix rotation.
const tunedRows = [];
for (const stage of [8,10,13]) for (const duration of [120,300]) {
  for (let arcaneCount = 0; arcaneCount <= 7; arcaneCount++) {
    for (const ballGap of [20,30,45]) for (const ballReserve of [0,135,250]) {
      const r = simulate(stage, { ...rules, arcaneCount }, { duration, ...scenarios.replacement, saveForLock: true, ballGap, ballReserve });
      tunedRows.push({ stage, duration, arcaneCount, ballGap, ballReserve, dps: r.dps,
        casts: r.casts, interval: r.meanLockInterval, allocation: r.build.prefixAllocation });
    }
  }
}
const balancePath = path.resolve(__dirname, '../../../../ElainaModSkills/Data/SkillBalance.json');
assert.equal(crypto.createHash('sha256').update(fs.readFileSync(balancePath)).digest('hex'), sourceSHA256);
const assumptions = [
  'Only accessory prefixes change; gear, skill levels, costs, damage budgets and cooldowns remain the saved values.',
  'Damage prefixes choose the best distribution of Menacing and Lucky for expected standard critical damage. allMenacing is reported separately.',
  'old: full starting mana and five50% instant charges, base5+2% maximum mana/sec. replacement: no charges, base5+3% maximum mana/sec with refreshed120second Moon Dew; no separate0.5% potion.',
  'Representative mixed and no-ball policies reserve mana for an earned finisher. Additional sweep tests ball gaps20/30/45seconds and reserves0/135/250mana. These are finite policy samples, not all possible casting policies.',
  'Single freezable zero-defense target, full hits, expected crit. No incoming damage, life conversion, armor procs, defense/healing costs or potion drinking action.',
  'Water ball uses formal15second cooldown. Finisher and post-thaw freeze cooldown remain60seconds, with saved water refunds and one freeze-earned permission.',
];
fs.writeFileSync(path.join(__dirname, 'prefix-comparison-results.json'), JSON.stringify({ sourceSHA256, rules, scenarios, assumptions, rows, tunedRows }, null, 2) + '\n');
const names = { 7: '巨鹿后', 8: '肉后初期', 10: '双子后', 13: '花后' };
const f = (n, d=0) => n == null ? '—' : n.toFixed(d);
let report = '# 冰水循环：增伤前缀与奥秘\n\n仅试算，不改游戏配置或HTML。\n\n';
for (const scenario of Object.keys(scenarios)) {
  report += `## ${scenario === 'old' ? '当前五份瞬间补魔储备' : '月露替换为1%持续回蓝并全程续药'}\n\n| 阶段 | 时长 | 全增伤DPS | 全奥秘DPS | 奥秘相对增伤 | 所测最高混搭DPS | 最高混搭奥秘条数 |\n|---|---:|---:|---:|---:|---:|---:|\n`;
  for (const stage of [7,8,10,13]) for (const duration of [60,120,300,600]) {
    const group = rows.filter(r => r.scenario === scenario && r.stage === stage && r.duration === duration && r.policy === 'mixed');
    const damage = group.find(r => r.arcaneCount === 0), arcane = group.at(-1);
    const best = group.reduce((a,b) => a.dps > b.dps ? a : b);
    report += `| ${names[stage]} | ${duration}秒 | ${f(damage.dps)} | ${f(arcane.dps)} | ${f((arcane.dps/damage.dps-1)*100,1)}% | ${f(best.dps)} | ${best.arcaneCount}/${best.prefixCount} |\n`;
  }
  report += '\n';
}
report += '## 新月露：全增伤→全奥秘的资源与单次伤害\n\n| 阶段 | 奥秘条数 | 魔力上限 | 每秒回复 | 单次期望伤害：增伤相对奥秘 |\n|---|---:|---:|---:|---:|\n';
for (const stage of [7,8,10,13]) {
  const group = rows.filter(r => r.scenario === 'replacement' && r.stage === stage && r.duration === 300 && r.policy === 'mixed');
  const a = group[0], b = group.at(-1);
  const damageRatio = ((1+a.damageBonus/100)*(1+a.crit/100))/((1+b.damageBonus/100)*(1+b.crit/100));
  report += `| ${names[stage]} | ${b.prefixCount} | ${a.maxMana}→${b.maxMana} | ${f(a.recovery,1)}→${f(b.recovery,1)} | +${f((damageRatio-1)*100,1)}% |\n`;
}
report += '\n## 新月露300秒：前缀条数扫描\n\n| 阶段 | 奥秘条数 | DPS | 冰凌锁 | 水球 | 锁平均间隔 |\n|---|---:|---:|---:|---:|---:|\n';
for (const r of rows.filter(r => r.scenario === 'replacement' && r.duration === 300 && r.policy === 'mixed')) {
  report += `| ${names[r.stage]} | ${r.arcaneCount} | ${f(r.dps)} | ${r.casts.lock} | ${r.casts.ball} | ${f(r.interval,1)} |\n`;
}
report += '\n## 新月露：同时选择水球间隔与留蓝量后的所测最高值\n\n比较原四技能循环、不放水球，以及水球间隔20/30/45秒、额外保留0/135/250蓝。它仍不是所有打法的全局最优解。\n\n| 阶段 | 时长 | 全增伤所测最高DPS | 全奥秘所测最高DPS | 所有前缀组合所测最高DPS | 对应奥秘条数 | 水球/冰凌锁 |\n|---|---:|---:|---:|---:|---:|---|\n';
for (const stage of [8,10,13]) for (const duration of [120,300]) {
  const candidates = [...tunedRows.filter(r => r.stage === stage && r.duration === duration),
    ...rows.filter(r => r.stage === stage && r.duration === duration && r.scenario === 'replacement' && r.policy !== 'allMenacing')];
  const highest = xs => xs.reduce((a,b) => a.dps > b.dps ? a : b);
  const damage = highest(candidates.filter(r => r.arcaneCount === 0));
  const arcane = highest(candidates.filter(r => r.arcaneCount === 7));
  const best = highest(candidates);
  report += `| ${names[stage]} | ${duration}秒 | ${f(damage.dps)} | ${f(arcane.dps)} | ${f(best.dps)} | ${best.arcaneCount}/7 | ${best.casts.ball}/${best.casts.lock} |\n`;
}
report += '\n当前无受伤模型中，全增伤在扩充打法搜索后均为所测最高；固定四技能循环接近持平不等于前缀已达到输出平衡。奥秘的额外魔力在减少水球支出后难以继续提高冰凌锁次数，增伤则提高已释放技能的全部伤害。\n\n';
report += '奥秘每条+20魔力上限，新月露覆盖时增加0.6蓝/秒；满初始魔力、120秒内理论增加92可用蓝，300秒增加200，均未扣溢出。七条奥秘多140蓝上限、4.2蓝/秒。护盾与施法共用当前魔力，这些额外资源可以用于吸收伤害，但不能同时重复计入输出和防御。生命转魔力也随上限提高，但此次未纳入。\n\n';
report += assumptions.map(s => '- ' + s).join('\n') + '\n';
fs.writeFileSync(path.join(__dirname, 'PREFIX-COMPARISON.md'), report);
console.log(report);
