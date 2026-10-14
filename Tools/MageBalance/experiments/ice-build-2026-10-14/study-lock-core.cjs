'use strict';
// Experimental budget reallocation only; live configuration is left untouched.
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
const crypto = require('node:crypto');
const S = require('./simulate-v3.cjs'), M = require('../moon-lord-master-2026-10-14/simulate.cjs');
const p = JSON.parse(fs.readFileSync(path.join(__dirname, 'candidate-parameters-v5.json'), 'utf8')).parameters;
const protectedFiles = ['../../index.html', '../../../../ElainaModSkills/Data/SkillBalance.json',
  '../../../../ElainaAttribute/ElainaMagicAttributes.cs', 'candidate-parameters-v5.json',
  'REPORT-v5.md', 'results-v5.json', 'REPORT-no-thunder.md', 'results-no-thunder.json',
  'simulate-v3.cjs', '../moon-lord-master-2026-10-14/simulate.cjs',
  '../moon-lord-master-2026-10-14/REPORT.md', '../moon-lord-master-2026-10-14/results.json',
].map(f => path.resolve(__dirname, f));
const hash = f => crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const before = protectedFiles.map(hash);
const base = {...p, filler: 'water', regenPercent: 2, omit: ['thunder']};
const recommended = {...base, lockPerSecondB: 4.5, ballK: 1.5};
const keys = ['water', 'ice', 'ball', 'lock', 'thunder'];
const names = {water: '水流', ice: '冰锥', ball: '水球', lock: '冰凌锁', thunder: '天雷'};
function compact(r) {
  const total = r.dps * 300;
  const share = Object.fromEntries(keys.map(k => [k, r.records[k].damage / total]));
  assert(Math.abs(Object.values(share).reduce((a, b) => a + b, 0) - 1) < 1e-9);
  return {ratio: r.ratio, share, freezes: r.freezeCount, potions: r.usedPotions,
    basicHandIceShare: r.records.ice.handSeconds / (r.records.water.handSeconds + r.records.ice.handSeconds),
    casts: Object.fromEntries(keys.map(k => [k, r.records[k].count])),
    idle: r.idleSeconds, endMana: r.endMana, policy: r.options.policy,
    order: r.options.order, reserveHorizon: r.options.reserveHorizon};
}
function pair(options, search = false) {
  const result = search ? S.search(options) : S.pair(options);
  return {arcane: compact(result.arcane), damage: compact(result.damage)};
}
const scan = [];
for (const lockPerSecondB of [2.5, 4, 4.5, 5, 5.5, 6]) for (const ballK of [1.8, 1.5, 1.4, 1.3]) {
  const options = {...base, hat: true, lockPerSecondB, ballK};
  scan.push({lockPerSecondB, ballK, fixed: pair(options), searched: pair(options, true)});
}
const cases = Object.fromEntries([['baseline', base], ['lockOnly', {...base, lockPerSecondB: 4.5}],
  ['recommended', recommended]].map(([name, options]) => [name,
  Object.fromEntries([false, true].map(hat => [hat ? 'hat' : 'noHat',
    {fixed: pair({...options, hat}), searched: pair({...options, hat}, true)}]))]));
for (const gear of Object.values(cases.recommended)) for (const strategy of Object.values(gear)) {
  for (const r of Object.values(strategy)) {
    assert(r.ratio >= 1.5 && r.ratio <= 2, 'ordinary graduation target range');
    assert(r.share.lock >= .35 && r.share.lock <= .4, 'core skill share target');
    assert(['water', 'ice', 'ball'].every(k => r.share.lock > r.share[k]), 'lock is largest single damage source');
    // Whole-action boundary: the no-hat searched damage case is49.994%, which rounds to50.0%.
    assert(r.basicHandIceShare >= .5 - 1e-4 && r.basicHandIceShare <= .6, 'water/ice hand-time balance within 0.01 percentage point');
    assert.equal(r.casts.thunder, 0);
  }
}
const ablations = [['thunder'], ['thunder', 'lock'], ['thunder', 'ice', 'lock'], ['thunder', 'ball']]
  .map(omit => ({omit, result: pair({...recommended, hat: true, omit}, true)}));
