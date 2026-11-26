// Run with node Tools/GoldenShockwavePreview/check-scale.cjs [path/to/index.html]
// Execute the actual draw functions; record sprite geometry before rasterization.
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');

const source = fs.readFileSync(process.argv[2] || path.join(__dirname, 'index.html'), 'utf8');
const script = source.match(/<script>([\s\S]*?)<\/script>/)[1];
class Context {
  constructor() { this.s = 1; this.stack = []; }
  save() { this.stack.push(this.s); }
  restore() { this.s = this.stack.pop(); }
  scale(x, y) { assert.equal(x, y); this.s *= x; }
  setTransform(a) { this.s = a; }
  getTransform() { return { a: this.s, b: 0, transformPoint: p => p }; }
  createRadialGradient() { return { addColorStop() {} }; }
  beginPath() {}
  arc() {}
  fill() {}
  stroke() {}
  drawImage() {}
}
const gl = new Proxy({}, { get: (_, key) => key.startsWith('get') ? () => true : () => ({}) });
const document = {
  getElementById: () => ({ width: 1280, height: 720, getContext: () => new Context() }),
  createElement: () => ({ getContext: type => type === 'webgl' ? gl : new Context() })
};
// Images intentionally remain pending: no browser event loop or UI setup is needed.
const sandbox = { document, Image: class {}, DOMPoint: class { constructor(x, y) { this.x = x; this.y = y; } }, performance: { now: () => 0 }, assert, Context };
vm.createContext(sandbox);
vm.runInContext(script + `
const recorded = [];
sprite = (g, name, rgb, x, y, w, h, rot = 0, alpha = 1) => {
  if (alpha <= .002 || w <= 0 || h <= 0) return;
  recorded.push([name, x * g.s, y * g.s, w * g.s, h * g.s, rot, alpha]);
};
polar.render = o => {
  recorded.push(['shader', o.Ri * o.scale, o.Ro * o.scale, o.front * o.scale, o.trail * o.scale, o.head, o.fade]);
};
this.trace = (variant, sh, style, aoe, age) => {
  gameSize = false; cur = variant; ringStyle = style; SK.aoe = aoe; SK.hole = aoe * 2.3 / 6.7;
  recorded.length = 0;
  drawBurst(new Context(), sh, age);
  return recorded.map(row => row.map((v, i) => i >= 1 && i <= 4 ? v / (SK.r * SK.aoe) : v));
};
this.checkGameSize = () => {
  const close = (a, b) => assert.ok(Math.abs(a - b) < 1e-8, a + ' != ' + b);
  // Explicit production expectations: full arena 800, main 600, small 100.
  gameSize = true;
  close(SK.R, 600); close(SK.r, 100); close(SK.orbit, 700);
  close(burstRadius('circle'), 1160); close(burstRadius('ring'), 1740);
  for (const variant of ['A', 'B', 'C']) {
    cur = variant;
    close(burstScale('circle') * variants[cur].radius, 1160);
  }
  const geo = ringGeometry();
  close(geo.Ro * geo.scale, 1740); close(geo.Ri * geo.scale, 500);
  // Preset must override custom design ranges, without destroying them.
  SK.aoe = 3.4; SK.hole = 1.2;
  close(burstRadius('circle'), 1160);
  close(ringGeometry().Ri * ringGeometry().scale, 500);
  const st = skillState(SK.first);
  close(Math.hypot(st.burst.x, st.burst.y), 700);
  close(st.th1, st.th2);
  // World pixel scale (and therefore player/terrain size) is independent of preset.
  shakeOn = false;
  for (const width of [1280, 1920, 2160, 3840]) {
    view.width = width; view.height = Math.round(width * 9 / 16);
    for (const camera of [1, 1.25, 2]) {
      cameraZoom = camera;
      const g = new Context();
      gameSize = true; worldTransform(g); close(g.s, width / 1920 * camera);
      gameSize = false; worldTransform(g); close(g.s, width / 1920 * camera);
    }
  }
  close(SK.R, 380); close(SK.r, 112); close(SK.orbit, 470);
  close(burstRadius('circle'), 112 * 3.4);
  close(ringGeometry().Ri * ringGeometry().scale, 112 * 1.2);
};
this.checkGeometry = () => {
  for (const aoe of [.5, 1, 3.4, 6.7, 8]) {
    for (const hole of [.05, 2.3, 6]) {
      SK.aoe = aoe; SK.hole = hole;
      const geo = ringGeometry();
      assert.ok(Math.abs(geo.Ro * geo.scale - SK.r * aoe) < 1e-8);
      assert.ok(Math.abs(geo.Ri * geo.scale - SK.r * Math.min(hole, aoe - .15)) < 1e-8);
    }
  }
  SK.aoe = 6.7; cur = 'A';
  const rings = [];
  drawRing = (g, age, ring) => rings.push({ age, ring, scale: g.s });
  variants.A.draw(new Context(), 22);
  assert.ok(Math.abs(ringRadius(20, rings[0].ring) * burstScale('circle') - SK.r * SK.aoe) < 1e-8);
};
`, sandbox);

