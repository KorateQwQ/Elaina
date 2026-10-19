'use strict';
// Compare representative strategies in the proposed freeze-charge model.
// This changes model inputs only; gameplay source and JSON are read-only.
const fs = require('node:fs'), path = require('node:path'), vm = require('node:vm');
const crypto = require('node:crypto'), assert = require('node:assert/strict');
const modelPath = path.join(__dirname, 'run.cjs');
let source = fs.readFileSync(modelPath, 'utf8').split('const rows = [];')[0];
const original = 'const wantLock = lockCharge > 0 && canPay(sk.lock.cost);';
assert(source.includes(original), 'Model changed: review the no-lock strategy adapter');
source = source.replace(original, 'const wantLock = options.noLock !== true && lockCharge > 0 && canPay(sk.lock.cost);');
source += '\nglobalThis.model = { simulate, config };';
const context = { require, __dirname, console };
vm.runInNewContext(source, context);
const { simulate, config } = context.model;
const configPath = path.resolve(__dirname, '../../../../ElainaModSkills/Data/SkillBalance.json');
const hash = () => crypto.createHash('sha256').update(fs.readFileSync(configPath)).digest('hex');
const sourceSHA256 = hash();
const strategies = {
  cycle: {},
  cycleFrozenIce: { iceWhileFrozen: true },
  iceOnly: { alwaysIce: true, noBall: true, noLock: true },
  iceLock: { alwaysIce: true, noBall: true },
  iceBallLock: { alwaysIce: true }
};
const rows = [];
for (const variant of ['current', 'proposed']) for (const ratio of [1.2, 1.3, 1.4, 1.5, 1.8, 2]) {
  config.basicAttacks.find(x => x.id === 'IceConeSkill').ratio = ratio;
  for (const stage of [7, 8, 13]) for (const duration of [30, 120, 300]) {
    const row = { variant, ratio, stage, duration, strategies: {} };
    for (const [name, options] of Object.entries(strategies)) {
      const r = simulate(stage, variant, { ...options, duration });
      row.strategies[name] = { dps: r.dps, potions: r.ledger.usedPotions, casts: r.casts,
        endMana: r.endMana, idleSeconds: r.ledger.idleTicks / 60, firstPotion: r.firstPotion };
    }
    rows.push(row);
  }
}
assert.equal(hash(), sourceSHA256, 'Config changed during calculation');
const result = { date: '2026-10-19', sourceSHA256,
  waterBallK: config.skills.find(x => x.id === 'WaterBallSkill').k,
  assumptions: [
    'All scenarios use proposed freeze-to-charge lock availability, not current runtime cooldowns.',
    'Current profile: saved ice cost15/buildup2.8; proposed profile: cost22.5/buildup5.6. Damage ratio swept independently.',
    'Same saved stage7/8/13 loadouts, levels35/40/65, capped skill levels; lock unavailable before hardmode.',
    'Single zero-defense persistent target, all hits land, expected crit, no armor procs; five50%-mana elixirs, no life conversion.',
    'Cycle means ice for buildup, water otherwise; cycleFrozenIce also uses ice during the4-second freeze.',
    'iceOnly never casts active skills; iceLock skips both water skills; iceBallLock skips only water beam.',
    'These are representative fixed strategies, not a search for the global optimum. Scarcity may delay or skip expensive skills.',
    'Short-fight results depend on the phase at which the fight ends. No boss HP, movement, defense or phase invulnerability is modeled.',
    'See run.cjs and REPORT.md for timing, resource, and freeze assumptions.'
  ], rows };
fs.writeFileSync(path.join(__dirname, 'ice-strategies-results.json'), JSON.stringify(result, null, 2) + '\n');
const labels = { 7: '巨鹿后', 8: '肉后初期', 13: '花后' };
const f = x => Math.round(x);
function table(list) {
  return '| 阶段 | 冰锥系数 | 冻结后转水 | 冻结期间继续冰锥 | 纯冰锥 | 冰锥＋锁 | 冰锥＋球＋锁（不用水流） |\n|---|---:|---:|---:|---:|---:|---:|\n' + list.map(r =>
    `| ${labels[r.stage]} | ${r.ratio}B | ${f(r.strategies.cycle.dps)} | ${f(r.strategies.cycleFrozenIce.dps)} | ${f(r.strategies.iceOnly.dps)} | ${f(r.strategies.iceLock.dps)} | ${f(r.strategies.iceBallLock.dps)} |`).join('\n');
}
let report = '# 冰锥增伤与技能切换收益\n\n2026-10-19。只进行模型试算，未修改正式参数。水球按本轮JSON的k=' + result.waterBallK + '。配置SHA256：`' + sourceSHA256 + '`。\n\n';
report += '所有结果均使用拟议的“冻结获得一次冰凌锁”规则；当前参数组仅指冰锥数值，不代表当前游戏内已经实现该机制。\n\n';
report += '## 当前冰锥参数，120秒\n\n' + table(rows.filter(r => r.variant === 'current' && r.duration === 120 && r.ratio === 1.2)) + '\n\n';
report += '## 高耗蓝、快速冻结参数，120秒\n\n每轮22.5魔力、每发5.6积蓄；数值为DPS。\n\n' + table(rows.filter(r => r.variant === 'proposed' && r.duration === 120 && [1.2, 1.5, 2].includes(r.ratio))) + '\n\n';
report += '## 高耗蓝参数，300秒\n\n' + table(rows.filter(r => r.variant === 'proposed' && r.duration === 300 && [1.2, 1.5, 2].includes(r.ratio))) + '\n\n';
report += '## 资源代价（高耗蓝参数，120秒）\n\n| 阶段 | 冻结后转水用药 | 冻结期间打冰锥用药 | 纯冰锥用药 | 冰锥＋锁用药 | 不用水流用药 |\n|---|---:|---:|---:|---:|---:|\n';
report += rows.filter(r => r.variant === 'proposed' && r.duration === 120 && r.ratio === 1.2).map(r => `| ${labels[r.stage]} | ${r.strategies.cycle.potions} | ${r.strategies.cycleFrozenIce.potions} | ${r.strategies.iceOnly.potions} | ${r.strategies.iceLock.potions} | ${r.strategies.iceBallLock.potions} |`).join('\n');
report += '\n\n## 使用技能的直接机会成本\n\n同基准B且全中时，水球k1.5整次预算6.75B；约1.37秒动作在冰锥1.2B下替代约1.64B直接伤害，尚有约5.11B净伤害收益。冰凌锁整次21.2B、占手0.75秒，替代约0.9B冰锥伤害。其约1.83秒攻击过程可以与之后的攻击重叠，不是1.83秒全程占手。以上尚未扣除额外魔力的机会成本或防御。\n\n';
report += '提高冰锥系数最先威胁水流的使用价值，而非冰凌锁。肉前没有冻结换冰凌锁奖励，水流加速下一次冻结主要带来控制，其输出竞争更脆弱。高耗蓝对长期续航有约束，但玩家在短战能用初始魔力和药剂换输出。不能将“高耗蓝”视为任意增伤的保障。\n\n';
report += result.assumptions.map(x => '- ' + x).join('\n') + '\n';
fs.writeFileSync(path.join(__dirname, 'ICE-STRATEGIES.md'), report);
console.log(table(rows.filter(r => r.variant === 'proposed' && r.duration === 120 && [1.2, 1.4, 1.5, 2].includes(r.ratio))));
console.log('\n300-second follow-up\n' + table(rows.filter(r => r.variant === 'proposed' && r.duration === 300 && [1.2, 1.5, 2].includes(r.ratio))));
