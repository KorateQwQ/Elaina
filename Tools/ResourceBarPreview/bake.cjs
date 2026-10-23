// NODE_PATH must contain playwright and sharp; CHROME_PATH can override Chromium.
const fs = require('node:fs/promises');
const path = require('node:path');
const http = require('node:http');
const {createHash} = require('node:crypto');
const {chromium} = require('playwright');
const sharp = require('sharp');
const root = path.resolve(__dirname, '../..');
const output = path.join(root, 'ElainaAttribute/ResourceBarAssets');
const types = {'.html':'text/html; charset=utf-8','.png':'image/png','.ttf':'font/ttf','.js':'text/javascript'};
// The portrait PNG would taint a file:// canvas, so serve the repository read-only over loopback.
function serve(){return new Promise(resolve=>{const server=http.createServer(async(req,res)=>{
 const file=path.resolve(root,'.'+decodeURIComponent(new URL(req.url,'http://x').pathname));
 if(!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return}
 try{const data=await fs.readFile(file);res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream'});res.end(data)}
 catch{res.writeHead(404);res.end()}});server.listen(0,'127.0.0.1',()=>resolve(server))})}
// Portrait layers: logical box [width, height], origin inside the box, bake scale, and whether 1×/1.5×/2× copies are needed.
const PORTRAIT=[['PortraitBase',66,66,33,33,4,true],['PortraitRim',66,72,33,39,4,true],['PortraitXp',62,62,31,31,4,true],
 ['PortraitXpGlow',62,62,31,31,4,true],['PortraitXpCapped',62,62,31,31,4,true],['PortraitXpMask',62,62,31,31,4,true],
 ['PortraitPulse',64,64,32,32,4,false],['PortraitGlint',6,6,3,3,8,false],['LevelFlourish',13,10,1.5,5,4,true]];
// Same states as the FNA preview: level, experience, cap, displayed life, flash/pulse, time.
const PORTRAIT_STATES=[['normal',24,65,30,.84,0,4],['low',24,12,30,.84,0,4],['capped',30,100,30,.84,0,4],
 ['levelup',25,5,30,.84,.6,4],['lowhp',7,40,30,.2,0,.26],['wide',120,50,120,.84,0,4]];
(async()=>{
 await fs.mkdir(output,{recursive:true});
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROME_PATH||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 try {
  const server=await serve();
  const page=await browser.newPage();
  await page.addInitScript(()=>{window.requestAnimationFrame=()=>0});
  await page.goto(`http://127.0.0.1:${server.address().port}/Tools/ResourceBarPreview/bars.html`);
  await page.evaluate(async()=>{await document.fonts.load('9px ElainaNum');
   if(!document.fonts.check('9px ElainaNum'))throw new Error('Gelasio (.vissandbox/gelasio-source) is required');
   if(!portrait.complete)await new Promise((ok,fail)=>{portrait.onload=ok;portrait.onerror=fail})});
  server.close();
  const assets=await page.evaluate(PORTRAIT=>{
   const result={};
   function bake(name,w,h,scale,draw){const cv=document.createElement('canvas');cv.width=w*scale;cv.height=h*scale;const c=cv.getContext('2d');c.scale(scale,scale);draw(c);result[name]=cv.toDataURL('image/png').split(',')[1]}
   for(const [kind,h] of [['hp',8],['mp',7]]){
    const name=kind==='hp'?'Life':'Mana',p=P(kind);
    bake(name+'Frame',322,40,4,c=>{c.translate(32,16);silverFrame(c,h,kind)});
    bake(name+'Slot',322,40,4,c=>{c.translate(32,16);FRAME.rail.under(c,h)});
    bake(name+'Base',L,h,4,c=>{const g=c.createLinearGradient(0,0,L,0);g.addColorStop(0,p.st[0]);g.addColorStop(.55,p.st[1]);g.addColorStop(1,p.st[2]);c.fillStyle=g;c.fillRect(0,0,L,h)});
    bake(name+'Shade',1,h,4,c=>{const g=c.createLinearGradient(0,0,0,h);g.addColorStop(0,`rgba(${p.top},${kind==='hp'?.38:.3})`);g.addColorStop(.3,'rgba(255,255,255,0)');g.addColorStop(1,`rgba(${p.shade},${kind==='hp'?.35:.4})`);c.fillStyle=g;c.fillRect(0,0,1,h)});
    bake(name+'Head',40,h,4,c=>{const g=c.createLinearGradient(0,0,40,0);g.addColorStop(0,`rgba(${p.head},0)`);g.addColorStop(1,`rgba(${p.head},.7)`);c.fillStyle=g;c.fillRect(0,0,40,h)});
   }
   bake('Petal',16,16,4,c=>petal(c,8,8,6,0,'white'));
   bake('Star',16,16,4,c=>star4(c,8,8,6,6,'white'));
   bake('Dot',16,16,4,c=>{c.fillStyle='white';c.beginPath();c.arc(8,8,6,0,TAU);c.fill()});
   bake('Nebula',64,64,2,c=>{const g=c.createRadialGradient(32,32,0,32,32,32);g.addColorStop(0,'white');g.addColorStop(1,'rgba(255,255,255,0)');c.fillStyle=g;c.fillRect(0,0,64,64)});
   bake('Line',2,4,4,c=>{c.fillStyle='white';c.fillRect(0,1.5,2,1)});
   bake('Pixel',1,1,1,c=>{c.fillStyle='white';c.fillRect(0,0,1,1)});
   const draw={PortraitBase:c=>portraitBase(c),PortraitRim:c=>portraitRim(c),PortraitXp:c=>portraitBand(c,'fill'),
    PortraitXpGlow:c=>portraitBand(c,'glow'),PortraitXpCapped:c=>portraitBand(c,'capped'),PortraitXpMask:c=>portraitBand(c,'mask'),
    PortraitPulse:c=>portraitPulse(c),PortraitGlint:c=>portraitGlint(c),LevelFlourish:c=>levelFlourish(c)};
   for(const [name,w,h,ox,oy,scale] of PORTRAIT)bake(name,w,h,scale,c=>{c.translate(ox,oy);draw[name](c)});
   return result;
  },PORTRAIT);
  const manifest={source:'Tools/ResourceBarPreview/bars.html',sourceSha256:createHash('sha256').update(await fs.readFile(path.join(__dirname,'bars.html'))).digest('hex'),encoding:'premultiplied RGBA; tML rawimg preserves RGB',frameScale:4,frameOrigin:[32,16],assets:[]};
  for(const [name,png] of Object.entries(assets)){
   const {data,info}=await sharp(Buffer.from(png,'base64')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
   for(let i=0;i<data.length;i+=4)for(let j=0;j<3;j++)data[i+j]=Math.round(data[i+j]*data[i+3]/255);
   await sharp(data,{raw:info}).png().toFile(path.join(output,name+'.png'));
   manifest.assets.push({name,width:info.width,height:info.height});
   const box=PORTRAIT.find(entry=>entry[0]===name);
   if(box)manifest.assets.at(-1).box=box.slice(1,5);
   if(name.endsWith('Frame')||name.endsWith('Slot')||box?.[6])for(const [suffix,scale] of [['S1',1],['S15',1.5],['S2',2]]){
    // Sharp filters alpha images in premultiplied space, then returns straight RGBA. Encode once afterward.
    const width=Math.round((box?box[1]:322)*scale),height=Math.round((box?box[2]:40)*scale);
    const lod=await sharp(Buffer.from(png,'base64')).resize(width,height,{kernel:'lanczos3'}).ensureAlpha().raw().toBuffer();
    for(let i=0;i<lod.length;i+=4)for(let j=0;j<3;j++)lod[i+j]=Math.round(lod[i+j]*lod[i+3]/255);
    await sharp(lod,{raw:{width,height,channels:4}}).png().toFile(path.join(output,name+suffix+'.png'));
    manifest.assets.push({name:name+suffix,width,height});
   }
  }
  await fs.writeFile(path.join(output,'manifest.json'),JSON.stringify(manifest,null,2)+'\n');
  // Reference frames use the same state/time as the native preview, without random particles.
  const reference=path.join(root,'.vissandbox/resource-bars/reference');await fs.mkdir(reference,{recursive:true});
  for(const [name,v,g,t] of [['normal',.84,.84,4],['damage',.18,.84,4],['empty',0,0,4],['full',1,1,4],['tiny',.003,.012,4],['motion',.84,.84,6]]){
   for(const scale of [1,1.5,3])for(const day of [false,true]){
    const png=await page.evaluate(({v,g,t,scale,day})=>{const cv=document.createElement('canvas');cv.width=322*scale;cv.height=76*scale;const c=cv.getContext('2d');c.scale(scale,scale);c.fillStyle=day?'#78a7f5':'#14121d';c.fillRect(0,0,322,76);
     for(const [kind,y,time] of [['hp',20,t],['mp',48,t+.7]]){c.save();c.translate(34,y);const f=kind==='mp'&&v===.84?.68:v,ghost=kind==='mp'&&g===.84?.68:g;drawBar(c,state[0][kind],time,{v:f,g:ghost,flash:0});c.restore()}return cv.toDataURL().split(',')[1]}, {v,g,t,scale,day});
    await fs.writeFile(path.join(reference,`${name}-${day?'day':'dark'}-${scale.toFixed(1)}.png`),Buffer.from(png,'base64'));
   }
  }
  // Portrait references: 90×100 logical, centre (45,40); no random particles.
  for(const [name,level,xp,cap,life,burst,time] of PORTRAIT_STATES)for(const scale of [1,1.5,3])for(const day of [false,true]){
   const png=await page.evaluate(({level,xp,cap,life,burst,time,scale,day})=>{const cv=document.createElement('canvas');cv.width=90*scale;cv.height=100*scale;const c=cv.getContext('2d');c.scale(scale,scale);c.fillStyle=day?'#78a7f5':'#14121d';c.fillRect(0,0,90,100);
    Object.assign(X,{lv:level,xp,cap});Object.assign(XA,{disp:level-1+xp/100,shown:level,flash:burst,pulse:burst,parts:[]});S.lvStyle='floral';A.hp.v=life;
    c.translate(0,40-PR.y);drawPortraitHud(c,45-PR.x,time);return cv.toDataURL().split(',')[1]},{level,xp,cap,life,burst,time,scale,day});
   await fs.writeFile(path.join(reference,`portrait-${name}-${day?'day':'dark'}-${scale.toFixed(1)}.png`),Buffer.from(png,'base64'));
  }
  console.log(`Baked ${manifest.assets.length} assets and ${36+PORTRAIT_STATES.length*6} HTML references.`);
 } finally {await browser.close()}
})().catch(e=>{console.error(e);process.exitCode=1});
