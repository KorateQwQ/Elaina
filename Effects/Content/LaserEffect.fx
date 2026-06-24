sampler uImage0 : register(s0);
sampler clipImage : register(s1);//使用此材质对图片进行消融
sampler clipImage2 : register(s2); //使用此材质对图片进行消融,此材质为整体裁切，不会滚动

float clipValue; //内部消融阈值
float clipValue2; //边缘裁切阈值

float Edge; //内部消融的边缘
float4 EdgeColor;
float4 imageColor;

float2 uTime;

float2 scale;


float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 inputColor : COLOR0) : COLOR0
{
    float2 move = coords - float2(0.5, 0.5);
    float2 finalScale = scale;
    finalScale = 1 / finalScale;
    float2 finalCoords = float2(0.5, 0.5) + move * finalScale;
    
    
    float4 color = tex2D(uImage0, finalCoords + uTime);
    float4 clipcolor = tex2D(clipImage, finalCoords + uTime);
    float4 clipcolor2 = tex2D(clipImage2, coords);

    float4 result = float4(0, 0, 0, 0);
    
    result = color;
    result.a = clipcolor2.r;//smoothstep(0, 1, clipcolor2.r);
    
    //处理外部裁切以及描边
    if ((result.r * result.a) <= clipValue2)
    {
        return float4(0, 0, 0, 0);
    }
        
    if (clipcolor.r < clipValue - Edge)
        return float4(0, 0, 0, 0);
    else if (clipcolor.r < clipValue)
    {
        result = result.r * EdgeColor;
        result.a = clipcolor.r * clipcolor2.r;
        return result;
    }
    
    return result * inputColor * imageColor;
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}