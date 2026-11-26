'use strict';
const el = id => document.getElementById(id);
const value = id => el(id).valueAsNumber;
const rad = degrees => degrees*Math.PI/180;
const add = (a,b,k=1) => a.map((x,i)=>x+b[i]*k);
const cross = (a,b) => [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];
const unit = a => a.map(x=>x/Math.hypot(...a));
const ground = el('ground').getContext('2d'), dust = el('dust').getContext('2d');
window.addEventListener('error',event=>{el('error').textContent=event.message;});
const drawBolts = makeRibbonRenderer(el('bolt'));
const target = [0,0,0];
let width,height,pixelRatio,zoom, paths=[], sparks=[], serial=0, age=.06, paused=false, previousTime;
const pitch=.39, cameraDistance=2450;
let viewSin=0, viewCos=1;

function project([x,y,z]) {
    const depth=x*viewSin+z*viewCos;
    const scale=zoom*cameraDistance/(cameraDistance-depth*Math.cos(pitch)-y*Math.sin(pitch));
    return {x:width*.50+(x*viewCos-z*viewSin)*scale,
        y:height*.68+(depth*Math.sin(pitch)-y*Math.cos(pitch))*scale,scale};
}

// Invert the same perspective camera on the ground plane, y=0.
function selectGround(px,py) {
    const u=(px-width*.50)/zoom, v=(py-height*.68)/zoom;
    const denominator=cameraDistance*Math.sin(pitch)+v*Math.cos(pitch);
    if(denominator<=0) return;
    const depth=v*cameraDistance/denominator, side=u*(1-depth*Math.cos(pitch)/cameraDistance);
    const yaw=rad(value('view'));
    const x=side*Math.cos(yaw)+depth*Math.sin(yaw), z=depth*Math.cos(yaw)-side*Math.sin(yaw);
    if(Math.max(Math.abs(x),Math.abs(z))>800) return;
    target.splice(0,3,x,0,z);
    fire();
    drawGround();
}

function randomStream(seed) {
    let state=seed>>>0;
    return ()=>{state=(Math.imul(state,1664525)+1013904223)>>>0; return state/4294967296;};
}

function makePath(start,end,radius,weight,bendDegrees,frequency,random,spacing=90) {
    const delta=add(end,start,-1), length=Math.hypot(...delta), axis=unit(delta);
    const right=unit(cross(axis,Math.abs(axis[1])>.95?[0,0,1]:[0,1,0])), up=cross(axis,right);
    // Each tooth has one short, steep leg and a longer return leg. Its 3D slope is tan(angle).
    const count=Math.max(1,Math.round(length*frequency/spacing));
    const spans=Array.from({length:count},()=>.7+.6*random());
    const spanTotal=spans.reduce((a,b)=>a+b,0);
    const slope=Math.tan(rad(bendDegrees)), phase=random()*Math.PI*2;
    const points=[start],arc=[0];
    let distance=0;
    for(let i=0;i<count;i++) {
        const span=length*spans[i]/spanTotal, leg=span*(.23+.15*random());
        const tipDistance=distance+(random()<.5?leg:span-leg);
        const bearing=phase+i*2.45+(random()-.5)*.35;
        const lateral=add(right.map(x=>x*Math.cos(bearing)),up,Math.sin(bearing));
        points.push(add(add(start,axis,tipDistance),lateral,leg*slope));
        distance+=span;
        points.push(i===count-1?end:add(start,axis,distance));
    }
    for(let i=1;i<points.length;i++) arc.push(arc[i-1]+Math.hypot(...add(points[i],points[i-1],-1)));
    return {points,arc,radius,weight};
}

