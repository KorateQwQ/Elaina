sampler uImage0 : register(s0);
float4 GivenColor;

float4 GivenColorFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    if (color.a < 0.2)
        return float4(0, 0, 0, 0);
    return float4(GivenColor.rgba);

}

technique Technique1
{

    pass DrawByGivenColor
    {
        PixelShader = compile ps_2_0 GivenColorFunction();

    }
}