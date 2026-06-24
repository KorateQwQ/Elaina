sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

int PixelSize;
float2 ImageSize;
bool IfgivenColor;
float4 givenColor;

float width;//边缘粗细
float width2; //光圈粗细

float2 center;

float uTime;
float velocity;
float velocityX;
float velocityY;
float2 rotatedBy(float2 vec, float radians)
{
    float num = (float) cos(radians);
    float num2 = (float) sin(radians);
    float2 v = vec;
    float2 result = float2(0, 0);
    result.x += (v.x * num - v.y * num2);
    result.y += (v.x * num2 + v.y * num);
    return result;
}
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0//以圆心往外消逝
{
    float len = length(coords - center);
    float lenForAlpha = len - uTime * velocity;
    if (len < uTime * velocity)
        return float4(0, 0, 0, 0);
    float4 color = tex2D(uImage0, coords);

    float alpha = 1;
    if (lenForAlpha < width)
        alpha = clamp(smoothstep(0, width, lenForAlpha), 0, 1);


    if (IfgivenColor)
    {
        if (!any(color))
            return float4(0, 0, 0, 0);
        if (lenForAlpha > width2)
            return float4(0, 0, 0, 0);
        return givenColor * alpha;

    }
    return color * alpha;
}
float4 PixelShaderFunction2(float2 coords : TEXCOORD0) : COLOR0 //以圆心往外出现
{
    
    float len = length(coords - center);
    float MaxLen = sqrt(pow(uTime * velocityX, 2) + pow(uTime * velocityY, 2));
    if (len >= MaxLen)
        return float4(0, 0, 0, 0);
    

    float4 color = tex2D(uImage0, coords);
    if (!any(color))
        return float4(0, 0, 0, 0);

    if (len > MaxLen - width)
    {
        return color * smoothstep(0, width, MaxLen-len) * givenColor;

    }
    
    return color * givenColor;
}

float4 MaskFunction(float2 coords : TEXCOORD0) : COLOR0//以圆心往外出现,但是以遮罩取代
{
    
    float len = length(coords - center);
    float MaxLen = sqrt(pow(uTime * velocityX, 2) + pow(uTime * velocityY, 2));
    if (len >= MaxLen)
        return float4(0, 0, 0, 0);
    
    float coordMask = clamp(MaxLen - len, 0, 0.5f);
    float4 color = tex2D(uImage0, coords);
    if(!any(color))
        return float4(0, 0, 0, 0);
    float4 Mask = tex2D(uImage1, float2(coordMask, 0.5f));
    
    return givenColor * Mask.r;
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
    pass ApplyMask
    {
        PixelShader = compile ps_3_0 MaskFunction();
    }
}