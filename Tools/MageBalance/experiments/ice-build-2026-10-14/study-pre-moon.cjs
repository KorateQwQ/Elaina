'use strict';
// Stage18 analysis only. Override the in-memory equipment setup, never a source file.
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
const crypto = require('node:crypto');
const E = require('./simulate.cjs'), S = require('./simulate-v3.cjs');
const M = require('../moon-lord-master-2026-10-14/simulate.cjs');
const original = JSON.parse(fs.readFileSync(path.join(__dirname, 'candidate-parameters-v5.json'), 'utf8')).parameters;
const core = JSON.parse(fs.readFileSync(path.join(__dirname, 'candidate-parameters-lock-core.json'), 'utf8')).parameters;
const inputs = ['../../index.html', '../../../../ElainaModSkills/Data/SkillBalance.json',
  'simulate.cjs', 'simulate-v3.cjs', 'candidate-parameters-v5.json', 'candidate-parameters-lock-core.json',
  '../moon-lord-master-2026-10-14/simulate.cjs'].map(f => path.resolve(__dirname, f));
const hash = f => crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const hashes = inputs.map(hash);
const keys = ['water', 'ice', 'ball', 'lock', 'thunder'];
const names = {water: '水流', ice: '冰锥', ball: '水球', lock: '冰凌锁', thunder: '天雷'};
const variants = {v5: original, lockCore: core};
const gear = Object.fromEntries(['arcane', 'damage'].map(prefix => {
  const {s, b} = M.gear({stage: 18, slots: 7, hat: true, prefix});
  assert.equal(s.baseDps, 1800);
  assert.equal(s.e.armor, 'spectre');
  assert.equal(s.e.items[3].id, 'elainaHat');
  return [prefix, {stage: 18, levelCap: 90, B: s.baseDps, armor: s.e.armor, slots: s.slots,
    items: s.e.items, maxMana: b.maxMana, regen: 5 + b.maxMana * .02,
    multiplier: b.e.multiplier, scale: s.baseDps * b.e.multiplier, vanillaDps: b.vanilla}];
}));
function compactDummy(r, parameters) {
  const total = r.dps * 300;
  return {dps: r.dps, ratio: r.ratio, scale: r.scale,
    share: Object.fromEntries(keys.map(k => [k, r.records[k].damage / total])),
    freezes: r.freezeCount, lockCasts: r.records.lock.count, potions: r.usedPotions, idle: r.idleSeconds,
    nominal: {waterDpsInCooldown: r.scale * .9, iceAttack: r.scale * .45, iceProjectile: r.scale * .15,
      ball: r.scale * 4.5 * parameters.ballK,
      lockAt3_8Seconds: r.scale * (parameters.lockBaseB + 3.8 * parameters.lockPerSecondB)}};
}
const dummy = [];
const originalSetup = E.setup;
try {
  E.setup = options => {
    const s = originalSetup(options);
    E.B.applyLoadout(s, 18, false);
    s.slots = options.slots ?? 7;
    if (options.hat) s.e.items[3].id = 'elainaHat';
    if (options.prefix === 'damage') E.B.applyDamagePrefixes(s, 'e');
    else for (const i of E.B.equippedSlots(s, 'e')) s.e.items[i].prefix = 'arcane';
    return s;
  };
  for (const slots of [6, 7]) for (const [variant, parameters] of Object.entries(variants)) {
    const r = S.pair({...parameters, slots, hat: true, regenPercent: 2, potionPercent: 50,
      filler: 'water', omit: ['thunder']});
    dummy.push({slots, variant, arcane: compactDummy(r.arcane, parameters), damage: compactDummy(r.damage, parameters)});
  }
} finally {
  E.setup = originalSetup;
}
assert.equal(E.setup, originalSetup);
function moon(options) {
  const runs = Array.from({length: 24}, (_, i) => M.run({stage: 18, slots: 7, hat: true,
    allowThunder: false, preBuild: ['ball'], regenPercent: 2, healthRegen: 2, healthPotionHeal: 150,
    lockPolicy: 'legacy', buildPolicy: 'focus',
    seed: i + 1, ...options}));
  assert(runs.every(r => r.B === 1800 && r.records.thunder.count === 0));
  const wins = runs.filter(r => r.killed), losses = runs.filter(r => !r.killed);
  const mean = (xs, fn) => xs.length ? xs.reduce((n, r) => n + fn(r), 0) / xs.length : null;
  return {samples: 24, completed: wins.length, died: runs.filter(r => r.playerDead).length,
    meanSeconds: mean(wins, r => r.time), minSeconds: wins.length ? Math.min(...wins.map(r => r.time)) : null,
    maxSeconds: wins.length ? Math.max(...wins.map(r => r.time)) : null,
    meanEffectiveDps: mean(wins, r => r.totalDamage / r.time),
    totalHpOverMeanTime: wins.length ? M.totalHP / mean(wins, r => r.time) : null,
    freezesAllSamples: mean(runs, r => r.freezeEvents.length), locksSuccessfulSamples: mean(wins, r => r.records.lock.count),
    shareSuccessfulSamples: Object.fromEntries(keys.map(k => [k, mean(wins, r => r.records[k].share)])),
    lockInvalidDamage: mean(wins, r => r.records.lock.invalidDamage), lockOverkill: mean(wins, r => r.records.lock.overkill),
    potions: mean(wins, r => r.usedPotions), meanLossTime: mean(losses, r => r.time),
    meanLossRemainingBossHp: mean(losses, r => r.totalHP - r.totalDamage)};
}
const boss = [];
for (const prefix of ['arcane', 'damage']) for (const profile of ['ideal', 'normal', 'heavy']) {
  for (const [variant, parameters] of Object.entries(variants)) {
    boss.push({prefix, profile, variant, result: moon({prefix, profile,
      tune: {lockPerSecondB: parameters.lockPerSecondB, ballB: 4.5 * parameters.ballK}})});
  }
}
const holdSensitivity = ['arcane', 'damage'].map(prefix => ({prefix,
  result: moon({prefix, profile: 'normal', tune: {lockPerSecondB: core.lockPerSecondB,
    ballB: 4.5 * core.ballK, lockHoldFreeze: true}})}));
