sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float uTime;
float zoom;
float radius;//最大0.5，此时正好为一个圆
float2 uScreenResolution;//分辨率
float2 iMouse;
float Strength;
float2 center;
bool reverse;
bool ifNoise;
float NoiseStrength;
float4 givenColor;
float edgeLen;
float4 edgeColor1;
float4 edgeColor2;
float2 lens_distortion(float2 r, float strength)
{
    return r * (1.0 + strength * dot(r, r));
    
}
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0//将矩形映射为圆形
{
    //float4 color = tex2D(uImage0, float2(coords.x + uTime, coords.y));
    // pos 就是中心了
    float2 pos = float2(0.5, 0.5);
    // offset 是中心到当前点的向量
    float2 offset = (coords - pos);
    // 因为长宽比不同进行修正
    float2 rpos = offset * float2(uScreenResolution.x / uScreenResolution.y, 1);
    if (length(rpos) > radius)
        return float4(0, 0, 0, 0);

    float ratio = length(rpos) / radius;
    float ratio2 = 0.5 / abs(offset.y);
    if (abs(coords.x - 0.5) > abs(coords.y - 0.5))
        ratio2 = 0.5 / abs(offset.x);
    
    float2 maxPoint = offset * ratio2;
    
    float2 realPoint = maxPoint*ratio;
    
    
    float dis = length(rpos);//开始处理放大效果

    
    float2 finalPos = pos + realPoint + offset * dis * zoom;
    finalPos.x += uTime;
    return tex2D(uImage0, finalPos) * givenColor;
}


float4 PixelShaderFunction2(float2 coords : TEXCOORD0) : COLOR0//鱼眼镜头
{
    float4 noise = tex2D(uImage1, float2(coords.x + uTime, coords.y + uTime));
    float2 uv = coords  * float2(uScreenResolution.x / uScreenResolution.y, 1);
    float2 mouse_uv = iMouse * float2(uScreenResolution.x / uScreenResolution.y, 1);
    float mouse_dist = distance(uv + noise.rg * NoiseStrength, mouse_uv);
    if (mouse_dist > radius)
    {
        float dis = mouse_dist - radius;
        if (dis > edgeLen)
            return float4(0, 0, 0, 0);
        return lerp(edgeColor1, edgeColor2, dis / edgeLen);

    }
        
        
        //Distort space inside the lens with pincushion distortion
    float2 distortion = lens_distortion((uv - mouse_uv) / radius, Strength);
        
        //Zoom in, keeping the distortion effect
    float2 FinalPoint = iMouse + distortion * radius / float2(uScreenResolution.x / uScreenResolution.y, 1);
    
    float4 resultColor = tex2D(uImage0, FinalPoint);
    if (ifNoise)
        resultColor = tex2D(uImage0, FinalPoint + noise.rg * NoiseStrength);
    float4 reverseColor = float4(1 - resultColor.r, 1 - resultColor.g, 1 - resultColor.b, resultColor.a);
    if (reverse)
    {
         return reverseColor * givenColor;

    }
    return resultColor * givenColor;
    //return resultColor * givenColor;

}

float4 PixelShaderFunction3(float2 coords : TEXCOORD0) : COLOR0 //反色
{
    float4 color = tex2D(uImage0, coords);
    float4 reverseColor = float4(1 - color.r, 1 - color.g, 1 - color.b, color.a);
    return reverseColor;
    //return resultColor * givenColor;

}
technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
    pass Apply2
    {
        PixelShader = compile ps_2_0 PixelShaderFunction2();
    }
    pass Apply3
    {
        PixelShader = compile ps_2_0 PixelShaderFunction3();
    }
}