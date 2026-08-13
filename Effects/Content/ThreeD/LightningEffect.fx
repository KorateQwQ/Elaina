sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float4x4 uWorld;
float4x4 uViewProjection;

float4 uBaseColor = float4(1.0, 1.0, 1.0, 1.0);
float4 uDissolveEdgeColor = float4(1.0, 1.0, 1.0, 1.0);
float2 uDissolveNoiseScale = float2(1.0, 1.0);
float uDissolveThreshold = 0.0;
float uDissolveEdgeWidth = 0.1;

struct VSInput
{
    float3 Pos : POSITION0;
    float4 Color : COLOR0;
    float2 Texcoord : TEXCOORD0;
    float3 Normal : NORMAL0;
};

struct PSInput
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR0;
    float2 Texcoord : TEXCOORD0;
};

PSInput VertexShaderBase(VSInput input)
{
    PSInput output;
    float4 worldPosition = mul(float4(input.Pos, 1.0), uWorld);

    output.Pos = mul(worldPosition, uViewProjection);
    output.Color = input.Color;
    output.Texcoord = input.Texcoord;
    return output;
}

float4 PixelShaderBase(PSInput input) : COLOR0
{
    float4 baseColor = tex2D(uImage0, input.Texcoord) * uBaseColor * input.Color;
    float noiseValue = tex2D(uImage1, input.Texcoord * uDissolveNoiseScale).r;
    float dissolveDistance = noiseValue - uDissolveThreshold;

    clip(dissolveDistance);

    float edgeWidth = max(uDissolveEdgeWidth, 0.0001);
    float edgeMask = 1.0 - saturate(dissolveDistance / edgeWidth);
    baseColor.rgb = lerp(baseColor.rgb, uDissolveEdgeColor.rgb, edgeMask * uDissolveEdgeColor.a);

    return baseColor;
}

technique Technique1
{
    pass Base
    {
        VertexShader = compile vs_3_0 VertexShaderBase();
        PixelShader = compile ps_3_0 PixelShaderBase();
    }
}
