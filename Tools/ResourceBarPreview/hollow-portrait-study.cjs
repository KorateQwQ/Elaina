// Hollow portrait background study: the transparent parts of PortraitSource.png show the world instead of the mist-purple fill.
// Uses the real bars.html portrait code; writes hollow-portrait-study.png only.
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
   const base=portraitBase;
   // Variant base: dark ring only between the portrait and the outline; inside, an optional veil, then the portrait
   // with its vignette clipped to the portrait's own alpha so see-through areas are not darkened.
   // ring: the gap between the XP band's outer edge and the white outline; null keeps it dark, a colour veils it, 'clear' hollows it.
   function ringPath(c,a,b){c.beginPath();for(const r of[a,b]){c.moveTo(0,-r);c.lineTo(r,0);c.lineTo(0,r);c.lineTo(-r,0);c.closePath()}}
   function hollow(veil,ring=null){return c=>{const{r:R,band,bw,outer}=PH,edge=band+bw/2*Math.SQRT2;c.save();c.lineCap='round';c.lineJoin='round';
    ringPath(c,ring?edge:outer,R);c.fillStyle='rgba(12,9,20,.94)';c.fill('evenodd');
    if(ring&&ring!=='clear'){ringPath(c,outer,edge);c.fillStyle=ring;c.fill('evenodd')}
    c.save();diamond(c,R);c.clip();
    if(veil){c.fillStyle=veil;c.fillRect(-R,-R,R*2,R*2)}
    const N=400,off=document.createElement('canvas');off.width=off.height=N;const o=off.getContext('2d');
    o.drawImage(portrait,0,0,N,N);o.globalCompositeOperation='source-atop';
    const vg=o.createRadialGradient(N/2,N/2,N/4,N/2,N/2,N/2*1.05);vg.addColorStop(0,'rgba(20,12,32,0)');vg.addColorStop(1,'rgba(20,12,32,.5)');
    o.fillStyle=vg;o.fillRect(0,0,N,N);c.drawImage(off,-R,-R,R*2,R*2);c.restore();
    diamond(c,R+.6);c.strokeStyle='#1a1630';c.lineWidth=1.2;c.stroke();
    diamond(c,band);c.strokeStyle='rgba(22,18,42,.92)';c.lineWidth=bw;c.stroke();c.restore()}}
   const VEIL='rgba(12,9,20,.35)';
   const VARIANTS=[
    ['现状 · 雾紫底 + 外圈黑',null],
    ['头像暗纱 + 外圈全镂空',hollow(VEIL,'clear')],
    ['头像暗纱 + 外圈同样 35% 暗纱',hollow(VEIL,VEIL)],
    ['头像全镂空 + 外圈全镂空',hollow(null,'clear')]];
   function style(){return null}
   // [label, level, xp fraction, cap, UI scale, day]
   const COLS=[['白天 ×3',24,.65,30,3,'day'],['白天有云 ×3',24,.65,30,3,'cloud'],['夜晚 ×3',24,.65,30,3,'dark'],
    ['地下 ×3',24,.65,30,3,'cave'],['白天有云 ×1.5（像素放大 2×）',24,.65,30,1.5,'cloud']];
   function setState(lv,frac,cap){X.lv=lv;X.cap=cap;X.xp=Math.round(frac*XPL);XA.disp=lv-1+frac;XA.shown=lv;XA.flash=0;XA.pulse=0;XA.parts=[]}
   // Crop in portrait-local units (before the 1.4 display scale): centred horizontally, from above the apex to below the label.
   const LX=36,TOP=-36,BOT=46,CW=Math.ceil(LX*2*1.4*3),CH=Math.ceil((BOT-TOP)*1.4*3),PAD=16,HEAD=28,LABEL=22;
   const rowH=LABEL+CH+PAD,W=PAD+COLS.length*(CW+PAD),H=HEAD+VARIANTS.length*rowH;
   const cv=document.createElement('canvas');cv.width=W;cv.height=H;const c=cv.getContext('2d');
   c.fillStyle='#14121d';c.fillRect(0,0,W,H);c.textBaseline='middle';
   c.font='13px "Microsoft YaHei",sans-serif';c.fillStyle='#b2a8c3';
   COLS.forEach(([n],i)=>c.fillText(n,PAD+i*(CW+PAD),HEAD/2));
   VARIANTS.forEach((v,r)=>{
    const y0=HEAD+r*rowH;portraitBase=v[1]||base;
    c.font='14px "Microsoft YaHei",sans-serif';c.fillStyle='#ede2d4';c.fillText(v[0],PAD,y0+LABEL/2);
    COLS.forEach(([,lv,frac,cap,K,day],i)=>{setState(lv,frac,cap);
     // Render at the true UI scale, then enlarge with nearest-neighbour so small scales show real pixels.
     const w=Math.ceil(LX*2*1.4*K),h=Math.ceil((BOT-TOP)*1.4*K),off=document.createElement('canvas');off.width=w;off.height=h;const o=off.getContext('2d');
     if(day==='dark')o.fillStyle='#14121d';
     else if(day==='cave'){const g=o.createLinearGradient(0,0,0,h);g.addColorStop(0,'#5a4636');g.addColorStop(1,'#3a2c22');o.fillStyle=g}
     else{const g=o.createLinearGradient(0,0,0,h);g.addColorStop(0,'#6a9cf4');g.addColorStop(1,'#86b0f7');o.fillStyle=g}
     o.fillRect(0,0,w,h);
     // A cloud drifting behind the portrait shows what the see-through areas reveal.
     if(day==='cloud'){o.fillStyle='rgba(255,255,255,.92)';for(const[x,y,r]of[[.3,.32,.16],[.45,.27,.2],[.62,.33,.15],[.46,.38,.18],[.75,.42,.1]]){o.beginPath();o.arc(w*x,h*y,w*r,0,TAU);o.fill()}}
     if(day==='cave'){o.fillStyle='#2a2019';for(let i=0;i<9;i++)for(let j=0;j<9;j++)if((i*7+j*3)%5===0)o.fillRect(i*w/9,j*h/9,w/9,h/9)}o.translate(w/2,-TOP*1.4*K);o.scale(K,K);o.translate(0,-PR.y);drawPortraitHud(o,-PR.x,4);
     c.imageSmoothingEnabled=false;c.drawImage(off,PAD+i*(CW+PAD),y0+LABEL,CW,CH)})});
   portraitBase=base;
   return cv.toDataURL('image/png').split(',')[1]});
  await fs.writeFile(path.join(__dirname,'hollow-portrait-study.png'),Buffer.from(png,'base64'));
 }finally{await browser.close()}
})();
