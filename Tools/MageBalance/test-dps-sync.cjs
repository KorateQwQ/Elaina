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
assert.equal(B.setDpsCurve(buffed),true);assert.equal(B.setDpsCurve(buffed),false);
assert.equal(B.levelDps(1),32);assert.equal(B.levelDps(3),36);assert.equal(B.levelDps(5),40);assert.equal(B.levelDps(10),50);
const s=B.defaults(),row=s.basicAttacks[0];assert.equal(B.progressionBudget(s,row,1).baseline,32);
for(const invalid of [[],[40,30,...original.slice(2)],[NaN,...original.slice(1)],[0,...original.slice(1)]]){
 assert.throws(()=>B.setDpsCurve(invalid));assert.deepEqual(Array.from(B.stageDps),buffed);
}
B.setDpsCurve(original);
console.log('PASS curve replacement, interpolation, progression baseline, invalid curve retention.');

async function browserTest(){
 const runtime=process.env.CODEX_NODE_MODULES||path.join(os.homedir(),'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
 const {chromium}=require(path.join(runtime,'playwright'));
 const temp=fs.mkdtempSync(path.join(os.tmpdir(),'mage-dps-sync-'));
 const sourcePath=path.join(temp,'KLDpsHelper.cs'),configPath=path.join(temp,'SkillBalance.json');
 // A controlled temporary fixture; do not assume the user's live curve is still 20,30,...
 const source='float[] stateDps = [\n20f, //initial\n30f,   //升级\n'+original.slice(2).map(v=>v+'f').join(',')+'];';
 const writeCurve=(first,second)=>fs.writeFileSync(sourcePath,source.replace('20f,',first+'f,').replace(/30f,(\s*\/\/升级)/,second+'f,$1'));
 writeCurve(20,30);
 // Old saved config with lab fields and a stale stage B; the page must ignore them.
 const config={...B.defaults(),stage:1,baseDpsMode:'stage',baseDps:20,combat:{duration:120}};
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
  const stageB=()=>page.locator('#stageDamageRows tr').first().locator('td').nth(2).textContent();
  const firstHit=()=>page.locator('#progressionDetailRows tr').first().locator('td').nth(2).textContent();
  await page.goto('http://127.0.0.1:'+port+'/#progression');
  await page.waitForFunction(()=>document.getElementById('fileStatus').textContent.includes('已读取'));
  assert.equal(await stageB(),'30');assert.equal(await firstHit(),'22');
  writeCurve(30,40);
  await page.waitForFunction(()=>document.querySelector('#stageDamageRows tr td:nth-child(3)').textContent==='40');
  assert.equal(await firstHit(),'32','Per-level preview follows the live curve');
  assert.match(await page.locator('#dpsSourceStatus').innerText(),/已同步/);
  // Reloading the JSON keeps the live curve; the B curve is never stored in the config.
  await page.locator('#fileStatus').evaluate(el=>el.textContent='');
  await page.locator('#loadMod').click();
  await page.waitForFunction(()=>document.getElementById('fileStatus').textContent.includes('已读取'));
  assert.equal(await stageB(),'40');
  await page.locator('#saveMod').click();
  await page.waitForFunction(()=>document.getElementById('fileStatus').textContent.includes('已保存'));
  const savedConfig=JSON.parse(fs.readFileSync(configPath,'utf8')).configuration;
  assert.deepEqual(Object.keys(savedConfig).sort(),Object.keys(B.defaults()).sort(),'Saving drops the old lab fields');
  fs.writeFileSync(sourcePath,'invalid');
  await page.waitForFunction(()=>document.getElementById('dpsSourceStatus').textContent.includes('同步失败'));
  assert.equal(await stageB(),'40','An invalid source keeps the last valid curve');
  writeCurve(20,30);
  await page.waitForFunction(()=>document.querySelector('#stageDamageRows tr td:nth-child(3)').textContent==='30');
  assert.match(await page.locator('#dpsSourceStatus').innerText(),/已同步/);
  assert.deepEqual(errors,[]);
  fs.mkdirSync(path.join(__dirname,'test-output'),{recursive:true});
  await page.screenshot({path:path.join(__dirname,'test-output/dps-sync-desktop.png')});
  await page.setViewportSize({width:390,height:844});
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
  await page.screenshot({path:path.join(__dirname,'test-output/dps-sync-mobile.png')});
  console.log('PASS browser: live temporary C# edits -> stage damage and per-level baseline; stale JSON, save, error recovery, no JS errors or mobile overflow.');
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
