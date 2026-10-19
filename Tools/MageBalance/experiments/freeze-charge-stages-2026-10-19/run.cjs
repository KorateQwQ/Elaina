'use strict';
// Proposed-design analytical model; never patches gameplay code or balance JSON.
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../../../..');
const read = p => fs.readFileSync(path.join(root, p), 'utf8');
const configText = read('ElainaModSkills/Data/SkillBalance.json');
const config = JSON.parse(configText).configuration;
const context = { module: { exports: {} } };
vm.runInNewContext(read('Tools/MageBalance/index.html').match(/<script id="balance-engine">([\s\S]*?)<\/script>/)[1], context);
const B = context.module.exports;
const cycleText = read('ElainaModSkills/Skills/Ice/IceCycleBalance.cs');
const cycleConstant = name => Number(cycleText.match(new RegExp(name + ' = ([\\d.]+)f?;'))[1]);
const skillIds = { ice: 'IceConeSkill', water: 'WaterLaserSKill', ball: 'WaterBallSkill', lock: 'IceShardLockSkill' };
const labels = { ice: '冰锥', water: '水流', ball: '水球', lock: '冰凌锁' };
const stageNames = { 7: '巨鹿后', 8: '肉后初期', 13: '花后' };
const mean = xs => xs.length ? xs.reduce((a, b) => a + b, 0) / xs.length : null;
const intervals = xs => xs.slice(1).map((x, i) => x - xs[i]);
const f = (x, n = 1) => x === null ? '—' : x.toFixed(n);

function setup(stage, variant) {
  const s = B.clone(config);
  B.applyLoadout(s, stage, false);
  const level = stage * 5, gear = B.gear(s, 'e');
  const maxMana = B.calculate(s).maxMana;
  const skills = {};
  for (const [key, id] of Object.entries(skillIds)) {
    const row = [...s.basicAttacks, ...s.skills].find(x => x.id === id);
    if (key === 'ice' && variant === 'proposed') { row.cost = 22.5; row.freezeBuildup = 5.6; }
    const nodes = B.progressionNodes(row, s.progression.phaseSize, level);
    const damageLevel = nodes.at(-1) ?? null;
    const budget = damageLevel === null ? null : B.progressionBudget(s, row, damageLevel);
    const damage = budget === null ? 0 : Math.max(1, Math.floor(Math.fround(Math.fround(budget.total) * Math.fround(1 + gear.damage / 100)) / row.shots));
    skills[key] = { ...row, available: nodes.length > 0 && (key !== 'lock' || stage >= 8),
      skillLevel: nodes.length, damageLevel, baseline: budget?.baseline ?? null, damage };
  }
  return { stage, variant, level, loadout: s.loadouts[stage - 1], maxMana, recovery: 5 + .02 * maxMana, gear, skills };
}

