// 冰锥锁定准星「寒霜菱形框」，由 Tools/IceReticlePreview 的 styleBracket 移植。
// 输出预乘 alpha，配合 BlendState.AlphaBlend 使用。
// p 以准星半径为单位、y 轴朝上；绘制四边形的半边长为 uExtent 个半径。
// ps_3_0 只有 512 指令槽（XNBCompiler 以 Debug 配置编译，不做优化），所以：
// 只依赖动画进度的量都在 CPU 端（IceConeTargetSystem）算好再传进来；霜纹用的 fbm 预烘焙成 uNoise 纹理；
// 画面拆成 Frame / Core 两个 pass，在同一个四边形上各画一次。

sampler uNoise : register(s1);  // R = fbm(q)，覆盖 q ∈ [-uNoiseRange, uNoiseRange]

float uHalfPx;        // 准星半径的屏幕像素 * 0.5，抗锯齿宽度为 1 像素
float uExtent;
float uTime;
float uAlpha;
float3 uDeep;
float3 uMid;

float uS;             // 菱形框半边长
float uE3;            // 主体淡入
float uAmbient;       // 中心寒气
float4 uSpike;        // 顶点冰刺：xy = 菱形半轴，z = 1/dot(b,b)，w = 沿对角线的偏移
float4 uSide;         // 两侧小冰刺：xy = 菱形半轴，z = 1/dot(b,b)，w = 强度
float uGrow;
float2 uFrost;        // x = 霜纹蔓延半径，y = 强度
float uNoiseRange;
float2 uCross;        // 十字准星旋转的 cos / sin
float uG0;            // 十字准星内端距离
float uPulse;         // 中心小菱形强度
float uMoteK;
float3 uRing;         // 锁定冲击环：半径 / 线宽 / 强度
float2 uFlash;        // 锁定星芒：尺寸 / 强度

#define TAU 6.28318530718
#define LOG2E 1.44269504
static const float3 WHITE = float3(0.95, 0.99, 1.0);
static const float INV_SQRT2 = 0.70710678;

static float gE;    // 累计能量，最后经色带映射成颜色
static float gCov;  // 实体覆盖率，决定 alpha（亮背景上保持可读）

float hash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float sdSeg(float2 p, float2 a, float2 b)
{
    float2 pa = p - a, ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h);
}

float sdRhombus(float2 p, float2 b, float invBB)
{
    p = abs(p);
    float2 q = b - 2.0 * p;
    float h = clamp((q.x * b.x - q.y * b.y) * invBB, -1.0, 1.0);
    float d = length(p - 0.5 * b * float2(1.0 - h, 1.0 + h));
    return (p.x * b.y + p.y * b.x - b.x * b.y) > 0.0 ? d : -d;
}

// 线条：线芯 + 紧贴光晕 + 大范围柔光
void stroke(float d, float th, float k)
{
    float c = saturate((th - d) * uHalfPx + 0.5);
    float x = max(d - th, 0.0);
    float halo = exp2(-x * (LOG2E / (th * 2.0 + 0.022)));
    float bloom = exp2(-x * (LOG2E / 0.16));
    gE += k * (c * 1.3 + halo * 0.45 + bloom * 0.07);
    gCov += k * c;
}

// 冰晶面：facet 0..1 控制这一面的明暗
void facetFill(float d, float k, float facet)
{
    float c = saturate(0.5 - d * uHalfPx);
    gE += k * c * lerp(0.22, 0.8, facet);
    gCov += k * c * 0.55;
    stroke(abs(d), 0.005, k);
}

// 二次元十字星芒
void glint(float2 q, float s, float k)
{
    q = abs(q / s);
    float arms = exp2(-(q.x * 36.0 + q.y * 3.2) * LOG2E) + exp2(-(q.y * 36.0 + q.x * 3.2) * LOG2E);
    float core = exp2(-length(q) * (11.0 * LOG2E));
    gE += k * (arms * 0.9 + core * 1.8);
    gCov += k * core * 0.4;
}

float3 ramp(float E)
{
    float3 c = lerp(uDeep * 0.55, uDeep, smoothstep(0.0, 0.3, E));
    c = lerp(c, uMid, smoothstep(0.25, 0.8, E));
    return lerp(c, WHITE, smoothstep(0.85, 1.7, E));
}

float bracket(float2 a, float s, float L)
{
    // 两条线段共用端点 (s, s)，分别沿 -y、-x 方向
    float2 d = a - s;
    float dy = length(float2(d.x, min(max(d.y, -L), 0.0) - d.y));
    float dx = length(float2(min(max(d.x, -L), 0.0) - d.x, d.y));
    return min(dx, dy);
}

void setup(float2 coords, out float2 p, out float2 a, out float2 ac)
{
    p = (coords * 2.0 - 1.0) * uExtent;
    p.y = -p.y;
    // 正方形转 45°，四个角指向上下左右
    a = abs(float2(p.x - p.y, p.x + p.y) * INV_SQRT2);
    ac = a - uS;
    gE = 0.0;
    gCov = 0.0;
}

