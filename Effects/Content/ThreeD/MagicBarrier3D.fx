sampler uFlowNoise : register(s0);
sampler uCellNoise : register(s1);
sampler uDissolveNoise : register(s2);

float4x4 uWorld;
float4x4 uWorldInverseTranspose;
float4x4 uViewProjection;

float3 uCameraPosition;
float3 uBaseColor;
float3 uRimColor;
float3 uFlowColor;
float3 uHighlightColor;

float2 uFlowScale;
float2 uTransitionScale;
float uTime;
float uCenterDepth;
float uDepthClipSide;
float uOpacity;
float uBodyAlpha;
float uRimPower;
float uRimStrength;
float uLowerRimStrength;
float uFlowStrength;
float uFlowWidth;
float uHighlightStrength;
float uRevealProgress;
float uFadeProgress;
float uTransitionWidth;
float uTransitionStrength;

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
    float3 ObjectPosition : TEXCOORD1;
    float3 WorldPosition : TEXCOORD2;
    float3 WorldNormal : TEXCOORD3;
};

PSInput VertexShaderBarrier(VSInput input)
{
    PSInput output;
    float4 worldPosition = mul(float4(input.Pos, 1.0), uWorld);
    float3 worldNormal = mul(float4(input.Normal, 0.0), uWorldInverseTranspose).xyz;
    worldNormal *= rsqrt(max(dot(worldNormal, worldNormal), 0.000001));

    output.Pos = mul(worldPosition, uViewProjection);
    output.Color = input.Color;
    output.Texcoord = input.Texcoord;
    output.ObjectPosition = input.Pos;
    output.WorldPosition = worldPosition.xyz;
    output.WorldNormal = worldNormal;
    return output;
}

float4 PixelShaderBarrier(PSInput input) : COLOR0
{
    if (uDepthClipSide != 0.0)
    {
        clip((input.WorldPosition.z - uCenterDepth) * uDepthClipSide);
    }

    float3 normal = normalize(input.WorldNormal);
    float3 viewDirection = normalize(uCameraPosition - input.WorldPosition);
    float viewFacing = saturate(abs(dot(normal, viewDirection)));
    float fresnel = pow(1.0 - viewFacing, max(uRimPower, 0.0001));

    float2 flowUV = input.Texcoord * uFlowScale;
    float noiseA = tex2D(uFlowNoise, flowUV + float2(uTime * 0.035, -uTime * 0.018)).r;
    float noiseB = tex2D(uFlowNoise,
        flowUV * float2(0.63, 0.91) + float2(-uTime * 0.021, uTime * 0.014)).r;
    float contourTarget = 0.5 + (noiseB - 0.5) * 0.24;
    float contour = 1.0 - smoothstep(uFlowWidth, uFlowWidth * 2.4,
        abs(noiseA - contourTarget));

    float cellNoise = tex2D(uCellNoise,
        input.Texcoord * float2(2.2, 1.55) + float2(uTime * 0.009, uTime * 0.005)).r;
    float cellRidges = smoothstep(0.76, 0.98, cellNoise + noiseA * 0.08);
    float flowMask = saturate(contour * 0.68 + cellRidges * 0.52);
    flowMask *= lerp(0.18, 1.0, fresnel);

    // Use a low-frequency field for large dissolve chunks; surface flow keeps its detail.
    float2 transitionUV = input.Texcoord * uTransitionScale
        + float2(uTime * 0.006, -uTime * 0.004);
    float transitionNoise = tex2D(uDissolveNoise, transitionUV).r;
    float transitionCells = tex2D(uDissolveNoise,
        transitionUV * float2(1.25, 0.82) + float2(-uTime * 0.003, uTime * 0.002)).r;
    float transitionField = saturate(transitionNoise * 0.74 + transitionCells * 0.26);
    float transitionWidth = max(uTransitionWidth, 0.001);
    float revealThreshold = 1.0 + transitionWidth * 2.0
        - saturate(uRevealProgress) * (1.0 + transitionWidth * 4.0);
    float revealMask = smoothstep(revealThreshold - transitionWidth,
        revealThreshold + transitionWidth, transitionField);
    float fadeThreshold = saturate(uFadeProgress) * (1.0 + transitionWidth * 4.0)
        - transitionWidth * 2.0;
    float fadeMask = smoothstep(fadeThreshold - transitionWidth,
        fadeThreshold + transitionWidth, transitionField);
    float revealEdge = 1.0 - smoothstep(0.0, 1.0,
        abs(transitionField - revealThreshold) / transitionWidth);
    float fadeEdge = 1.0 - smoothstep(0.0, 1.0,
        abs(transitionField - fadeThreshold) / transitionWidth);
    float revealActivity = smoothstep(0.14, 0.5, uRevealProgress)
        * (1.0 - smoothstep(0.62, 1.0, uRevealProgress));
    float fadeActivity = smoothstep(0.0, 0.78, uFadeProgress);
    float transitionGlow = saturate(revealEdge * revealActivity + fadeEdge * fadeActivity)
        * uTransitionStrength;
    float transitionMask = revealMask * fadeMask;
    // Keep the dissolving boundary alive while clipping the fully hidden fragments.
    clip(max(transitionMask, transitionGlow * 0.35) - 0.002);

    float lowerHemisphere = saturate(input.ObjectPosition.y * 0.5 + 0.5);
    float lowerRim = fresnel * lowerHemisphere * lowerHemisphere * lowerHemisphere;

    float glintA = pow(saturate(dot(normal, normalize(float3(-0.45, -0.65, -0.62)))), 34.0);
    float glintB = pow(saturate(dot(normal, normalize(float3(0.58, 0.78, -0.24)))), 46.0);
    float glint = saturate(glintA + glintB * 0.72);

    float pulse = 0.92 + sin(uTime * 1.7 + noiseA * 6.283185) * 0.08;
    float bodyEnergy = uBodyAlpha * (0.7 + noiseA * 0.3);
    float rimEnergy = fresnel * uRimStrength + lowerRim * uLowerRimStrength;
    float flowEnergy = flowMask * uFlowStrength;
    float highlightEnergy = glint * uHighlightStrength;
    float transitionEnergy = transitionGlow * (0.65 + fresnel * 1.35);
    float alpha = saturate((bodyEnergy + rimEnergy + flowEnergy + highlightEnergy)
        * transitionMask + transitionEnergy) * uOpacity * pulse * input.Color.a;

    // Keep each visual layer's color independent: Fresnel must not recolor
    // the body/flow layer through a broad RGB lerp.
    float3 color = uBaseColor * bodyEnergy;
    color += uRimColor * rimEnergy;
    color += uFlowColor * flowEnergy;
    color += uHighlightColor * highlightEnergy;
    color += uHighlightColor * transitionGlow * 1.35;
    color *= input.Color.rgb;
    return float4(color, alpha);
}

technique Technique1
{
    pass Barrier
    {
        VertexShader = compile vs_3_0 VertexShaderBarrier();
        PixelShader = compile ps_3_0 PixelShaderBarrier();
    }
}
