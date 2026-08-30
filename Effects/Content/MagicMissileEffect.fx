sampler uImage0 : register(s0); // 扰动纹理
sampler clipImage : register(s1); // 消融纹理

// 圆形参数
float CircleRadius = 0.4f; // 圆的半径（0-0.5）
float2 CircleCenter = float2(0.5f, 0.5f); // 圆心位置
float CircleSoftness = 0.05f; // 圆边缘柔和度
float4 CircleColor = float4(1.00f, 0.42f, 0.92f, 1.00f); // 圆的颜色

// 扰动参数
float DistortionStrength = 0.1f; // 扰动强度
float2 uTime = float2(0, 0); // 扰动纹理滚动时间

// 消融参数
float clipValue = 0.35f; // 消融阈值

float Luminance(float4 color)
{
    return dot(color, float4(0.2125, 0.7154, 0.0721, 0));
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    // === 1. 读取扰动纹理并应用滚动 ===
    float4 distortionTex = tex2D(uImage0, coords + uTime);
    float2 distortion = (distortionTex.rg - 0.5f) * 2.0f * DistortionStrength;

    // === 2. 计算到圆心的距离（应用扰动）===
    float2 toCenter = coords - CircleCenter;
    toCenter += distortion; // 应用扰动扭曲
    float dist = length(toCenter);

    // === 3. 基础圆形遮罩（带柔和边缘）===
    float circleMask = 1.0f - smoothstep(CircleRadius - CircleSoftness, CircleRadius + CircleSoftness, dist);
    if (circleMask <= 0)
        return float4(0, 0, 0, 0);

    // === 4. 消融效果 ===
    float4 clipTex = tex2D(clipImage, coords + uTime);
    float clipAlpha = Luminance(clipTex);

    // 根据消融阈值裁剪
    if (clipAlpha < clipValue)
        return float4(0, 0, 0, 0);

    // === 5. 最终合成 ===
    float4 finalColor = CircleColor * inputColor;
    finalColor.a *= circleMask;

    return finalColor;
}

/*float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    float4 color = tex2D(uImage0, coords + uTime);
    //float4 clipcolor = tex2D(clipImage, coords + uTime);
    float4 clipcolor2 = tex2D(clipImage2, coords);

    float4 result = float4(0, 0, 0, 0);
    
    if (tex2D(clipImage, coords + uTime).r < clipValue)
        return float4(0, 0, 0, 0);
    
    result = color;
    result.a = clipcolor2.r; //smoothstep(0, 1, clipcolor2.r);
    if ((result.r * result.a) <= 0)
    {
        float dx = 2 / ImageSize.x;
        float dy = 2 / ImageSize.y;
        
        [unroll(3)]
        for (int i = -1; i <= 1; i++)
        {
            [unroll(3)]
            for (int j = -1; j <= 1; j++)
            {
                float2 eachcoord = float2(coords.x + dx * i, coords.y + dy * j);
                if (tex2D(uImage0, eachcoord + uTime).r * tex2D(clipImage2, eachcoord).r > 0)
                    return OutLineColor;
            }
        }
        
        return float4(0, 0, 0, 0);
    }
        
    if ((result.r * result.a) < clipValue2 - Edge)
        return float4(0, 0, 0, 0);
    else if ((result.r * result.a) < clipValue2)
    {
        result = result.r * EdgeColor;
        result.a = clipcolor2.r;
        return result;
    }
    
    return result * inputColor * imageColor;
}*/
    

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
