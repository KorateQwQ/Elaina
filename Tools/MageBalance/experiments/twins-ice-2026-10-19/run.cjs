'use strict';
// Analytical single-target model, not a Terraria runtime or boss fight.
const fs = require('node:fs'), path = require('node:path'), vm = require('node:vm'), crypto = require('node:crypto');
const root = path.resolve(__dirname, '../../../..');
const read = p => fs.readFileSync(path.join(root, p), 'utf8');
const context = { module: { exports: {} } };
vm.runInNewContext(read('Tools/MageBalance/index.html').match(/<script id="balance-engine">([\s\S]*?)<\/script>/)[1], context);
const B = context.module.exports;
const sourceText = read('ElainaModSkills/Data/SkillBalance.json');
const source = JSON.parse(sourceText).configuration;
const cycle = read('ElainaModSkills/Skills/Ice/IceCycleBalance.cs');
function constant(name) { return Number(cycle.match(new RegExp(name + ' = ([\\d.]+)f?;'))[1]); }
const base = B.clone(source);
B.applyLoadout(base, 10, false);
const names = { ice: 'IceConeSkill', water: 'WaterLaserSKill', lock: 'IceShardLockSkill', ball: 'WaterBallSkill' };
function setup(s, stageBall = false) {
  const gear = B.gear(s, 'e'), maxMana = B.calculate(s).maxMana;
  const skills = {};
  for (const [key, id] of Object.entries(names)) {
    const row = [...s.basicAttacks, ...s.skills].find(x => x.id === id);
    const nodes = B.progressionNodes(row, s.progression.phaseSize, 50);
    const level = stageBall && key === 'ball' ? 50 : nodes.at(-1) ?? row.unlockLevel;
    const budget = B.progressionBudget(s, row, level);
    // Mirrors game-side float damage application and per-hit flooring.
    const damage = Math.max(1, Math.floor(Math.fround(Math.fround(budget.total) * Math.fround(1 + gear.damage / 100)) / row.shots));
    skills[key] = { ...row, skillLevel: Math.max(1, nodes.length), damageLevel: level, baseline: budget.baseline, damage };
  }
  return { gear, maxMana, recovery: 5 + maxMana * 0.02, skills };
}
function simulate(s, options = {}) {
  const { gear, maxMana, recovery, skills } = setup(s, options.stageBall);
  const duration = options.duration ?? 300, ticks = duration * 60;
  let mana = maxMana, minMana = mana, potions = options.potions ?? 0, usedPotions = 0;
  let lockCD = 0, ballCD = 0, freezeCD = 0, freezeUntil = -1, build = 0, rewardAfter = 0;
  let busyUntil = 0, channel = false, nextPulse = 0, firstUnaffordable = null, seed = 20261019;
  const events = new Map(), locks = [], freezes = [], samples = [];
  const damage = { ice: 0, water: 0, lock: 0, ball: 0 }, casts = { ice: 0, water: 0, lock: 0, ball: 0 };
  const ledger = { spent: 0, regen: 0, potion: 0, overflow: 0 };
  function queue(t, key, amount) { if (!events.has(t)) events.set(t, []); events.get(t).push({ key, amount }); }
  function pay(cost, t) {
    if (mana + 1e-6 < cost && firstUnaffordable === null) firstUnaffordable = t / 60;
    while (mana + 1e-6 < cost && potions > 0) { mana += maxMana * .5; ledger.potion += maxMana * .5; potions--; usedPotions++; }
    if (mana + 1e-6 < cost) return false;
    mana -= cost; ledger.spent += cost; minMana = Math.min(minMana, mana); return true;
  }
  function hit(key, amount, t) {
    seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0;
    if (seed / 4294967296 >= (key === 'lock' ? 1 : options.hitChance ?? 1)) return;
    if (key === 'water' && freezeCD <= 0) amount *= .7 / .9;
    damage[key] += amount * (1 + gear.crit / 100);
    if (key === 'ice' && t >= freezeUntil && freezeCD <= 0) {
      build += skills.ice.freezeBuildup;
      if (build >= 100) {
        build = 0; freezeUntil = t + 240; freezes.push(t / 60);
        if (t >= rewardAfter) { lockCD = Math.max(0, lockCD - constant('FreezeLockRefund') * 60); rewardAfter = t + 900; }
      }
    }
    if (key === 'water' || key === 'ball') {
      const prefix = key === 'ball' ? 'Ball' : 'Beam';
      lockCD = Math.max(0, lockCD - constant(prefix + 'LockRefund') * 60);
      freezeCD = Math.max(0, freezeCD - constant(prefix + 'FreezeRefund') * 60);
    }
  }
  for (let t = 0; t < ticks; t++) {
    lockCD = Math.max(0, lockCD - 1); ballCD = Math.max(0, ballCD - 1);
    if (t === freezeUntil) freezeCD = 3600;
    else freezeCD = Math.max(0, freezeCD - 1);
    const regen = recovery / 60; ledger.regen += regen;
    ledger.overflow += Math.max(0, mana + regen - maxMana); mana = Math.min(maxMana, mana + regen);
    for (const e of events.get(t) ?? []) hit(e.key, e.amount, t);
    events.delete(t);
    if (t % 60 === 0) samples.push({ time: t / 60, mana });
    if (t < busyUntil) continue;
    const useLock = lockCD <= 0 && (mana >= skills.lock.cost || potions > 0);
    const useBall = options.ball && ballCD <= 0 && (mana >= skills.ball.cost || potions > 0);
    const useIce = freezeCD <= 0; // Continue ice during freeze for its higher direct damage.
    if (channel && (useLock || useBall || useIce)) { channel = false; busyUntil = t + 20; continue; }
    if (useLock && pay(skills.lock.cost, t)) {
      casts.lock++; locks.push(t / 60); lockCD = skills.lock.cd * 60; busyUntil = t + 45;
      const total = skills.lock.damage, final = Math.floor((total + 1) / 2), shards = total - final;
      // Exact total and endpoints; intermediate impact spacing is approximated.
      for (let i = 0; i < 15; i++) queue(t + Math.round(29 + i * 67 / 14), 'lock', Math.floor(shards / 15) + (i < shards % 15 ? 1 : 0));
      queue(t + 110, 'lock', final); continue;
    }
    if (options.ball && ballCD <= 0 && pay(skills.ball.cost, t)) {
      casts.ball++; ballCD = skills.ball.cd * 60; busyUntil = t + 82;
      queue(t + 70, 'ball', skills.ball.damage); continue;
    }
    if (useIce) {
      if (pay(skills.ice.cost, t)) {
        casts.ice++; busyUntil = t + 45;
        for (const offset of [32, 35, 38]) queue(t + offset, 'ice', skills.ice.damage);
      }
    } else {
      if (!channel) { channel = true; nextPulse = t; }
      if (t >= nextPulse) {
        if (pay(skills.water.cost, t)) { casts.water++; hit('water', skills.water.damage, t); nextPulse = t + 12; }
        else { channel = false; busyUntil = t + 20; }
      }
    }
  }
  const total = Object.values(damage).reduce((a, b) => a + b, 0);
  const intervals = locks.slice(1).map((x, i) => x - locks[i]);
  const residual = maxMana + ledger.regen + ledger.potion - ledger.overflow - ledger.spent - mana;
  if (Math.abs(residual) > 1e-5) throw new Error('Mana accounting mismatch: ' + residual);
  return { options, stats: { gear, maxMana, recovery }, skills, dps: total / duration,
    damage, shares: Object.fromEntries(Object.entries(damage).map(([k, v]) => [k, v / total])), casts,
    lockTimes: locks, meanLockInterval: intervals.reduce((a, b) => a + b, 0) / intervals.length,
    freezeTimes: freezes, endMana: mana, minMana, firstUnaffordable, usedPotions, ledger, samples };
}
const updated = B.clone(base); updated.loadouts = B.loadoutDefaults(); B.applyLoadout(updated, 10, false);
const rows = [
  ['saved-core-120s', base, { duration: 120 }],
  ['saved-current-ball-120s', base, { ball: true, potions: 5, duration: 120 }],
  ['saved-core', base, {}],
  ['saved-current-ball', base, { ball: true }],
  ['saved-current-ball-five-elixirs', base, { ball: true, potions: 5 }],
  ['saved-stage50-ball', base, { ball: true, stageBall: true, potions: 5 }],
  ['saved-core-75pct-hit', base, { hitChance: .75 }],
  ['updated-core', updated, {}],
  ['updated-current-ball', updated, { ball: true, potions: 5 }],
].map(([name, s, options]) => ({ name, loadout: s.loadouts[9], result: simulate(s, options) }));
const result = { date: '2026-10-19', sourceSHA256: crypto.createHash('sha256').update(sourceText).digest('hex'), assumptions: [
  'Level 50; learned skills upgraded to allowed cap; WaterBall Lv1 uses level 61 baseline unless explicitly normalized.',
  'Saved loadout is used without browser migration; updated default is a separate scenario.',
  'Zero defense, single persistent target; expected standard double-damage crit; no random damage variation.',
  'No haste, consumable buffs, incoming damage, shield/healing cost, life conversion or external resource pickups.',
  '120 or 300 seconds as specified, full opening mana, zero or five 50%-mana elixirs; runtime regen 5+2% maximum, not saved web 2.5%.',
  'Priority: ready lock, affordable ball, ice while freeze accumulation is available or target is frozen, water otherwise.',
  '45-frame ice/lock actions, 20-frame beam recovery, approximate 82-frame minimum ball action; fixed short projectile travel.',
  'Actual ice hits use source-configured buildup; freeze lasts 240 frames then starts 3600-frame cooldown.',
  'Lock control does not trigger or consume elemental freeze. Intermediate shard hit spacing is approximate.',
  'Ball counted once per single target; ignores environmental water, terrain, splash, enemy movement and phase invulnerability.',
  'This model estimates mechanics; it does not run the game or predict a particular boss kill time.'
], rows };
fs.writeFileSync(path.join(__dirname, 'results.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(rows.map(({ name, result: r }) => ({ name, stats: r.stats, dps: r.dps, casts: r.casts,
  shares: r.shares, meanLockInterval: r.meanLockInterval, firstLocks: r.lockTimes.slice(0, 4), freezes: r.freezeTimes.length,
  endMana: r.endMana, minMana: r.minMana, firstUnaffordable: r.firstUnaffordable, usedPotions: r.usedPotions,
  spendPerSecond: r.ledger.spent / (r.options.duration ?? 300) })), null, 2));
