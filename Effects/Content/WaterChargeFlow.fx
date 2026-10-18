// KL trail textures are intensity masks, often with opaque black backgrounds.
// Convert RGB intensity into coverage explicitly; never composite their black rectangle.
sampler2D TrailSampler : register(s0);
sampler2D NoiseSampler : register(s1);
float4x4 Transform;
float Time;
float Layer;
float NoiseStrength;
float TextureRepeat;
float TextureScrollSpeed;
float NoiseScrollSpeed;
bool UseTextureAlpha;
float MaterialColorStrength;
float FoamThreshold;
float FoamSoftness;
float FoamSpread;

struct VertexInput
{
    float3 Position : POSITION0;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
};
struct PixelInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
};

PixelInput MainVS(VertexInput input)
{
    PixelInput output;
    output.Position = mul(float4(input.Position, 1), Transform);
    output.Color = input.Color;
    output.UV = input.UV;
    return output;
}

float4 SampleWater(float2 uv)
{
    // These 1K source images are drawn across a strip only a few pixels wide.
    // Average a pixel footprint so their fine specular grain doesn't turn into dots.
    float2 dx = ddx(uv) * .35;
    float2 dy = ddy(uv) * .35;
    return (tex2D(TrailSampler, uv - dx - dy) + tex2D(TrailSampler, uv + dx - dy)
        + tex2D(TrailSampler, uv - dx + dy) + tex2D(TrailSampler, uv + dx + dy)) * .25;
}

float4 MainPS(PixelInput input) : COLOR0
{
    float2 uv = input.UV;
    if (Layer > 2.5)
    {
        // Atlas UVs already select one crest; no flow distortion across adjacent cells.
        float4 splash = SampleWater(uv);
        return float4(input.Color.rgb * lerp(float3(1, 1, 1), splash.rgb, .2) * splash.a,
            input.Color.a * splash.a);
    }
    float edge = smoothstep(0, .12, uv.y) * smoothstep(0, .12, 1 - uv.y);
    if (Layer < 0.5)
    {
        float4 halo = tex2D(TrailSampler, float2(.5, uv.y));
        return input.Color * halo.r * halo.a * edge;
    }
    // Material travels at a fixed rate along actual path distance. Only gently
    // refract its interior: noise no longer cuts holes into the whole ribbon.
    float2 noiseUV = float2(uv.x * .9 - Time * NoiseScrollSpeed, uv.y * .5);
    float noise = tex2D(NoiseSampler, noiseUV).r - .5;
    float2 flowUV = float2(uv.x * TextureRepeat - Time * TextureScrollSpeed,
        uv.y + noise * NoiseStrength * edge);
    float4 material = SampleWater(flowUV);
    if (Layer > 1.5)
    {
        // White foam is achromatic: cyan can have G/B near 1 but a low R.
        // Using max(RGB) here would paint the entire blue water white.
        float white = min(material.r, min(material.g, material.b));
        float4 upper = SampleWater(flowUV + float2(0, FoamSpread));
        float4 lower = SampleWater(flowUV - float2(0, FoamSpread));
        white = max(white, min(upper.r, min(upper.g, upper.b)) * upper.a);
        white = max(white, min(lower.r, min(lower.g, lower.b)) * lower.a);
        float softness = max(FoamSoftness, fwidth(white) * .5);
        float foam = smoothstep(FoamThreshold - softness, FoamThreshold + softness, white);
        float coverage = saturate(foam * material.a * edge);
        // Premultiplied, normally blended white occludes the cyan under it.
        // A=0 additive glints alone cannot provide this distinct foam layer.
        return input.Color * coverage;
    }
    float intensity = max(material.r, max(material.g, material.b));
    float mask = UseTextureAlpha ? material.a : intensity * material.a;

    // Alpha-backed water keeps its cyan body and white foam. Opaque black-backed
    // trails use intensity for coverage; their dark background remains transparent.
    float3 tint = lerp(float3(1, 1, 1), material.rgb, saturate(MaterialColorStrength));
    float coverage = saturate(mask * edge);
    return float4(input.Color.rgb * tint * coverage, input.Color.a * coverage);
}

technique WaterFlow
{
    pass Draw
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
}
