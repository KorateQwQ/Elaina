// Local mesh to clip coordinates, supplied for each vortex and viewport.
float4x4 Transform;
// x = age, y = accumulated distortion, z = accumulated rotation, w = tension.
float4 Motion = float4(0,0,0,.45);
// x = UV density, y = accumulated texture travel, z = HDR emission, w = inner-face gain.
float4 Surface = float4(1,0,1,.8);
// Base color plus particle emission, supplied by WindVortexDrawing before the particle halo and dust passes.
float ParticleRgbGain = 1.0;
// Screen UV offset per local pixel along X/Y, supplied for the current camera and core size.
float4 LensBasis = float4(.001,0,0,.001);
// x = sampled scene, y = transparent environment. Complementary weights for direct backbuffer drawing.
float2 SceneWeights = float2(0,1);
// Radial dark base under the vortex: rgb = premultiplied-free shade, a = center coverage.
float4 DarkBase = float4(.035,.012,.04,.7);
// x = radius in core-local pixels, y = falloff exponent; the edge reaches zero with zero slope.
float2 DarkShape = float2(760,1.6);
texture HazeTexture; // baked GlowDot radial falloff
sampler Haze = sampler_state { Texture=<HazeTexture>; MinFilter=Linear; MagFilter=Linear; AddressU=Clamp; AddressV=Clamp; };
texture MaskTexture; // packed KL blade red mask
texture NoiseTexture; // packaged periodic RGB core noise
texture SceneTexture; // current scene during KL capture, noise bound when sampling is disabled
sampler Blade = sampler_state { Texture=<MaskTexture>; MinFilter=Linear; MagFilter=Linear; AddressU=Clamp; AddressV=Clamp; };
sampler Noise = sampler_state { Texture=<NoiseTexture>; MinFilter=Linear; MagFilter=Linear; AddressU=Wrap; AddressV=Wrap; };
sampler Scene = sampler_state { Texture=<SceneTexture>; MinFilter=Linear; MagFilter=Linear; AddressU=Clamp; AddressV=Clamp; };
struct Input { float4 pos:POSITION0; float4 color:COLOR0; float2 uv:TEXCOORD0; };
struct Vertex { float4 pos:POSITION0; float4 color:COLOR0; float2 uv:TEXCOORD0; float2 screen:TEXCOORD1; };
Vertex VS(Input i) {
    Vertex o; o.pos=mul(float4(i.pos.xy,0,1),Transform); o.color=i.color; o.uv=i.uv;
    o.screen=o.pos.xy*float2(.5,-.5)+.5; return o;
}
Vertex BladeVS(Input i) {
    Vertex o = VS(i);
    o.screen.x = i.pos.z; // interpolated section width, in the same units as uv.y
    return o;
}
float4 BladePS(Vertex i):COLOR0 {
    float u=i.uv.x-Surface.y;
    float folded = 1-abs(frac(u*.5)*2-1);
    float v = .5 + i.uv.y / max(i.screen.x,.0001);
    // The KL image has no mip chain. Average its red channel across a screen
    // pixel, especially where the bent strip approaches an edge-on silhouette.
    float2 bladeUV = float2(folded,v);
    float2 dx = clamp(ddx(bladeUV),-.18,.18)*.28;
    float2 dy = clamp(ddy(bladeUV),-.18,.18)*.28;
    float mask = .25*(tex2D(Blade,bladeUV-dx-dy).r+tex2D(Blade,bladeUV+dx-dy).r+
                       tex2D(Blade,bladeUV-dx+dy).r+tex2D(Blade,bladeUV+dx+dy).r);
    float footprint = max(fwidth(v),.0001);
    float left = saturate(.5 + v / footprint);
    float right = saturate(.5 + (1-v) / footprint);
    mask *= max(0,left+right-1);
    float outer=i.color.r;
    float3 tint=lerp(float3(.12,.055,.11)*Surface.w,float3(1,.59,.84),outer);
    float alpha=mask*i.color.a;
    // Spread emitted light across the blade section. The source mask still
    // determines surface coverage, including transparent holes in the material.
    // Sample a wider copy of the ribbon, with a short filter along its length.
    // Fixed transverse rows used to project narrow peaks across the entire halo.
    float2 glowUV = float2(folded, .5 + (v-.5)*.18);
    float2 blur = float2(.045,.16);
    float energy = .4*tex2D(Blade,glowUV).r
                 + .15*(tex2D(Blade,glowUV+blur).r+tex2D(Blade,glowUV-blur).r
                       +tex2D(Blade,glowUV+float2(blur.x,-blur.y)).r+tex2D(Blade,glowUV+float2(-blur.x,blur.y)).r);
    float envelope = exp(-2.4*(v-.5)*(v-.5))*(1-smoothstep(1.,1.45,abs(v-.5)));
    float halo = energy*envelope*.46*i.color.a*outer*Surface.z*smoothstep(0,.24,i.color.g);
    return float4(tint*alpha+float3(.9,.34,.65)*halo,alpha);
}
float4 HazeAt(float2 uv) {
    float opacity=pow(tex2D(Haze,uv).r,.52)*.27;
    return float4(float3(.96,.46,.75)*opacity,opacity);
}
float4 HazePS(Vertex i):COLOR0 { return HazeAt(i.uv)*i.color.a; }
float4 DarkAt(float2 uv) {
    float r=saturate(length(uv-.5)*2);
    float opacity=pow(1-smoothstep(0,1,r),DarkShape.y)*DarkBase.a;
    return float4(DarkBase.rgb*opacity,opacity);
}
float4 DarkBasePS(Vertex i):COLOR0 { return DarkAt(i.uv)*i.color.a; }
float4 ParticleHaloPS(Vertex i):COLOR0 {
    // Match the approved .85 halo at particle bloom strength 3.5.
    float energy = tex2D(Haze, i.uv).r * i.color.a * ((ParticleRgbGain - 1) * (.85 / 3.5));
    float3 tint = i.color.rgb * i.color.rgb * i.color.rgb;
    return float4(tint * energy, 0);
}
float4 DustPS(Vertex i):COLOR0 {
    // Integrate coverage at both edges of the subpixel triangle.
    float3 bary=float3(i.uv,1-i.uv.x-i.uv.y);
    float3 width=max(fwidth(bary),.0001);
    float3 coverage=saturate(bary/width+.5);
    float alpha=i.color.a*coverage.x*coverage.y*coverage.z;
    return float4(i.color.rgb * (alpha * ParticleRgbGain), alpha);
}
float4 CorePS(Vertex i):COLOR0 {
    float2 p=(i.uv-.5)*240; float r=length(p);
    float2 n=p/max(.001,r), tangent=float2(-n.y,n.x);
    float angle=atan2(n.y,n.x+.000001)/6.2831853;
    float3 edge=tex2D(Noise,n*.23+float2(.044,-.031)*Motion.y).rgb;
    float3 broad=tex2D(Noise,n*.38+float2(.146,-.087)*Motion.y).rgb;
    float3 detail=tex2D(Noise,n*.72+float2(-.101,.193)*Motion.y+.12*(broad.gb-.5)).rgb;
    float pressure=Motion.w*(4.6*(broad.r-.5)+2*(detail.b-.5));
    float pull=1.8*(broad.b-.5)+.8*(detail.g-.5);
    float d=r-39-1.55*(edge.g-.5)-pressure;
    float dist=max(d,0), winding=log(1+dist*.070);
    float2 flow=float2(angle+.32*winding+.014*Motion.w*pull*exp(-dist/24),dist*.0075);
    float3 slow=tex2D(Noise,flow+float2(-.057*Motion.z,.093*Motion.x)).rgb;
    float2 shear=slow.gr-.5;
    float3 fine=tex2D(Noise,flow*float2(2,1.45)+.30*shear+float2(.033*Motion.z,.115*Motion.x)).rgb;
    float phase=6.2831853*(angle*3+.95*winding+.14*Motion.z+.52*slow.r+.24*fine.g);
    float patch=smoothstep(.22,.72,.65*slow.b+.35*fine.r);
    float gas=exp(-dist/24)*(.09*slow.b+patch*(exp(-6*abs(sin(phase)))+.50*exp(-8*abs(sin(phase+.55+1.4*fine.b)))));
    float strain=Motion.w*(.24*(broad.g-.5)+.12*(detail.r-.5));
    float lip=exp(-abs(d)/1.12)*(.57+.52*fine.r)*(1+.6*strain);
    float shoulder=exp(-abs(d)/4.7)*(.14+.35*patch)*(1+strain);
    float ext=smoothstep(-.7,.6,d), lens=ext*exp(-dist/19);
    float2 shift=lens*(n*(3+7*slow.r+pressure*.48+strain*4)+tangent*(12*shear.x+3*(fine.b-.5)+Motion.w*pull*1.8));
    float4 bg=tex2D(Scene,i.screen+shift.x*LensBasis.xy+shift.y*LensBasis.zw);
    float coverage=1-ext, refract=lens*SceneWeights.x;
    float fade=(1-smoothstep(87,111,r))*i.color.a;
    float3 light=float3(1,.28,.66)*(gas*ext*1.6+shoulder)+float3(1,.65,.86)*lip;
    float edgeGlow = .75*exp(-abs(d)/7.) + .25*exp(-abs(d)/18.);
    float3 scattered = Surface.z*float3(.85,.32,.68)*edgeGlow*(.54+.35*patch);
    // The captured game scene precedes our haze. Include it in the bent sample.
    // The captured game scene also precedes the dark base drawn beneath everything.
    float4 bentDark = DarkAt(.5+(p+shift)/(2*DarkShape.x))*i.color.a;
    float4 bentHaze = HazeAt(.5+(p+shift)/492.)*i.color.a;
    float3 behind = (bg.rgb*(1-bentDark.a)+bentDark.rgb)*(1-bentHaze.a)+bentHaze.rgb;
    float3 rgb = float3(.027,.012,.032)*coverage+behind*ext*refract+light+scattered;
    return float4(rgb*fade,(coverage+ext*refract)*fade);
}
technique DarkBasePass { pass P { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 DarkBasePS(); } }
technique HazePass { pass P { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 HazePS(); } }
technique ParticleHaloPass { pass P { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 ParticleHaloPS(); } }
technique DustPass { pass P { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 DustPS(); } }
technique BladePass { pass P { VertexShader=compile vs_3_0 BladeVS(); PixelShader=compile ps_3_0 BladePS(); } }
technique CorePass { pass P { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 CorePS(); } }
