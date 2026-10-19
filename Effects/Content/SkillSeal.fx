sampler uImage0 : register(s0);
float time;
float flashTime;
float ready;
float seal;
static const float PI = 3.14159265;

float Luminance(float3 c) { return dot(c, float3(0.3, 0.59, 0.11)); }

float4 PSFunction(float4 tint : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 v = uv - .5;
    float angle = atan2(v.y, v.x) + PI / 2;
    bool hidden = angle > 0 ? angle > lerp(0, PI, time / .5)
        : angle > lerp(-PI, 0, (time - .5) / .5);
    float4 source = tex2D(uImage0, uv);
    float4 result;
    // Same cooldown-sector behavior as KL's CDEffect; seal tone is independent.
    if (time < 1) {
        if (ready > .5) result = (hidden ? float4(source.rgb * .4, source.a) : source) * tint;
        else if (hidden) result = 0;
        else result = Luminance(source.rgb) * .5;
    }
    else result = source * tint * lerp(2, 1, abs(flashTime - .5) / .5);
    float l = Luminance(result.rgb);
    float3 tone = lerp(float3(l, l, l), l * float3(.82, .74, .98), .55) * .62;
    return float4(lerp(result.rgb, tone, saturate(seal)), result.a);
}

technique Technique1 {
    pass Apply { PixelShader = compile ps_3_0 PSFunction(); }
}
