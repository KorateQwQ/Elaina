sampler uImage0 : register(s0);
float2 uImageSize;

float strenth; //强度1到0之间
float3 lightcolor; //rgb

float4 PSFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords); //图片取样
    float dx = 1 / uImageSize.x; //每个像素的纹理大小
    float dy = 1 / uImageSize.y; //每个像素的纹理大小
    if (any(color))//有颜色则返回
        return color;
    float alpha = 0;
    for (int i = -3; i <= 3; i++)//对于每个透明像素，寻找周边半径3个像素范围内的所有像素
    {
        for (int j = -3; j <= 3; j++)
        {
            float4 c = tex2D(uImage0, coords + float2(dx * i, dy * j));
            // 如果任何一个像素有颜色
            if (any(c))
            {   
                float length = sqrt(i * i + j * j);
                alpha = lerp(strenth, 0.2, length/10);
                break;
            }
        }
    }
    if (alpha < 0.05)
        return float4(0,0,0,0);
    return float4(lightcolor,  alpha); //把这个透明像素涂上颜色，距离越远，颜色亮度越低
}
technique Technique1
{
    pass move
    {
        PixelShader = compile ps_3_0 PSFunction();
    }
}