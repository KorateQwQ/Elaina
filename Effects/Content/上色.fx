sampler uImage0 : register(s0);
bool smooth;
float4 color1;
float4 color2;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    if (color.r<0.1)
        return float4(0, 0, 0, 0);
    float4 result = color;
    if (smooth)
    {
        
        result = lerp(color1, color2, smoothstep(0, 1, color.r) / 1);
        return result;
    }
    result = lerp(color1, color2, color.r / 1);

    return result;
}
float4 PixelShaderFunction2(float2 coords : TEXCOORD0) : COLOR0//这个版本还乘以了本体颜色
{
    float4 color = tex2D(uImage0, coords);
    if (!any(color))
        return float4(0, 0, 0, 0);
    float4 result = color;
    if (smooth)
    {
        result = lerp(color1, color2, smoothstep(0, 1, color.r) / 1);
        return result*color;
    }
    result = lerp(color1, color2, color.r / 1);

    return result * color;
}

technique Technique1
{
    pass Apply1
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
    pass Apply2
    {
        PixelShader = compile ps_2_0 PixelShaderFunction2();
    }
}