sampler uImage0 : register(s0);

float radius;
float2 uImageSize;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0 //黑白闪,低于指定颜色为黑，否则为白
{
    float4 color = tex2D(uImage0, coords);
    if(!any(color))
        return color;
    bool edgeColor = false;
    // 获取每个像素的正确大小
    float dx = 1 / uImageSize.x;
    float dy = 1 / uImageSize.y;
    
    [unroll(3)]
    for (int i = -radius; i <= radius; i++)
    {    
        [unroll(3)]
        for (int j = -radius; j <= radius; j++)
        {
            float4 c = tex2D(uImage0, coords + float2(dx * i, dy * j));
            // 如果任何一个像素没有颜色
            if (!any(c))
            {
                edgeColor = true;
                break;
            }
        }
    }
    if (edgeColor)
        return float4(1, 1, 1, 1);

    return color;
}


technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}