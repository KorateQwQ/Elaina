'use strict';
// Isolated sensitivity adapter: existing simulators remain byte-for-byte unchanged.
// Only cooldown scheduling changes. Base damage budgets, per-hit buildup, costs,
// cast times, effect durations and target-owned freeze cooldown remain unchanged.
const fs = require('node:fs'), path = require('node:path');
const Module = require('node:module'), assert = require('node:assert/strict');
function load(relative, patches) {
  const filename = path.resolve(__dirname, relative);
  let source = fs.readFileSync(filename, 'utf8');
  for (const [before, after, count = 1] of patches) {
    assert.equal(source.split(before).length - 1, count, `Adapter source changed: ${before}`);
    source = source.split(before).join(after);
  }
  const adapted = new Module(filename, module);
  adapted.filename = filename;
  adapted.paths = Module._nodeModulePaths(path.dirname(filename));
  adapted._compile(source, filename);
  return adapted.exports;
}
const helpers = `
  const haste = Math.max(0, Math.min(500, Number(options.haste ?? 0)));
  assert(Number.isFinite(haste));
  const hasteMultiplier = 1 / (1 + haste / 100);
  const cooldownFrames = seconds => Math.max(1, Math.ceil(seconds * hasteMultiplier * 60 - 1e-7),
    options.waitForBaseCadence ? Math.ceil(seconds * 60 - 1e-7) : 0);
  // fixed: the v5 basic hand interval remains a fixed animation/channel gate.
  // cooldown: hypothetical cooldown-limited cadence, with no additional hand gate.
  const basicSeconds = seconds => options.hasteBasics === 'cooldown' && !options.waitForBaseCadence
    ? Math.max(1, Math.ceil(seconds * hasteMultiplier * 60 - 1e-7)) / 60 : seconds;
`;
const dummy = load('simulate-v3.cjs', [
  ['function run(options={}) {', 'function run(options={}) {' + helpers],
  ...['lock', 'ball', 'thunder'].map(k => [`frames(o.${k}CD)`, `cooldownFrames(o.${k}CD)`]),
  ["cast('ice',ice.cost,ice.interval)", "cast('ice',ice.cost,basicSeconds(ice.interval))"],
  ["cast('water',water.cost,water.interval)", "cast('water',water.cost,basicSeconds(water.interval))"],
  ['cast(key,basic.cost,basic.interval)', 'cast(key,basic.cost,basicSeconds(basic.interval))'],
  ['f.time-f.buildStart+EPS>=o.freezeBuildSeconds',
    'f.time-f.buildStart+EPS>=o.freezeBuildSeconds*basicSeconds(ice.interval)/ice.interval'],
]);
const moon = load('../moon-lord-master-2026-10-14/simulate.cjs', [
  ['function run(options={}) {', 'function run(options={}) {' + helpers],
  ['frames(c.lockCD)', 'cooldownFrames(c.lockCD)'],
  ['frames(c.ballCD)', 'cooldownFrames(c.ballCD)', 2],
  ['frames(c.thunderCD)', 'cooldownFrames(c.thunderCD)', 2],
  ["cast('ice',4.5,.5,target)", "cast('ice',4.5,basicSeconds(.5),target)"],
  ["cast('water',1.5,.2,target)", "cast('water',1.5,basicSeconds(.2),target)"],
]);
module.exports = {dummy, moon};
