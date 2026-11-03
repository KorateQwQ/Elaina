// 火焰射线主体：把火焰龙卷的叠层放到横躺的圆柱上——
// 火焰色带着色的噪声底 + Additive 顺轴火纹 + HDR 高亮碎片，外层暗色条带与焦黑碎块消融出发光烧边。
// 顶点 UV：x = 距主体起点的像素，y = 0~1 横跨条带。输出预乘颜色，配合 AlphaBlend；超过 1 的部分交给 Bloom。
// 圆柱贴图坐标同龙卷：U 绕轴一周，V 沿轴。
sampler WaterSampler : register(s0);    // 水波：火焰底色
sampler WindSampler : register(s1);     // wind1：顺轴火纹
sampler CellSampler : register(s2);     // cellnoise：高亮碎片
sampler BandSampler : register(s3);     // Eff_Noise_705：外层暗色条带
sampler PerlinSampler : register(s4);   // PerlinX：消融与焦黑碎块
sampler DisplaceSampler : register(s5); // Eff_Noise_20：轮廓置换
sampler RampSampler : register(s6);     // 火焰色带（横向 0~1 对应亮度）

float4x4 Transform;
float Time;
float Length;           // 主体长度（像素）
float HalfWidth;        // 条带半宽（像素）
float Period;           // 沿轴一次纹理重复的像素长度
float2 Radius;          // x=暗色外壳半径, y=火焰内芯半径（占条带半宽）
float3 Taper;           // x=起点收拢长度, y=末端燃尽长度, z=起点燃起长度（像素）
float StartRadius;      // 起点处半径比例
float3 Displace;        // x=轮廓置换幅度, y=噪声沿轴缩放, z=流速
float FadePixels;       // 轮廓软边宽度（像素）
// 每层流动：x=绕轴旋转（圈/秒）, y=沿轴流动（周期/秒）, z=螺旋倾斜, w=纹理缩放
float4 FireFlow;
float4 StreakFlow;
float4 SparkFlow;
float4 BandFlow;
float4 StoneFlow;
float RampGain;         // 底色噪声亮度 → 色带位置
float EdgeShade;        // 圆柱侧面压暗（0=不压暗）
float CoreHeat;         // 中心额外的色带位置偏移
float3 StreakColor;     // Additive 火纹（可 HDR）
float3 SparkColor;      // Additive 高亮碎片（HDR）
float SparkThreshold;   // 高亮碎片的消融阈值，越大越稀
float CoreGlow;         // 高亮碎片向中心集中的程度
float3 BandColor;       // 暗色条带
float BandOpacity;      // 条带贴图覆盖度倍率
float BandThreshold;
float3 StoneColor;      // 焦黑碎块
float StoneThreshold;
float StoneEdgeWidth;  // 焦黑碎块的烧边宽度（碎块小，需比条带宽才看得出亮边）
float EdgeWidth;        // 烧边宽度（消融噪声单位）
float3 EdgeColor;       // 烧边 HDR
float EndBurn;          // 末端额外消融量

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

static const float INV_PI = 0.31830988618;

// 横截面：y 为 -1~1 的条带坐标，r 为该处圆柱半径。返回 x=绕轴坐标(-0.25~0.25), y=朝向(中心 1、轮廓 0), z=轮廓软边
float3 Section(float y, float r)
{
    float yn = clamp(y / max(r, 1e-4), -1, 1);
    float around = asin(yn) * INV_PI * 0.5;
    float facing = sqrt(saturate(1 - yn * yn));
    float fade = saturate((r - abs(y)) * HalfWidth / FadePixels);
    return float3(around, facing, fade);
}

float2 CylUV(float around, float along, float4 flow)
{
    return float2(around + Time * flow.x, along - Time * flow.y + around * flow.z) * flow.w;
}

void Over(inout float4 dst, float3 color, float alpha)
{
    dst.rgb = color * alpha + dst.rgb * (1 - alpha);
    dst.a = alpha + dst.a * (1 - alpha);
}

