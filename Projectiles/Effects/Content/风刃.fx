sampler uImage0 : register(s0);
texture2D tex0;
texture2D tex1;
sampler2D uImage1 = sampler_state //voronoi噪声图
{
    Texture = <tex0>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
sampler2D uImage2 = sampler_state //noise噪声图
{
    Texture = <tex1>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
float uTime; //0到1之间
float4 color;


float4 PSFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 mask = tex2D(uImage0, float2(coords.x, coords.y));
    if(mask.a<0.05&&coords.x>=0.55)
        return float4(0, 0, 0, 0);
    float2 x1 = float2(0.3, 1);
    float2 x2 = float2(0.3, 0);
    float2 xx = float2(1, 0.5);
    float2 point1 = lerp(x1, xx, coords.y);
    float2 point2 = lerp(xx, x2, coords.y);
    float2 final = lerp(point1, point2, coords.y);
    //float maxX = final.x; //0.7-1之间
    float4 noise = tex2D(uImage1, float2(coords.x * 0.1 + uTime, coords.y)); //* 0.1 + uTime
    //float final = lerp(1, 0.5, Yoffset/0.5);
    float x = 0.3;
    float alphaX = smoothstep(final.x -0.4+x, final.x+x , coords.x); //根据maxX做一个减淡
    float maskAlpha = smoothstep(0.5, 1, coords.x);
    //if (coords.x > final.x)
      //  return float4(0, 0, 0, 0);
    float strength = smoothstep(0,1.2,coords.x);
    float strength2 = lerp(0, 1, coords.x/1);
    float4 result = color * ((noise * strength) + strength2) / 2 * alphaX;

    return result + (float4(mask.rgb, 0) * maskAlpha);
    /*

    float2 x1 = float2(0.6, 1);
    float2 x2 = float2(1, 1);
    float2 x3 = float2(1, 0);
    float2 x4 = float2(0.6, 0);

    float2 point1 = lerp(x1, x2, coords.y);
    float2 point2 = lerp(x2, x3, coords.y);
    float2 point3 = lerp(x3, x4, coords.y);
    
    float2 p1 = lerp(point1, point2, coords.y);
    float2 p2 = lerp(point2, point3, coords.y);
    float2 final = lerp(p1, p2, coords.y);
    */
}

float4 NoiseCanvas(float2 coords : TEXCOORD0) : COLOR0
{
    float Yoffset = abs(0.5 - coords.y); //越大说明越接近图片边缘，maxX也越小,0-0.5之间

    float4 Noise = tex2D(uImage2, float2(coords.x + uTime, coords.y));
    float4 mask = tex2D(uImage0, float2(coords.x, coords.y));
    //if (mask.a < 0.05 && coords.x >= 0.55)
      //  return float4(0, 0, 0, 0);
    float2 x1 = float2(0.6, 1);
    float2 x2 = float2(0.6, 0);
    float2 xx = float2(1, 0.5);
    float2 point1 = lerp(x1, xx, coords.y);
    float2 point2 = lerp(xx, x2, coords.y);
    float2 final = lerp(point1, point2, coords.y);
    
    float x = -0.3;
    float X = -0.2;

    float alphaX = smoothstep(final.x - 0.2 + x, final.x + x, coords.x); //根据maxX做一个减淡
    float alphaXX = smoothstep(final.x + 0.4 + X, final.x + 0.2 + X, coords.x);
    float streath2 = smoothstep(0.8, 0, abs(0.5 - coords.y)); //越靠近图片边缘，透明度越低
    return Noise * alphaXX * alphaX * streath2;

}
technique Technique1
{
    pass move
    {
        PixelShader = compile ps_2_0 PSFunction();
    }
    pass noise
    {
        PixelShader = compile ps_2_0 NoiseCanvas();

    }
}