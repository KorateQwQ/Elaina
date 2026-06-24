sampler uImage0 : register(s0);
float uTime;
float4 PSFunction(float2 coords : TEXCOORD0) : COLOR0//减淡靠近图片顶端和低端
{   
    float4 color = tex2D(uImage0, coords);
    {
        float streath2 = smoothstep(0.5, 0, abs(0.5 - coords.y)); //越靠近图片边缘，透明度越低
        return color * streath2;
    }

}

float4 PSFunction2(float2 coords : TEXCOORD0) : COLOR0//球形减淡
{
    float4 color = tex2D(uImage0, float2(coords.x + uTime, coords.y));//如果utime不为0则还会进行一个从左到右的平移
    return color * smoothstep(0.5, 0.2, length(coords.xy - 0.5));
}
technique Technique1
{
    pass move
    {
        PixelShader = compile ps_2_0 PSFunction();
    }
    pass move2
    {
        PixelShader = compile ps_2_0 PSFunction2();
    }
}
