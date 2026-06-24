sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float uTime; //0到1之间
float4 color;
float scale;
float gap;
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0 //以某个圆心往外径向模糊
{
    float x = (coords.x / scale) + uTime;
    float4 noise = tex2D(uImage0, float2(x, coords.y));
    float4 mask = tex2D(uImage1, float2(coords.x, coords.y));
    float4 result = noise * color * mask.r;
    if(result.a<0.05f)
        return float4(0,0,0,0);
    return result;
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }

}