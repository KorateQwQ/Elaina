// Level label position study: draws candidate placements with the real bars.html portrait code.
// NODE_PATH must contain playwright; output is level-position-study.png next to this script. Does not touch formal assets.
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
   if(!portrait.complete)await new Promise((ok,fail)=>{portrait.onload=ok;portrait.onerror=fail});
   if(!minimap.complete)await new Promise(ok=>{minimap.onload=ok;minimap.onerror=ok})});
  server.close();
  const png=await page.evaluate(()=>{
   const O=PH.outer;
   // Number with a soft dark halo so it stays readable where it crosses the white outline.
   function ink(c,txt,capped,y,size=12){
    const m=c.getTransform(),px=Math.hypot(m.a,m.b);
    c.font=`${size}px ${NUM}`;c.textAlign='center';c.textBaseline='middle';
    c.save();c.shadowBlur=2.4*px;c.shadowColor='rgba(14,10,26,.95)';c.fillStyle='rgba(14,10,26,.55)';
    c.fillText(txt,0,y+.4);c.fillText(txt,0,y+.4);c.restore();
    c.fillStyle=capped?'#cfc8da':'rgb(238,233,245)';c.fillText(txt,0,y+.4);
    return c.measureText(txt).width}
   function flourishes(c,e,y,rot=0,dy=0){for(const s of[1,-1]){c.save();c.scale(s,1);c.translate(e,y+dy);c.rotate(rot);c.scale(FLORAL.scale,FLORAL.scale);levelFlourish(c);c.restore()}}
   const STYLES={
    // A: number centre just below the tip; it covers only the white outline, the XP band tip stays visible.
    light(c,txt,capped){c.font=`12px ${NUM}`;const w=c.measureText(txt).width;flourishes(c,w/2+FLORAL.gap,0);ink(c,txt,capped,0)},
    // B: deeper overlap with a dark plaque (same fill as the portrait base) and a white rim; the plaque reads as a setting.
    plaque(c,txt,capped){c.font=`11px ${NUM}`;const w=c.measureText(txt).width,pw=w+7,ph=10.5;
     rr(c,-pw/2,-ph/2,pw,ph,ph/2);c.fillStyle='rgba(12,9,20,.94)';c.fill();c.strokeStyle='rgba(240,236,250,.92)';c.lineWidth=.55;c.stroke();
     flourishes(c,pw/2+1.2,0);c.font=`11px ${NUM}`;c.textAlign='center';c.textBaseline='middle';
     c.fillStyle=capped?'#cfc8da':'rgb(238,233,245)';c.fillText(txt,0,.45)},
    // C: same light overlap, flourishes tilted up so they follow the diamond's lower edges.
    edge(c,txt,capped){c.font=`12px ${NUM}`;const w=c.measureText(txt).width;flourishes(c,w/2+1.6,0,-.62,-1.2);ink(c,txt,capped,0)}};
   Object.assign(LEVEL_STYLES,STYLES);
   const VARIANTS=[
    ['现状 · 右下方',{style:'floral',x:26,y:22}],
    ['A · 轻压外线（只盖白色外线，经验起点露出）',{style:'light',x:0,y:O+2}],
    ['B · 深压 + 深色底托（盖住经验起点）',{style:'plaque',x:0,y:O-.6}],
    ['C · 轻压 + 花饰沿斜边上扬',{style:'edge',x:0,y:O+2}]];
   const STATES=[['Lv 24 · 经验 65%',24,.65,30],['Lv 7 · 经验 6%（起点）',7,.06,30],['Lv 120 · 三位数',120,.5,130]];
   function setState(lv,frac,cap){X.lv=lv;X.cap=cap;X.xp=Math.round(frac*XPL);XA.disp=lv-1+frac;XA.shown=lv;XA.flash=0;XA.pulse=0;XA.parts=[]}
   const CW=330,CH=360,K=3,GW=520*1.5,GH=150*1.5,PAD=18,HEAD=30,LABEL=22;
   const rowH=LABEL+Math.max(CH,GH)+PAD,W=PAD+STATES.length*(CW+PAD)+GW+PAD,H=HEAD+VARIANTS.length*rowH;
   const cv=document.createElement('canvas');cv.width=W;cv.height=H;const c=cv.getContext('2d');
   c.fillStyle='#14121d';c.fillRect(0,0,W,H);
   c.font='13px "Microsoft YaHei",sans-serif';c.fillStyle='#b2a8c3';c.textBaseline='middle';
   STATES.forEach(([n],i)=>c.fillText(`特写 ×${(K*PR.scale).toFixed(1)} · ${n}`,PAD+i*(CW+PAD),HEAD/2));
   c.fillText('游戏内 · UI 150% · 含原版小地图',PAD+STATES.length*(CW+PAD),HEAD/2);
   const game=views.find(v=>v.v.hud&&v.v.kind==='game');
   VARIANTS.forEach(([name,v],r)=>{
    const y0=HEAD+r*rowH;Object.assign(FLORAL,{x:v.x,y:v.y});S.lvStyle=v.style;
    c.font='14px "Microsoft YaHei",sans-serif';c.fillStyle='#ede2d4';c.fillText(name,PAD,y0+LABEL/2);
    STATES.forEach(([,lv,frac,cap],i)=>{setState(lv,frac,cap);const x0=PAD+i*(CW+PAD),yy=y0+LABEL;
     c.save();c.beginPath();c.rect(x0,yy,CW,CH);c.clip();
     const g=c.createLinearGradient(0,yy,0,yy+CH);g.addColorStop(0,'#6a9cf4');g.addColorStop(1,'#86b0f7');c.fillStyle=g;c.fillRect(x0,yy,CW,CH);
     // Portrait centre sits at 45% height of the crop.
     c.translate(x0+CW/2,yy+CH*.42);c.scale(K,K);c.translate(0,-PR.y);drawPortraitHud(c,-PR.x,4);c.restore()});
    setState(24,.65,30);render(4);
    c.drawImage(game.cv,PAD+STATES.length*(CW+PAD),y0+LABEL,GW,GH)});
   return cv.toDataURL('image/png').split(',')[1]});
  await fs.writeFile(path.join(__dirname,'level-position-study.png'),Buffer.from(png,'base64'));
 }finally{await browser.close()}
})();
