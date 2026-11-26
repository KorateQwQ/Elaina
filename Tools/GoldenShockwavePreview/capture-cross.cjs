const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { chromium } = require(process.argv[2] || path.resolve(__dirname, '../../../CoVFX/Tools/SlashStudio/Checks/browser/node_modules/playwright'));
const output = path.join(__dirname, 'cross-preview');
fs.mkdirSync(output, { recursive: true });
(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1100 } });
  page.on('pageerror', error => { throw error; });
  await page.goto('http://127.0.0.1:5608/?full=0&shape=cross&frame=8&shake=0&radial=0&width=1920');
  await page.waitForFunction(() => !!window.preview);
  const flowCheck = await page.evaluate(() => {
    const canvas = document.createElement('canvas'); canvas.width = 1600; canvas.height = 240;
    const g = canvas.getContext('2d');
    const sheet = document.createElement('canvas'); sheet.width = 1600; sheet.height = 720;
    const sg = sheet.getContext('2d');
    const samples = [];
    const originalWidth = starBeamOptions.width;
    starBeamOptions.width = 120;
    for (const [i, time] of [8, 10, 12].entries()) {
      g.setTransform(1, 0, 0, 1, 0, 0); g.clearRect(0, 0, 1600, 240);
      g.translate(800, 120); starlightBody.draw(g, 8, time, 1500, 120);
      const decoded = beamBloomPipeline.draw(canvas, .25);
      const sample = document.createElement('canvas'); sample.width = 1600; sample.height = 240;
      const sampleContext = sample.getContext('2d'); sampleContext.drawImage(decoded, 0, 0);
      samples.push(sampleContext.getImageData(0, 0, 1600, 240).data);
      sg.fillStyle = '#101729'; sg.fillRect(0, i * 240, 1600, 240); sg.drawImage(decoded, 0, i * 240);
      sg.fillStyle = '#dce1eb'; sg.font = '18px sans-serif'; sg.fillText('Fixed width / texture time ' + time, 16, i * 240 + 26);
    }
    starBeamOptions.width = originalWidth;
    let delta = 0, count = 0;
    for (let i = 0; i < samples[0].length; i += 4) {
      if (samples[0][i + 3] > 20) { for (let c = 0; c < 4; c++) delta += Math.abs(samples[0][i + c] - samples[1][i + c]); count += 4; }
    }
    return { delta: delta / count, sheet: sheet.toDataURL().split(',')[1] };
  });
  assert.ok(flowCheck.delta > 2, 'Fixed geometry must show actual texture flow');
  fs.writeFileSync(path.join(output, 'texture-flow.png'), Buffer.from(flowCheck.sheet, 'base64'));
  for (const shape of ['beam', 'cross']) {
    const frames = [2, 8, 20, 32, 40, 48, 56, 68];
    const result = await page.evaluate(({ shape, frames }) => {
      const tileHeight = shape === 'beam' ? 270 : 540;
      const sheet = document.createElement('canvas'); sheet.width = 1920; sheet.height = tileHeight * 4;
      const sg = sheet.getContext('2d');
      const images = [];
      for (let i = 0; i < frames.length; i++) {
        preview.set('A', frames[i], { full: false, shape, shake: false });
        images.push(view.toDataURL('image/png'));
        const x = i % 2 * 960, y = Math.floor(i / 2) * tileHeight;
        // Single beam uses a vertical crop to retain visible line detail in the contact sheet.
        if (shape === 'beam') sg.drawImage(view, 0, 180, 1920, 540, x, y, 960, 270);
        else sg.drawImage(view, x, y, 960, 540);
        sg.fillStyle = '#dce1eb'; sg.font = '16px sans-serif'; sg.fillText(shape + ' / frame ' + frames[i], x + 14, y + 24);
      }
      return { images, sheet: sheet.toDataURL('image/png') };
    }, { shape, frames });
    const write = (name, data) => fs.writeFileSync(path.join(output, name), Buffer.from(data.split(',')[1], 'base64'));
    result.images.forEach((data, i) => write(shape + '-' + frames[i] + '.png', data));
    write(shape + '-sheet.png', result.sheet);
  }
  const metrics = await page.evaluate(() => {
    let segments = 0, inside = false;
    const probe = document.createElement('canvas'); probe.width = 1920; probe.height = 128;
    const pg = probe.getContext('2d'); pg.translate(960, 64);
    starlightBody.draw(pg, 52, 52, starBeamOptions.length, starBeamOptions.width * beamEnvelope(52));
    const pixels = pg.getImageData(0, 64, 1920, 1).data;
    for (let i = 0; i < 1920; i++) {
      const live = pixels[i * 4 + 3] > 2;
      if (live && !inside) segments++;
      inside = live;
    }
    const clean = () => {
      const c = document.createElement('canvas'); c.width = 1920; c.height = 1080;
      const g = c.getContext('2d'); g.translate(960, 540); drawStarCross(g, 72);
      return g.getImageData(0, 0, 1920, 1080).data.every(v => v === 0);
    };
    preview.set('A', 40, { full: false, shape: 'beam' }); const first = view.toDataURL();
    preview.set('A', 8, { full: false, shape: 'cross' });
    preview.set('A', 40, { full: false, shape: 'beam' }); const second = view.toDataURL();
    const rotations = [], originalDraw = drawStarlightBeam;
    drawStarlightBeam = (g, age, rotation) => rotations.push(rotation);
    drawStarCross(null, 8); drawStarlightBeam = originalDraw;
    return { segments, rotations, emptyAtEnd: clean(), repeatable: first === second, widths: [0, 8, 20, 28, 40, 62, 72].map(beamEnvelope) };
  });
  assert.ok(metrics.segments >= 3, 'Release must split into multiple segments');
  assert.ok(metrics.emptyAtEnd && metrics.repeatable);
  assert.deepEqual(metrics.rotations, [0, Math.PI / 2]);
  metrics.flowDelta = flowCheck.delta;
  assert.ok(metrics.widths[1] > metrics.widths[2] && metrics.widths[2] > metrics.widths[3]);
  await page.locator('[data-shape="cross"]').click();
  assert.ok(await page.locator('#beamPanel').isVisible());
  for (const [width, age, full] of [[1280, 8, false], [2160, 48, false], [3840, 20, false], [1920, 188, true], [1920, 228, true]]) {
    const png = await page.evaluate(({ width, age, full }) => {
      for (const canvas of [view, fx, burstFx, chargeFx, blurBuf, polar.canvas]) { canvas.width = width; canvas.height = Math.round(width * 9 / 16); }
      preview.set('A', age, { full, shape: 'cross' });
      return view.toDataURL().split(',')[1];
    }, { width, age, full });
    fs.writeFileSync(path.join(output, 'cross-' + width + '-' + age + '-' + full + '.png'), Buffer.from(png, 'base64'));
  }
  const animation = path.join(output, 'frames'); fs.mkdirSync(animation, { recursive: true });
  for (let age = 0; age <= 90; age += 2) {
    const png = await page.evaluate(age => {
      for (const canvas of [view, fx, burstFx, chargeFx, blurBuf, polar.canvas]) { canvas.width = 960; canvas.height = 540; }
      preview.set('A', age, { full: false, shape: 'cross', beamLength: 760 });
      return view.toDataURL().split(',')[1];
    }, age);
    fs.writeFileSync(path.join(animation, String(age).padStart(3, '0') + '.png'), Buffer.from(png, 'base64'));
  }
  fs.writeFileSync(path.join(output, 'checks.json'), JSON.stringify(metrics, null, 2));
  console.log(JSON.stringify(metrics));
  await browser.close();
})();
