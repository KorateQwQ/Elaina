sampler uImage0 : register(s0);
sampler uImage1 : register(s1);


float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float4 color2 = tex2D(uImage1, coords);

    return color*color2;
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}