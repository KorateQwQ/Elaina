sampler uImage0 : register(s0);

float strength;
float2 velocity;
float Iteration;
float alpha;
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0 //以某个圆心往外径向模糊
{


    float2 blurVector = velocity * strength;

    float4 acumulateColor = float4(0, 0, 0, 0);
    [unroll(30)]
    for (int j = 0; j < Iteration; j++)
    {
        acumulateColor += tex2D(uImage0, coords);
        coords += blurVector;
    }

    return acumulateColor * alpha / Iteration;
}
float2 rotatedBy(float2 vec,float radians)
{
    float num = (float)cos(radians);
    float num2 = (float)sin(radians);
    float2 v = vec;
    float2 result = float2(0,0);
    result.x += v.x * num - v.y * num2;
    result.y += v.x * num - v.y * num2;
    return result;
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }

}