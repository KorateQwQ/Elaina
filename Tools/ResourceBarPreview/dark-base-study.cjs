// Dark backing study for the level label: a feathered dark glow (the Nebula radial texture, tinted and stretched)
// behind the number and both flourishes. Uses the real bars.html portrait code; writes dark-base-study.png only.
// NODE_PATH must contain playwright.
const fs = require('node:fs/promises');
const path = require('node:path');
const http = require('node:http');
const {chromium} = require('playwright');
const root = path.resolve(__dirname, '../..');
const types = {'.html':'text/html; charset=utf-8','.png':'image/png','.ttf':'font/ttf','.js':'text/javascript'};
function serve(){return new Promise(resolve=>{const server=http.createServer(async(req,res)=>{
 const file=path.resolve(root,'.'+decodeURIComponent(new URL(req.url,'http://x').pathname));
 if(!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return}
 try{const data=await fs.readFile(file);res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream'});res.end(data)}
 catch{res.writeHead(404);res.end()}});server.listen(0,'127.0.0.1',()=>resolve(server))})}
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROME_PATH||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 try{
  const server=await serve();
  const page=await browser.newPage();
  await page.addInitScript(()=>{window.requestAnimationFrame=()=>0});
  await page.goto(`http://127.0.0.1:${server.address().port}/Tools/ResourceBarPreview/bars.html`);
  await page.evaluate(async()=>{await document.fonts.load('12px ElainaNum');
   if(!portrait.complete)await new Promise((ok,fail)=>{portrait.onload=ok;portrait.onerror=fail})});
  server.close();
  const png=await page.evaluate(()=>{
   const HALO=LEVEL_HALO.map(x=>[...x]);
   // Same falloff as the baked Nebula texture: linear alpha from the centre to the edge, stretched to an ellipse.
   function darkBase(c,txt,alpha,padY){
    c.font=`${FLORAL.size}px ${NUM}`;const wn=c.measureText(txt).width;
    const rx=wn/2+FLORAL.gap+13*FLORAL.scale+3,ry=padY,cy=FLORAL.cy+.4;
    c.save();c.translate(0,cy);c.scale(rx/32,ry/32);
    const g=c.createRadialGradient(0,0,0,0,0,32);g.addColorStop(0,`rgba(18,12,32,${alpha})`);g.addColorStop(1,'rgba(18,12,32,0)');
    c.fillStyle=g;c.fillRect(-32,-32,64,64);c.restore()}
   const VARIANTS=[
    ['现状 · 无暗底，数字偏移暗影',null,true],
    ['淡暗底 35% + 数字暗影',[.35,8],true],
    ['暗底 50% + 数字暗影',[.5,8],true],
    ['暗底 42%，去掉数字暗影',[.42,8],false]];
   function style(v){return(c,txt,capped)=>{
    if(v[1])darkBase(c,txt,...v[1]);
    LEVEL_HALO.length=0;if(v[2])LEVEL_HALO.push(...HALO.map(x=>[...x]));
    levelFloral(c,txt,capped)}}
   // [label, level, xp fraction, cap, UI scale, day]
   const COLS=[['白天 ×3 · Lv 24',24,.65,30,3,true],['白天 ×3 · Lv 120',120,.5,130,3,true],['夜晚 ×3 · Lv 24',24,.65,30,3,false],
    ['白天 ×1.5（像素放大 2×）',24,.65,30,1.5,true],['白天 ×1.0（像素放大 3×）',24,.65,30,1,true]];
   function setState(lv,frac,cap){X.lv=lv;X.cap=cap;X.xp=Math.round(frac*XPL);XA.disp=lv-1+frac;XA.shown=lv;XA.flash=0;XA.pulse=0;XA.parts=[]}
   // Crop in portrait-local units (before the 1.4 display scale): centred horizontally, from above the apex to below the label.
   const LX=36,TOP=-36,BOT=46,CW=Math.ceil(LX*2*1.4*3),CH=Math.ceil((BOT-TOP)*1.4*3),PAD=16,HEAD=28,LABEL=22;
   const rowH=LABEL+CH+PAD,W=PAD+COLS.length*(CW+PAD),H=HEAD+VARIANTS.length*rowH;
   const cv=document.createElement('canvas');cv.width=W;cv.height=H;const c=cv.getContext('2d');
   c.fillStyle='#14121d';c.fillRect(0,0,W,H);c.textBaseline='middle';
   c.font='13px "Microsoft YaHei",sans-serif';c.fillStyle='#b2a8c3';
   COLS.forEach(([n],i)=>c.fillText(n,PAD+i*(CW+PAD),HEAD/2));
   VARIANTS.forEach((v,r)=>{
    const y0=HEAD+r*rowH;LEVEL_STYLES.study=style(v);S.lvStyle='study';
    c.font='14px "Microsoft YaHei",sans-serif';c.fillStyle='#ede2d4';c.fillText(v[0],PAD,y0+LABEL/2);
    COLS.forEach(([,lv,frac,cap,K,day],i)=>{setState(lv,frac,cap);
     // Render at the true UI scale, then enlarge with nearest-neighbour so small scales show real pixels.
     const w=Math.ceil(LX*2*1.4*K),h=Math.ceil((BOT-TOP)*1.4*K),off=document.createElement('canvas');off.width=w;off.height=h;const o=off.getContext('2d');
     if(day){const g=o.createLinearGradient(0,0,0,h);g.addColorStop(0,'#6a9cf4');g.addColorStop(1,'#86b0f7');o.fillStyle=g}else o.fillStyle='#14121d';
     o.fillRect(0,0,w,h);o.translate(w/2,-TOP*1.4*K);o.scale(K,K);o.translate(0,-PR.y);drawPortraitHud(o,-PR.x,4);
     c.imageSmoothingEnabled=false;c.drawImage(off,PAD+i*(CW+PAD),y0+LABEL,CW,CH)})});
   LEVEL_HALO.length=0;LEVEL_HALO.push(...HALO);
   return cv.toDataURL('image/png').split(',')[1]});
  await fs.writeFile(path.join(__dirname,'dark-base-study.png'),Buffer.from(png,'base64'));
 }finally{await browser.close()}
})();
