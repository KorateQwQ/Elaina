sampler2D iChannel0 : register(s0);//噪声图，决定颜色
sampler2D iChannel1 : register(s1);//水晶图，决定透明度


// C#侧传入：三种主色（rgb有效，a可忽略）
float4 ColorA = float4(1,0,0,1);
float4 ColorB= float4(0,0.0,1,1);
float4 ColorC= float4(0,1,0,1);

// C#侧可选传入：贴图缩放（值越大，花纹越密）
float2 NoiseTiling = float2(1.0, 1.0);
float2 CrystalTiling = float2(1.0, 1.0);

// 透明度与边缘柔化
float AlphaStrength = 1.0;
float EdgeSoftness = 0.05;

// C#侧传入：时间（秒）
float iTime = 0.0;

// 颜色闪烁控制：速度与强度
float FlickerSpeed = 2.0;
float FlickerAmount = 0.15;

// 噪声流动速度（UV 每秒偏移量）
float2 NoiseFlowSpeed = float2(0.0, 0.0);

// 颜色边界描边（当三种颜色接近时，用描边强调花纹边界）
float4 OutlineColor = float4(0, 0, 0, 1);
float OutlineStrength = 0.65;   // 0~1，越大描边越明显
float OutlineThreshold = 0.03;  // 边界判定阈值（越小越容易出描边）
float OutlineWidth = 0.02;      // 过渡宽度（越大越"粗/软"）

// 反向菲涅尔（Rim Light）：让球体边缘出现高光
float4 RimColor = float4(1, 1, 1, 1);
float RimStrength = 0.35; // 0~1，越大边缘越亮
float RimPower = 3.0;     // 越大越贴边、更“细”

// 新增：最终绘制缩放控制（1.0为原始大小，小于1.0缩小，大于1.0放大）
float FinalScale = 1.0;

float Luminance(float3 color)//亮度
{
    return dot(color, float3(0.3, 0.59, 0.11));
}

float4 MainPS(float2 texCoord : TEXCOORD0) : COLOR0
{
    // 应用缩放：将UV坐标从中心缩放
    float2 scaledCoord = (texCoord - 0.5) / FinalScale + 0.5;
    
    // 如果缩放后坐标超出[0,1]范围，则返回透明
    if (scaledCoord.x < 0.0 || scaledCoord.x > 1.0 || scaledCoord.y < 0.0 || scaledCoord.y > 1.0)
    {
        return float4(0, 0, 0, 0);
    }
    
    // 把 [0,1] UV 映射到单位圆盘 [-1,1]
    float2 p = scaledCoord * 2.0 - 1.0;
    float r2 = dot(p, p);

    // 圆外直接裁掉
    clip(1.0 - r2);

    // 半球法线（朝向观察者）
    float z = sqrt(saturate(1.0 - r2));
    float3 n = normalize(float3(p.x, p.y, z));

    // 简单球面UV（经纬度）
    const float INV_TWO_PI = 0.15915494309189535; // 1/(2*pi)
    const float INV_PI = 0.3183098861837907;      // 1/pi
    float2 sphereUV;
    sphereUV.x = atan2(n.z, n.x) * INV_TWO_PI + 0.5;
    sphereUV.y = asin(n.y) * INV_PI + 0.5;

    float2 noiseUV = sphereUV * NoiseTiling + iTime * NoiseFlowSpeed;
    float3 noiseRGB = tex2D(iChannel0, frac(noiseUV)).rgb;
    float lum = saturate(Luminance(noiseRGB));

    // 让颜色映射随时间抖动，看起来像水晶在闪烁
    float flicker = sin(iTime * FlickerSpeed + lum * 6.2831853);
    float lumAnim = saturate(lum + flicker * FlickerAmount);

    // lumAnim: 0~0.5 在 A->B，0.5~1 在 B->C
    float3 colAB = lerp(ColorA.rgb, ColorB.rgb, saturate(lumAnim * 2.0));
    float3 colBC = lerp(ColorB.rgb, ColorC.rgb, saturate((lumAnim - 0.5) * 2.0));
    float3 finalRGB = lerp(colAB, colBC, step(0.5, lumAnim));

    // 描边：用 lumAnim 的屏幕空间梯度检测"花纹边界"
    float2 g = float2(ddx(lumAnim), ddy(lumAnim));
    float edgeMetric = length(g);
    float outline = smoothstep(OutlineThreshold, OutlineThreshold + OutlineWidth, edgeMetric);
    finalRGB = lerp(finalRGB, OutlineColor.rgb, saturate(outline * OutlineStrength));

    // 反向菲涅尔（边缘高光）：V 近似为 (0,0,1)，因此 N·V = n.z
    float ndv = saturate(n.z);
    float rim = pow(saturate(1.0 - ndv), RimPower);
    finalRGB += RimColor.rgb * (rim * RimStrength);

    float alpha = tex2D(iChannel1, frac(sphereUV * CrystalTiling)).r * AlphaStrength;

    // 边缘柔化（避免硬切）
    float r = sqrt(r2);
    float edge = 1.0 - smoothstep(1.0 - EdgeSoftness, 1.0, r);
    alpha *= edge;

    return float4(finalRGB, saturate(alpha));
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 MainPS();
    }

}