sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float radius;
float strength;
float Iteration;
float2 center;
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0//以某个圆心往外径向模糊
{

    
    float2 blurVector = (coords - center) * radius * strength;

    float4 acumulateColor = float4(0, 0, 0, 0);
    [unroll(30)]
    for (int j = 0; j < Iteration; j++)
    {
        acumulateColor += tex2D(uImage0, coords);
        coords += blurVector;
    }

    return acumulateColor / Iteration;
}
float4 PixelShaderFunction2(float2 coords : TEXCOORD0) : COLOR0 //以某个圆心往外径向模糊,使用噪声图影响模糊强度
{
    float4 noise = tex2D(uImage1, coords);
    float2 blurVector = (center - coords) * radius * strength * noise.r;

    float4 acumulateColor = float4(0, 0, 0, 0);
    [unroll(30)]
    for (int j = 0; j < Iteration; j++) 
    {
        acumulateColor += tex2D(uImage0, coords);
        coords += blurVector;
    }

    return acumulateColor / Iteration;
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
    pass ApplyNoise
    {
        PixelShader = compile ps_3_0 PixelShaderFunction2();
    }
}