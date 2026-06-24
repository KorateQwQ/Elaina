sampler uImage0 : register(s0);
float2 uScreenResolution;
float2 pos; // pos 就是中心了
float intensity; //最大放大倍数
float range; //放大半径

float4 PSFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float2 offset = (coords - pos);
    float2 rpos = offset * float2(uScreenResolution.x / uScreenResolution.y, 1);
    float dis = length(rpos);
    float a = 1; //用一个变量来控制放大程度
    if (dis < range)
        a = dis * (1 - 1 / intensity) / range + 1 / intensity; 
    return tex2D(uImage0, pos + offset * a);
}
technique Technique1
{
    pass expand
    {
        PixelShader = compile ps_2_0 PSFunction();
    }
}