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
texture2D tex1;
sampler2D uImage2 = sampler_state //对tex1的纹理采样
{
    Texture = <tex1>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

float uTimex;
float uTimey;
float strength;
float4 GivenColor;
float dissolveFactor;

float4 PSFunction(float2 coords : TEXCOORD0) : COLOR0 //通过uImage1,也就是noise的纹理去扰动uImage0
{
    float4 noise = tex2D(uImage1, float2(coords.x + uTimex, coords.y + uTimey));
    float2 move = noise.rg * strength;
    float streath2 = smoothstep(0.5, 0, abs(0.5 - coords.y));
    float4 color = tex2D(uImage0, coords + move);
    return color;
}
float4 PSFunction2(float4 color0 : COLOR0, float2 coords : TEXCOORD0) : COLOR0 //通过uImage1,也就是noise的纹理去扰动uImage0
{
    float4 noise = tex2D(uImage1, float2(coords.x + uTimex, coords.y + uTimey));
    float2 move = noise.rg * strength;
    float streath2 = smoothstep(0.5, 0, abs(0.5 - coords.y));
    float4 color = tex2D(uImage0, coords + move);
    return color * color0;
}
float4 PSFunction3(float2 coords : TEXCOORD0) : COLOR0 //扰动与消融结合
{
    float4 shape = tex2D(uImage2, coords);
    if (shape.r < dissolveFactor || dissolveFactor >= 1)
        return float4(0, 0, 0, 0);
    
    float4 noise = tex2D(uImage1, float2(coords.x + uTimex, coords.y + uTimey));
    float2 move = noise.rg * strength;
    float4 color = tex2D(uImage0, coords + move);

    return color;
    
}

technique Technique1
{
    pass move
    {
        PixelShader = compile ps_2_0 PSFunction();
    }
    pass moveAndColor
    {
        PixelShader = compile ps_2_0 PSFunction2();
    }
    pass moveAndDistort
    {
        PixelShader = compile ps_3_0 PSFunction3();
    }
}
