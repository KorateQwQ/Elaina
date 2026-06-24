sampler uImage0 : register(s0);
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{   
    float4 color = tex2D(uImage0, coords);
    float4 color1 = float4(color.rgb,1);
    float4 result = color1 * color.a;
    return result;
}



technique Technique1
{
    pass DrawEffect
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }

}