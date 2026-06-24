sampler uImage0 : register(s0);
int PixelSize;
int PixelSizew;
int PixelSizeh;

float2 ImageSize;

bool IfgivenColor;
float4 givenColor;
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float pixelW = 1 / ImageSize.x; //假设为9
    float pixelH = 1 / ImageSize.y; //假设为12
    //先给自己定位，看看自己属于哪个大像素中，比如像素大小为3，在一个9*12的画布中，
    //一个像素坐标为2/9, 5/12
    int pixelX = (coords.x * ImageSize.x) / PixelSizew; //2/3 = 0,所以x坐标为0
    int pixelY = (coords.y * ImageSize.y) / PixelSizeh; //5/3 = 1, 所以y坐标为1
    
    float4 acumulateColor = float4(0, 0, 0, 0);

    float realX = pixelX / ImageSize.x * PixelSizew;
    float realY = pixelY / ImageSize.y * PixelSizeh;
    //float4 realColor = float4(realX)
    if (pixelY % 2 == 0)//偶数排
    {
        if (pixelX % 2 == 0)
            return float4(1, 1, 1, 1);
        return float4(0, 0, 0, 1);
    }
    else
    {
        if (pixelX % 2 == 0)
            return float4(0, 0, 0, 1);

        return float4(1, 1, 1, 1);

    }
    
    return float4(0, 1, 0, 1);

}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}