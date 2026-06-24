sampler uImage0 : register(s0);
texture2D tex0;
sampler2D uImage1 = sampler_state //对tex0的纹理采样
{
    Texture = <tex0>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
float alpha;


float4 PSFunction(float2 coords : TEXCOORD0) : COLOR0 
{
    float4 color = tex2D(uImage0, float2(coords.x, coords.y));
    float4 mask = tex2D(uImage1, float2(coords.x, coords.y));
    if(color.a<=0.1)
        return float4(0, 0, 0, 0);
    
    return color * mask * alpha;
}

technique Technique1
{
    pass Color
    {
        PixelShader = compile ps_2_0 PSFunction();
    }
}
