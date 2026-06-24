sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float alpha;
float startY;
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float y = fmod(coords.y, 0.25);
    float4 color2 = tex2D(uImage1, float2(coords.x, 1 - y * 4 + startY));
    if(!any(color))
        return float4(0, 0, 0, 0);
    if (!any(color2))
        return float4(0,0,0,0);
        // 灰度 = r*0.3 + g*0.59 + b*0.11
    return float4(color.rgb, alpha);
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}