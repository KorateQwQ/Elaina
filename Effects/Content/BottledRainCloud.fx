// Bottled Rain's production volume cloud, shared with the FNA preview.
// SpriteBatch supplies the vertex shader; s1 is a repeatable 256x256 scalar noise field.
sampler2D NoiseSampler : register(s1);
float Time;
float Opacity;
float Density;
float FlowSpeed;
float Reveal;
float RevealGlow;

float SmoothUnion(float a, float b, float width)
{
    float h = saturate(.5 + .5 * (a - b) / width);
    return lerp(b, a, h) + width * h * (1 - h);
}

float Ellipsoid(float3 p, float3 center, float3 radius)
{
    return (1 - length((p - center) / radius)) * min(radius.x, min(radius.y, radius.z));
}

float Noise3(float3 p)
{
    float3 cell = floor(p);
    float3 f = frac(p);
    f = f * f * (3 - 2 * f);
    float2 uv = cell.xy + float2(37, 17) * cell.z + f.xy;
    float a = tex2Dlod(NoiseSampler, float4((uv + .5) / 256, 0, 0)).r;
    float b = tex2Dlod(NoiseSampler, float4((uv + float2(37, 17) + .5) / 256, 0, 0)).r;
    return lerp(a, b, f.z);
}

float CloudDensity(float3 p)
{
    float shape = Ellipsoid(p, float3(-2.05, -.12, 0), float3(.69, .43, .59));
    shape = SmoothUnion(shape, Ellipsoid(p, float3(-1.18, .06, .02), float3(.88, .64, .74)), .24);
    shape = SmoothUnion(shape, Ellipsoid(p, float3(-.22, .22, -.10), float3(.96, .82, .85)), .27);
    shape = SmoothUnion(shape, Ellipsoid(p, float3(.82, -.04, .11), float3(.91, .59, .77)), .26);
    shape = SmoothUnion(shape, Ellipsoid(p, float3(1.72, .08, -.05), float3(.83, .64, .68)), .25);
    shape = SmoothUnion(shape, Ellipsoid(p, float3(2.38, -.18, .04), float3(.48, .36, .46)), .19);
    float3 flow = float3(Time * FlowSpeed, -.04 * Time * FlowSpeed, .16 * Time * FlowSpeed);
    float n = Noise3(p * 2.45 + flow) * .68 + Noise3(p * 5.1 - flow * .62 + 11.4) * .32;
    // The field moves within the volume: no scrolling flat cloud image, no hard circular outline.
    return saturate((shape + (n - .52) * .52) * 4.3) * Density;
}

float4 CloudPS(float2 uv : TEXCOORD0) : COLOR0
{
    float2 xy = float2((uv.x - .5) * 6.6, (.5 - uv.y) * 2.8);
    // 固定空间噪声驱动显形，不再缩放/上浮整个云体；中心略早出现，细节沿不规则边界展开。
    // G 通道是上传前平滑合成的二维消融噪声；R 通道继续用于体积云的三维噪声。
    float dissolveField = tex2Dlod(NoiseSampler, float4(uv * float2(1, .45), 0, 0)).g * .82 + abs(xy.x) * .058;
    float edgeDistance = dissolveField - Reveal;
    clip(.105 - edgeDistance);
    float coverage = smoothstep(-.055, .055, -edgeDistance);
    float edgeCore = saturate(1 - abs(edgeDistance) * 22);
    edgeCore = edgeCore * edgeCore * RevealGlow;
    float edgeGlow = saturate(1 - abs(edgeDistance) * 10) * RevealGlow;
    float3 lightDirection = normalize(float3(-.5, .8, .6));
    float3 sum = 0;
    float transmittance = 1;
    const float stepSize = .15;
    [loop]
    for (int i = 0; i < 16; i++)
    {
        float3 p = float3(xy, 1.125 - i * stepSize);
        // 两次相同密度函数共用循环体，避免旧版 ps_3_0 编译器内联两份代码超出指令槽。
        float2 densityPair = 0;
        [loop]
        for (int sampleIndex = 0; sampleIndex < 2; sampleIndex++)
        {
            float sampled = CloudDensity(p + lightDirection * (.38 * sampleIndex));
            densityPair += sampled * float2(1 - sampleIndex, sampleIndex);
        }
        float density = densityPair.x;
        float shadowDensity = densityPair.y;
        float sunlight = exp(-shadowDensity * 1.65);
        float topLight = saturate(p.y * .45 + .58);
        float3 shade = lerp(float3(.29, .31, .46), float3(.86, .93, 1.0), sunlight * .79 + topLight * .14);
        // Subtle cool light scattering at the thinner edges; output remains premultiplied.
        shade += float3(.09, .15, .21) * sunlight * (1 - saturate(density)) * .35;
        float absorption = 1 - exp(-density * stepSize * 3.8);
        sum += transmittance * absorption * shade;
        transmittance *= 1 - absorption;
    }
    float cloudAlpha = 1 - transmittance;
    // RGB 中的额外发光有意超过 Alpha，让消融边界在原 AlphaBlend 下加色；完整成形后完全关闭。
    float3 emission = (float3(.56, .84, 1.0) * edgeCore * .55
        + float3(.38, .27, .92) * edgeGlow * .18) * cloudAlpha;
    return float4(sum * coverage + emission, cloudAlpha * coverage) * Opacity;
}

technique CloudVolume
{
    pass RenderCloud { PixelShader = compile ps_3_0 CloudPS(); }
}
