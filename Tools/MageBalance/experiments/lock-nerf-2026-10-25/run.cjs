'use strict';
// Compare damage budgets with identical casting decisions and current saved equipment.
const fs = require('node:fs'), path = require('node:path'), vm = require('node:vm');
const assert = require('node:assert/strict');
const modelDir = path.resolve(__dirname, '../freeze-charge-stages-2026-10-19');
const adapter = fs.readFileSync(path.join(modelDir, 'prefix-comparison.cjs'), 'utf8')
  .split(/\r?\nconst scenarios = \{/)[0] + '\nglobalThis.adapter = { code, rules };';
const outer = { require, __dirname: modelDir, console };
vm.runInNewContext(adapter, outer);
// Historical experiment predates the skill rename. Keep historical sources intact.
const code = outer.adapter.code.replaceAll('IceShardLockSkill', 'ShardbindSkill');
const context = { require, __dirname: modelDir, console };
vm.runInNewContext(code, context);
const { simulate, config } = context.model;
const lock = config.skills.find(s => s.id === 'ShardbindSkill');
const savedK = lock.k;
const rows = [];
for (const stage of [13, 15, 19]) for (const prefix of ['damage', 'arcane']) {
  for (const duration of [120, 300]) for (const k of [1.06, 0.4, 0.35]) {
    lock.k = k;
    const r = simulate(stage, { ...outer.adapter.rules, arcaneCount: prefix === 'arcane' ? 7 : 0 },
      { duration, potions: 0, buffSeconds: duration, saveForLock: true, waterUntilLockReady: true });
    rows.push({ stage, prefix, duration, k, dps: r.dps, shares: r.shares,
      basicShare: r.shares.ice + r.shares.water, casts: r.casts,
      lockDamage: r.build.skills.lock.damage, lockInterval: r.meanLockInterval,
      endMana: r.endMana });
  }
}
lock.k = savedK;
for (const r of rows.filter(r => r.k !== 1.06)) {
  const old = rows.find(x => x.stage === r.stage && x.prefix === r.prefix && x.duration === r.duration && x.k === 1.06);
  assert.deepEqual(r.casts, old.casts, 'Damage-only change must preserve fixed-policy casting counts');
  assert.equal(r.endMana, old.endMana, 'Damage-only change must preserve mana');
  assert(r.basicShare > old.basicShare && r.dps < old.dps);
}
const assumptions = [
  '正式JSON配装、成长节点、技能伤害和冰水返还；对比仅修改模型内冰凌锁k，不写回配置。',
  '固定策略：冰锥取得冻结资格，水流推进冷却，水球可用且不挤占即将就绪的冰凌锁魔力时释放。',
  '120/300秒单体零防御木桩、全命中、期望暴击、无急速；不计装备追击、走位、阶段转换及过量伤害。',
  '沿用历史模型的基础回蓝5+2%最大魔力/秒，并假定全程额外1%最大魔力/秒；这是固定供蓝假设，不复刻截图药剂。',
  '冰锥45帧、冰凌锁占手45帧/110帧完成、水球占手82帧/70帧命中、水流退出20帧；是近似循环，非游戏实测。',
  '截图82377为用户提供的面板数值，只按k比例换算，未重建其完整配装。'
];
fs.writeFileSync(path.join(__dirname, 'results.json'), JSON.stringify({ savedK, assumptions, rows }, null, 2) + '\n');
let report = '# 冰凌锁爆发下调与普攻占比\n\n2026-10-25。正式采用 k=0.4：31.8B → 12B，单次伤害降低约62.3%。截图同条件面板约82377 → 31086。\n\n';
report += '保留250魔力、60秒基础CD、冻结解封、水系返还和普攻伤害。优先减少一次爆发及每次减CD兑现的伤害收益，不延长普攻积蓄阶段。\n\n';
report += '| 阶段 | 前缀 | 秒数 | 普攻占比：原→现 | 冰凌锁占比：原→现 | 总DPS变化 |\n|---|---|---:|---:|---:|---:|\n';
for (const r of rows.filter(r => r.k === .4)) {
  const old = rows.find(x => x.stage === r.stage && x.prefix === r.prefix && x.duration === r.duration && x.k === 1.06);
  const f = n => n.toFixed(1);
  report += `| ${r.stage} | ${r.prefix} | ${r.duration} | ${f(old.basicShare)}% → ${f(r.basicShare)}% | ${f(old.shares.lock)}% → ${f(r.shares.lock)}% | ${f((r.dps / old.dps - 1) * 100)}% |\n`;
}
report += '\n阶段13/15/19分别使用角色65/75/95级与对应已保存配装。保留k=0.35作为更低爆发的对照，当前未采用。\n\n' + assumptions.map(x => '- ' + x).join('\n');
report += '\n\n复现：`node Tools/MageBalance/experiments/lock-nerf-2026-10-25/run.cjs`。真实程序集行为验证另运行 `Tools/IceCycleChecks/run.ps1`。\n';
fs.writeFileSync(path.join(__dirname, 'REPORT.md'), report);
console.log(report);
