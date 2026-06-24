sampler uImage0 : register(s0);
int PixelSize;
float2 ImageSize;
bool IfgivenColor;
float4 givenColor;
float3 edgeStrength;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float pixelW = 1 / ImageSize.x; //假设为9
    float pixelH = 1 / ImageSize.y; //假设为12
    //先给自己定位，看看自己属于哪个大像素中，比如像素大小为3，在一个9*12的画布中，
    //一个像素坐标为2/9, 5/12
    
    int pixelX = (coords.x * ImageSize.x) / PixelSize; //2/3 = 0,所以x坐标为0
    int pixelY = (coords.y * ImageSize.y) / PixelSize; //5/3 = 1, 所以y坐标为1
    
    float realX = pixelX / ImageSize.x * PixelSize;
    float realY = pixelY / ImageSize.y * PixelSize;
    //float4 realColor = float4(realX)
    float4 acumulateColor=float4(0,0,0,0);
    [unroll(7)]
    for (float i = 0; i < PixelSize; i++)
    {
        [unroll(7)]
        for (float j = 0; j < PixelSize; j++)
        {
            acumulateColor += tex2D(uImage0, float2(realX + i * pixelW, realY + j * pixelH));

        }
    }
    if (!any(acumulateColor))
        return float4(0,0,0,0);
    
    float len = length(realX - 0.5f);
    float alpha = lerp(0, 1, len / 0.5f);
    if (IfgivenColor)
        return givenColor * alpha;
    
    if (length(realY - coords.y) <= pixelH)//|| length(realY + pixelH*PixelSize - coords.y) == pixelH 
    {
        float4 result = acumulateColor / (PixelSize * PixelSize);
        return result + float4(result.rgb + edgeStrength, result.a) * result.a;

    }
    if (length(realX - coords.x) <= pixelW)//|| length(realX + pixelW * PixelSize - coords.x) <= pixelW
    {
        float4 result = acumulateColor / (PixelSize * PixelSize);
        return result + float4(result.rgb + edgeStrength, result.a) * result.a;

    }
    return acumulateColor / (PixelSize * PixelSize);
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}