float4 MainPS(PixelInput input) : COLOR0
{
    float s = input.UV.x;
    float y = input.UV.y * 2 - 1;
    float along = s / Period;
    float startT = smoothstep(0, 1, saturate(s / Taper.x));
    float endT = saturate((Length - s) / Taper.y);
    float profile = lerp(StartRadius, 1, startT);

    // 上下轮廓各取一条噪声，随旋转与流动起伏，形成火舌状边缘
    float edgeSide = y >= 0 ? 0.25 : -0.25;
    float n = tex2D(DisplaceSampler, float2(edgeSide + Time * BandFlow.x, along * Displace.y - Time * Displace.z)).r;
    float wobble = 1 + (n - 0.5) * 2 * Displace.x;
    float shellR = Radius.x * profile * wobble;
    float fireR = Radius.y * profile * wobble;

    // 末端：消融阈值随距离抬高，火柱向头部锥形逐渐烧尽；起点同样从法阵里燃起，避免硬切边
    float startBurn = saturate(s / Taper.z);
    float burn = max((1 - endT) * EndBurn, (1 - startBurn) * 0.8);

    float4 result = 0;

    // ===== 火焰内芯 =====
    // 不用分支包住采样：ps_3_0 动态分支内不能用带梯度的 tex2D；软边为 0 处 Over 不改变结果。
    float3 fire = Section(y, fireR);
    {
        float2 uvFire = CylUV(fire.x, along, FireFlow);
        float base = tex2D(WaterSampler, uvFire).r;
        float shade = lerp(1 - EdgeShade, 1, fire.y);
        // 中心朝向镜头处整体提亮，形成龙卷那样的亮芯
        float heat = base * RampGain * shade + CoreHeat * fire.y * fire.y;
        float3 color = tex2D(RampSampler, float2(saturate(heat), 0.5)).rgb;

        float2 uvStreak = CylUV(fire.x, along, StreakFlow);
        float4 streak = tex2D(WindSampler, uvStreak);
        // 底色与火纹不超过 1：KL Bloom 会把亮度超过 1 的像素整体提取，细火柱上大面积超阈值会把暗部全部晕成橙色。
        // 发光只留给高亮碎片和烧边。
        color = saturate(color + streak.rgb * streak.a * StreakColor * shade);

        float2 uvSpark = CylUV(fire.x, along, SparkFlow);
        float spark = tex2D(CellSampler, uvSpark).r;
        float sparkMask = step(SparkThreshold + burn, tex2D(PerlinSampler, uvSpark * 0.5).r);
        color += spark * sparkMask * SparkColor * lerp(1, fire.y * fire.y, CoreGlow);

        float visible = step(burn, tex2D(PerlinSampler, uvFire * 0.7).r);
        Over(result, color, fire.z * visible);
    }

    // ===== 暗色外壳：条带与焦黑碎块，消融边发光 =====
    float3 shell = Section(y, shellR);
    {
        float2 uvBand = CylUV(shell.x, along, BandFlow);
        float4 band = tex2D(BandSampler, uvBand);
        float d = tex2D(PerlinSampler, uvBand * 0.6).r;
        float threshold = BandThreshold + burn;
        float visible = step(threshold, d);
        float edge = visible * (1 - step(threshold + EdgeWidth, d));
        float alpha = saturate(band.a * BandOpacity) * visible;
        Over(result, lerp(BandColor, EdgeColor, edge), saturate(alpha + edge) * shell.z);

        float2 uvStone = CylUV(shell.x, along, StoneFlow);
        float ds = tex2D(PerlinSampler, uvStone).r;
        float stoneThreshold = StoneThreshold + burn;
        float stone = step(stoneThreshold, ds);
        float stoneEdge = stone * (1 - step(stoneThreshold + StoneEdgeWidth, ds));
        Over(result, lerp(StoneColor, EdgeColor * 0.7, stoneEdge), stone * shell.z);
    }

    return result * input.Color.a * step(0.001, endT) * smoothstep(0, 0.6, startBurn);
}

technique Technique1
{
    pass Main
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
}
