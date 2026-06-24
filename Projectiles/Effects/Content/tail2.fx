sampler uImage0 : register(s0); //粉色拖尾
sampler uImage1 : register(s1); //拖尾
sampler uImage2 : register(s2); //渐变

float4x4 uTransform;
float uTime;


struct VSInput
{
    float2 Pos : POSITION0;
    float4 Color : COLOR0;
    float3 Texcoord : TEXCOORD0;
};

struct PSInput
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR0;
    float3 Texcoord : TEXCOORD0;
};

float3 hsv2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs((c.xxx + K.xyz - floor(c.xxx + K.xyz)) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float4 PixelShaderFunction(PSInput input) : COLOR0
{

    float3 coord = input.Texcoord;
    float4 shape = tex2D(uImage1, float2(coord.x, coord.y));
    float pos = length(coord.y - 0.5); //pos为0.5-0，Y为中心时为0
    float res = smoothstep(0.5, 0, pos); //pos为0时颜色拉满，pos为0.5时透明
    float2 choosecolor = float2(shape.r, 0);
    float4 color = tex2D(uImage0, float2(1-res, 0));
    float4 result = color * res * coord.z;
    if (result.r < 0.1)
        return float4(0, 0, 0, 0);
    return result;

}

PSInput VertexShaderFunction(VSInput input)
{
    PSInput output;
    output.Color = input.Color;
    output.Texcoord = input.Texcoord;
    output.Pos = mul(float4(input.Pos, 0, 1), uTransform);
    return output;
}


technique Technique1
{
    pass ColorBar
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}