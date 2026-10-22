// NODE_PATH must contain playwright and sharp; CHROME_PATH can override Chromium.
const fs = require('node:fs/promises');
const path = require('node:path');
const {pathToFileURL} = require('node:url');
const {createHash} = require('node:crypto');
const {chromium} = require('playwright');
const sharp = require('sharp');
const root = path.resolve(__dirname, '../..');
const output = path.join(root, 'ElainaAttribute/ResourceBarAssets');
(async()=>{
 await fs.mkdir(output,{recursive:true});
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROME_PATH||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 try {
  const page=await browser.newPage();
  await page.addInitScript(()=>{window.requestAnimationFrame=()=>0});
  await page.goto(pathToFileURL(path.join(__dirname,'bars.html')).href);
  const assets=await page.evaluate(()=>{
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
   return result;
  });
  const manifest={source:'Tools/ResourceBarPreview/bars.html',sourceSha256:createHash('sha256').update(await fs.readFile(path.join(__dirname,'bars.html'))).digest('hex'),encoding:'premultiplied RGBA; tML rawimg preserves RGB',frameScale:4,frameOrigin:[32,16],assets:[]};
  for(const [name,png] of Object.entries(assets)){
   const {data,info}=await sharp(Buffer.from(png,'base64')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
   for(let i=0;i<data.length;i+=4)for(let j=0;j<3;j++)data[i+j]=Math.round(data[i+j]*data[i+3]/255);
   await sharp(data,{raw:info}).png().toFile(path.join(output,name+'.png'));
   manifest.assets.push({name,width:info.width,height:info.height});
   if(name.endsWith('Frame')||name.endsWith('Slot'))for(const [suffix,scale] of [['S1',1],['S15',1.5],['S2',2]]){
    // Sharp filters alpha images in premultiplied space, then returns straight RGBA. Encode once afterward.
    const width=322*scale,height=40*scale;
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
  console.log(`Baked ${manifest.assets.length} assets and 36 HTML references.`);
 } finally {await browser.close()}
})().catch(e=>{console.error(e);process.exitCode=1});