float4 finish()
{
    float3 col = ramp(gE) * (1.0 - exp2(-gE * (1.8 * LOG2E)));
    float alpha = saturate(gCov) * 0.55 + saturate(gE) * 0.06;
    return float4(col, alpha) * uAlpha;
}

// 第一遍：菱形框、顶点冰刺、霜纹
float4 PSFrame(float2 coords : TEXCOORD0) : COLOR0
{
    float2 p, a, ac;
    setup(coords, p, a, ac);
    float s = uS;

    [loop]
    for (int i = 0; i < 2; i++)
    {
        float fi = (float)i;
        stroke(bracket(a, s - 0.07 * fi, lerp(0.42, 0.26, fi)), lerp(0.013, 0.003, fi), uE3 * lerp(1.0, 0.6, fi));
    }

    // 顶点冰刺（分面）+ 两侧小冰刺
    float2 cp = float2(ac.x + ac.y, ac.y - ac.x) * INV_SQRT2;
    float2 sideOffset = float2(s + 0.055, s - 0.26);
    [loop]
    for (int j = 0; j < 3; j++)
    {
        float2 q;
        float4 shape;
        float facet;
        if (j == 0)
        {
            q = cp - float2(uSpike.w, 0.0);
            shape = float4(uSpike.xyz, uGrow);
            facet = saturate(q.y * 125.0 + 0.5);
        }
        else
        {
            q = (j == 1 ? a : a.yx) - sideOffset;
            shape = uSide;
            facet = step(0.0, q.y);
        }
        facetFill(sdRhombus(q, shape.xy, shape.z), shape.w, facet);
    }

    // 锁定后从四角蔓延的霜纹：噪声等值线按梯度近似换算成距离，画成发光细线
    if (uFrost.y > 0.0)
    {
        float spread = saturate(1.0 - length(ac) / uFrost.x);
        float mask = smoothstep(0.0, 1.0, spread) * step(a.x, s) * step(a.y, s);
        float n = tex2D(uNoise, p * (7.0 / (uNoiseRange * 2.0)) + 0.5).r;
        stroke(abs(n - 0.5) / 3.5, 0.0025, smoothstep(0.0, 0.25, mask) * uFrost.y);
    }

    // 顶点之间的淡菱形轮廓
    float box = length(max(ac, 0.0)) + min(max(ac.x, ac.y), 0.0);
    stroke(abs(box), 0.0015, uE3 * 0.3);

    return finish();
}

// 第二遍：寒气、十字准星、冰尘、锁定闪光
float4 PSCore(float2 coords : TEXCOORD0) : COLOR0
{
    float2 p, a, ac;
    setup(coords, p, a, ac);
    float r = length(p);

    // 寒气笼罩目标
    gE += smoothstep(1.0, 0.1, r) * uAmbient;

    // 十字准星：锁定过程中从 45° 旋正。四向折叠到 +x 扇区，保留扇区内的角度符号
    float2 cq = float2(uCross.x * p.x - uCross.y * p.y, uCross.y * p.x + uCross.x * p.y);
    float2 xq = abs(cq.y) > abs(cq.x) ? float2(cq.y, -cq.x) : cq;
    xq = xq.x < 0.0 ? -xq : xq;
    stroke(sdSeg(xq, float2(uG0 + 0.04, 0.0), float2(uG0 + 0.24, 0.0)), 0.004, uE3);
    float2 hl = xq - float2(uG0 + 0.02, 0.0);
    facetFill(sdRhombus(hl, float2(0.05, 0.026), 1.0 / (0.05 * 0.05 + 0.026 * 0.026)), uE3 * 0.9, step(0.0, hl.y));
    stroke(abs(sdRhombus(p, float2(0.05, 0.05), 200.0)), 0.004, uPulse);

    // 向目标螺旋吸入的冰尘
    if (uMoteK > 0.0)
    {
        [loop]
        for (int i = 0; i < 14; i++)
        {
            float fi = (float)i;
            float h1 = hash(float2(fi, 7.1)), h2 = hash(float2(fi, 3.3));
            float life = frac(uTime * (0.3 + 0.25 * h2) + h1);
            float an = h1 * TAU + life * 1.8;
            float2 cs;
            sincos(an, cs.y, cs.x);
            glint(p - lerp(1.75, 0.4, life) * cs, 0.05 + 0.05 * h2, sin(life * 3.14159) * uMoteK);
        }
    }

    // 锁定瞬间：冲击环 + 中心大星芒
    if (uRing.z > 0.0)
    {
        stroke(abs(r - uRing.x), uRing.y, uRing.z);
        glint(p, uFlash.x, uFlash.y);
    }

    return finish();
}

technique Technique1
{
    pass Frame
    {
        PixelShader = compile ps_3_0 PSFrame();
    }
    pass Core
    {
        PixelShader = compile ps_3_0 PSCore();
    }
}
