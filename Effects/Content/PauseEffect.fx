sampler uImage0 : register(s0);
float2 uScreenResolution;
float gauss[5][5] =
{
    0.0030, 0.0133, 0.0219, 0.0133, 0.0030,
    0.0133, 0.0596, 0.0983, 0.0596, 0.0133,
    0.0219, 0.0983, 0.1621, 0.0983, 0.0219,
    0.0133, 0.0596, 0.0983, 0.0596, 0.0133,
    0.0030, 0.0133, 0.0219, 0.0133, 0.0030
   
};
float4 Pause(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    if (!any(color))
        return color;
    color = float4(0, 0, 0, 0);
    for (int i = -1; i <= 3; i++)
    {
        for (int j = -1; j <= 3; j++)
        {
            color += gauss[i + 1][j + 1] * tex2D(uImage0, float2(coords.x + (2 / uScreenResolution.x) * i, coords.y + 2 / (uScreenResolution.y) * j));
        }
    }
    return color;
}
technique PauseEffects
{
    pass PauseEffect
    {
        PixelShader = compile ps_3_0 Pause();

    }
}