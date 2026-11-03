sampler Trail : register(s0);
sampler Noise : register(s1);
float4x4 uTransform;
float2 ImageScale;
float Flow;
float Shrink;
float NoiseScale;
float MaxShrinkRate;
bool UseRedAlpha;

struct VSInput { float2 Position : POSITION0; float4 Color : COLOR0; float3 UV : TEXCOORD0; };
struct PSInput { float4 Position : SV_POSITION; float4 Color : COLOR0; float3 UV : TEXCOORD0; };
PSInput VertexMain(VSInput input)
{
    PSInput output;
    output.Position = mul(float4(input.Position, 0, 1), uTransform);
    output.Color = input.Color;
    output.UV = input.UV;
    return output;
}
float4 PixelMain(PSInput input) : COLOR0
{
    // 与魔力飞弹拖尾相同：固定轴向噪声决定局部截面归零的速度，而非整条统一淡掉。
    float noise = tex2D(Noise, frac(float2(input.UV.x * NoiseScale + 0.173, 0.417))).r;
    float rate = lerp(1.0, MaxShrinkRate, smoothstep(0.32, 0.72, noise));
    float width = saturate(1.0 - Shrink * rate);
    float side = abs(input.UV.y - 0.5) * 2.0;
    if (width <= 0.0 || side > width) return 0;
    float2 uv = 0.5 + (input.UV.xy - 0.5) / ImageScale + float2(Flow, 0);
    float4 tex = tex2D(Trail, uv);
    float4 color = UseRedAlpha ? float4(input.Color.rgb, tex.r) : tex * input.Color;
    // 退场只收细和断裂：保留持续态的 HDR 亮度与贴图透明度，残线仍参与 Bloom。
    // 可见面积由局部截面收至零，不再整体淡出成暗橙色遮罩。
    return color;
}
technique Technique1
{
    pass Pass0
    {
        VertexShader = compile vs_3_0 VertexMain();
        PixelShader = compile ps_3_0 PixelMain();
    }
}
