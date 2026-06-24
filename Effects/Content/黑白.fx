sampler uImage0 : register(s0);
float alpha;
float dark;
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    if (!any(color))
        return color;
        // 灰度 = r*0.3 + g*0.59 + b*0.11
    float gs = dot(float3(0.3, 0.59, 0.11), color.rgb);
    return float4(gs * dark, gs * dark, gs * dark, color.a);
}
float4 PixelShaderFunction2(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    if (!any(color))
        return color;
        // 灰度 = r*0.3 + g*0.59 + b*0.11
    float gs = dot(float3(0.3, 0.59, 0.11), color.rgb);
    return float4(gs * dark, gs * dark, gs * dark, color.a) * alpha;
}
technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
    pass Apply2
    {
        PixelShader = compile ps_2_0 PixelShaderFunction2();
    }
}