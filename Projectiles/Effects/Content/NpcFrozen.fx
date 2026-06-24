sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float widthRatio;
float heightRatio;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float4 frozen = tex2D(uImage1, float2(0.3 + coords.x * widthRatio, 0.3 + coords.y * heightRatio));
    if (!any(color))
        return color;
        // 灰度 = r*0.3 + g*0.59 + b*0.11
    //float gs = dot(float3(0.3, 0.59, 0.11), color.rgb);
    return (color * 5 + frozen * 10) / 15;
}
float4 PixelShaderFunction2(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float4 frozen = tex2D(uImage1, float2(coords.x - widthRatio, coords.y - heightRatio));
    if (!any(color))
        return color;
        // 灰度 = r*0.3 + g*0.59 + b*0.11
    //float gs = dot(float3(0.3, 0.59, 0.11), color.rgb);
    return (color * 5 + frozen * 10) / 15;
}
technique Technique1
{
    pass FrozenEffect
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
    pass FrozenEffect2
    {
        PixelShader = compile ps_2_0 PixelShaderFunction2();
    }
}
