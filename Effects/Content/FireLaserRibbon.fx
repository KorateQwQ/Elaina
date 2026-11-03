// 火焰射线的螺旋缎带材质。KL 拖尾/噪声贴图是强度遮罩（常带不透明黑底），
// 取 RGB 最大值作覆盖度，不使用贴图自身的黑色矩形。
// 输出为预乘颜色：RGB 乘 HDR 倍率交给 Bloom 提取，Alpha 只表示覆盖度。
sampler2D MainSampler : register(s0);
sampler2D NoiseSampler : register(s1);
sampler2D DissolveSampler : register(s2);
float4x4 Transform;
float Time;
float Intensity;
float Coverage;
float ScrollSpeed;
float NoiseScrollSpeed;
// 消融：阈值 0 关闭；边缘带宽内以 HDR 烧边颜色绘制，交给 Bloom 晕出（同火焰龙卷暗层）
float DissolveThreshold;
float DissolveEdgeWidth;
float3 DissolveEdgeColor;
float DissolveScrollSpeed;

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

float Mask(float4 c)
{
    return max(c.r, max(c.g, c.b));
}

float4 MainPS(PixelInput input) : COLOR0
{
    float2 uv = input.UV;
    float y = abs(uv.y * 2 - 1);
    // 只沿长度方向取火焰纹理（不跨宽度采样，避免多道条纹），噪声流动出明暗起伏。
    float noise = tex2D(NoiseSampler, float2(uv.x * .45 - Time * NoiseScrollSpeed, uv.y * .6)).r;
    float flame = Mask(tex2D(MainSampler, float2(uv.x - Time * ScrollSpeed, .5 + (uv.y - .5) * .16)));
    float body = (.4 + flame * .6) * (.55 + noise * .45);
    float rim = smoothstep(.35, .8, y) * .4;
    float m = saturate(body + rim * body) * smoothstep(1, .65, y);
    float4 color = float4(input.Color.rgb * m * Intensity, saturate(input.Color.a * m * Coverage));
    if (DissolveThreshold <= 0.0001)
        return color;
    // 两层噪声以不同尺度反向流动再混合，边界形状持续变化，不会像贴在带子上的固定花纹；
    // 阈值沿长度缓慢起伏，烧穿处时开时合。
    float2 p = uv * .12;
    float n1 = tex2D(DissolveSampler, p + float2(-Time * DissolveScrollSpeed, Time * .05)).r;
    float n2 = tex2D(DissolveSampler, p * 1.7 + float2(Time * DissolveScrollSpeed * 2.3, -Time * .09)).r;
    float n = n1 * .6 + n2 * .4;
    float threshold = DissolveThreshold + sin(Time * 2.1 + uv.x * 2.7) * .035;
    float soft = .012;
    float visible = smoothstep(threshold - soft, threshold + soft, n);
    float edge = visible * (1 - smoothstep(threshold + DissolveEdgeWidth - soft,
        threshold + DissolveEdgeWidth + soft, n));
    // 烧边只出现在带身上，随顶点 alpha（深度、首尾淡出）减弱
    float edgeMask = edge * smoothstep(1, .65, y) * input.Color.a;
    color.rgb = lerp(color.rgb, DissolveEdgeColor * input.Color.a, edge);
    color.a = max(color.a, edgeMask);
    return color * visible;
}

technique Technique1
{
    pass Main
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
}
