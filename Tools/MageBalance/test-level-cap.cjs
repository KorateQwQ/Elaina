'use strict';
// Disposable browser and an in-memory file service: never overwrites the user's configuration.
const fs=require('node:fs'),path=require('node:path'),os=require('node:os'),assert=require('node:assert/strict');
const runtime=process.env.CODEX_NODE_MODULES||path.join(os.homedir(),'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(runtime,'playwright'));
const html=fs.readFileSync(path.join(__dirname,'index.html'),'utf8');
let payload=JSON.parse(fs.readFileSync(path.join(__dirname,'../../ElainaModSkills/Data/SkillBalance.json'),'utf8').replace(/^\uFEFF/,''));
(async()=>{
 const browser=await chromium.launch({headless:true,channel:process.env.BALANCE_BROWSER_CHANNEL||'chrome'});
 try{
  const page=await browser.newPage({viewport:{width:1920,height:1080}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.route('http://127.0.0.1:18768/**',async route=>{
   if(new URL(route.request().url()).pathname==='/api/skill-balance'){
    if(route.request().method()==='PUT')payload=route.request().postDataJSON();
    await route.fulfill({json:{saved:true,path:'in-memory test configuration',data:payload}});
   }else await route.fulfill({contentType:'text/html',body:html});
  });
  await page.goto('http://127.0.0.1:18768/#progression');
  await page.waitForFunction(()=>document.querySelector('#fileStatus').textContent.includes('已读取'));
  const row=()=>page.locator('#progressionRows tr').filter({hasText:'AlchemySkill'});
  const cap=()=>row().locator('[data-progression-field="maxLevel"]');
  assert.equal(await cap().inputValue(),'8');
  await page.locator('#progressionDetailSkill').selectOption('AlchemySkill');
  for(const level of [39,40,95,1000]){
   await page.locator('#progressionPreviewLevel').fill(String(level));
   assert.match(await row().locator('[data-progression-cap]').innerText(),new RegExp(`Lv${level===39?7:8}（角色 ${level}）`));
  }
  assert.equal(await page.locator('#progressionDetailRows tr').count(),8);
  assert.match(await page.locator('#progressionDetailRows tr').last().innerText(),/Lv8\s*· 可达\s+40/);
  assert.match(await page.locator('#progressionNotice').innerText(),/最高技能等级 Lv8/);
  const missile=page.locator('#progressionRows tr').filter({hasText:'MagicMissileSkill'});
  assert.equal(await missile.locator('[data-progression-field="maxLevel"]').inputValue(),'0');
  for(const mode of ['phase','interval']){
   await row().locator('[data-progression-mode]').selectOption(mode);
   await cap().fill('3');
   assert.equal(await page.locator('#progressionDetailRows tr').count(),3);
   await page.locator('#saveMod').click();
   await page.waitForFunction(()=>document.querySelector('#fileStatus').textContent.includes('已保存'));
   assert.equal(payload.configuration.skills.find(x=>x.id==='AlchemySkill').maxLevel,3);
   await page.reload();
   await page.waitForFunction(()=>document.querySelector('#fileStatus').textContent.includes('已读取'));
   assert.equal(await cap().inputValue(),'3');
   await page.locator('#progressionDetailSkill').selectOption('AlchemySkill');
   await cap().fill('0');
   assert.equal(await page.locator('#progressionDetailRows tr').count(),20);
  }
  await row().locator('[data-progression-mode]').selectOption('phase');
  await cap().fill('8');
  await page.locator('#progressionPreviewLevel').fill('40');
  const out=path.join(__dirname,'test-output');fs.mkdirSync(out,{recursive:true});
  await row().scrollIntoViewIfNeeded();
  await page.screenshot({path:path.join(out,'level-cap-desktop.png')});
  await page.setViewportSize({width:390,height:844});
  await cap().scrollIntoViewIfNeeded();
  assert.equal(await cap().isVisible(),true);
  await page.screenshot({path:path.join(out,'level-cap-mobile.png')});
  assert.deepEqual(errors,[]);
  console.log('PASS browser: actual alchemy Lv8 cap, unlimited damage skill, both growth modes, save/reload, desktop/mobile, zero JS errors');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
