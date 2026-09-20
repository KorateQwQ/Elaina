const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { chromium } = require('playwright');
const sharp = require('sharp');

const root = path.resolve(__dirname, '../..');
const ui = path.join(root, 'ElainaModSkills/ElainaSkillUI');
const output = path.join(ui, 'BattleBar');
const scratch = path.join(root, '.vissandbox/skillbar-preview');

async function premultiplied(png, target) {
  const { data, info } = await sharp(png).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  for (let i = 0; i < data.length; i += 4)
    for (let c = 0; c < 3; c++) data[i + c] = Math.round(data[i + c] * data[i + 3] / 255);
  await sharp(data, { raw: info }).png().toFile(target);
}

(async () => {
  await fs.mkdir(output, { recursive: true });
  await fs.mkdir(scratch, { recursive: true });
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1500, height: 940 }, deviceScaleFactor: 2 });
    await page.goto(pathToFileURL(path.join(ui, 'NewUIExample/elaina-battle-eight-skills.html')).href);
    await page.locator('.battle-rack-art').evaluate(img => img.decode());
    const source = await page.evaluate(() => ({
      rack: document.querySelector('.battle-rack-art').src,
      selected: getComputedStyle(document.querySelector('.battle-slot.current .battle-disc'), '::after').backgroundImage,
      resting: getComputedStyle(document.querySelector('.battle-slot:not(.current) .battle-disc'), '::after').backgroundImage,
      css: document.querySelector('style').textContent
    }));
    await page.locator('.battle-rack').screenshot({ path: path.join(scratch, 'html-reference.png') });
    await page.setContent(`<style>html,body{margin:0;background:transparent}img{position:absolute;left:24px;top:24px;width:646px;height:94px;filter:drop-shadow(0 5px 9px #07070c66)}</style><img src="${source.rack}">`);
    await page.locator('img').evaluate(img => img.decode());
    await premultiplied(await page.screenshot({ omitBackground: true, clip: { x: 0, y: 0, width: 694, height: 142 } }), path.join(output, 'Rack.png'));

    // Keep the selected frame, but remove its two baked stars: markers now animate independently.
    const selectedFrame = await page.evaluate(background => {
      const uri = background.match(/^url\("(.*)"\)$/)[1];
      const svg = new DOMParser().parseFromString(decodeURIComponent(uri.slice(uri.indexOf(',') + 1)), 'image/svg+xml');
      for (const path of svg.querySelectorAll('path'))
        if (path.getAttribute('fill') !== 'none') path.remove();
      return `url("data:image/svg+xml,${encodeURIComponent(new XMLSerializer().serializeToString(svg))}")`;
    }, source.selected);
    const tiles = [];
    const styles = [
      `background:${source.resting} center/68px 68px no-repeat`,
      `background:${selectedFrame} center/68px 68px no-repeat;filter:drop-shadow(0 0 2px #b99bda40)`,
      '', '', '', ''
    ];
    for (let i = 0; i < 6; i++) {
      let content = '<div class="art"></div>';
      if (i === 2) content = '<div class="disc selected"><i></i></div>';
      // Four-point empty-slot ornament; no labels or game text are rasterized.
      if (i === 3) content = '<svg width="96" height="96"><path d="M48 41 L50 47 L55 49 L50 51 L48 57 L46 51 L41 49 L46 47 Z" fill="none" stroke="white" stroke-width=".8"/></svg>';
      if (i === 4) content = '<div class="disc" style="background:#201a2d14"></div>';
      if (i === 5) content = '<div class="disc" style="background:#baa1cf0c"></div>';
      await page.setContent(`<style>html,body{margin:0;background:transparent}.art{width:96px;height:96px}.disc{position:absolute;left:18px;top:18px;width:60px;height:60px;border-radius:2px}.selected{background:radial-gradient(ellipse,#b99bd029,#9072aa0d 60%,transparent 76%)}i{position:absolute;inset:5px;border-radius:2px;background:linear-gradient(145deg,#d1b4ed26,#b08ad90c 60%,#c9adeb20);filter:blur(5px);opacity:.7}</style>${content}`);
      if (i < 2) await page.locator('.art').evaluate((element, css) => { element.style.cssText = css; }, styles[i]);
      tiles.push({ input: await page.screenshot({ omitBackground: true, clip: { x: 0, y: 0, width: 96, height: 96 } }), left: i * 192, top: 0 });
    }
    // Antialiased cross-section for the runtime orbit, with transparent filter padding.
    const brush = Buffer.alloc(4 * 8 * 4);
    const alpha = [0, 48, 224, 255, 255, 224, 48, 0];
    for (let y = 0; y < 8; y++) for (let x = 0; x < 4; x++) {
      const p = (y * 4 + x) * 4;
      brush[p] = brush[p + 1] = brush[p + 2] = 255; brush[p + 3] = alpha[y];
    }
    tiles.push({ input: await sharp(brush, { raw: { width: 4, height: 8, channels: 4 } }).png().toBuffer(), left: 0, top: 192 });
    // Independent 2x marker cores and glows, with enough padding for filtering.
    for (let i = 0; i < 4; i++) {
      const diamond = i >= 2;
      const glow = i % 2 === 1;
      const shape = diamond ? 'M16 13L18 16L16 19L14 16Z' :
        'M16 10Q16.8 14.9 21 16Q16.8 17.1 16 22Q15.2 17.1 11 16Q15.2 14.9 16 10Z';
      await page.setContent(`<style>html,body{margin:0;background:transparent}</style><svg width="32" height="32" viewBox="0 0 32 32"><defs><filter id="glow" x="-100%" y="-100%" width="300%" height="300%"><feGaussianBlur stdDeviation="${diamond ? 1.2 : 1.8}"/></filter></defs><path d="${shape}" fill="${glow ? '#c2a1f0' : diamond ? '#d9c4ee' : '#f4ecff'}" ${glow ? 'filter="url(#glow)"' : ''}/></svg>`);
      tiles.push({ input: await page.screenshot({ omitBackground: true, clip: { x: 0, y: 0, width: 32, height: 32 } }), left: 16 + i * 64, top: 192 });
    }
    const atlas = await sharp({ create: { width: 1152, height: 256, channels: 4, background: '#0000' } }).composite(tiles).png().toBuffer();
    await premultiplied(atlas, path.join(output, 'Ornaments.png'));
    const reference = await browser.newPage({ viewport: { width: 710, height: 150 }, deviceScaleFactor: 1.5 });
    const laser = (await fs.readFile(path.join(ui, '../Skills/Fire/FireLaserSkill.png'))).toString('base64');
    const tornado = (await fs.readFile(path.join(ui, '../Skills/Fire/FireTornadoSkill.png'))).toString('base64');
    const slots = Array.from({ length: 8 }, (_, i) => `<button class="battle-slot ${i === 2 ? 'current' : ''} ${i === 1 || i === 2 ? '' : 'empty'}"><span class="battle-disc">${i === 1 || i === 2 ? `<span class="icon-art"><img src="data:image/png;base64,${i === 1 ? laser : tornado}"></span>` : '&#10023;'}</span></button>`).join('');
    await reference.setContent(`<style>${source.css}html,body{min-width:0;min-height:0;width:710px;height:150px;overflow:hidden;background:#13141c}.battle-rack{position:absolute;left:32px;top:20px}.battle-slot.current:before,.battle-slot.current .battle-disc:after{animation-delay:-.7s!important;animation-play-state:paused!important}</style><div class="battle-rack"><img class="battle-rack-art" src="${source.rack}"><div class="battle-slots">${slots}</div></div>`);
    await reference.locator('img').evaluateAll(images => Promise.all(images.map(image => image.decode())));
    await reference.screenshot({ path: path.join(scratch, 'html-match-1.5.png') });
    console.log('Baked Rack.png (1388x284), Ornaments.png (1152x256), premultiplied RGBA.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
