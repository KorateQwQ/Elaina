'use strict';
const assert=require('node:assert/strict');
const fs=require('node:fs');
const os=require('node:os');
const path=require('node:path');
const vm=require('node:vm');
const {spawn}=require('node:child_process');
const html=fs.readFileSync(path.join(__dirname,'index.html'),'utf8');
const ctx={module:{exports:{}}};
vm.runInNewContext(html.match(/<script id="balance-engine">([\s\S]*?)<\/script>/)[1],ctx);
const B=ctx.module.exports,original=Array.from(B.stageDps),buffed=[30,40,...original.slice(2)];
const stage=B.defaults(1),manual=B.defaults(1);
manual.baseDps=37;manual.baseDpsMode='manual';
assert.equal(B.setDpsCurve(buffed),true);
B.syncBaseDps(stage);B.syncBaseDps(manual);
assert.equal(stage.baseDps,40);assert.equal(manual.baseDps,37);
assert.equal(B.levelDps(1),32);assert.equal(B.levelDps(3),36);assert.equal(B.levelDps(5),40);
assert.equal(B.levelDps(10),50);assert.equal(B.dpsLevel(36),3);
assert.equal(B.defaults(1).baseDps,40);
const legacy={stage:1,baseDps:30};B.normalizeBaseDps(legacy);B.syncBaseDps(legacy);assert.equal(legacy.baseDps,40);
const custom={stage:1,baseDps:37};B.normalizeBaseDps(custom);B.syncBaseDps(custom);assert.equal(custom.baseDps,37);
const row=stage.basicAttacks[0];assert.equal(B.progressionBudget(stage,row,1).baseline,32);
for(const invalid of [[],[40,30,...original.slice(2)],[NaN,...original.slice(1)],[0,...original.slice(1)]]){
 assert.throws(()=>B.setDpsCurve(invalid));assert.deepEqual(Array.from(B.stageDps),buffed);
}
B.setDpsCurve(original);
console.log('PASS curve replacement, stage/manual modes, old saved values, interpolation, inverse, invalid curve retention.');

async function browserTest(){
 const runtime=process.env.CODEX_NODE_MODULES||path.join(os.homedir(),'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
 const {chromium}=require(path.join(runtime,'playwright'));
 const temp=fs.mkdtempSync(path.join(os.tmpdir(),'mage-dps-sync-'));
 const sourcePath=path.join(temp,'KLDpsHelper.cs'),configPath=path.join(temp,'SkillBalance.json');
 // A controlled temporary fixture; do not assume the user's live curve is still 20,30,...
 const source='float[] stateDps = [\n20f, //initial\n30f,   //升级\n'+original.slice(2).map(v=>v+'f').join(',')+'];';
 const writeCurve=(first,second)=>fs.writeFileSync(sourcePath,source.replace('20f,',first+'f,').replace(/30f,(\s*\/\/升级)/,second+'f,$1'));
 writeCurve(20,30);
 const config=B.defaults(1);delete config.baseDpsMode; // old saved stage baseline
 fs.writeFileSync(configPath,JSON.stringify({schema:'elaina-mage-balance-v2',configuration:config}));
 const child=spawn('python',['-u','-c','import server,sys; s=server.make_server(0,sys.argv[1],sys.argv[2]); print("PORT="+str(s.server_port),flush=True); s.serve_forever()',configPath,sourcePath],{cwd:__dirname,windowsHide:true});
 let browser;
 try{
  const port=await new Promise((resolve,reject)=>{
   const timer=setTimeout(()=>reject(new Error('Test server startup timeout')),10000);
   child.once('error',e=>{clearTimeout(timer);reject(e);});
   child.once('exit',code=>{clearTimeout(timer);reject(new Error('Test server exited: '+code));});
   child.stdout.on('data',chunk=>{const match=chunk.toString().match(/PORT=(\d+)/);if(match){clearTimeout(timer);resolve(+match[1]);}});
  });
  browser=await chromium.launch({headless:true,channel:process.env.BALANCE_BROWSER_CHANNEL||'chrome'});
  const page=await browser.newPage({viewport:{width:1440,height:1000}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:'+port);
  await page.waitForFunction(()=>document.getElementById('fileStatus').textContent.includes('已读取'));
  assert.equal(await page.locator('#baseDps').inputValue(),'30');
  const initial=await page.locator('#simActual').innerText();
  writeCurve(30,40);
  await page.waitForFunction(()=>document.getElementById('baseDps').value==='40');
  assert.notEqual(await page.locator('#simActual').innerText(),initial);
  assert.equal(await page.evaluate(()=>Balance.levelDps(1)),32);
  assert.equal(await page.evaluate(()=>Balance.progressionBudget(Balance.defaults(1),Balance.defaults(1).basicAttacks[0],1).baseline),32);
  // Reloading an old JSON must not restore its old stage B.
  await page.locator('#fileStatus').evaluate(el=>el.textContent='');
  await page.locator('#loadMod').click();
  await page.waitForFunction(()=>document.getElementById('fileStatus').textContent.includes('已读取'));
  assert.equal(await page.locator('#baseDps').inputValue(),'40');
  await page.locator('#baseDps').fill('37');
  writeCurve(35,45);
  await page.waitForFunction(()=>Balance.stageDps[1]===45);
  assert.equal(await page.locator('#baseDps').inputValue(),'37');
  await page.locator('#saveMod').click();
  await page.waitForFunction(()=>document.getElementById('fileStatus').textContent.includes('已保存'));
  await page.reload();
  await page.waitForFunction(()=>document.getElementById('fileStatus').textContent.includes('已读取'));
  assert.equal(await page.locator('#baseDps').inputValue(),'37');
  await page.locator('#followDps').click();
  assert.equal(await page.locator('#baseDps').inputValue(),'45');
  fs.writeFileSync(sourcePath,'invalid');
  await page.waitForFunction(()=>document.getElementById('dpsSourceStatus').textContent.includes('同步失败'));
  assert.equal(await page.locator('#baseDps').inputValue(),'45');
  writeCurve(20,30);
  await page.waitForFunction(()=>document.getElementById('baseDps').value==='30');
  await page.locator('#preset').selectOption('2');
  assert.equal(await page.locator('#baseDps').inputValue(),'50');
  assert.deepEqual(errors,[]);
  fs.mkdirSync(path.join(__dirname,'test-output'),{recursive:true});
  await page.screenshot({path:path.join(__dirname,'test-output/dps-sync-desktop.png')});
  await page.setViewportSize({width:390,height:844});
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
  await page.screenshot({path:path.join(__dirname,'test-output/dps-sync-mobile.png')});
  console.log('PASS browser: live temporary C# edits -> DPS recalculation; stale JSON, manual save/reload, error recovery, stage switching, no JS errors or mobile overflow.');
 }finally{
  if(browser)await browser.close();
  if(child.exitCode===null&&child.signalCode===null)await new Promise(resolve=>{child.once('exit',resolve);child.kill();});
  // All files below were created by this test in its unique temporary directory.
  assert.equal(path.dirname(path.resolve(temp)),path.resolve(os.tmpdir()));
  assert.ok(path.basename(temp).startsWith('mage-dps-sync-'));
  fs.rmSync(temp,{recursive:true,force:true});
 }
}
if(process.argv.includes('--browser'))browserTest().catch(error=>{console.error(error);process.exitCode=1;});
