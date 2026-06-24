sampler uImage0 : register(s0);

float4 white;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);

    return color+white;
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}