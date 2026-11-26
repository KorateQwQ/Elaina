// SpriteBatch binds the beam's Eff_Splash_322_288 material to s0.
sampler Water : register(s0);
// Written before every draw: fill, phase, crest intensity and position in the track.
float4 RingState = float4(.4, 0, 0, 0);
// Current half-width in local arc pixels, supplied for every draw.
float RingWidth = 2.45;
// Supplied each draw: reference sweep and start in turns, curvature, quad size in local pixels.
float4 ArcGeometry = float4(1, 0, 1, 64);
// Beam palette and travel rates (crest, ripple, surface foam, inner foam).
float3 WaterColor = float3(.501961, .749020, 1);
float3 FoamColor = float3(.960784, .992157, 1);
float3 TrackColor = float3(.137255, .227451, .309804);
float3 BorderColor = float3(.741176, .854902, .917647);
float4 FlowRates = float4(2, 1, 1.4, 1.4);

float4 SampleWater(float2 uv)
{
    // Average the footprint across the narrow ribbon so fine foam does not sparkle.
    float2 dx = ddx(uv), dy = ddy(uv);
    // At twelve o'clock the integer U wrap is a seam in coordinates only.
    dx.x = frac(dx.x + .5) - .5;
    dy.x = frac(dy.x + .5) - .5;
    dx *= .30;
    dy *= .30;
    return .25 * (tex2D(Water, uv - dx - dy) + tex2D(Water, uv + dx - dy)
        + tex2D(Water, uv - dx + dy) + tex2D(Water, uv + dx + dy));
}

float4 Over(float3 tint, float alpha, float4 behind)
{
    return float4(tint * alpha, alpha) + behind * (1 - alpha);
}

// Distance to the arc centreline, including its rounded end caps.
float ArcDistance(float radial, float angle, float sweep, float period)
{
    float startGap = abs(angle), endGap = abs(angle - sweep);
    float endpoint = min(min(startGap, endGap), period - max(startGap, endGap));
    float beyond = angle < 0 || angle > sweep ? max(endpoint, 0) : 0;
    return length(float2(radial, beyond * 125.663706)); // radius 20, full circumference
}

float4 RingPS(float2 uv : TEXCOORD0, float4 tint : COLOR0, uniform bool full) : COLOR0
{
    const float tau = 6.2831853;
    float2 p = (uv - .5) * ArcGeometry.w;
    float extent = saturate(ArcGeometry.x), bend = saturate(ArcGeometry.z);
    float middle = tau * (ArcGeometry.y + .5 * extent);
    float2 normal = float2(sin(middle), -cos(middle));
    float2 local = float2(dot(p, float2(-normal.y, normal.x)), dot(p, normal) - 20);
    // Work around the fixed arc midpoint. The rationalized radial distance stays
    // accurate as radius 20/bend approaches infinity, including the straight limit.
    float2 polar = float2(bend * local.x, 20 + bend * local.y);
    float d = (40 * local.y + bend * dot(local, local)) / (length(polar) + 20);
    float period = 1 / max(.00001, bend);
    float along = bend > .00001 ? atan2(polar.x, polar.y + .00001) * period : local.x / 20;
    float angle = extent * .5 + along / tau;
    float aa = max(.20, fwidth(d) * .5);
    float phase = RingState.y;
    float fill = full ? extent : RingState.x * extent;

    float width = RingWidth;
    float trackEdge = ArcDistance(d, angle, extent, period);
    float interior = 1 - smoothstep(width + .20 - aa, width + .20 + aa, trackEdge);
    float outline = 1 - smoothstep(width + 1.05 - aa, width + 1.05 + aa, trackEdge);
    float4 result = float4(TrackColor, 1) * (.75 * interior)
                  + float4(BorderColor, 1) * (.75 * (outline - interior));
    if (full) {
        const float skirtWidth = 6.0;
        const float3 glowColor = float3(.34, .86, 1.0);
        float falloff = saturate(1.0 - (trackEdge - width - 1.05) / skirtWidth);
        float halo = .59 * falloff * falloff * (1.0 - result.a);
        result += float4(glowColor, 1.0) * halo;
    }

    // Advection is restricted to material coordinates. Neither bank moves.
    float crest = .5 + .5 * sin(tau * (2 * angle - FlowRates.x * phase));
    float ripple = sin(tau * (3 * angle - FlowRates.y * phase + .12));
    float swell = RingState.z * saturate(1 - abs(angle - RingState.w * extent) * 8);
    float v = d / width;
    // Round the ends of the filled arc; keep the twelve o'clock seam closed at full.
    float edge = full ? trackEdge : ArcDistance(d, angle, fill, period);
    float band = fill > 0 ? 1 - smoothstep(width - aa, width + aa, edge) : 0;

    // Shear across the stream as it bends. Each bank advects foam independently.
    float shear = .14 * ripple + .09 * sin(tau * (angle + phase)) * v;
    float crossUV = saturate(.5 + .48 * v + (.12 * crest - .06) * saturate(1 - v*v));
    float4 body = SampleWater(float2(2 * angle - FlowRates.z * phase + shear, crossUV));
    float4 bank = SampleWater(float2(3 * angle - FlowRates.w * phase - shear, saturate(.54 - .45 * v)));
    result = Over(WaterColor, band * .80, result);
    float bubbles = max(body.r * body.a, bank.r * bank.a * (.68 + .22 * ripple));
    float softness = max(.12, fwidth(bubbles) * .55);
    float foam = smoothstep(.42 - softness, .42 + softness, bubbles);
    float inside = 1 - smoothstep(.68, 1, abs(v));
    float glint = saturate(foam + swell) * inside;
    return extent > 0 ? Over(FoamColor, band * glint, result) * tint.a : float4(0, 0, 0, 0);
}

technique WaterEnergyRing { pass Ring { PixelShader = compile ps_3_0 RingPS(false); } }
technique FullEnergyRing { pass Ring { PixelShader = compile ps_3_0 RingPS(true); } }
