sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float uTime;
float widthRatio;
float heightRatio;
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float4 noise = tex2D(uImage1, float2(coords.x * widthRatio,  coords.y * heightRatio));
    if (noise.r < uTime) return float4(0, 0, 0, 0);
    return color;
}

technique Technique1
{
    pass DissolveEffect
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