const alternative = pair({...recommended, hat: true, filler: 'ice'}, true);
assert(cases.recommended.hat.searched.arcane.ratio > alternative.arcane.ratio);
assert(cases.recommended.hat.searched.damage.ratio > alternative.damage.ratio);
function moon(prefix, profile, tune) {
  const runs = Array.from({length: 24}, (_, i) => M.run({stage: 19, hat: true, prefix, profile,
    allowThunder: false, preBuild: ['ball'], regenPercent: 2, healthRegen: 9,
    lockPolicy: 'legacy', buildPolicy: 'focus',
    healthPotionHeal: 200, seed: i + 1, tune}));
  const wins = runs.filter(r => r.killed);
  const mean = fn => wins.length ? wins.reduce((n, r) => n + fn(r), 0) / wins.length : null;
  return {samples: 24, completed: wins.length, meanSeconds: mean(r => r.time),
    share: Object.fromEntries(keys.map(k => [k, mean(r => r.records[k].share)])),
    freezes: mean(r => r.freezeEvents.length), lockCasts: mean(r => r.records.lock.count),
    lockInvalidDamage: mean(r => r.records.lock.invalidDamage), lockOverkill: mean(r => r.records.lock.overkill)};
}
const boss = [];
for (const prefix of ['arcane', 'damage']) for (const profile of ['ideal', 'normal', 'heavy']) {
  for (const [name, tune] of [['baseline', {}], ['recommended', {ballB: 6.75, lockPerSecondB: 4.5}],
    ['holdFreezeSensitivity', {ballB: 6.75, lockPerSecondB: 4.5, lockHoldFreeze: true}]]) {
    boss.push({prefix, profile, name, result: moon(prefix, profile, tune)});
  }
}
assert(protectedFiles.every((f, i) => hash(f) === before[i]), 'existing experiments and formal files unchanged');
const checks = ['recommended ordinary rotation stays between 1.5x and 2x in both hats/prefixes/strategy sets',
  'lock contributes 35-40% and is the largest single damage source',
  'ice keeps approximately 50-60% of basic attack hand time (0.01 percentage point boundary tolerance)', 'no thunder casts',
  'water cooldown strategy beats compared ice filler', 'existing simulator mana/time/freeze invariants passed',
  'existing experiments and formal files unchanged'];
const result = {date: '2026-10-14', experimentOnly: true, recommended, scan, cases, ablations, alternative, boss, checks,
  assumptions: {dummy: '300 seconds, six slots, stage19, nebula damage three stacks, no injury, five 30% mana potions, 2% recovery',
    moon: 'Seven slots, hat, stage19, no thunder, ball opener, maintained nebula life recovery9HP/s, 24 illustrative seeded hit/dodge profiles',
    scope: 'Candidate budget for the ice rotation only. No live skill values or other builds modified. Short-fight core identity is not solved by the damage reallocation.',
    limits: 'No in-game measurement, no global optimum proof, no real projectile movement or MoonLeech healing.'}};
fs.writeFileSync(path.join(__dirname, 'results-lock-core.json'), JSON.stringify(result, null, 2) + '\n');
fs.writeFileSync(path.join(__dirname, 'candidate-parameters-lock-core.json'), JSON.stringify({experimentOnly: true,
  description: 'Ice lock as core finisher; thunder excluded from ordinary rotation. Budget candidate, not live configuration.',
  parameters: {...p, lockPerSecondB: 4.5, ballK: 1.5}, ordinaryRotation: ['water', 'ice', 'ball', 'lock'],
  regenPercent: 2, shortFightIdentityResolved: false}, null, 2) + '\n');
