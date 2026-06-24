sampler uImage0 : register(s0);
float uTime;
float4 color;
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float min = 0;//最小半径
    float max = 0.5;//最大半径
    //coords.y = uTime + coords.y;//控制线条移动
    float XdisToMid = abs(coords.x - 0.5);//横坐标与中点距离
    float move = lerp(max, min, 1-coords.y); //越接近顶端，半径越小
    float fx = coords.x;
    if (XdisToMid > move)
        return float4(0, 0, 0, 0);
    if (fx > 0.5)
        fx = lerp(0.5, 1, XdisToMid / move);
    else if (fx < 0.5)
        fx = lerp(0.5, 0, XdisToMid / move);
    return tex2D(uImage0, float2(fx, uTime + coords.y));
}
float4 PixelShaderFunction2(float2 coords : TEXCOORD0) : COLOR0//应用颜色的版本
{
    float min = 0; //最小半径
    float max = 0.5; //最大半径
    //coords.y = uTime + coords.y;//控制线条移动
    float XdisToMid = abs(coords.x - 0.5); //横坐标与中点距离
    float move = lerp(max, min, 1 - coords.y); //越接近顶端，半径越小
    float fx = coords.x;
    if (XdisToMid > move)
        return float4(0, 0, 0, 0);
    if (fx > 0.5)
        fx = lerp(0.5, 1, XdisToMid / move);
    else if (fx < 0.5)
        fx = lerp(0.5, 0, XdisToMid / move);
    return tex2D(uImage0, float2(fx, uTime + coords.y)).r * color;
}
technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
    pass Apply2
    {
        PixelShader = compile ps_2_0 PixelShaderFunction2();
    }
}