function simulate(stage, variant, options = {}) {
  const build = setup(stage, variant), { skills: sk, gear, maxMana, recovery } = build;
  const duration = options.duration ?? 120;
  const maxTicks = Math.round(duration * 60), events = new Map();
  let mana = maxMana, minMana = mana, potions = options.potions ?? 5;
  let ballCD = 0, frozenUntil = -1, freezeCD = 0, buildup = 0, lockCharge = 0;
  let busyUntil = 0, channel = false, nextPulse = 0, firstPotion = null, seed = 20261019;
  const damage = { ice: 0, water: 0, ball: 0, lock: 0 };
  const casts = { ice: 0, water: 0, ball: 0, lock: 0 };
  const hits = { ice: 0, water: 0, ball: 0, lock: 0 };
  const handTicks = { ice: 0, water: 0, ball: 0, lock: 0, recovery: 0 };
  const lockTimes = [], freezeTimes = [], ballTimes = [], chargeGrants = [], manaSamples = [];
  const ledger = { spent: 0, regen: 0, potion: 0, overflow: 0, usedPotions: 0, idleTicks: 0 };
  function enqueue(t, key, amount) {
    if (!events.has(t)) events.set(t, []);
    events.get(t).push({ key, amount });
  }
  function canPay(cost) { return mana + potions * maxMana * .5 + 1e-6 >= cost; }
  function pay(cost, t) {
    if (!canPay(cost)) return false;
    while (mana + 1e-6 < cost && potions > 0) {
      mana += maxMana * .5; ledger.potion += maxMana * .5; ledger.usedPotions++; potions--;
      firstPotion ??= t / 60;
    }
    mana -= cost; ledger.spent += cost; minMana = Math.min(minMana, mana); return true;
  }
  function doHit(key, amount, t) {
    seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0;
    if (key !== 'lock' && seed / 4294967296 >= (options.hitChance ?? 1)) return;
    hits[key]++;
    if (key === 'water' && freezeCD <= 0) amount *= .7 / .9;
    damage[key] += amount * (1 + gear.crit / 100);
    if (key === 'ice' && t >= frozenUntil && freezeCD <= 0) {
      buildup += sk.ice.freezeBuildup;
      if (buildup >= 100) {
        buildup = 0; frozenUntil = t + 240; freezeTimes.push(t / 60);
        if (sk.lock.available && lockCharge === 0) { lockCharge = 1; chargeGrants.push(t / 60); }
      }
    }
    // In this proposal water only reduces the target's freeze cooldown.
    if (key === 'ball' || key === 'water') {
      freezeCD = Math.max(0, freezeCD - cycleConstant(key === 'ball' ? 'BallFreezeRefund' : 'BeamFreezeRefund') * 60);
    }
  }
  function occupy(key, ticks, t) { busyUntil = t + ticks; handTicks[key] += Math.min(ticks, maxTicks - t); }
  for (let t = 0; t < maxTicks; t++) {
    ballCD = Math.max(0, ballCD - 1);
    if (t === frozenUntil) freezeCD = 3600;
    else freezeCD = Math.max(0, freezeCD - 1);
    const regen = recovery / 60; ledger.regen += regen;
    ledger.overflow += Math.max(0, mana + regen - maxMana); mana = Math.min(maxMana, mana + regen);
    for (const e of events.get(t) ?? []) doHit(e.key, e.amount, t);
    events.delete(t);
    if (t % 60 === 0) manaSamples.push({ time: t / 60, mana });
    if (t < busyUntil) continue;
    const wantLock = lockCharge > 0 && canPay(sk.lock.cost);
    const wantBall = options.noBall !== true && sk.ball.available && ballCD <= 0 && canPay(sk.ball.cost);
    const wantIce = options.alwaysIce === true || (freezeCD <= 0 && (t >= frozenUntil || options.iceWhileFrozen === true));
    if (channel && (wantLock || wantBall || wantIce)) {
      channel = false; occupy('recovery', 20, t); continue;
    }
    if (wantLock && pay(sk.lock.cost, t)) {
      lockCharge = 0; casts.lock++; lockTimes.push(t / 60); occupy('lock', 45, t);
      const total = sk.lock.damage, final = Math.floor((total + 1) / 2), shards = total - final;
      // All shards have the same radial trajectory; arrivals are spawn delay + 28 frames.
      const spawnFrames = [1, 15, 27, 37, 44, 49, 52, 54, 56, 58, 60, 62, 64, 66, 68];
      spawnFrames.forEach((frame, i) => enqueue(t + frame + 28, 'lock', Math.floor(shards / 15) + (i < shards % 15 ? 1 : 0)));
      enqueue(t + cycleConstant('LockBurstFrame'), 'lock', final); continue;
    }
    if (wantBall && pay(sk.ball.cost, t)) {
      casts.ball++; ballTimes.push(t / 60); ballCD = Math.round(sk.ball.cd * 60);
      occupy('ball', 82, t); enqueue(t + 70, 'ball', sk.ball.damage); continue;
    }
    if (wantIce) {
      if (pay(sk.ice.cost, t)) {
        casts.ice++; occupy('ice', 45, t);
        for (const offset of [32, 35, 38]) enqueue(t + offset, 'ice', sk.ice.damage);
      } else ledger.idleTicks++;
    } else {
      if (!channel) { channel = true; nextPulse = t; }
      handTicks.water++;
      if (t >= nextPulse) {
        if (pay(sk.water.cost, t)) { casts.water++; doHit('water', sk.water.damage, t); nextPulse = t + Math.round(sk.water.interval * 60); }
        else { channel = false; occupy('recovery', 20, t); }
      }
    }
  }
  const total = Object.values(damage).reduce((a, b) => a + b, 0);
  const shares = Object.fromEntries(Object.entries(damage).map(([key, value]) => [key, 100 * value / total]));
  const residual = maxMana + ledger.regen + ledger.potion - ledger.overflow - ledger.spent - mana;
  assert(Math.abs(residual) < 1e-5, `Mana conservation: ${residual}`);
  assert(minMana >= -1e-6 && mana <= maxMana + 1e-6);
  assert(casts.lock <= chargeGrants.length && chargeGrants.length <= freezeTimes.length);
  assert(casts.lock <= freezeTimes.length && (lockTimes.length === 0 || lockTimes[0] >= freezeTimes[0]));
  assert(sk.lock.available || casts.lock === 0);
  assert(ballTimes.every((time, i) => i === 0 || time - ballTimes[i - 1] >= sk.ball.cd - 1e-6));
  const expectedLockDamage = sk.lock.damage * (1 + gear.crit / 100) * lockTimes.filter(t => t * 60 + 110 < maxTicks).length;
  assert(damage.lock + 1e-6 >= expectedLockDamage, 'Completed locks preserve their entire damage budget');
  return { build, options, duration, dps: total / duration, damage, shares, casts, hits, handTicks,
    meanLockInterval: mean(intervals(lockTimes)), meanFreezeInterval: mean(intervals(freezeTimes)),
    firstFreeze: freezeTimes[0] ?? null, firstLock: lockTimes[0] ?? null,
    lockTimes, freezeTimes, ballTimes, endMana: mana, minMana, firstPotion, ledger, manaSamples };
}

