sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

bool reverse;
float threshold;

float2 ImageSize;
int start;
int end;
int move;
int time;
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0//黑白闪,低于指定颜色为黑，否则为白
{
    float4 color = tex2D(uImage0, coords);
    if (!any(color))
        return color;
        // 灰度 = r*0.3 + g*0.59 + b*0.11
    float gs = dot(float3(0.3, 0.59, 0.11), color.rgb);
    float c = gs;
    
    float gap = 1 - smoothstep(start, end, time);
    if (reverse)
        c = 1 - gs;
    if (c < gap)
        return float4(0, 0, 0, 1);
    else
        return float4(1, 1, 1, 1);

}
float4 PixelShaderFunction2(float2 coords : TEXCOORD0) : COLOR0 //色彩偏移
{
    //色彩偏移
    float4 color = tex2D(uImage0, coords);
    float pixelW = 1 / ImageSize.x; //像素宽度
    float pixelH = 1 / ImageSize.y; //像素高度
    
    float r = tex2D(uImage0, coords - float2(pixelW*move,0)).r;

    return float4(r, color.g, color.b, color.a);

}
float4 PixelShaderFunction3(float2 coords : TEXCOORD0) : COLOR0
{
    //重新写个反色不知道什么bug不生效
    float4 color = tex2D(uImage0, coords);
    return float4(1-color.r, 1-color.g, 1-color.b, color.a);

}
float4 PixelShaderFunction4(float2 coords : TEXCOORD0) : COLOR0//正统黑白闪，低于指定颜色则绘制黑白闪线条，否则为白
{
    float4 color = tex2D(uImage0, coords);
    float4 Mask = tex2D(uImage1, coords);
    if(!any(Mask))
        return float4(0, 0, 0, 0);
    float MainColor = dot(float3(0.3, 0.59, 0.11), color.rgb);//灰白化后的主颜色

    if (reverse)
        MainColor = 1 - MainColor;
    
    if (MainColor < threshold)
        return Mask;
    else
        return float4(1,1,1,1);
    
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
    pass Apply2
    {
        PixelShader = compile ps_3_0 PixelShaderFunction2();
    }
    pass Apply3
    {
        PixelShader = compile ps_3_0 PixelShaderFunction3();
    }
    pass Apply4
    {
        PixelShader = compile ps_3_0 PixelShaderFunction4();
    }

}