sampler uImage0 : register(s0);
sampler clipImage : register(s1);//使用此材质对图片进行消融
sampler clipImage2 : register(s2); //使用此材质对图片进行消融,此材质为整体裁切，不会滚动

float clipValue; //内部消融阈值
float clipValue2; //边缘裁切阈值

float Edge; //内部消融的边缘
float4 EdgeColor;
float4 imageColor;

float2 uTime;


float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    float4 color = tex2D(uImage0, coords + uTime);
    float4 clipcolor2 = tex2D(clipImage2, coords);

    float4 result = float4(0, 0, 0, 0);
    
    result = color;
    result.a = clipcolor2.r;//smoothstep(0, 1, clipcolor2.r);
    
    //处理外部裁切以及描边
    if ((result.r * result.a) <= clipValue2)
    {
        return float4(0, 0, 0, 0);
    }
        
    if ((result.r * result.a) < clipValue - Edge)
        return float4(0, 0, 0, 0);
    else if ((result.r * result.a) < clipValue)
    {
        return EdgeColor;
    }
    
    return result * inputColor * imageColor;
}

/*float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    float4 color = tex2D(uImage0, coords + uTime);
    //float4 clipcolor = tex2D(clipImage, coords + uTime);
    float4 clipcolor2 = tex2D(clipImage2, coords);

    float4 result = float4(0, 0, 0, 0);
    
    if (tex2D(clipImage, coords + uTime).r < clipValue)
        return float4(0, 0, 0, 0);
    
    result = color;
    result.a = clipcolor2.r; //smoothstep(0, 1, clipcolor2.r);
    if ((result.r * result.a) <= 0)
    {
        float dx = 2 / ImageSize.x;
        float dy = 2 / ImageSize.y;
        
        [unroll(3)]
        for (int i = -1; i <= 1; i++)
        {
            [unroll(3)]
            for (int j = -1; j <= 1; j++)
            {
                float2 eachcoord = float2(coords.x + dx * i, coords.y + dy * j);
                if (tex2D(uImage0, eachcoord + uTime).r * tex2D(clipImage2, eachcoord).r > 0)
                    return OutLineColor;
            }
        }
        
        return float4(0, 0, 0, 0);
    }
        
    if ((result.r * result.a) < clipValue2 - Edge)
        return float4(0, 0, 0, 0);
    else if ((result.r * result.a) < clipValue2)
    {
        result = result.r * EdgeColor;
        result.a = clipcolor2.r;
        return result;
    }
    
    return result * inputColor * imageColor;
}*/
    

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}