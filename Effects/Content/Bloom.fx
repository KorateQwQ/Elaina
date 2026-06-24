sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float2 ImageSize;
float strength;
float strength2;

bool grey;
bool DrawBlur;

float Astrength;
float Bstrength;

float gauss[3][3] =
{
    0.075, 0.124, 0.075,
    0.124, 0.204, 0.124,
    0.075, 0.124, 0.075
};
float gauss2[5][5] =
{
    0.0297, 0.0133, 0.0219, 0.0133, 0.0297,
    0.0133, 0.0596, 0.0983, 0.0596, 0.0133,
    0.0219, 0.0983, 0.1627, 0.0983, 0.0219,
    0.0133, 0.0596, 0.0983, 0.0596, 0.0133,
    0.0297, 0.0133, 0.0219, 0.0133, 0.0297,
};

float Luminance(float3 color)//灰度化
{
    return dot(color, float3(0.3, 0.59, 0.11));
}

float OverLay(float A, float B)//叠加混合模式，增加对比度
{
    if (B <= 0.5f)
    {
        return 2 * A * B;

    }
    return 1 - 2 * (1 - A) * (1 - B);
}
float HardLight(float B, float A)//强光模式
{
    if (B <= 0.5f)
    {
        return 2 * A * B;

    }
    return 1 - 2 * (1 - A) * (1 - B);

}
float Add(float A, float B)
{
    if (B > A)
        return A+B*strength;
    return A+B*strength2;
}
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float4 blurResult = tex2D(uImage1, coords);
    float colorGrey = Luminance(color.rgb);
    float GreyEffect = smoothstep(0, 1, colorGrey);
    if (grey)
    {
        //blurResult = blurResult * strength;
        float3 blurResult2 = blurResult *= Bstrength;
        blurResult *= Astrength;
        float3 OverLayResult = float3(HardLight(blurResult.r, blurResult2.r), HardLight(blurResult.g, blurResult2.g), HardLight(blurResult.b, blurResult2.b));
        if (DrawBlur)
            return float4(OverLayResult, 1);
        return float4(Add(color.r, OverLayResult.r ), Add(color.g, OverLayResult.g), Add(color.b, OverLayResult.b ), color.a);

    }
        //return float4(color.rgb + blurResult.rgb * strength * GreyEffect, color.a);
    if (DrawBlur)
        return float4(blurResult.rgb, 1);
    return float4(color.rgb + blurResult.rgb * strength, color.a);
}
float4 PixelShaderFunction2(float2 coords : TEXCOORD0) : COLOR0//黑白再模糊
{
    float4 color = tex2D(uImage0, coords);
    if (!any(color))
        return color;
        // 灰度 = r*0.3 + g*0.59 + b*0.11
    
    float3 blurResult = float3(0, 0, 0);
    float dx = 2 / ImageSize.x;
    float dy = 2 / ImageSize.y;
    for (int i = -2; i <= 2; i++)
    {
        for (int j = -2; j <= 2; j++)
        {
            float k = gauss2[i + 2][j + 2] * Luminance(tex2D(uImage0, float2(coords.x + dx * i, coords.y + dy * j)).rgb);
            blurResult += float3(k, k, k);
        }
    }
    return float4(blurResult,1);
}

float4 PixelShaderFunction3(float2 coords : TEXCOORD0) : COLOR0//一次5*5高斯模糊
{
    float4 color = tex2D(uImage0, coords);
    float4 blurResult = tex2D(uImage1, coords);

    if (!any(color))
        return color;
    float dx = 2 / ImageSize.x;
    float dy = 2 / ImageSize.y;
    color = float4(0, 0, 0, 0);
    for (int i = -1; i <= 1; i++)
    {
        for (int j = -1; j <= 1; j++)
        {
            color += gauss[i + 1][j + 1] * tex2D(uImage0, float2(coords.x + dx * i, coords.y + dy * j));
        }
    }
    return float4(color.rgb + blurResult.rgb * strength2, color.a);
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
    pass Apply2
    {
        PixelShader = compile ps_3_0 PixelShaderFunction2();
    }
    pass Apply3
    {
        PixelShader = compile ps_3_0 PixelShaderFunction3();
    }
}