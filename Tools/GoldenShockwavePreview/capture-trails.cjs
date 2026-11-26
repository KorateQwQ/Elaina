const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.argv[3] || path.resolve(__dirname, '../../../CoVFX/Tools/SlashStudio/Checks/browser/node_modules/playwright'));
const label = process.argv[2] || 'after';
const out = path.join(__dirname, 'trail-checks');
(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const page = await browser.newPage();
  page.on('pageerror', error => { throw error; });
  await page.goto('http://127.0.0.1:5603/?full=0&shape=circle&size=game&frame=12&shake=0&radial=0&width=1920');
  await page.waitForFunction(() => !!window.preview);
  const sample = await page.evaluate(() => {
    const canvas = document.createElement('canvas'); canvas.width = 1200; canvas.height = 480;
    const g = canvas.getContext('2d');
    g.fillStyle = '#19202d'; g.fillRect(0, 0, 1200, 480);
    for (let i = 0; i < 4; i++) {
      const age = [5, 12, 22, 32][i], angle = -.45;
      const p = { ang: angle, dist: 320, delay: 0, life: 40, size: 12, star: false, rot: .2, spin: .04, phase: 0, rise: 0 };
      const t = age / p.life, d = p.dist * outQuart(t);
      g.save(); g.translate(i * 300 + 185, 210); g.scale(3, 3);
      g.translate(-Math.cos(angle) * d, -Math.sin(angle) * d - 30 * t * t);
      g.globalCompositeOperation = 'lighter'; drawSparks(g, age, [p], { gravity: 30 }); g.restore();
      g.fillStyle = 'white'; g.font = '16px sans-serif'; g.fillText('age ' + age + ' / 40', i * 300 + 12, 30);
    }
    return canvas.toDataURL('image/png').split(',')[1];
  });
  fs.writeFileSync(path.join(out, label + '-particles.png'), Buffer.from(sample, 'base64'));
  for (const [width, age, skyMode, radial] of [[1920, 12, 'day', false], [1920, 22, 'night', true], [2160, 9, 'night', true], [3840, 12, 'night', false]]) {
    const png = await page.evaluate(({ width, age, skyMode, radial }) => {
      for (const canvas of [view, fx, burstFx, blurBuf, polar.canvas]) { canvas.width = width; canvas.height = Math.round(width * 9 / 16); }
      frame = age; sky = skyMode; radialOn = radial; render();
      return view.toDataURL('image/png').split(',')[1];
    }, { width, age, skyMode, radial });
    fs.writeFileSync(path.join(out, `${label}-${width}-${skyMode}-${age}.png`), Buffer.from(png, 'base64'));
  }
  for (const age of [12, 22, 32]) {
    const png = await page.evaluate(age => {
      for (const canvas of [view, fx, burstFx, blurBuf, polar.canvas]) { canvas.width = 1920; canvas.height = 1080; }
      gameSize = false; SK.aoe = 3.4; cameraZoom = 1; frame = age; sky = 'night'; radialOn = false; render();
      return view.toDataURL('image/png').split(',')[1];
    }, age);
    fs.writeFileSync(path.join(out, `${label}-whole-circle-${age}.png`), Buffer.from(png, 'base64'));
  }
  await browser.close();
  console.log('Captured actual preview spark particles and full scenes: ' + label);
})();
