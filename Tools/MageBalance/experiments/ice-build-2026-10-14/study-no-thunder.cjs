'use strict';
// Independent analysis: sustainable ice rotation and an optional costly ultimate.
const fs = require('node:fs'), path = require('node:path');
const assert = require('node:assert/strict'), crypto = require('node:crypto');
const S = require('./simulate-v3.cjs');
const M = require('../moon-lord-master-2026-10-14/simulate.cjs');
const p = JSON.parse(fs.readFileSync(path.join(__dirname, 'candidate-parameters-v5.json'), 'utf8')).parameters;
const protectedFiles = [
  '../../index.html', '../../../../ElainaModSkills/Data/SkillBalance.json',
  '../../../../ElainaAttribute/ElainaMagicAttributes.cs',
  'REPORT-v5.md', 'candidate-parameters-v5.json', 'results-v5.json',
  '../moon-lord-master-2026-10-14/REPORT.md', '../moon-lord-master-2026-10-14/results.json',
].map(f => path.resolve(__dirname, f));
const hash = f => crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const before = protectedFiles.map(hash);
assert(/CombatMaxMagicPointRecoveryPercentPerSecond\s*=>\s*0\.02f/.test(fs.readFileSync(protectedFiles[2], 'utf8')));
const base = {...p, filler: 'water', regenPercent: 2};
const keys = ['water', 'ice', 'ball', 'lock', 'thunder'];
const names = {water: '水流', ice: '冰锥', ball: '水球', lock: '冰凌锁', thunder: '天雷'};
function compact(r) {
  const total = Object.values(r.records).reduce((n, x) => n + x.damage, 0);
  const share = Object.fromEntries(keys.map(k => [k, r.records[k].damage / total]));
  assert(Math.abs(Object.values(share).reduce((a, b) => a + b, 0) - 1) < 1e-9);
  return {ratio: r.ratio, dps: r.dps, share, basicShare: share.water + share.ice,
    freezes: r.freezeCount, casts: Object.fromEntries(keys.map(k => [k, r.records[k].count])),
    potions: r.usedPotions, idle: r.idleSeconds, endMana: r.endMana, maxMana: r.M, regen: r.regen,
    thunderPaidSeconds: r.thunderPaidSeconds,
    policy: r.options.policy, order: r.options.order, reserveHorizon: r.options.reserveHorizon};
}
function pair(options, search = false) {
  const r = search ? S.search(options) : S.pair(options);
  return {arcane: compact(r.arcane), damage: compact(r.damage), arcaneLead: r.lead};
}
const dummy = Object.fromEntries([false, true].map(hat => [hat ? 'hat' : 'noHat', {
  withThunder: pair({...base, hat}),
  withoutThunder: pair({...base, hat, omit: ['thunder']}),
  searchedWithoutThunder: pair({...base, hat, omit: ['thunder']}, true),
}]));
const nerfs = [.9, .8, .7].map(ratio => ({ratio,
  result: pair({...base, hat: true, omit: ['thunder'], iceRatio: ratio, waterRatio: ratio, waterReadyRatio: .7 * ratio / .9}),
}));
function moon(options) {
  const runs = Array.from({length: 24}, (_, i) => M.run({stage: 19, hat: true,
    healthRegen: 9, healthPotionHeal: 200, regenPercent: 2, seed: i + 1,
    lockPolicy: 'legacy', buildPolicy: 'focus', ...options}));
  const wins = runs.filter(r => r.killed);
  const mean = fn => wins.length ? wins.reduce((n, r) => n + fn(r), 0) / wins.length : null;
  if (options.allowThunder === false) assert(runs.every(r => r.records.thunder.count === 0));
  return {samples: 24, completed: wins.length,
    meanSeconds: mean(r => r.time), minSeconds: wins.length ? Math.min(...wins.map(r => r.time)) : null,
    maxSeconds: wins.length ? Math.max(...wins.map(r => r.time)) : null,
    freezes: mean(r => r.freezeEvents.length), locks: mean(r => r.lockEvents.length),
    potions: mean(r => r.usedPotions), endMana: mean(r => r.endMana),
    share: Object.fromEntries(keys.map(k => [k, mean(r => r.records[k].share)]))};
}
const boss = [];
for (const prefix of ['arcane', 'damage']) for (const profile of ['ideal', 'normal', 'heavy']) {
  for (const preBuild of [[], ['ball']]) boss.push({prefix, profile, preBuild,
    result: moon({prefix, profile, preBuild, allowThunder: false})});
}
const ultimateCandidates = [];
for (const startup of [200, 300]) for (const drain of [15, 20]) {
  ultimateCandidates.push({startup, drain, fullCost: startup + drain * 30, cooldown: 180, damageBudgetB: 48,
    result: pair({...base, hat: true, thunderStartup: startup, thunderDrain: drain, thunderCD: 180})});
}
assert(protectedFiles.every((f, i) => hash(f) === before[i]), 'existing inputs and formal configuration unchanged');
const results = {date: '2026-10-14', experimentOnly: true, parameters: p, regenPercent: 2,
  assumptions: {dummy: '300s, six slots, graduation, nebula damage three stacks, no injury, five 30% mana potions',
    moon: 'Seven slots, hat, graduation, 24 fixed seeds, maintained nebula life regen 9HP/s, five 50% mana potions; illustrative hit/dodge profiles',
    ultimate: 'Cost/CD sensitivity only. No earned ultimate gauge or limited charge system simulated. Damage kept at 48B regardless of cost.',
    limits: 'Existing simplified simulators; no game measurement or global optimum proof. Moon movement and projectiles are idealized, MoonLeech healing omitted.'},
  dummy, nerfs, boss, ultimateCandidates, inputHashes: Object.fromEntries(protectedFiles.map((f, i) => [f, before[i]]))};