const rows = [];
for (const stage of [7, 8, 13]) for (const variant of ['current', 'proposed']) {
  rows.push({ name: `${stage}-${variant}-120`, ...simulate(stage, variant) });
  rows.push({ name: `${stage}-${variant}-300`, ...simulate(stage, variant, { duration: 300 }) });
  rows.push({ name: `${stage}-${variant}-120-no-potions`, ...simulate(stage, variant, { potions: 0 }) });
  rows.push({ name: `${stage}-${variant}-120-ice-while-frozen`, ...simulate(stage, variant, { iceWhileFrozen: true }) });
}
// A strategy check: before lock unlock, does the player have a damage incentive to cycle ice/water?
for (const variant of ['current', 'proposed']) {
  rows.push({ name: `7-${variant}-120-always-ice`, ...simulate(7, variant, { alwaysIce: true }) });
}
// Controlled sensitivity: isolate the recent water-ball budget increase in the recommended proposal.
const originalBallK = config.skills.find(s => s.id === 'WaterBallSkill').k;
config.skills.find(s => s.id === 'WaterBallSkill').k = 1.5;
for (const stage of [7, 8, 13]) rows.push({ name: `${stage}-proposed-120-ball-k1.5`, ...simulate(stage, 'proposed') });
config.skills.find(s => s.id === 'WaterBallSkill').k = originalBallK;