const f = (n, digits = 3) => n === null ? '—' : n.toFixed(digits), pct = n => f(n * 100, 1) + '%';
const old = cases.baseline.hat.fixed, next = cases.recommended.hat.fixed;
const normal = (name, prefix) => boss.find(x => x.name === name && x.prefix === prefix && x.profile === 'normal').result;
const report = `# 冰凌锁作为冻结流派核心：预算调整候选

模型修正说明：本报告月总表保留旧同目标策略，不能代表跨部位引爆的玩家收益。最新月前月总结果见[跨部位引爆修正报告](REPORT-cross-targeting.md)；长战预算分析仍可使用。

2026-10-14。仅评估无天雷的常规冻结循环，使用当前2%自然回蓝。天雷继续作为独立奥义，不计入常规达标基线。本次新增实验记录，未修改正式配置和既有报告。

当前冰凌锁约24%伤害，与普攻接近，并低于全奥秘配装水球约29%。按“冰凌锁是流派核心终结技”的目标，这份分配不足。候选目标为长战占35%～40%，成为最大单项伤害来源；水球保留推进冻结CD和施加潮湿的功能，普攻保留积累与填充输出。

## 推荐参数

| 项目 | 原值 | 候选 |
|---|---:|---:|
| 冰凌锁基础伤害 | 3.2B | 3.2B |
| 冰凌锁每秒剩余冻结伤害 | 2.5B | 4.5B |
| 0.2秒施法后剩3.8秒时整次伤害 | 12.7B | 20.3B |
| 水球K/整次伤害 | 1.8/8.1B | 1.5/6.75B |

冰凌锁满窗口伤害增加约59.8%，增量全部来自成功冻结的条件收益，无冻结时仍只有3.2B。水球伤害降低16.7%，150蓝、15秒CD、1.35秒施法、每次减冻结CD4秒和15秒潮湿均保留。冰凌锁100蓝、20秒自身CD、0.2秒施法、1秒命中延迟，14秒有效冰锥积累及两种普攻伤害均保留。

这是冻结流派预算候选。正式接入时要核查通用水球在其他流派的作用，不应未经比较就把所有流派一起调低。

## 300秒伤害构成

毕业六槽戴帽、星云伤害满层、五瓶30%回蓝药、无受击，同一固定循环。

| 技能 | 原奥秘 | 候选奥秘 | 原增伤 | 候选增伤 |
|---|---:|---:|---:|---:|
${keys.filter(k => k !== 'thunder').map(k => `| ${names[k]} | ${pct(old.arcane.share[k])} | ${pct(next.arcane.share[k])} | ${pct(old.damage.share[k])} | ${pct(next.damage.share[k])} |`).join('\n')}

| 配装 | 原DPS/原版 奥秘/增伤 | 候选固定循环 奥秘/增伤 | 候选所测最高 奥秘/增伤 |
|---|---:|---:|---:|
${['noHat', 'hat'].map(k => {const a = cases.baseline[k].fixed, b = cases.recommended[k];return `| ${k === 'hat' ? '戴帽' : '无帽'} | ${f(a.arcane.ratio)}/${f(a.damage.ratio)} | ${f(b.fixed.arcane.ratio)}/${f(b.fixed.damage.ratio)} | ${f(b.searched.arcane.ratio)}/${f(b.searched.damage.ratio)} |`;}).join('\n')}

所测策略的冰凌锁占比为35.0%～37.2%，均为最大单项输出。冰锥仍占普攻动作时间约50%～51%，冻结次数不因伤害参数调整改变，保留低切换循环。搜索只覆盖已有资源策略、优先级和预留窗口，不能证明全局最优。

只提高冻结伤害系数到4.5、保留水球8.1B时，戴帽增伤所测最高为${f(cases.lockOnly.hat.searched.damage.ratio)}倍，超出2倍。因此推荐同时重分配水球伤害预算。更强的每秒5B方案即便水球K降到1.5，所测最高仍约2.046倍；未作为推荐。

移除冰凌锁后，候选戴帽所测最高降到奥秘${f(ablations[1].result.arcane.ratio)}／增伤${f(ablations[1].result.damage.ratio)}倍，流派对核心终结技的依赖已经明显建立。这是技能移除后的整个循环差值，不能全部当作冰凌锁直接伤害占比。

## 月总短战仍需处理兑现时机

七槽戴帽、无天雷、先水球再冰锥，采用既有简化AI窗口与命中/停手假设，24个固定随机种子。下面只列普通操作，完成样本均24/24，并非实测成功率。

| 配装/方案 | 平均秒数 | 冰凌锁伤害占比 | 水球伤害占比 |
|---|---:|---:|---:|
${['arcane', 'damage'].flatMap(prefix => ['baseline', 'recommended', 'holdFreezeSensitivity'].map(name => {const x = normal(name, prefix);return `| ${prefix === 'arcane' ? '奥秘' : '增伤'}/${{baseline:'原值',recommended:'预算候选',holdFreezeSensitivity:'候选＋冻结保留至延迟命中'}[name]} | ${f(x.meanSeconds, 1)} | ${pct(x.share.lock)} | ${pct(x.share.ball)} |`;})).join('\n')}

提高单次预算无法保证短战占比。目标在14秒有效冰锥积累期间已经损失大量血量，引爆伤害经常超过其剩余血量；月总分部位结算，不能把一次引爆预算免费复制给全部部位。增伤普通操作候选冰凌锁平均过量伤害约${f(normal('recommended', 'damage').lockOverkill, 0)}，这部分不计入实际伤害。

此外，施法完成即消耗冻结、1秒后才命中，眼睛可能在这1秒内关闭。保留冻结到命中结束的敏感性测试可以去除所测闭眼丢伤，但奥秘占比也只到${pct(normal('holdFreezeSensitivity', 'arcane').share.lock)}，增伤仍约${pct(normal('holdFreezeSensitivity', 'damage').share.lock)}；说明过量伤害与首轮窗口才是另一个主要问题。本候选没有改动正式读取/消耗冻结的机制。

结论：长战预算已能让冰凌锁承担核心地位；短战核心身份尚未解决。下一步应针对首轮积累门槛、引爆落点与伤害落地时机单独比较，而不是继续无限提高单次倍率。不要据木桩35%～37%的结果承诺月总也能达到同样占比。

## 验证

${checks.map(x => '- ' + x).join('\n')}

复跑：node Tools/MageBalance/experiments/ice-build-2026-10-14/study-lock-core.cjs
`;
fs.writeFileSync(path.join(__dirname, 'REPORT-lock-core.md'), report);
console.log(JSON.stringify({report: path.join(__dirname, 'REPORT-lock-core.md'),
  recommendedHat: cases.recommended.hat, checks}, null, 2));
