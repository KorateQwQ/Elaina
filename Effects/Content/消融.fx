sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);

float dissolveFactor;
float2 uTime;

bool useMask;


float4 lineColor;
float lineWidth;



float4 PixelShaderFunction(float4 color0 : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords + uTime);
    float4 shape = tex2D(uImage1, coords);
    float4 mask = tex2D(uImage2, coords);
    if (useMask)
        shape *= mask;
    


    if(shape.r<dissolveFactor||dissolveFactor>=1)
        return float4(0, 0, 0, 0);
    
    if(dissolveFactor<=0)
        return color * color0;

    if (lineColor.a != 0)
    {
        float t = 1-smoothstep(0, lineWidth, shape.r - dissolveFactor);
        if (t > 0)
            return color * lineColor * (1 - t);
    }
    return color * color0;
}
technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}