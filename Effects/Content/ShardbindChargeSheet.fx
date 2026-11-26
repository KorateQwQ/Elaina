// SpriteBatch supplies UVs from the packaged WhitePixel. DrawRays sets all
// uniforms before Apply; SheetColor.a includes the current ray's fade-in.
float4 SheetColor;
float EdgeSoftness;
float RootWidth; // Current root half-width divided by the animated outer half-width.
float TailStart;
float RootFade;

// Keep straight iso-alpha edges except where the side and tail meet.
float SmoothIntersection(float a, float b)
{
    const float softness = 0.18;
    float h = max(softness - abs(a - b), 0.0) / softness;
    return max(min(a, b) - h * h * h * softness / 6.0, 0.0);
}

float4 SheetPixel(float2 uv : TEXCOORD0) : COLOR0
{
    float halfWidth = lerp(RootWidth, 1.0, uv.x);
    float across = abs(uv.y * 2.0 - 1.0);
    float distance = halfWidth - across;
    float aa = max(fwidth(distance), 0.0001);
    float coverage = smoothstep(0.0, max(halfWidth * EdgeSoftness, aa), distance);
    float crossPosition = across / halfWidth;
    float profile = pow(saturate(1.0 - crossPosition * crossPosition), 2.0);
    float tail = 1.0 - smoothstep(TailStart, 1.0, uv.x);
    float root = smoothstep(0.0, RootFade, uv.x);
    float alpha = SheetColor.a * coverage * SmoothIntersection(profile, tail) * root;

    return float4(SheetColor.rgb, alpha);
}

technique ChargeSheet
{
    pass P0 { PixelShader = compile ps_3_0 SheetPixel(); }
}