assert(inputs.every((file, i) => hash(file) === hashes[i]), 'source and existing candidates unchanged');
const result = {date: '2026-10-14', experimentOnly: true, gear, dummy, boss, holdSensitivity,
  assumptions: {dummy: 'Stage18, B1800, spectre mask armor, hat, 300s, ideal hits, no defense or injuries, five50% mana potions, 2% recovery; six/seven slots reported separately',
    moon: 'Single-player Master, seven slots, no thunder,24 seeded profiles, 2HP/s survival and150HP potion; no nebula armor/buffs, spectre chase or lifesteal',
    damage: 'Nominal damage includes gear multiplier and expected crit; actual hit text and defended/overkill-adjusted damage differ. Shared stage B approximates skill training at the stage cap.',
    limits: 'Simplified opening/closing AI, illustrative hit/dodge/hurt profiles, no actual flight or MoonLeech healing, no global optimum proof.'},
  checks: ['stage18 B1800 and spectre armor confirmed', 'no thunder', 'in-memory setup restored',
    'existing simulator resource/time/shared-freeze invariants passed', 'source and existing candidates unchanged']};
fs.writeFileSync(path.join(__dirname, 'results-pre-moon.json'), JSON.stringify(result, null, 2) + '\n');
const f = (x, d = 1) => x === null ? '—' : x.toFixed(d), pct = x => x === null ? '—' : f(x * 100) + '%';
const select = (variant, prefix, profile) => boss.find(x => x.variant === variant && x.prefix === prefix && x.profile === profile).result;
const seven = variant => dummy.find(x => x.variant === variant && x.slots === 7);
const report = `# 月前冻结流派：当前方案与冰凌锁核心候选

模型修正说明：本报告月总表保留旧同目标策略作历史对照，其低冰凌锁占比不代表玩家跨部位操作。2026-10-14已修正选目标和反应等待策略；应以[跨部位引爆修正报告](REPORT-cross-targeting.md)评价月总效果。木桩预算和单次伤害仍可使用。

2026-10-14。改用月前阶段18、等级上限90、B1800、幽灵面具套、伊蕾娜帽子；没有星云套或星云强化，天雷不参与。两种方案分别为最终v5（锁每秒2.5B、水球8.1B）与上一轮加强候选（锁每秒4.5B、水球6.75B）。所有数字来自独立模型，非游戏实测。

配方核验补充：原版星云头/身/腿分别需要10/20/15星云碎片以及8/16/12夜明锭；全套36夜明锭，首次月总前不可制作。本地原版Recipe.cs第12059、12066、12073行有对应配方。当前伊蕾娜和KL源码未找到取消夜明锭要求的改动。因此本报告的幽灵套是原版首次挑战口径，阶段18默认配装并不因星云套而构成配置错误；若额外模组改变配方，应另设明确的兼容配装情景。

此前月后毕业装备打月总的数据不能作为月前首次挑战的主判断依据。月前战斗更长，可以完成约两次冻结；但不能据此推定冰凌锁实际贡献已经达标，需要区分触发冻结、成功施放、实际命中与过量伤害。

## 装备和数值口径

七槽为大师模式，帽子占其中一槽；巫师徽章、复仇者徽章、天界徽章，其余机动/生存占位。全奥秘上限620、回蓝17.4/s；全增伤上限480、回蓝14.6/s。双方不计算幽灵追击或吸血。原版对照DPS为${f(gear.arcane.vanillaDps)}。所有技能以当前阶段B1800作预算近似，实际技能训练等级较低时会更低。

## 300秒无防御木桩

七槽戴帽、满蓝开场、五瓶50%回蓝药、无受击、理想命中，采用相同固定循环。50%为当前游戏药效；此前月后木桩常用30%，不应直接拿两者做单变量阶段对照。

| 方案 | 奥秘DPS | 增伤DPS | 相对原版 奥秘/增伤 | 冰凌锁占比 奥秘/增伤 |
|---|---:|---:|---:|---:|
${Object.keys(variants).map(variant => {const x = seven(variant);return `| ${variant === 'v5' ? '原v5' : '冰凌锁核心候选'} | ${f(x.arcane.dps, 0)} | ${f(x.damage.dps, 0)} | ${f(x.arcane.ratio, 3)}/${f(x.damage.ratio, 3)} | ${pct(x.arcane.share.lock)}/${pct(x.damage.share.lock)} |`;}).join('\n')}

核心候选在月前全增伤已达${f(seven('lockCore').damage.ratio, 3)}倍原版，略超过2倍；因此上一轮“所测范围内达标”只适用于当时月后装备与30%药效条件，不能扩展到月前。六槽候选木桩为奥秘${f(dummy.find(x => x.variant === 'lockCore' && x.slots === 6).arcane.dps, 0)}／增伤${f(dummy.find(x => x.variant === 'lockCore' && x.slots === 6).damage.dps, 0)}DPS，也略超2倍的增伤范围。是否正式接入需要阶段校准。

## 单次预算伤害

七槽戴帽，已含装备与暴击期望，未扣目标防御、未扣过量伤害；不是每次弹出的伤害数字。冰凌锁按完美及时引爆、施法结束剩3.8秒计算，普通操作反应延迟会减少剩余时间。

| 技能 | 原v5 奥秘/增伤 | 核心候选 奥秘/增伤 |
|---|---:|---:|
${[['iceAttack', '冰锥整次三发'], ['ball', '水球整次'], ['lockAt3_8Seconds', '冰凌锁及时引爆']].map(([k, label]) => `| ${label} | ${f(seven('v5').arcane.nominal[k], 0)}/${f(seven('v5').damage.nominal[k], 0)} | ${f(seven('lockCore').arcane.nominal[k], 0)}/${f(seven('lockCore').damage.nominal[k], 0)} |`).join('\n')}

水流CD期间和冰锥的满时DPS均为奥秘${f(seven('lockCore').arcane.nominal.waterDpsInCooldown, 0)}／增伤${f(seven('lockCore').damage.nominal.waterDpsInCooldown, 0)}，主动占手与躲避会减少普攻输出时间。

## 首次挑战单人大师月总

七槽戴帽、先水球再冰锥、不使用天雷，沿用既有模型。普通操作：冰锥75%命中、每12秒停手1.5秒、每18秒受150点普通防御结算后的伤害；健康回复2HP/s、150HP治疗药。每行24个固定随机种子，成功样本不是实测成功率。

| 方案/配装 | 理想平均秒数 | 普通平均秒数 | 普通整场有效DPS | 普通冻结/施放锁次数 | 普通锁伤害占比 |
|---|---:|---:|---:|---:|---:|
${Object.keys(variants).flatMap(variant => ['arcane', 'damage'].map(prefix => {const n = select(variant, prefix, 'normal'), i = select(variant, prefix, 'ideal');return `| ${variant === 'v5' ? '原v5' : '核心候选'}/${prefix === 'arcane' ? '奥秘' : '增伤'} | ${f(i.meanSeconds)} | ${f(n.meanSeconds)} | ${f(n.totalHpOverMeanTime, 0)} | ${f(n.freezesAllSamples, 2)}/${f(n.locksSuccessfulSamples, 2)} | ${pct(n.shareSuccessfulSamples.lock)} |`;})).join('\n')}

有效DPS采用277311总血量除以平均战斗时间，包含闭眼、躲避和空打，与无防御木桩DPS不同。普通操作以上四组均24/24完成；高压力在当前生存假设下四组均0/24完成，约50秒阵亡，不能给出有效击杀时间。

月前确实有更多冻结机会：核心候选普通操作约2次，而上一轮月后无天雷约1次。冰凌锁占比仍偏低，说明“月后装备把Boss打得太快”只解释了部分问题。模型中仍出现冻结后未及时施放、1秒延迟期间目标关闭或死亡、以及过量伤害。普通奥秘核心候选平均约${f(select('lockCore', 'arcane', 'normal').lockInvalidDamage, 0)}点预算因目标不可受伤而丢失，另有约${f(select('lockCore', 'arcane', 'normal').lockOverkill, 0)}点过量伤害。

因此候选在普通操作并没有比原v5更快：奥秘${f(select('v5', 'arcane', 'normal').meanSeconds)}→${f(select('lockCore', 'arcane', 'normal').meanSeconds)}秒，增伤${f(select('v5', 'damage', 'normal').meanSeconds)}→${f(select('lockCore', 'damage', 'normal').meanSeconds)}秒。水球削弱能立即影响有效输出，锁的提高则未必兑现；时间差也包含离散AI窗口和命中序列变化，不能当作所有实战必然更慢的证明。

作为单独敏感性测试，保留冻结直到1秒延迟结束，核心候选普通奥秘为${f(holdSensitivity[0].result.meanSeconds)}秒、锁占${pct(holdSensitivity[0].result.shareSuccessfulSamples.lock)}；增伤为${f(holdSensitivity[1].result.meanSeconds)}秒、锁占${pct(holdSensitivity[1].result.shareSuccessfulSamples.lock)}。这仍不能保证短战占比，机制候选未接入正式游戏。

结论：月前的核心候选约7400～8900木桩DPS，单次及时引爆约7.8万～9.6万预算伤害。需要用月前配装作为首次挑战的主评估对象；不应根据月后刷Boss数据直接缩短积累，同时也不应仅凭木桩占比就确认当前候选能在月前稳定体现核心地位。

复跑：node Tools/MageBalance/experiments/ice-build-2026-10-14/study-pre-moon.cjs
`;
fs.writeFileSync(path.join(__dirname, 'REPORT-pre-moon.md'), report);
console.log(JSON.stringify({report: path.join(__dirname, 'REPORT-pre-moon.md'),
  dummySevenSlots: dummy.filter(x => x.slots === 7).map(x => ({variant: x.variant,
    dps: [x.arcane.dps, x.damage.dps], ratio: [x.arcane.ratio, x.damage.ratio]})), checks: result.checks}, null, 2));
