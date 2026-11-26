const byId = id => document.getElementById(id);
const canvas = byId('ring');
const ctx = canvas.getContext('2d');
const surface = document.createElement('canvas');
const gl = surface.getContext('webgl2', {alpha: true, premultipliedAlpha: true});
const assets = '../../ElainaModSkills/Skills/Water/';
const materialColors = ['WaterColor','FoamColor','TrackColor','BorderColor'];

async function image(name) {
    const img = new Image();
    img.src = assets + name;
    await img.decode();
    return img;
}

// Only adapt the syntax. Both targets run RingPS from the mod's FX source.
function fragmentShader(fx) {
    const body = fx.slice(fx.indexOf('float4 SampleWater'), fx.indexOf('technique WaterEnergyRing'))
        .replace(/\s*:\s*(?:TEXCOORD0|COLOR0)/g, '')
        .replace('uniform bool full', 'bool full')
        .replace(/\bfloat([234])\b/g, 'vec$1')
        .replace(/(?<![\w.])\d+(?![\w.])/g, '$&.0');
    return `#version 300 es
precision highp float;
uniform vec4 RingState, FlowRates;
uniform vec4 ArcGeometry;
uniform bool FullEnergy;
uniform float RingWidth;
uniform vec3 WaterColor, FoamColor, TrackColor, BorderColor;
uniform sampler2D Water;
in vec2 uv; out vec4 pixel;
#define tex2D texture
#define ddx dFdx
#define ddy dFdy
#define atan2 atan
#define frac fract
#define saturate(x) clamp(x,0.0,1.0)
${body}
void main(){pixel=RingPS(uv,vec4(1.0),FullEnergy);}`;
}

function compile(type, source) {
    const shader = gl.createShader(type);
    gl.shaderSource(shader, source);
    gl.compileShader(shader);
    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) throw Error(gl.getShaderInfoLog(shader));
    return shader;
}