function makeFilament(main,axis,bearing,random) {
    const right=unit(cross(axis,Math.abs(axis[1])>.95?[0,0,1]:[0,1,0]));
    const normal=add(right.map(x=>x*Math.cos(bearing)),cross(axis,right),Math.sin(bearing));
    const depth=cross(axis,normal);
    const total=main.arc.at(-1), phase=random()*Math.PI*2;
    const spread=Math.min(36,total*.085);
    const points=[main.points[0]],arc=[0];
    for(let i=1;i<main.points.length;i++) {
        const start=main.points[i-1], delta=add(main.points[i],start,-1);
        const span=main.arc[i]-main.arc[i-1], steps=Math.ceil(span/22);
        for(let j=1;j<=steps;j++) {
            const f=j/steps, s=main.arc[i-1]+span*f;
            const envelope=Math.min(1,s/38,(total-s)/38);
            const offset=(.48+.38*Math.sin(s/43+phase)+.42*(random()-.5))*spread;
            const p=add(add(add(start,delta,f),normal,offset*envelope),depth,(random()-.5)*spread*.55*envelope);
            points.push(i===main.points.length-1&&j===steps?main.points.at(-1):p);
            arc.push(arc.at(-1)+Math.hypot(...add(points.at(-1),points.at(-2),-1)));
        }
    }
    return {points,arc,filament:true,radius:.5,weight:.78};
}

function generate() {
    const seed=30241+serial*1919, random=randomStream(seed+1), a=rad(value('azimuth')), e=rad(value('elevation')), length=value('length');
    const toSource=[Math.cos(e)*Math.cos(a),Math.sin(e),Math.cos(e)*Math.sin(a)];
    const steepness=value('steepness'), frequency=value('bendFrequency');
    const source=add(target,toSource,length), main=makePath(source,target,11,1,steepness,frequency,randomStream(seed));
    paths=[main];
    if(el('branchStyle').value==='filaments') {
        const filamentRandom=randomStream(seed+17), bearing=filamentRandom()*Math.PI*2;
        paths.push(makeFilament(main,toSource,bearing,filamentRandom),
            makeFilament(main,toSource,bearing+Math.PI*.87,filamentRandom));
    } else {
        const branchRandom=randomStream(seed+1), count=Math.max(1,Math.round(length/85));
        for(let i=0;i<count;i++) {
            const t=.1+.73*(i+branchRandom()*.8)/count;
            const index=t*(main.points.length-1), segment=Math.floor(index);
            const root=add(main.points[segment],add(main.points[segment+1],main.points[segment],-1),index-segment);
            const scatter=unit([branchRandom()-.5,branchRandom()-.5,branchRandom()-.5]);
            const reach=Math.min(length*(1-t)*.72,42+branchRandom()*68);
            const tip=add(add(root,toSource,-reach*.68),scatter,reach*.75);
            const fork=makePath(root,tip,2.8,.76,.55*steepness,frequency,randomStream(seed+17+i),44);
            fork.tapered=true;
            paths.push(fork);
        }
    }
    sparks=Array.from({length:13},(_,i)=>{
        const wide=i<10, angle=-Math.PI/2+(random()-.5)*(wide?2.8:1.2);
        const variation=random()*Math.PI*2, r=Math.sqrt(random());
        return {wide,angle,offset:random()*(wide?50:200),life:(wide?20:15)+Math.floor(random()*10)-5,
            scaleX:2+.8*r*Math.cos(variation),scaleY:1.5+.1*r*Math.sin(variation)};
    });
}

function drawGround() {
    ground.clearRect(0,0,width,height);
    function line(a,b) {
        const p=project(a), q=project(b);
        ground.moveTo(p.x,p.y);ground.lineTo(q.x,q.y);
    }
    ground.beginPath();
    for(let i=-800;i<=800;i+=50) {
        line([i,0,-800],[i,0,800]);line([-800,0,i],[800,0,i]);
    }
    ground.lineWidth=.7;ground.strokeStyle='rgba(106,156,185,.19)';ground.stroke();
    for(const radius of [22,30]) {
        ground.beginPath();
        for(let i=0;i<=96;i++) {
            const a=i*Math.PI/48, p=project(add(target,[Math.cos(a)*radius,0,Math.sin(a)*radius]));
            if(i===0)ground.moveTo(p.x,p.y);else ground.lineTo(p.x,p.y);
        }
        ground.strokeStyle=radius===22?'#7bafc3':'#42637a';ground.stroke();
    }
    el('coords').textContent=`落点　X ${Math.round(target[0])}　 Y 0　 Z ${Math.round(target[2])}`;
}

