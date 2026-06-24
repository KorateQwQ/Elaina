sampler uImage0 : register(s0);
int PixelSize;
float2 ImageSize;
float gauss[3][3] =
{
    0.075, 0.124, 0.075,
    0.124, 0.204, 0.124,
    0.075, 0.124, 0.075
};

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    if (!any(color))
        return color;
    
    color = float4(0, 0, 0, 0);
    
    float pixelW = 2 / ImageSize.x; //假设为9
    float pixelH = 2 / ImageSize.y; //假设为12

    //float4 realColor = float4(realX)
    for (int i = -1; i <= 1; i++)
    {
        for (int j = -1; j <= 1; j++)
        {
            color += tex2D(uImage0, float2(coords.x + i * pixelW, coords.y + j * pixelH));

        }
    }

    return color / 9;
}

float4 PixelShaderFunction2(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    if (!any(color))
        return color;
    float dx = 2 / ImageSize.x;
    float dy = 2 / ImageSize.y;
    color = float4(0, 0, 0, 0);
    for (int i = -1; i <= 1; i++)
    {
        for (int j = -1; j <= 1; j++)
        {
            color += gauss[i + 1][j + 1] * tex2D(uImage0, float2(coords.x + dx * i, coords.y + dy * j));
        }
    }
    return color;
    
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
    pass Gauss
    {
        PixelShader = compile ps_3_0 PixelShaderFunction2();
    }
}