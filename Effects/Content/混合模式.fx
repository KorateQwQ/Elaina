sampler uImage0 : register(s0);
sampler uImagel : register(s1);

float2 scale;
float2 move;


float4 functionA(float2 coords : TEXCOORD0) : COLOR0 //颜色减淡
{
    float2 center = float2(0.5, 0.5);
    float2 vec = coords - center; //当前坐标与中心的距离
    float2 finalPos = (center + vec * scale) + move;
    finalPos = clamp(finalPos, float2(0, 0), float2(1, 1));
    float4 color = tex2D(uImage0, coords);
    float4 color2 = tex2D(uImagel, finalPos)*0.7f;
    float4 finalColor = color + (color * color2 / (float4(1,1,1,1) - color2));
    float effect = smoothstep(0, 1, color2.r);
    finalColor = float4(finalColor.rgb * effect, finalColor.a);
    return finalColor;


}
technique Technique1
{
    pass apply1
    {
        PixelShader = compile ps_2_0 functionA();
    }
}