function resize() {
    const rect=el('stage').getBoundingClientRect();
    width=rect.width;height=rect.height;pixelRatio=window.devicePixelRatio||1;
    zoom=Math.min(width/650,height/590)*value('magnification')/100;
    for(const id of ['ground','bolt','dust']) {
        const canvas=el(id);
        canvas.width=Math.round(width*pixelRatio); canvas.height=Math.round(height*pixelRatio);
    }
    for(const ctx of [dust,ground]) ctx.setTransform(pixelRatio,0,0,pixelRatio,0,0);
    drawGround();paint();
}

async function loadAtlas(name,cols,rows) {
    const image=new Image();image.src=`particles/${name}.png`;
    await image.decode();return {image,cols,w:image.width/cols,h:image.height/rows};
}
let arcs,loops,impact;
// Store 1/8 of additive radiance in an opaque RGB surface so overlapping sprites can exceed 1.
const radiance=document.createElement('canvas').getContext('2d',{alpha:false});
const excess=document.createElement('canvas').getContext('2d',{alpha:false});
function sizeLightTarget(ctx,w,h){
    if(ctx.canvas.width!==w||ctx.canvas.height!==h){ctx.canvas.width=w;ctx.canvas.height=h;}
}
function sprite(atlas,frame,x,y,scaleX,scaleY,rotation=0) {
    radiance.save();radiance.translate(x,y);radiance.rotate(rotation);radiance.scale(scaleX,scaleY);
    radiance.drawImage(atlas.image,(frame%atlas.cols)*atlas.w,Math.floor(frame/atlas.cols)*atlas.h,
        atlas.w,atlas.h,-atlas.w/2,-atlas.h/2,atlas.w,atlas.h);
    radiance.restore();
}
function drawParticles() {
    dust.clearRect(0,0,width,height);
    const t=age-.032;
    if(!el('particles').checked||!arcs||t<0||t>=25/60)return;
    const center=project(target);
    const ticks=t*60;
    const size=value('particleSize');
    const spriteScale=center.scale*size;
    // Keep the travelling arcs inside the capture even when the sprites are small.
    const captureScale=Math.max(center.scale,spriteScale);
    const left=Math.max(0,Math.floor((center.x-440*captureScale)*pixelRatio));
    const top=Math.max(0,Math.floor((center.y-480*captureScale)*pixelRatio));
    const w=Math.min(el('dust').width,Math.ceil((center.x+440*captureScale)*pixelRatio))-left;
    const h=Math.min(el('dust').height,Math.ceil((center.y+180*captureScale)*pixelRatio))-top;
    if(w<=0||h<=0)return;
    sizeLightTarget(radiance,w,h);
    radiance.setTransform(1,0,0,1,0,0);radiance.clearRect(0,0,w,h);
    radiance.setTransform(pixelRatio,0,0,pixelRatio,-left,-top);
    radiance.globalCompositeOperation='lighter';
    radiance.globalAlpha=3*(25-ticks)/30/8;
    sprite(impact,Math.floor((5+ticks)/3),center.x,center.y-70*spriteScale,.8*spriteScale,.8*spriteScale);
    for(const spark of sparks) {
        const remaining=spark.life-ticks;
        if(remaining<=0)continue;
        const distance=(spark.offset+135*(1-Math.pow(.9,ticks)))*center.scale;
        const x=center.x+Math.cos(spark.angle)*distance, y=center.y+Math.sin(spark.angle)*distance;
        radiance.globalAlpha=7.5/8*Math.max(0,(remaining-(spark.wide?5:0))/spark.life);
        const frame=Math.floor((remaining+spark.life)*(spark.wide?.4:.2))%(spark.wide?9:4);
        sprite(spark.wide?arcs:loops,frame,x,y,.2*spark.scaleX*spriteScale,.2*spark.scaleY*spriteScale,spark.angle);
    }
    // For additive capture KL extracts max(rgb - 1, 0). Keep the excess at 1/3 scale for filtering.
    sizeLightTarget(excess,Math.max(1,Math.ceil(w/2)),Math.max(1,Math.ceil(h/2)));
    excess.filter='brightness(160%) contrast(166.6667%)';
    excess.drawImage(radiance.canvas,0,0,excess.canvas.width,excess.canvas.height);
    dust.save();dust.setTransform(1,0,0,1,0,0);
    dust.filter='brightness(800%)';dust.drawImage(radiance.canvas,left,top);
    dust.globalCompositeOperation='lighter';
    for(const [radius,gain] of [[2,1.6],[7,1.5],[18,1.2]]){
        dust.filter=`blur(${radius*pixelRatio}px) brightness(${gain})`;
        dust.drawImage(excess.canvas,left,top,w,h);
    }
    dust.restore();
}
function paint() {
    const scrub=el('scrub');
    const milliseconds=Math.min(scrub.max,Math.round(age*1000));
    if(scrub.valueAsNumber!==milliseconds){
        scrub.value=milliseconds;
        scrub.parentElement.querySelector('output').value=milliseconds+' ms';
    }
    drawBolts(paths,project,width,height,age,serial%701,value('edge')/100,value('gapScale'),value('thickness'));
    drawParticles();
}
function fire() {
    serial++;
    age=paused ? .06 : 0;
    generate();paint();
}
function tick(time) {
    if(previousTime!==undefined&&!paused) {
        age+=(time-previousTime)/1000;
        if(el('repeat').checked && age>=value('interval')/1000)fire();
        paint();
    }
    previousTime=time;requestAnimationFrame(tick);
}
document.addEventListener('visibilitychange',()=>{previousTime=undefined;});
for(const slider of document.querySelectorAll('input[type=range]')) {
    function label(){slider.parentElement.querySelector('output').value=slider.value+slider.dataset.unit;}
    label();slider.oninput=()=>{
        label();
        if(slider.id==='scrub'){
            paused=true;el('pause').textContent='继续';age=slider.valueAsNumber/1000;
        }
        if(slider.id==='view'){
            const yaw=rad(value('view'));
            viewSin=Math.sin(yaw);viewCos=Math.cos(yaw);drawGround();
        }
        if(slider.id==='magnification')resize();
        if(['azimuth','elevation','length'].includes(slider.id)){generate();if(age>.26)age=.06;}
        if(slider.id==='steepness'||slider.id==='bendFrequency')generate();
        paint();
    };
}
el('particles').onchange=paint;
el('branchStyle').onchange=()=>{generate();paint();};
el('stage').onpointerdown=e=>{const r=el('stage').getBoundingClientRect();selectGround(e.clientX-r.left,e.clientY-r.top);};
el('pause').onclick=()=>{paused=!paused;previousTime=undefined;el('pause').textContent=paused?'继续':'暂停';};
el('fire').onclick=()=>{el('repeat').checked=false;fire();};
Promise.all([loadAtlas('LightningDust',3,3),loadAtlas('LightningDust3',2,2),loadAtlas('LightningDustTex',1,10)]).then(atlases=>{
    [arcs,loops,impact]=atlases;generate();resize();new ResizeObserver(resize).observe(el('stage'));requestAnimationFrame(tick);
},error=>{el('error').textContent=`粒子贴图加载失败：${error.message}`;throw error;});
