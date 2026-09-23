// Fills an empty potion bottle with animated liquid.
// s0: empty bottle texture (the texture passed to SpriteBatch.Draw)
// s1: liquid mask of the same size; R = liquid visibility, G = flask interior
// s2: tileable grayscale noise (KL PerlinX), sampled with wrap addressing
sampler2D BottleTexture : register(s0);
sampler2D MaskTexture : register(s1);
sampler2D NoiseTexture : register(s2);

float4 LiquidColor = float4(0.36, 0.30, 0.82, 1.0); // rgb = liquid color, a = liquid opacity
float FillLevel = 0.52;       // 0 = empty, 1 = full
float Time = 0.0;             // seconds
float FlowSpeed = 1.0;
float WaveStrength = 1.0;     // surface wave amplitude in texels
float2 TextureSize = float2(50, 50);
float2 LiquidRange = float2(0.26, 0.94); // v of the surface when full, v of the flask bottom
float2 GlowCenter = float2(0.54, 0.76);  // uv of the inner glow

float4 PixelShaderFunction(float4 vertexColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 bottle = tex2D(BottleTexture, uv);
    float2 texelSize = 1.0 / TextureSize;
    float2 texel = (floor(uv * TextureSize) + 0.5) * texelSize;
    float4 mask = tex2D(MaskTexture, texel);

    float t = Time * FlowSpeed;
    float2 px = texel * TextureSize;
    float bottom = LiquidRange.y * TextureSize.y;
    float surface = lerp(bottom, LiquidRange.x * TextureSize.y, saturate(FillLevel));
    surface += (sin(px.x * 0.45 + t * 2.4) * 0.8 + sin(px.x * 0.21 - t * 1.5) * 0.6) * WaveStrength;
    float below = px.y - surface;

    if (mask.g < 0.004 || below < 0.0 || FillLevel <= 0.0)
        return bottle * vertexColor;

    float depth = saturate(below / max(bottom - surface, 1.0));
    float3 light = lerp(LiquidColor.rgb, 1.0, 0.45);
    float3 deep = LiquidColor.rgb * 0.42;
    float3 col = lerp(light, LiquidColor.rgb, smoothstep(0.0, 0.45, depth));
    col = lerp(col, deep, smoothstep(0.45, 1.0, depth));

    // tex2Dlod: sampled after the early return, where gradient instructions are not allowed.
    float2 flowUV = texel * 0.35;
    float n = tex2Dlod(NoiseTexture, float4(flowUV + float2(t * 0.024, -t * 0.013), 0, 0)).r * 0.65
            + tex2Dlod(NoiseTexture, float4(flowUV * 2.1 + float2(-t * 0.035, t * 0.02), 0, 0)).r * 0.35;
    col *= 0.93 + 0.14 * n;
    float streakBase = saturate(1.0 - abs(n * 2.0 - 1.0) * 2.2);
    float streak = streakBase * streakBase * streakBase;
    col = lerp(col, light, streak * 0.25);

    float glow = saturate(1.0 - length(px - GlowCenter * TextureSize) / 10.0);
    col = lerp(col, light, glow * glow * 0.45);

    // Shape the liquid against the round flask instead of leaving a flat color fill.
    // The bottle arc is centered near (25, 33.7) with a radius of about 14 pixels.
    // A curved dark band at that boundary gives the glass and liquid a weighted bottom.
    float arcDx = px.x - 25.0;
    // A parabola is a close, instruction-light approximation of the flask's round base.
    float curvedBottom = 47.5 - arcDx * arcDx * 0.035;
    float bottomShade = saturate((px.y - curvedBottom + 5.5) * 0.18);
    col = lerp(col, deep * 0.78, bottomShade * 0.78);

    float bubbles = 0.0;
    [unroll] for (int i = 0; i < 4; i++)
    {
        float seed = i * 1.618 + 0.37;
        float life = frac(t * (0.18 + frac(seed * 7.13) * 0.14) + frac(seed * 3.71));
        float2 pos = float2(17.0 + frac(seed * 5.19) * 16.0 + sin(t * 1.7 + seed * 6.0) * 0.8,
                            lerp(bottom - 1.5, surface + 1.0, life));
        float radius = 0.6 + frac(seed * 9.3) * 0.5;
        bubbles = max(bubbles, step(length(px - pos), radius) * (1.0 - life * 0.5));
    }
    col = lerp(col, lerp(light, 1.0, 0.5), bubbles * 0.75);

    col = lerp(col, lerp(light, 1.0, 0.35), saturate(1.0 - below) * 0.8);

    float4 liquid = float4(col * LiquidColor.a, LiquidColor.a) + bottle * (1.0 - LiquidColor.a);
    return lerp(bottle, liquid, mask.r) * vertexColor;
}

technique Technique1
{
    pass PotionLiquid
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
