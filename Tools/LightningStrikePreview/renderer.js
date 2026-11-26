// Projected 3D ribbons carry physical distance along the bolt into the fragment shader.
// KL Lightning.fx: independent side widths, end taper and edge erosion.
function makeRibbonRenderer(canvas) {
    const gl = canvas.getContext('webgl', {alpha:true, premultipliedAlpha:true, depth:false});
    if (!gl) throw new Error('此预览需要启用 WebGL。');
    const program = gl.createProgram();
    function attach(type, text) {
        const shader = gl.createShader(type);
        gl.shaderSource(shader, text);
        gl.compileShader(shader);
        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(shader));
        gl.attachShader(program, shader);
    }
    attach(gl.VERTEX_SHADER, `attribute vec2 pos; attribute vec4 profile; attribute vec4 segment;
        uniform vec2 viewport; varying highp vec4 v, seg;
        void main(){ v=profile; seg=segment; gl_Position=vec4((pos/viewport*2.-1.)*vec2(1.,-1.),0.,1.); }`);
    attach(gl.FRAGMENT_SHADER, `precision highp float;
        varying vec4 v, seg; // transverse coordinate, along segment (pixels), radius, branch weight; arc start/span, projected span, total arc
        uniform float age, seed, edge, gapScale, pixelSize;
        uniform bool filament, tapered;
        float hash(float x){return fract(sin(mod(x,3331.)*13.713)*4375.31);}
        float noise(float x){float i=floor(x), f=fract(x);return mix(hash(i),hash(i+1.),f*f*(3.-2.*f));}
        void main(){
            float u=clamp(v.y,0.,seg.z);
            float s=seg.x+seg.y*(u/seg.z), t=s/seg.w, branch=v.w;
            float cap=(v.y-u)/v.z;
            float ends=smoothstep(0.,.065,min(t,1.-t));
            // Long swells and necks belong to the intact channel, before edge erosion starts.
            float phase=s/104.+seed*.71+.62*noise(s/132.+seed+47.);
            float swell=.5+.5*cos(6.2831853*phase);
            float baseWidth=mix(.30,.86,swell);
            float sideWidth=baseWidth+.14*noise(s/42.+seed+(v.x>=0.?19.:83.));
            if(tapered)sideWidth=(.72+.16*swell)*mix(.12,1.,pow(1.-t,2.4));
            float y=v.x/(sideWidth*mix(.27,1.,ends));
            // Freeze the erosion field for this strike. Neighbouring sections pinch at different times.
            float erosionDistance=s/gapScale;
            float field=.69*noise(erosionDistance*.041+seed)+.31*noise(erosionDistance*.104+seed+31.);
            float topRate=.59+1.55*field+.38*noise(erosionDistance*.083+seed+57.);
            float bottomRate=.59+1.55*field+.38*noise(erosionDistance*.071+seed+83.);
            float erosion=1.42*edge*smoothstep(.075,.365,age);
            float upper=max(0.,1.-erosion*topRate);
            float lower=-max(0.,1.-erosion*bottomRate);
            float halfWidth=(upper-lower)*.5;
            if(halfWidth<=0.) discard;
            float head=1.-smoothstep(age*32.,age*32.+.035,t);
            if(filament){
                // Capsule padding covers one physical pixel of antialiasing, separate from the luminous width.
                float radius=v.z-.5*pixelSize;
                float transverse=v.x*v.z-(upper+lower)*.5*radius;
                float distancePx=length(vec2(transverse,v.y-u));
                float r=radius*halfWidth;
                float coverage=clamp((r-distancePx)/pixelSize+.5,0.,1.)
                              -clamp((-r-distancePx)/pixelSize+.5,0.,1.);
                float strength=coverage*branch*head*(1.-smoothstep(.20,.43,age));
                gl_FragColor=vec4(.47,.84,1.,1.)*strength;
                return;
            }
            float d=length(vec2(y-(upper+lower)*.5,cap/(sideWidth*mix(.27,1.,ends))))/halfWidth;
            if(d>=1.) discard;
            float core=exp(-d*d*32.), sheath=exp(-d*d*6.);
            float glow=exp(-d*3.7)*(1.-smoothstep(.72,1.,d));
            float reflash=exp(-pow((age-.124)/.027,2.));
            float energy=(.76+.28*exp(-age*8.)+reflash*.45)*branch;
            float fade=(1.-smoothstep(.31,.46,age))*smoothstep(0.,.10,halfWidth);
            vec3 rgb=(vec3(.98,1.,1.)*core+vec3(.19,.76,1.)*sheath+vec3(.045,.29,.52)*glow)*energy*head*fade;
            gl_FragColor=vec4(rgb,clamp(max(rgb.r,max(rgb.g,rgb.b)),0.,1.));
        }`);
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program,gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(program));
    gl.useProgram(program);
    const buffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER,buffer);
    for (const [name,size,offset] of [['pos',2,0],['profile',4,8],['segment',4,24]]) {
        const loc=gl.getAttribLocation(program,name);
        gl.enableVertexAttribArray(loc);
        gl.vertexAttribPointer(loc,size,gl.FLOAT,false,40,offset);
    }
    const uniform = Object.fromEntries(['viewport','age','seed','edge','gapScale','pixelSize','filament','tapered'].map(k=>[k,gl.getUniformLocation(program,k)]));
    gl.enable(gl.BLEND);
    const blend=gl.getExtension('EXT_blend_minmax');
    if(!blend)throw new Error('雷击接缝需要 WebGL 的 EXT_blend_minmax 扩展。');
    gl.blendEquation(blend.MAX_EXT);
    return function render(paths,project,width,height,age,seed,edge,gapScale=1,thickness=1) {
        gl.viewport(0,0,canvas.width,canvas.height);
        gl.clearColor(0,0,0,0);
        gl.clear(gl.COLOR_BUFFER_BIT);
        if(age>.46) return;
        gl.uniform2f(uniform.viewport,width,height);
        gl.uniform1f(uniform.age,age);
        gl.uniform1f(uniform.seed,seed);
        gl.uniform1f(uniform.edge,edge);
        gl.uniform1f(uniform.gapScale,gapScale);
        const pixelSize=width/canvas.width;
        gl.uniform1f(uniform.pixelSize,pixelSize);
        for(const path of paths) {
            gl.uniform1i(uniform.filament,path.filament?1:0);
            gl.uniform1i(uniform.tapered,path.tapered?1:0);
            const screen=path.points.map(project), vertices=[];
            // Individual capsules share the joint sample. MAX blends their union without doubled light.
            for(let i=1;i<screen.length;i++) {
                const a=screen[i-1], b=screen[i], length=Math.hypot(b.x-a.x,b.y-a.y);
                if(length<.001)continue;
                const tx=(b.x-a.x)/length, ty=(b.y-a.y)/length;
                const arcStart=path.arc[i-1], arcSpan=path.arc[i]-arcStart;
                for(const [end,side] of [[0,-1],[0,1],[1,-1],[1,-1],[0,1],[1,1]]) {
                    const p=end?b:a;
                    const radius=path.filament
                        ? Math.min(.55,path.radius*p.scale*Math.sqrt(thickness))+.5*pixelSize
                        : path.radius*p.scale*thickness;
                    const pad=end?radius:-radius;
                    vertices.push(p.x+tx*pad-ty*side*radius,p.y+ty*pad+tx*side*radius,
                        side,end*length+pad,radius,path.weight,arcStart,arcSpan,length,path.arc.at(-1));
                }
            }
            gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(vertices),gl.STREAM_DRAW);
            gl.drawArrays(gl.TRIANGLES,0,vertices.length/10);
        }
    };
}