async function start() {
    if (!gl) throw Error('此预览需要浏览器启用 WebGL 2。');
    const [response, water] = await Promise.all([
        fetch('../../Effects/Content/WaterEnergyRing.fx', {cache:'no-store'}),
        image('Eff_Splash_322_288.png')]);
    if (!response.ok) throw Error(`读取 WaterEnergyRing.fx 失败 (${response.status})`);
    const fx = await response.text();
    const program = gl.createProgram();
    gl.attachShader(program, compile(gl.VERTEX_SHADER, `#version 300 es
out vec2 uv;
void main(){ vec2 p=vec2(gl_VertexID==1?3.:-1., gl_VertexID==2?3.:-1.);
 gl_Position=vec4(p,0.,1.); uv=p*vec2(.5,-.5)+.5; }`));
    gl.attachShader(program, compile(gl.FRAGMENT_SHADER, fragmentShader(fx)));
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program,gl.LINK_STATUS)) throw Error(gl.getProgramInfoLog(program));
    gl.useProgram(program);
    const uniform = Object.fromEntries(['RingState','ArcGeometry','FlowRates',...materialColors,'Water','FullEnergy','RingWidth']
        .map(name => [name,gl.getUniformLocation(program,name)]));
    gl.bindTexture(gl.TEXTURE_2D,gl.createTexture());
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);
    gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,water);
    gl.uniform1i(uniform.Water,0);
    surface.className='water-layer';
    surface.setAttribute('aria-hidden','true');
    canvas.before(surface);

    const defaults = name => fx.match(new RegExp(`\\b${name} = float\\d\\(([^)]*)\\)`))[1].split(',').map(Number);
    const rates = defaults('FlowRates');
    let phase = 0, surge = 0, fill = .5, previous, paused = matchMedia('(prefers-reduced-motion: reduce)').matches;
    let cast=null;
    let fullAge=0, wasFull=false;
    const isFull=()=>!cast && +byId('fill').value===100 && fill>.995;
    const smooth=(x)=>{ const v=Math.min(1,Math.max(0,x)); return v*v*(3-2*v); };
    function reset() {
        cast=null;
        fullAge=0; wasFull=false;
        for (const name of materialColors) byId(name).value = '#' + defaults(name)
            .map(c => Math.round(c*255).toString(16).padStart(2,'0')).join('');
        for (const [id,i] of [['crest',0],['surface',2],['inner',3]]) byId(id).value=rates[i];
        for (const id of ['sweep','curvature','startAngle','offsetX','offsetY']) byId(id).value=byId(id).defaultValue;
        byId('fill').value=byId('fill').defaultValue; byId('zoom').value='1.5'; byId('backdrop').value='#41918c';
        phase=surge=0; fill=Number(byId('fill').value)/100;
    }
    reset();
    const controls=Array.from(document.querySelectorAll('.panel input, .panel select'));
    const saved=new URLSearchParams(location.hash.slice(1));
    for(const control of controls) if(saved.has(control.id)) control.value=saved.get(control.id);
    if(saved.has('sweep') && !saved.has('curvature')) byId('curvature').value=100;
    fill=+byId('fill').value/100;
    function remember() {
        const params=new URLSearchParams(controls.map(input=>[input.id,input.value]));
        history.replaceState(null,'',`#${params}`);
    }
    document.querySelector('.panel').addEventListener('change',remember);
    function hit(value) { cast=null; byId('fill').value=value; surge=1; if(paused) fill=+byId('fill').value/100; remember(); }
    byId('hit').onclick=byId('scene').onclick=()=>hit(Math.min(100,+byId('fill').value+20));
    byId('full').onclick=()=>hit(100);
    byId('fill').oninput=()=>{cast=null;fill=+byId('fill').value/100;};
    byId('spend').onclick=()=>{
        const initial=+byId('fill').value/100;
        if(initial<=0) return;
        cast={initial, elapsed:0, seconds:(initial===1?40:120-Math.round(40*initial))/60};
        fill=initial; surge=0;
    };
    byId('pause').onclick=()=>{paused=!paused;};
    byId('step').onclick=()=>{paused=true;advance(1/60);};
    byId('reset').onclick=()=>{reset();history.replaceState(null,'',location.pathname);};
    document.addEventListener('visibilitychange',()=>{previous=undefined;});
    function advance(dt) {
        phase+=dt*.85*(cast?-1:1); surge=Math.max(0,surge-dt*3.5);
        if(cast) {
            cast.elapsed+=dt;
            fill=cast.initial*Math.max(0,1-cast.elapsed/cast.seconds);
        } else fill+=(+byId('fill').value/100-fill)*(1-Math.exp(-dt*16));
        const full=isFull();
        fullAge=full && wasFull ? fullAge+dt : 0;
        wasFull=full;
    }
    function frame(now) {
        if (!paused && previous!==undefined) advance(Math.min(.1,(now-previous)/1000));
        previous=now;
        const zoom=Number(byId('zoom').value), size=80*zoom, pixels=Math.round(size*devicePixelRatio);
        if(canvas.width!==pixels || canvas.style.width!==`${size}px`) {
            canvas.width=canvas.height=pixels; canvas.style.width=`${size}px`;
        }
        const extent=Number(byId('sweep').value)/360, bend=Number(byId('curvature').value)/100;
        const side=bend===1?64:Math.max(64,Math.ceil(50+Math.PI*40*extent));
        const surfaceSize=size*side/64, surfacePixels=Math.round(surfaceSize*devicePixelRatio);
        if(surface.width!==surfacePixels || surface.style.width!==`${surfaceSize}px`) {
            surface.width=surface.height=surfacePixels; surface.style.width=`${surfaceSize}px`;
            gl.viewport(0,0,surfacePixels,surfacePixels);
        }
        byId('scene').style.backgroundColor=byId('backdrop').value;
        byId('pause').textContent=paused?'播放':'暂停';
        for(const id of ['fill','crest','surface','inner']) document.querySelector(`output[for=${id}]`).value=byId(id).value+(id==='fill'?'%':'');
        document.querySelector('output[for=curvature]').value=byId('curvature').value+'%';
        for(const id of ['sweep','startAngle','offsetX','offsetY']) document.querySelector(`output[for=${id}]`).value=byId(id).value+(id.startsWith('offset')?' px':'°');
        surface.style.transform=`translate(-50%, -50%) translate(${byId('offsetX').value*zoom}px, ${byId('offsetY').value*zoom}px)`;
        surface.style.opacity=cast?Math.max(0,Math.min(1,1-(cast.elapsed-cast.seconds)/.22)):1;
        for(const name of materialColors) gl.uniform3fv(uniform[name],byId(name).value.slice(1).match(/../g).map(h=>parseInt(h,16)/255));
        gl.uniform4f(uniform.FlowRates,+byId('crest').value,rates[1],+byId('surface').value,+byId('inner').value);
        const full=isFull();
        const fraction=full?1:fill;
        gl.uniform1i(uniform.FullEnergy, full);
        const width=fullAge<.10 ? 2.45+(4.5-2.45)*smooth(fullAge/.10) : 4.5+(3.35-4.5)*smooth((fullAge-.10)/.24);
        gl.uniform1f(uniform.RingWidth, full ? width : 2.45);
        const crest = cast ? .85 : surge * .36;
        const crestPosition = cast ? fraction : fraction * (1 - surge);
        gl.uniform4f(uniform.RingState,fraction,phase,crest,crestPosition);
        gl.uniform4f(uniform.ArcGeometry,extent,+byId('startAngle').value/360,bend,side);
        gl.drawArrays(gl.TRIANGLES,0,3);
        ctx.setTransform(pixels/64,0,0,pixels/64,0,0); ctx.clearRect(0,0,64,64);
        ctx.fillStyle='#cf364e'; ctx.strokeStyle='#f5e5de'; ctx.lineWidth=1.25; ctx.beginPath();
        ctx.moveTo(27,26); ctx.lineTo(38,32); ctx.lineTo(33,33); ctx.lineTo(31,38); ctx.closePath(); ctx.fill(); ctx.stroke();
        requestAnimationFrame(frame);
    }
    requestAnimationFrame(frame);
}
start().catch(error=>{byId('error').textContent=error.message; throw error;});