let compared = 0;
for (const [variant, shape, style] of [['A', 'circle', 'wave'], ['B', 'circle', 'wave'], ['C', 'circle', 'wave'], ['A', 'ring', 'wave'], ['A', 'ring', 'chord'], ['A', 'ring', 'mark']]) {
  for (const age of [3, 8, 12, 22, 32, 44, 64]) {
    // The marker's inner rim starts at the fixed world-space small-array radius.
    // Test proportional geometry after that intentional six-frame transition.
    if (shape === 'ring' && (style === 'mark' || style === 'chord') && age < 6) continue;
    const baseline = sandbox.trace(variant, shape, style, 3.4, age);
    for (const aoe of [.5, 1, 6.7, 8]) {
      const actual = sandbox.trace(variant, shape, style, aoe, age);
      const label = variant + '/' + shape + '/' + style + ' age=' + age + ' aoe=' + aoe;
      assert.equal(actual.length, baseline.length, label + ': sprite lifetime/count changed');
      for (let i = 0; i < baseline.length; i++) {
        assert.equal(actual[i][0], baseline[i][0], label);
        for (let j = 1; j < baseline[i].length; j++) {
          assert.ok(Number.isFinite(actual[i][j]) && Math.abs(actual[i][j] - baseline[i][j]) < 1e-8,
            label + ': inconsistent position/size/brightness at sprite ' + i + ', component ' + j);
        }
      }
      compared++;
    }
  }
}
let bounded = 0;
for (const variant of ['A', 'B', 'C']) {
  for (let age = 0; age <= 110; age += .5) {
    for (const aoe of [.5, 6.7, 8]) {
      for (const [name, x, y, w, h, rot] of sandbox.trace(variant, 'circle', 'wave', aoe, age)) {
        if (!['Star4', 'StarFlare', 'SparkTail'].includes(name) || Math.hypot(x, y) < 1e-8) continue;
        for (const sx of [-1, 1]) for (const sy of [-1, 1]) {
          const dx = sx * w / 2, dy = sy * h / 2;
          const r = Math.hypot(x + dx * Math.cos(rot) - dy * Math.sin(rot), y + dx * Math.sin(rot) + dy * Math.cos(rot));
          assert.ok(r <= 1 + 1e-8, variant + ' age=' + age + ' ' + name + ' extends outside explosion: ' + r);
        }
        bounded++;
      }
    }
  }
}
console.log('PASS: ' + bounded + ' star/trail bounds across A/B/C lifetimes and three explosion sizes.');
sandbox.checkGeometry();
if (sandbox.checkGameSize) sandbox.checkGameSize();
console.log('PASS: game geometry, preserved design settings and 12 camera/resolution combinations.');
console.log('PASS: ' + compared + ' scale comparisons; A wave arrival and 15 independent inner/outer radius combinations.');