const result = {
  date: '2026-10-19', sourceSHA256: crypto.createHash('sha256').update(configText).digest('hex'),
  assumptions: [
    'Design proposal only: own freeze grants one lock charge; cap one; no opening charge, natural lock cooldown, lock-CD refunds or 15-second reward limiter.',
    'Lock damage is deliberately held at the current 21.2B budget and cost100; the cd60 field is only used for existing damage pricing, not availability.',
    'Planned unlock order: no lock at stage7, lock learned through the planned blizzard unlock at stage8 onward. That event is not implemented by this model.',
    'Current ice uses saved JSON; proposed ice overrides cost22.5 and freezeBuildup5.6, retaining saved ratio1.2 and interval0.75.',
    'Stage7/8/13 use levels35/40/65, saved loadouts, capped learned skill levels, full starting mana, Ashen Witch on.',
    'Runtime regeneration5+2% maximum mana; up to five50%-mana elixirs, except no-potions rows; no life conversion, shields, healing, incoming damage or extra buffs.',
    'Single persistent zero-defense target; expected standard crit, all attacks land, no armor procs including spectre or titanium procs.',
    'WaterBall uses current k3 except explicitly labeled k1.5 sensitivity; formal15-second cooldown, one single-target hit, no absorption/splash/terrain effects.',
    '45-frame ice/lock action, 20-frame water recovery, approximate82-frame minimum ball action; fixed short travel, no target motion or phase invulnerability.',
    'Priority lock > affordable ball > ice when freeze buildup is allowed > water otherwise, including frozen duration. Extra rows test ice during frozen duration or always-ice filler.',
    'Freeze240 frames, then cooldown3600 frames; water/ball subtract0.5/4 seconds. Lock never applies buildup or consumes elemental freeze.',
    'First-hit timings depend on fixed travel assumptions. DPS and shares are analytical estimates, not game measurements or specific boss kill times.'
  ], checks: ['resource conservation', 'mana bounds', 'lock charges require freezes', 'no pre-hardmode locks', 'ball formal cooldown spacing', 'completed lock damage budget'], rows
};
const currentHash = crypto.createHash('sha256').update(read('ElainaModSkills/Data/SkillBalance.json')).digest('hex');
assert.equal(currentHash, result.sourceSHA256, 'Configuration changed while running');
fs.writeFileSync(path.join(__dirname, 'results.json'), JSON.stringify(result, null, 2) + '\n');

