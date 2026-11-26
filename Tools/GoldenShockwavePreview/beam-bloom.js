// HTML 方案的 HDR 传输：RGB / 8 存进 Canvas，所有模糊层均保持相同编码。
// 参照 KL Bloom.fx 的 Overflow / Underflow：先按亮度提取，再扩散彩色能量。
// 这是 WebGL 预览实现，不是正式 FNA 渲染验收。
const beamBloomPipeline = (() => {
  const canvas = document.createElement('canvas');
  const gl = canvas.getContext('webgl', { premultipliedAlpha: true, preserveDrawingBuffer: true });
  if (!gl) throw new Error('金色光束 Bloom 需要 WebGL。');
  const halfFloat = gl.getExtension('OES_texture_half_float');
  if (!halfFloat || !gl.getExtension('OES_texture_half_float_linear') || !gl.getExtension('EXT_color_buffer_half_float'))
    throw new Error('金色光束 Bloom 需要可线性采样的半浮点渲染目标。');
  const vertex = 'attribute vec2 pos; varying vec2 uv; void main(){uv=pos*.5+.5;gl_Position=vec4(pos,0.,1.);}';
  function program(fragment) {
    const p = gl.createProgram();
    for (const [type, code] of [[gl.VERTEX_SHADER, vertex], [gl.FRAGMENT_SHADER, fragment]]) {
      const s = gl.createShader(type); gl.shaderSource(s, code); gl.compileShader(s);
      if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(s));
      gl.attachShader(p, s);
    }
    gl.linkProgram(p);
    if (!gl.getProgramParameter(p, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(p));
    return p;
  }
  const header = 'precision highp float; varying vec2 uv; uniform sampler2D source; uniform float energyScale;';
  const extract = program(header + `void main(){
    vec3 energy=texture2D(source,uv).rgb;
    float luminance=dot(energy*energyScale,vec3(.3,.59,.11));
    gl_FragColor=vec4(energy*smoothstep(.8,1.2,luminance),1.);
  }`);
  const blur = program(header + `uniform vec2 texel; void main(){
    vec3 c=texture2D(source,uv).rgb*.1621;
    vec2 a=texel*(1.+.0219/.1202), b=texel*(1.+.0133/.0729);
    c+=(texture2D(source,uv+vec2(a.x,0)).rgb+texture2D(source,uv-vec2(a.x,0)).rgb
      +texture2D(source,uv+vec2(0,a.y)).rgb+texture2D(source,uv-vec2(0,a.y)).rgb)*.1202;
    c+=(texture2D(source,uv+b).rgb+texture2D(source,uv-b).rgb
      +texture2D(source,uv+vec2(b.x,-b.y)).rgb+texture2D(source,uv+vec2(-b.x,b.y)).rgb)*.0892;
    gl_FragColor=vec4(c,1.);
  }`);
  const combine = program(header + `
    uniform sampler2D level0,level1,level2,level3,level4,level5;
    uniform float bloomEnabled;
    void main(){
      vec3 energy=texture2D(source,uv).rgb*energyScale;
      float luminance=dot(energy,vec3(.3,.59,.11));
      vec3 base=energy/max(1.,luminance);
      vec3 bloom=texture2D(level0,uv).rgb*.42+texture2D(level1,uv).rgb*.30
        +texture2D(level2,uv).rgb*.22+texture2D(level3,uv).rgb*.16
        +texture2D(level4,uv).rgb*.12+texture2D(level5,uv).rgb*.08;
      vec3 result=min(vec3(1.),base+bloom*energyScale*.65*bloomEnabled);
      gl_FragColor=vec4(result,max(result.r,max(result.g,result.b)));
    }`);
  const quad = gl.createBuffer(); gl.bindBuffer(gl.ARRAY_BUFFER, quad);
  gl.bufferData(gl.ARRAY_BUFFER,new Float32Array([-1,-1,1,-1,-1,1,1,1]),gl.STATIC_DRAW);
  function texture() {
    const t=gl.createTexture(); gl.bindTexture(gl.TEXTURE_2D,t);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);
    return t;
  }
  const input=texture();
  const targets=Array.from({length:7},()=>({texture:texture(),framebuffer:gl.createFramebuffer(),width:0,height:0}));
  function target(t,w,h) {
    gl.bindFramebuffer(gl.FRAMEBUFFER,t.framebuffer);
    if(t.width!==w||t.height!==h){
      t.width=w;t.height=h;gl.bindTexture(gl.TEXTURE_2D,t.texture);
      gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,w,h,0,gl.RGBA,halfFloat.HALF_FLOAT_OES,null);
      gl.framebufferTexture2D(gl.FRAMEBUFFER,gl.COLOR_ATTACHMENT0,gl.TEXTURE_2D,t.texture,0);
      if(gl.checkFramebufferStatus(gl.FRAMEBUFFER)!==gl.FRAMEBUFFER_COMPLETE) throw new Error('光束 Bloom 渲染目标不完整');
    }
    gl.viewport(0,0,w,h);
  }
  function use(p,source){
    gl.useProgram(p);gl.bindBuffer(gl.ARRAY_BUFFER,quad);
    const attr=gl.getAttribLocation(p,'pos');gl.enableVertexAttribArray(attr);gl.vertexAttribPointer(attr,2,gl.FLOAT,false,0,0);
    gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,source);gl.uniform1i(gl.getUniformLocation(p,'source'),0);
  }
  function draw(src,scale,enabled=true,age=16){
    if(canvas.width!==src.width||canvas.height!==src.height){canvas.width=src.width;canvas.height=src.height;}
    gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,input);
    gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,true);
    gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL,true);
    gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,src);
    target(targets[0],src.width,src.height);use(extract,input);gl.uniform1f(gl.getUniformLocation(extract,'energyScale'),8*(1+2.2*Math.exp(-Math.pow((age-3)/3.4,2))));gl.drawArrays(gl.TRIANGLE_STRIP,0,4);
    for(let i=1;i<targets.length;i++){
      const previous=targets[i-1], divisor=Math.pow(2,i)*Math.max(.25,scale);
      target(targets[i],Math.max(1,Math.round(src.width/divisor)),Math.max(1,Math.round(src.height/divisor)));
      use(blur,previous.texture);
      gl.uniform2f(gl.getUniformLocation(blur,'texel'),1/targets[i].width,1/targets[i].height);
      gl.drawArrays(gl.TRIANGLE_STRIP,0,4);
    }
    gl.bindFramebuffer(gl.FRAMEBUFFER,null);gl.viewport(0,0,canvas.width,canvas.height);use(combine,input);
    gl.uniform1f(gl.getUniformLocation(combine,'energyScale'),8*(1+2.2*Math.exp(-Math.pow((age-3)/3.4,2))));
    for(let i=0;i<6;i++){
      gl.activeTexture(gl.TEXTURE1+i);gl.bindTexture(gl.TEXTURE_2D,targets[i+1].texture);
      gl.uniform1i(gl.getUniformLocation(combine,'level'+i),i+1);
    }
    gl.uniform1f(gl.getUniformLocation(combine,'bloomEnabled'),enabled?1:0);
    gl.drawArrays(gl.TRIANGLE_STRIP,0,4);
    const error=gl.getError();if(error!==gl.NO_ERROR) throw new Error('光束 Bloom WebGL 错误：'+error);
    return canvas;
  }
  return {draw};
})();
