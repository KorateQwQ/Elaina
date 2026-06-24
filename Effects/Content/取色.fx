sampler uImage0 : register(s0);
float4 givenColor;
float minC;
float maxC;
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    if (color.r < minC)
        return float4(0,0,0,0);
    if(color.r>maxC)
        return float4(0, 0, 0, 0);

    return color * givenColor;
}
float4 PixelShaderFunction2(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    color = smoothstep(minC, maxC, color.r);
    //if (color.r < minC)
      //  return float4(0, 0, 0, 0);
   // if (color.r > maxC)
        //return float4(0, 0, 0, 0);
    return color * givenColor;
}

technique Technique1
{
    pass Delete
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
    pass Transfer
    {
        PixelShader = compile ps_2_0 PixelShaderFunction2();
    }
}