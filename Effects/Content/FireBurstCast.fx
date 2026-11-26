// Existing assets are premultiplied. Filter first, then recover straight RGB
// for NonPremultiplied; opacity alone controls fading and scene coverage.
sampler uImage0 : register(s0);
float Intensity;

float4 CastPixel(float2 uv : TEXCOORD0, float4 tint : COLOR0) : COLOR0
{
    float4 tex = tex2D(uImage0, uv);
    float3 straight = tex.rgb / max(tex.a, 0.00001);
    // Sparkle assets contain opaque grey/black padding. Move that brightness
    // into coverage so the padding cannot darken a daytime background.
    float mask = max(straight.r, max(straight.g, straight.b));
    float3 hue = straight / max(mask, 0.00001);
    return float4(hue * tint.rgb * Intensity, tex.a * saturate(mask) * tint.a);
}

technique Cast
{
    pass Apply
    {
        PixelShader = compile ps_3_0 CastPixel();
    }
}
