sampler uImage0 : register(s0);
sampler uImage1 : register(s1); // 正式 PerlinX

float4x4 uTransform;
float uTimex;
float2 NoiseOffset;
float2 NoiseScale;
float TrailAge;
float MaxShrinkRate;
float ShrinkExponent;
bool UseShrink;

struct VSInput
{
    float2 Pos : POSITION0;
    float4 Color : COLOR0;
    float3 Texcoord : TEXCOORD0;
};

struct PSInput
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR0;
    float3 Texcoord : TEXCOORD0;
};

PSInput VertexShaderFunction(VSInput input)
{
    PSInput output;
    output.Color = input.Color;
    output.Texcoord = input.Texcoord;
    output.Pos = mul(float4(input.Pos, 0, 1), uTransform);
    return output;
}

float4 PixelShaderFunction(PSInput input) : COLOR0
{
    float3 coord = input.Texcoord;
    float4 color = tex2D(uImage0, float2(coord.x + uTimex, coord.y));
    if (UseShrink)
    {
        float noise = tex2D(uImage1, frac(float2(coord.x * NoiseScale.x + NoiseOffset.x,
            0.5 * NoiseScale.y + NoiseOffset.y))).r;
        float rate = lerp(1.0, MaxShrinkRate, smoothstep(0.34, 0.70, noise));
        float width = pow(saturate(1.0 - TrailAge * rate), ShrinkExponent);
        float side = abs(coord.y - 0.5) * 2.0;
        if (side > width)
            return float4(0, 0, 0, 0);
    }
    return color * input.Color * coord.z;
}

// 头部爆点：与拖尾同样用自带顶点着色器的四边形绘制图集中的一帧（不经 SpriteBatch，避免沿用不确定的顶点着色器），
// 外圈先被噪声蚀去、核心最后消失，消融前沿提亮
float4 HeadFrame;      // 当前帧在图集中的 UV 矩形 (x, y, w, h)
float HeadDissolve;    // 0 完整 → 1 消尽
float HeadEdge;        // 消融前沿宽度
float2 HeadNoiseScale;
float3 HeadColor;      // HDR
float3 HeadEdgeColor;  // 前沿 HDR
float HeadNoiseInvert; // 1 时反相噪声
float HeadNoiseWeight; // 噪声在消融场中的占比，其余为“中心最后消失”的半径项

float4 HeadPixelShader(PSInput input) : COLOR0
{
    float2 uv = input.Texcoord.xy;
    float4 tex = tex2D(uImage0, uv);
    float2 local = (uv - HeadFrame.xy) / HeadFrame.zw;
    // 帧内爆点的可见范围约为半宽的 0.75，按此归一化，消融进度才均匀落在可见部分
    float radius = saturate(length(local - 0.5) * 2.0 / 0.75);
    float noise = tex2D(uImage1, frac(local * HeadNoiseScale + NoiseOffset)).r;
    noise = lerp(noise, 1.0 - noise, HeadNoiseInvert);
    // 越大越晚消失：半径项让中心最后消失，噪声打碎边界
    float field = (1.0 - radius) * (1.0 - HeadNoiseWeight) + noise * HeadNoiseWeight;
    float cut = HeadDissolve * (1.0 + HeadEdge) - HeadEdge;
    float visible = smoothstep(cut, cut + HeadEdge, field);
    float rim = (1.0 - smoothstep(cut + HeadEdge, cut + HeadEdge * 2.0, field)) * saturate(HeadDissolve * 8.0);
    float3 tint = lerp(HeadColor, HeadEdgeColor, rim);
    // 贴图已预乘；顶点 alpha 为覆盖度
    return float4(tex.rgb * tint, tex.a * input.Color.a) * visible;
}

technique Technique1
{
    pass ColorBar
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
    pass Head
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 HeadPixelShader();
    }
}