fs.writeFileSync(path.join(__dirname, 'results-no-thunder.json'), JSON.stringify(results, null, 2) + '\n');
const f = (n, d = 3) => n === null ? '—' : n.toFixed(d), pct = n => f(n * 100, 1) + '%';
const a = dummy.hat.withoutThunder.arcane, d = dummy.hat.withoutThunder.damage;
const normal = prefix => boss.find(x => x.prefix === prefix && x.profile === 'normal' && x.preBuild.length).result;
const an = normal('arcane'), dn = normal('damage');
const report = `# 冻结流派：无天雷基线与奥义定位分析

模型修正说明：本报告月总数据使用旧同目标策略，未充分计入跨部位引爆与玩家调度。最新月前月总对照见[跨部位引爆修正报告](REPORT-cross-targeting.md)；木桩与奥义费用分析不受此修正影响。

2026-10-14。沿用最终v5参数，按当前代码2%回蓝重新计算。本次仅创建分析文件，现有候选、报告、正式配置和游戏代码均未改动。

结论：水流＋冰锥＋水球＋冰凌锁可以独立形成毕业循环。天雷适合额外的奥义选择，不必计入常规流派达标基线。现阶段不建议削弱普攻；优先确定常规强度目标、奥义可用条件和启动代价。

## 300秒木桩

毕业六槽、星云伤害满层、无受击、五瓶30%回蓝药。原版参考沿用同一装备预算，倍率均为整场DPS/原版。策略搜索只覆盖已有有限规则，不证明全局最优。

| 配装 | 有天雷固定循环 奥秘/增伤 | 无天雷固定循环 奥秘/增伤 | 无天雷所测最高 奥秘/增伤 |
|---|---:|---:|---:|
${['noHat', 'hat'].map(k => {const x = dummy[k];return `| ${k === 'hat' ? '戴帽' : '无帽'} | ${f(x.withThunder.arcane.ratio)}/${f(x.withThunder.damage.ratio)} | ${f(x.withoutThunder.arcane.ratio)}/${f(x.withoutThunder.damage.ratio)} | ${f(x.searchedWithoutThunder.arcane.ratio)}/${f(x.searchedWithoutThunder.damage.ratio)} |`;}).join('\n')}

无天雷戴帽常规循环仍在1.5～2倍范围；无帽全奥秘${f(dummy.noHat.withoutThunder.arcane.ratio)}倍，略低于1.5倍。若要求无帽也必须达标，应另补常规技能预算，不能用低可用率奥义填补这个缺口。

| 常规技能 | 戴帽奥秘伤害占比 | 戴帽增伤伤害占比 |
|---|---:|---:|
${keys.filter(k => k !== 'thunder').map(k => `| ${names[k]} | ${pct(a.share[k])} | ${pct(d.share[k])} |`).join('\n')}

两种普攻合计${pct(a.basicShare)}／${pct(d.basicShare)}，主动水球和冰凌锁合计其余伤害。普攻同时负责减冻结CD和积累冻结，当前占比可作为常规循环基线，不宜继续大幅削弱。

| 同比降低水/冰普攻 | 无天雷戴帽DPS/原版 奥秘/增伤 |
|---|---:|
${nerfs.map(x => `| ${f(x.ratio, 1)}B | ${f(x.result.arcane.ratio)}/${f(x.result.damage.ratio)} |`).join('\n')}

本对照同步缩放水流CD外伤害，保持耗蓝、积累及主动伤害不变。0.8B使奥秘接近下限，0.7B使其跌到下限以下。

无天雷时全奥秘相对全增伤固定循环少${f((1 - a.dps / d.dps) * 100, 1)}%输出，所测最高策略少${f((1 - dummy.hat.searchedWithoutThunder.arcane.dps / dummy.hat.searchedWithoutThunder.damage.dps) * 100, 1)}%。水球和冰凌锁受CD/冻结窗口限制，多出的魔力无法无限转成伤害；原先依靠天雷大额消耗形成的前缀平衡不再成立。是否需要持平，应与盾的资源价值一并决定。

## 月总短战

毕业七槽戴帽、单人大师，使用既有闭眼、命中、躲避和生存模型。下表均为开场水球后冰锥，无天雷；每格24种固定随机种子。

| 操作 | 奥秘平均秒数/完成样本 | 增伤平均秒数/完成样本 |
|---|---:|---:|
${['ideal', 'normal', 'heavy'].map(profile => {const xs = ['arcane', 'damage'].map(prefix => boss.find(x => x.prefix === prefix && x.profile === profile && x.preBuild.length).result);return `| ${{ideal:'理想',normal:'普通',heavy:'高压力'}[profile]} | ${f(xs[0].meanSeconds, 1)}秒/${xs[0].completed} | ${f(xs[1].meanSeconds, 1)}秒/${xs[1].completed} |`;}).join('\n')}

普通操作全奥秘水流${pct(an.share.water)}、冰锥${pct(an.share.ice)}、水球${pct(an.share.ball)}、冰凌锁${pct(an.share.lock)}；平均${f(an.freezes, 2)}次冻结、${f(an.locks, 2)}次冰凌锁，${f(an.potions, 2)}瓶回蓝药。移除天雷后，冻结联动有机会兑现。

增伤普通操作冰凌锁只占${pct(dn.share.lock)}，平均冻结${f(dn.freezes, 2)}次、施放冰凌锁${f(dn.locks, 2)}次。短战中目标在冻结后很快死亡、后续目标未必能及时引爆，冻结身份在所有配装上仍未充分保证。上述完成样本不是实测成功率。

## 天雷作为奥义

当前零启动费、15蓝/秒持续30秒，总费用450蓝。七条奥秘戴帽魔力上限620，自然回蓝17.4蓝/秒，30秒回复522蓝，超过天雷自身450蓝消耗；普攻、其他技能和盾仍会争用魔力，但单独维持天雷的门槛较低。120秒CD也允许300秒木桩放三轮。

建议把“获得释放资格”和“支付释放代价”分别定义：

- 常规四技能按无天雷基线定价。奥义属于额外爆发，不承担维持流派基础强度的职责。
- 可优先试验完成2～3轮冻结引爆后获得一次奥义资格，释放消耗资格。首次进入战斗从零开始，重复施放需重新积累。击杀、脱战、换目标与联机归属需要明确规则。该资格机制本次未模拟。
- 在资格之外设置启动耗蓝，例如200蓝；维持15蓝/秒，共650蓝。启动扣费立即减少盾与后续冰凌锁的可用魔力，使释放时机有选择成本。
- 不因提高费用或延长CD自动增加天雷伤害。本次费用对照固定48B预算；实际爆发时机、命中和持续时间再单独验证。

只测试启动费和CD的对照如下。全部仍允许按CD自动尝试释放，没有奥义资格；CD暂设180秒、持续30秒、伤害48B，固定策略。

| 启动蓝/每秒蓝 | 完整费用 | 300秒施放次数 奥秘/增伤 | 戴帽DPS/原版 奥秘/增伤 |
|---|---:|---:|---:|
${ultimateCandidates.map(x => `| ${x.startup}/${x.drain} | ${x.fullCost} | ${x.result.arcane.casts.thunder}/${x.result.damage.casts.thunder} | ${f(x.result.arcane.ratio)}/${f(x.result.damage.ratio)} |`).join('\n')}

费用不能一味上调。300启动＋20/秒共900蓝时，奥秘固定策略输出${f(ultimateCandidates.find(x => x.startup === 300 && x.drain === 20).result.arcane.ratio)}倍，已略低于不放天雷的${f(a.ratio)}倍；它挤掉常规技能的资源，收益不足以抵偿。200启动＋15/秒适合作为首先验证的成本候选，奥义资格和短时爆发价值仍需额外建模。

复跑：node Tools/MageBalance/experiments/ice-build-2026-10-14/study-no-thunder.cjs
`;
fs.writeFileSync(path.join(__dirname, 'REPORT-no-thunder.md'), report);
console.log(JSON.stringify({report: path.join(__dirname, 'REPORT-no-thunder.md'),
  noThunderHatRatios: [a.ratio, d.ratio], ordinaryMoonSeconds: [an.meanSeconds, dn.meanSeconds],
  ultimateCandidates: ultimateCandidates.map(x => ({startup: x.startup, drain: x.drain,
    ratios: [x.result.arcane.ratio, x.result.damage.ratio]})), protectedInputsUnchanged: true}, null, 2));
