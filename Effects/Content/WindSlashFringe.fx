// SpriteBatch slot 0: the same grayscale gradient used by the original ArcFlame.
sampler2D arcMaterial : register(s0);
// The caller binds the scrolling water noise to slot 1 before each pass.
sampler2D flowNoise : register(s1);
float4 EffectColor = float4(1.5, 1.18, 1.42, 1);
float2 ArcCenter = float2(.5, .5);
float OuterRadius = .4;  // transverse UV radius, set for the current sprite
float OuterRadiusY = .33; // forward UV radius
float FringeDepth = .19; // length of the soft dissolve in sprite U
float CanvasStretch = 1; // supplied by C# when long wisps need more drawable space
float FringeSpacing = 1; // larger values spread the same flowing streaks further apart
float NoiseAmplitude = .74;
float2 LengthScale = float2(1, 1); // short and long wisp depth multipliers
float2 ArcLengthScale = float2(1, 1); // center and wing multipliers, mirrored across the flight axis
float Phase = 0; // animation phase supplied per draw
float4 DirectRimLight; // Direct pass: C# supplies RGB radiance and radial falloff per draw.

float4 MainPS(float2 uv : TEXCOORD0, float4 tint : COLOR0) : COLOR0
{
    float side = (uv.y - ArcCenter.y) / OuterRadius;
    float front = OuterRadiusY * sqrt(saturate(1 - side * side));
    float behind = front - (uv.x - ArcCenter.x);
    float aa = max(fwidth(behind), .0001);
    float outside = smoothstep(-aa, aa, behind);
    float endFade = 1 - smoothstep(.90, 1, abs(side));

    // Move the original material's sampling origin along the front arc. Each
    // row starts at its arc position; position and flowing noise scale its depth.
    float materialX = ArcCenter.x + (OuterRadiusY - behind) * CanvasStretch;
    float materialY = .5 + (uv.y - .5) * (4.0 / 3.0); // original sprite height
    float noiseX = ((1 - materialY) * 2 - 1) / FringeSpacing + 1;
    float2 noiseUV = float2(noiseX, materialX * .05 + Phase);
    float noise = tex2D(flowNoise, frac(noiseUV)).r;
    float gradient = tex2D(arcMaterial, float2(materialX * .5 + .15, materialY * .5 + .25)).r;
    float depthScale = lerp(LengthScale.x, LengthScale.y, smoothstep(.18, .82, noise));
    depthScale *= lerp(ArcLengthScale.x, ArcLengthScale.y, smoothstep(0, 1, abs(side)));
    float fade = 1 - smoothstep(0, 1, behind / (FringeDepth * depthScale) + (.5 - noise) * NoiseAmplitude);
    float coverage = gradient * fade * endFade * outside;
    return float4(EffectColor.rgb * tint.rgb, coverage * EffectColor.a * tint.a);
}

// Direct-to-screen path. Apply coverage before UNORM clips the HDR channels.
float4 DirectPS(float2 uv : TEXCOORD0, float4 tint : COLOR0) : COLOR0
{
    float4 fringe = MainPS(uv, tint);
    float2 p = (uv - ArcCenter) / float2(OuterRadiusY, OuterRadius);
    float rim = exp2(-DirectRimLight.w * abs(length(p) - 1)) * smoothstep(0, .4, p.x);
    float3 rimLight = DirectRimLight.rgb * rim * EffectColor.a * tint.a;
    return float4(fringe.rgb * fringe.a + rimLight, fringe.a);
}

technique MainTechnique {
    pass P0 { PixelShader = compile ps_3_0 MainPS(); }
    pass Direct { PixelShader = compile ps_3_0 DirectPS(); }
}