function table(list) {
  return '| 阶段 | 冰锥 | 水流 | 水球 | 冰凌锁 | 总DPS | 冰凌锁次数 | 水球次数 | 药剂次数 |\n|---|---:|---:|---:|---:|---:|---:|---:|---:|\n'
    + list.map(r => `| ${stageNames[r.build.stage]} | ${f(r.shares.ice)}% | ${f(r.shares.water)}% | ${f(r.shares.ball)}% | ${f(r.shares.lock)}% | ${f(r.dps, 0)} | ${r.casts.lock} | ${r.casts.ball} | ${r.ledger.usedPotions} |`).join('\n');
}
const stageBuilds = [7, 8, 13].map(stage => setup(stage, 'proposed'));
let report = `# 冻结换冰凌锁：三个阶段的伤害分配\n\n2026-10-19。按拟议机制试算，未修改游戏逻辑或正式JSON。配置SHA256：\`${result.sourceSHA256}\`。\n\n`;
report += '## 配装与技能\n\n| 阶段 | 等级 | 护甲 | 魔力 | 回蓝/秒 | 冰锥等级 | 水流等级 | 水球等级 | 冰凌锁等级 |\n|---|---:|---|---:|---:|---:|---:|---:|---:|\n';
report += stageBuilds.map(b => `| ${stageNames[b.stage]} | ${b.level} | ${B.armors[b.loadout.armor].name} | ${b.maxMana} | ${f(b.recovery)} | ${b.skills.ice.skillLevel} | ${b.skills.water.skillLevel} | ${b.skills.ball.skillLevel} | ${b.skills.lock.available ? b.skills.lock.skillLevel : '未解锁'} |`).join('\n');
report += '\n\n阶段配装逐项快照在results.json中；巨鹿后6槽中只有5件有效饰品。肉后初期指按保存配装准备好钛金套并完成计划中的暴雪解锁，非刚击败血肉墙尚未采矿的瞬间。各技能均升满阶段上限；肉后冰凌锁若仅Lv1，伤害会低于本表Lv2。\n\n';
report += '## 120秒：当前冰锥数值＋冻结换次数\n\n冰锥1.2B、15魔力/轮、2.8积蓄/发；水球k=3。\n\n' + table(rows.filter(r => /-current-120$/.test(r.name))) + '\n\n';
report += '## 120秒：建议冰锥数值＋冻结换次数\n\n冰锥1.2B、22.5魔力/轮、5.6积蓄/发；水球k=3。\n\n' + table(rows.filter(r => /-proposed-120$/.test(r.name))) + '\n\n';
report += '## 300秒：建议参数的持续战斗\n\n' + table(rows.filter(r => /-proposed-300$/.test(r.name))) + '\n\n';
report += '## 建议参数的节奏与资源\n\n| 阶段 | 首次冻结 | 冰凌锁平均间隔 | 平均耗蓝/秒 | 最低魔力 | 120秒结束魔力 | 首次用药 |\n|---|---:|---:|---:|---:|---:|---:|\n';
report += rows.filter(r => /-proposed-120$/.test(r.name)).map(r => `| ${stageNames[r.build.stage]} | ${f(r.firstFreeze)}秒 | ${f(r.meanLockInterval)}秒 | ${f(r.ledger.spent/r.duration)} | ${f(r.minMana)} | ${f(r.endMana)} | ${r.firstPotion === null ? '未使用' : f(r.firstPotion)+'秒'} |`).join('\n');
report += '\n\n开场优先使用水球，故首次冻结时刻包含水球占手；4.5秒是冰锥本身所需的六轮施法时间，不是本模型从开战到冻结的总时间。\n\n';
report += '## 冻结期间继续冰锥的操作对照\n\n主表在冻结后转水系恢复魔力；以下在冻结4秒内继续冰锥输出，解冻后才转水系。\n\n' + table(rows.filter(r => /-proposed-120-ice-while-frozen$/.test(r.name))) + '\n\n';
report += '## 敏感性对照\n\n| 场景 | 总DPS | 冰锥占比 | 水球占比 | 冰凌锁占比 | 药剂次数 |\n|---|---:|---:|---:|---:|---:|\n';
report += rows.filter(r => /always-ice$|ball-k1.5$/.test(r.name)).map(r => `| ${r.name} | ${f(r.dps, 0)} | ${f(r.shares.ice)}% | ${f(r.shares.ball)}% | ${f(r.shares.lock)}% | ${r.ledger.usedPotions} |`).join('\n');
report += '\n\n## 模型限制\n\n' + result.assumptions.map(x => '- ' + x).join('\n');
report += '\n\n花后仅计算幽灵套的静态属性，幽灵追击未模拟，不能将表中总DPS称为完整幽灵套实战DPS。未计防御、多部位、溅射、空枪或生存消耗。冻结奖励机制尚未正式接入，这些数值是设计方案比较。\n\n';
report += '运行 `node Tools/MageBalance/experiments/freeze-charge-stages-2026-10-19/run.cjs` 刷新结果和本报告。检查：' + result.checks.join('、') + '。\n';
fs.writeFileSync(path.join(__dirname, 'REPORT.md'), report);
console.log(JSON.stringify(rows.map(r => ({ name: r.name, dps: r.dps, shares: r.shares, casts: r.casts,
  firstFreeze: r.firstFreeze, lockInterval: r.meanLockInterval, freezeInterval: r.meanFreezeInterval,
  potions: r.ledger.usedPotions, firstPotion: r.firstPotion, mana: r.endMana, spentPerSecond: r.ledger.spent/r.duration,
  idleSeconds: r.ledger.idleTicks/60 })), null, 2));
