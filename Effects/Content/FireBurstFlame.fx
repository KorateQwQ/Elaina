// 炎爆火柱：流纹贴图 × 火柱包络 低于外层阈值的部分裁掉（同原先 KL CommonMagic 配合 lightMask 的裁切），着色为三色平滑分层：
// 火焰亮度在 外层 → 中层 → 内层 三个阈值之间连续插值颜色（可 HDR，内层由 Bloom 晕出白芯），再乘纹理亮度。
// 包络用公式计算（拟合原 lightMask：椭圆范围内，沿长度向底部指数增强并饱和，横向衰减），分辨率无关，不受采样方式影响。
// 透明度乘 1/外层阈值，轮廓内不透明；轮廓按屏幕像素宽度做一像素软边，不出现阈值硬切的锯齿。
// 流纹贴图被放大数倍，双线性插值的格子会在颜色分层上显出台阶，改用 4 次双线性采样实现的三次 B 样条插值。
// 贴图坐标：u 沿长度（1 为底部），v 横跨火柱（0.5 为中线）。
// Rubble 通道：火柱里被喷起的碎石，做法同火焰龙卷的石块：PerlinX 高于阈值的岛状区域为碎块，
// 阈值附近一圈为 HDR 橙色烧边，由 Bloom 晕开。阈值随包络降低（火柱中心碎块多、边缘少），随时间升高时碎块边烧边缩小。
// WindRing 通道：爆发气流环的顶点带（自带顶点着色器，不经 SpriteBatch）。顶点颜色为 HDR 预乘值，乘贴图遮罩后按 AlphaBlend 混合；
// 遮罩取贴图透明度或亮度，便于直接换用带透明度的气流贴图或不透明的灰度贴图。
sampler uImage0 : register(s0); // 流纹贴图（Eff_Trail_2014）：决定形状与流纹
sampler DissolveSampler : register(s1); // PerlinX：碎石溶解

float4 BandOuter;
float4 BandMid;
float4 BandInner;
float ClipOuter;
float ClipMid;
float ClipInner;
float2 uTime;
float2 TexSize;     // 流纹贴图尺寸（像素）
float3 MaskEllipse; // 椭圆中心 u、沿长度半径、横向半径
float3 MaskAlong;   // 沿长度：增强速率、饱和位置、饱和过渡锐度
float3 MaskAcross;  // 横向：衰减速率、中线附加亮度、中线附加宽度

float Envelope(float2 uv)
{
    float e = 1 - pow((uv.x - MaskEllipse.x) / MaskEllipse.y, 2);
    float t = abs(uv.y - 0.5) / max(MaskEllipse.z * sqrt(max(e, 0)), 1e-4);
    float z = MaskAlong.z * (MaskAlong.y - uv.x);
    float along = exp(-MaskAlong.x * (max(z, 0) + log(1 + exp(-abs(z)))) / MaskAlong.z);
    float across = (1 - t * t) * exp(-MaskAcross.x * t) + MaskAcross.y * exp(-pow(t / MaskAcross.z, 2));
    return e > 0 && t < 1 ? along * across : 0;
}

// 三次 B 样条：每轴的四个权重合并成两次双线性采样（采样器须为线性过滤）
float4 SampleBicubic(float2 uv)
{
    float2 p = uv * TexSize - 0.5;
    float2 f = frac(p);
    float2 i = p - f;
    float2 f2 = f * f, f3 = f2 * f;
    float2 w0 = (1 - 3 * f + 3 * f2 - f3) / 6;
    float2 w1 = (4 - 6 * f2 + 3 * f3) / 6;
    float2 w2 = (1 + 3 * f + 3 * f2 - 3 * f3) / 6;
    float2 w3 = f3 / 6;
    float2 g0 = w0 + w1, g1 = w2 + w3;
    float2 h0 = (i - 0.5 + w1 / g0) / TexSize;
    float2 h1 = (i + 1.5 + w3 / g1) / TexSize;
    return g0.y * (g0.x * tex2D(uImage0, float2(h0.x, h0.y)) + g1.x * tex2D(uImage0, float2(h1.x, h0.y)))
         + g1.y * (g0.x * tex2D(uImage0, float2(h0.x, h1.y)) + g1.x * tex2D(uImage0, float2(h1.x, h1.y)));
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    float4 color = SampleBicubic(coords + uTime);
    float mask = Envelope(coords);
    float field = color.r * mask;
    float edge = saturate((field - ClipOuter) / max(fwidth(field), 1e-4) + 0.5);

    float midT = saturate((field - ClipOuter) / (ClipMid - ClipOuter));
    float innerT = saturate((field - ClipMid) / (ClipInner - ClipMid));
    float3 rgb = color.rgb * lerp(lerp(BandOuter.rgb, BandMid.rgb, midT), BandInner.rgb, innerT);
    return float4(rgb, saturate(mask / ClipOuter)) * edge * inputColor;
}

float2 QuadSize;        // 碎石四边形尺寸（像素，u 方向、v 方向）
float2 RubbleOffset;    // 噪声偏移（像素）：x 随时间增大即碎块上升，y 区分不同组
float RubbleScale;      // 噪声尺寸（每像素）
float RubbleThreshold;  // 包络最亮处的阈值，越低碎块越多越大
float RubbleBurn;       // 叠加在阈值上的消融量：大时碎块缩小、烧尽
float3 StoneColor;      // 碎块颜色（乘噪声亮度）
float BurnEdge;         // 烧边宽度（噪声单位）
float3 BurnColor;       // 烧边（HDR）

float4 RubbleFunction(float2 coords : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    float2 pixel = coords * QuadSize;
    float noise = tex2D(DissolveSampler, (pixel + RubbleOffset) * RubbleScale).r;
    float threshold = lerp(1.0, RubbleThreshold, saturate(Envelope(coords) * 2)) + RubbleBurn;
    float over = noise - threshold;
    float aa = max(fwidth(noise), 1e-4);
    float visible = saturate(over / aa + 0.5);
    float edge = 1 - saturate((over - BurnEdge) / aa + 0.5);
    float3 rgb = lerp(StoneColor * noise, BurnColor, edge);
    return float4(rgb, visible) * inputColor;
}

float4x4 uTransform;   // 屏幕坐标到裁剪空间
float MaskFromRed;     // 1：遮罩取 RGB 最大值（不透明灰度或预乘贴图）；0：取透明度（非预乘带透明度贴图）

struct RingVertexInput
{
    float2 Pos : POSITION0;
    float4 Color : COLOR0;
    float2 Coords : TEXCOORD0;
};

struct RingVertexOutput
{
    float4 Pos : POSITION0;
    float4 Color : COLOR0;
    float2 Coords : TEXCOORD0;
};

RingVertexOutput RingVertex(RingVertexInput input)
{
    RingVertexOutput output;
    output.Pos = mul(float4(input.Pos, 0, 1), uTransform);
    output.Color = input.Color;
    output.Coords = input.Coords;
    return output;
}

float4 RingPixel(float4 color : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 c = tex2D(uImage0, coords);
    float mask = lerp(c.a, max(c.r, max(c.g, c.b)), MaskFromRed);
    return color * mask;
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
    pass Rubble
    {
        PixelShader = compile ps_3_0 RubbleFunction();
    }
    pass WindRing
    {
        VertexShader = compile vs_3_0 RingVertex();
        PixelShader = compile ps_3_0 RingPixel();
    }
}
