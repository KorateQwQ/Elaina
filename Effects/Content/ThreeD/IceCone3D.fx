sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float4x4 uWorld;
float4x4 uViewProjection;
float3 uLightDirection = float3(-1.0, -1.0, -0.75);
float3 uLightColor = float3(1.0, 1.0, 1.0);
float3 uCameraPosition = float3(0.0, 0.0, -1000.0);
float4 uBaseColor = float4(1.0, 1.0, 1.0, 1.0);
float3 uFresnelColor = float3(0.7, 0.9, 1.0);
float4 uDissolveEdgeColor = float4(1.0, 1.0, 1.0, 1.0);
float uAmbientStrength = 0.7;
float uDiffuseStrength = 1.0;
float uFresnelStrength = 0.65;

// 消融参数
float uDissolveNoiseScale = 1.0;
float uDissolveThreshold = 0.5;
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
    float3 WorldNormal : TEXCOORD1;
    float3 WorldPosition : TEXCOORD2;
};

PSInput VertexShaderBase(VSInput input)
{
    PSInput output;
    float4 worldPos4 = mul(float4(input.Pos, 1.0), uWorld);
    float4 worldNormal4 = mul(float4(input.Normal, 0.0), uWorld);
    output.Pos = mul(worldPos4, uViewProjection);
    output.Color = input.Color;
    output.Texcoord = input.Texcoord;
    output.WorldNormal = normalize(worldNormal4.xyz);
    output.WorldPosition = worldPos4.xyz;
    return output;
}

float4 PixelShaderBase(PSInput input) : COLOR0
{
    float3 normal = normalize(input.WorldNormal);
    float3 viewDirection = normalize(uCameraPosition - input.WorldPosition);
    float fresnelDot = saturate(abs(dot(normal, viewDirection)));
    float fresnel = pow(1.0 - fresnelDot, 4.0) * uFresnelStrength;

    float4 baseColor = float4((tex2D(uImage0, input.Texcoord).rgb*uBaseColor.rgb),uBaseColor.a);
    float3 emissiveColor = baseColor.rgb * input.Color.rgb;
    baseColor.rgb = emissiveColor + uFresnelColor * fresnel;
    baseColor.a *= input.Color.a;

    // 消融效果
    float noiseValue = tex2D(uImage1, input.Texcoord * uDissolveNoiseScale).r;
    float dissolve = uDissolveThreshold - noiseValue;

    if (dissolve > uDissolveEdgeWidth)
    {
        clip(-1);
    }

    float edgeAlpha = 1.0 - saturate(dissolve / uDissolveEdgeWidth);
    float edgeMask = 1.0 - edgeAlpha;
    baseColor = lerp(baseColor, uDissolveEdgeColor, edgeMask);
    baseColor.a *= edgeAlpha;

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
