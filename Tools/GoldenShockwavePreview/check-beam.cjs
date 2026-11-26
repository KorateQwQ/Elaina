const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { chromium } = require(path.resolve(__dirname, '../../../CoVFX/Tools/SlashStudio/Checks/browser/node_modules/playwright'));
const output = path.join(__dirname, 'beam-checks');
fs.mkdirSync(output, {recursive:true});
const save = (name, png) => fs.writeFileSync(path.join(output,name+'.png'),Buffer.from(png.split(',')[1],'base64'));
(async()=>{
  const browser=await chromium.launch({channel:'msedge',headless:true});
  const page=await browser.newPage({viewport:{width:1440,height:1100}});
  page.on('pageerror',e=>{throw e;});
  page.on('console',m=>{if(m.type()==='error')throw new Error(m.text());});
  await page.goto('http://127.0.0.1:5608/?full=0&shape=cross&frame=3&shake=0&radial=0&width=1920');
  await page.waitForFunction(()=>!!window.preview);
  const motion=await page.evaluate(()=>{
    let checks=0;
    for(const width of [30,120,480,640,800]){
      starBeamOptions.width=width;starBeamOptions.length=width/120*1600;
      for(const p of beamStars){
        const born=beamStarState(p,p.born);
        if(born.vy!==0)throw new Error('星光必须沿束轴运动');
        if(Math.abs(born.y)>width*beamEnvelope(p.born)*.35)throw new Error('出生点离开束内发射范围');
        for(const t of [1,8,16]){
          const s=beamStarState(p,p.born+t),past=beamStarState(p,p.born+t-Math.min(t,7));
          if(Math.abs(s.y-born.y)>1e-9)throw new Error('星光出现斜向漂移');
          if(Math.hypot(s.tailX-past.x,s.tailY-past.y)>1e-8)throw new Error('尾迹脱离历史位置');
          checks++;
        }
      }
      const p={...beamStars[0],y:.4,born:8},q={...p,born:32};
      if(Math.abs(beamStarState(q,32).y)>=Math.abs(beamStarState(p,8).y))throw new Error('发射范围未收束');
    }
    return checks;
  });
  for(const width of [480,640])for(const shape of ['beam','cross']){
    for(const frame of [3,8,16,24,40,48,52,56,64,72]){
      const png=await page.evaluate(({width,shape,frame})=>{
        preview.set('A',frame,{full:false,shape,beamWidth:width,beamLength:width/120*1600,shake:false});return view.toDataURL();
      },{width,shape,frame});
      save(`${shape}-${width}-${frame}`,png);
    }
  }
  const bounds=await page.evaluate(()=>{
    const originalSprite=sprite,originalBody=starlightBody.draw;
    let geometry,records=[],checked=0,lagging=0;
    starlightBody.draw=(_g,_age,_flow,length,width)=>{geometry={length,width};};
    sprite=(_g,name,_color,x,y,width,height,rotation,alpha)=>{
      if(alpha>0)records.push({name,x,y,width,height,rotation});
    };
    const g={save(){},restore(){},rotate(){}};
    const maxima={};
    for(const length of [760,1600,6400,8533.333])for(const width of [120,480,640]){
      starBeamOptions.length=length;starBeamOptions.width=width;
      let maximum=0;
      for(let age=.5;age<72;age+=.5){
        records=[];drawStarlightBeam(g,age,0);
        maximum=Math.max(maximum,records.filter(r=>r.name==='BeamStar').length);
        for(const r of records){
          const rx=(Math.abs(Math.cos(r.rotation))*r.width+Math.abs(Math.sin(r.rotation))*r.height)/2;
          const ry=(Math.abs(Math.sin(r.rotation))*r.width+Math.abs(Math.cos(r.rotation))*r.height)/2;
          const delayedAge=age<=8?age:Math.max(8,age-18);
          const particleHalfWidth=width*beamEnvelope(delayedAge)*.40;
          if(Math.abs(r.x)+rx>geometry.length/2+1e-6||Math.abs(r.y)+ry>particleHalfWidth+1e-6)
            throw new Error(`星芒或拖尾超过延后收束范围：${length}/${width}/${age}/${r.name}`);
          if(age>16&&age<36&&Math.abs(r.y)+ry>geometry.width*.40)lagging++;
          checked++;
        }
      }
      maxima[`${length}/${width}`]=maximum;
    }
    sprite=originalSprite;starlightBody.draw=originalBody;
    if(maxima['1600/480']>45||maxima['760/480']>24)throw new Error('短束星光仍然过密');
    if(!lagging)throw new Error('星光未保留晚于光柱的收束');
    if(maxima['8533.333/480']<=maxima['1600/480'])throw new Error('粒子数量没有适应束长');
    return {checked,maxima,lagging};
  });
  for(const length of [760,1600])for(const frame of [8,16,24,32,40,48]){
    save(`short-${length}-${frame}`,await page.evaluate(({length,frame})=>{
      preview.set('A',frame,{full:false,shape:'cross',beamWidth:480,beamLength:length,shake:false});
      return view.toDataURL();
    },{length,frame}));
  }
  const fadeTiming=await page.evaluate(()=>{
    const originalBody=starlightBody.draw,originalSprite=sprite,originalStar=drawBeamStar;
    starlightBody.draw=()=>{};sprite=()=>{};
    let current=new Map();
    drawBeamStar=(_g,p,_state,intensity)=>current.set(p,intensity);
    const g={save(){},restore(){},rotate(){}};
    let worstDrop=0,disappearances=0,checked=0;
    for(const length of [760,1600,8533.333])for(const width of [120,480,640]){
      starBeamOptions.length=length;starBeamOptions.width=width;
      let previous=new Map();
      for(let age=8;age<=72;age++){
        current=new Map();drawStarlightBeam(g,age,0);
        for(const [p,before] of previous){
          const after=current.get(p)||0;
          worstDrop=Math.max(worstDrop,before-after);checked++;
          if(!after){
            disappearances++;
            if(before>.06)throw new Error(`星光未淡至足够暗就消失：${length}/${width}/${age}/${before}; born=${p.born}, end=${beamStarEnd(p)}`);
          }
        }
        previous=current;
      }
    }
    starlightBody.draw=originalBody;sprite=originalSprite;drawBeamStar=originalStar;
    if(worstDrop>.18)throw new Error(`单帧亮度下降过快：${worstDrop}`);
    return {worstDrop,disappearances,checked};
  });
  for(let age=16;age<=48;age++){
    save(`fade-${age}`,await page.evaluate(age=>{
      preview.set('A',age,{full:false,shape:'beam',beamWidth:480,beamLength:1600,shake:false});
      const c=document.createElement('canvas');c.width=960;c.height=180;
      c.getContext('2d').drawImage(view,0,230,1920,360,0,0,960,180);
      return c.toDataURL();
    },age));
  }
  const visibility=await page.evaluate(()=>{
    // 在用户常见的页面显示宽度比较实际合成画面，不能以绘制次数证明星光看得见。
    const c=document.createElement('canvas');c.width=1212;c.height=682;
    const g=c.getContext('2d'),results=[];
    const originalSprite=sprite;
    for(const [length,width] of [[1600,480],[8533.333,640]])for(const age of [16,24,32,40,48]){
      preview.set('A',age,{full:false,shape:'beam',beamWidth:width,beamLength:length,shake:false});
      g.drawImage(view,0,0,c.width,c.height);
      const on=g.getImageData(0,0,c.width,c.height).data;
      sprite=(...args)=>{if(args[1]!=='BeamStar'&&args[1]!=='SparkTail')originalSprite(...args);};
      preview.set('A',age,{shape:'beam'});
      g.drawImage(view,0,0,c.width,c.height);
      const off=g.getImageData(0,0,c.width,c.height).data;
      sprite=originalSprite;
      const mask=new Uint8Array(c.width*c.height);
      let changed=0,clusters=0;
      for(let i=0;i<mask.length;i++){
        const delta=Math.max(on[i*4]-off[i*4],on[i*4+1]-off[i*4+1],on[i*4+2]-off[i*4+2]);
        if(delta>40){mask[i]=1;changed++;}
      }
      for(let start=0;start<mask.length;start++)if(mask[start]){
        const stack=[start];mask[start]=0;let area=0;
        while(stack.length){
          const p=stack.pop();area++;
          const x=p%c.width,y=Math.floor(p/c.width);
          for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++){
            if(x+dx<0||x+dx>=c.width||y+dy<0||y+dy>=c.height)continue;
            const q=p+dx+dy*c.width;if(mask[q]){mask[q]=0;stack.push(q);}
          }
        }
        if(area>=3)clusters++;
      }
      if(clusters<3)throw new Error(`缩小后可辨识星光不足：${length}/${age}，${clusters} 个亮部`);
      results.push({length,age,changed,clusters});
    }
    return results;
  });
  const diagnostics=await page.evaluate(()=>{
    preview.set('A',52,{full:false,shape:'beam',beamWidth:640,beamLength:8533.333});
    const source=burstFx.getContext('2d').getImageData(0,0,1920,1080).data;
    const on=beamBloomPipeline.draw(burstFx,640/480).toDataURL();
    const off=beamBloomPipeline.draw(burstFx,640/480,false).toDataURL();
    const target=document.createElement('canvas');target.width=1920;target.height=1080;
    const g=target.getContext('2d');
    g.drawImage(beamBloomPipeline.draw(burstFx,640/480),0,0);
    const lit=g.getImageData(0,0,1920,1080).data;
    let halo=0,whiteCore=0,coloredHalo=0;
    for(let i=0;i<lit.length;i+=4){
      // getImageData 返回直通道，乘 alpha 后才是实际的加法发光贡献。
      const a=lit[i+3]/255,r=lit[i]*a,green=lit[i+1]*a,b=lit[i+2]*a;
      if(source[i+3]===0&&r>6){halo++;if(r>green*1.1&&green>b*1.5)coloredHalo++;}
      if(r>245&&green>235&&b>205)whiteCore++;
    }
    const first=view.toDataURL();preview.set('A',8,{shape:'cross'});preview.set('A',52,{shape:'beam'});
    const repeatable=first===view.toDataURL();
    preview.set('A',72,{shape:'cross'});
    const empty=burstFx.getContext('2d').getImageData(0,0,1920,1080).data.every(x=>x===0);
    return {on,off,halo,coloredHalo,whiteCore,repeatable,empty};
  });
  save('residue-bloom-on',diagnostics.on);save('residue-bloom-off',diagnostics.off);
  delete diagnostics.on;delete diagnostics.off;
  assert.ok(diagnostics.halo>100&&diagnostics.coloredHalo/diagnostics.halo>.7,'残片周围必须是有色光晕');
  assert.ok(diagnostics.whiteCore>100,'残片必须有高亮白芯');
  assert.ok(diagnostics.repeatable&&diagnostics.empty);
  const energy=await page.evaluate(()=>{
    // 固定同一份细残片源，隔离形状变化，验证爆发极大值及收尾稳定增益。
    preview.set('A',52,{full:false,shape:'beam',beamWidth:640,beamLength:8533.333});
    const c=document.createElement('canvas');c.width=1920;c.height=1080;
    const g=c.getContext('2d');
    const measure=age=>{
      g.clearRect(0,0,c.width,c.height);g.drawImage(beamBloomPipeline.draw(burstFx,640/480,true,age),0,0);
      const data=g.getImageData(0,0,c.width,c.height).data;
      let sum=0;for(let i=0;i<data.length;i+=4)sum+=(data[i]+data[i+1]+data[i+2])*data[i+3]/255;
      return sum;
    };
    const spike=measure(3),steady=measure(16),late=measure(56);
    return {spike,steady,late};
  });
  assert.ok(energy.spike>energy.steady*1.5,'爆发峰值必须显著增强光晕');
  assert.ok(Math.abs(energy.late-energy.steady)<energy.steady*.001,'收尾不能降低 Bloom 增益');
  for(const width of [1280,2160,3840]){
    await page.goto(`http://127.0.0.1:5608/?full=0&shape=cross&frame=24&shake=0&radial=0&width=${width}`);
    await page.waitForFunction(()=>!!window.preview);
    save('resolution-'+width,await page.evaluate(()=>view.toDataURL()));
  }
  console.log(JSON.stringify({motion,bounds,fadeTiming,visibility,...diagnostics,energy}));
  fs.writeFileSync(path.join(output,'checks.json'),JSON.stringify({motion,bounds,fadeTiming,visibility,...diagnostics,energy},null,2));
  await browser.close();
})();
