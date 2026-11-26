    // Rounded glass channel. Keep the banks narrow at the tips of the arc.
    float span = saturate(angle / max(extent, .0001));
    float belly = 4.0 * span * (1.0 - span);
    float radius = width * (.67 + .33 * belly);
    float trackEdge = ArcDistance(d, angle, extent, period);
    float edge = full ? trackEdge : ArcDistance(d, angle, fill, period);
    float bank = saturate(.5 + (radius - trackEdge) / (2.0 * aa));
    float rim = saturate(.5 + (radius + .84 - trackEdge) / (2.0 * aa));
    float band = fill > 0 ? saturate(.5 + (radius - edge) / (2.0 * aa)) : 0;
    float crossSection = saturate(.5 + .5 * d / radius);
    float4 result = float4(.07, .20, .29, 1) * (.61 * bank);
    float3 rimColor = lerp(float3(.22,.43,.54), float3(.75,.94,.97), crossSection);
    result += float4(rimColor, 1) * (.82 * (rim - bank));
    if (full) {
        float skirt = saturate(1.0 - (trackEdge - radius - .84) / 5.5);
        result += float4(.26,.84,.94,1) * (.46 * skirt * skirt * (1.0 - result.a));
    }

    // A shaded inner bank and a translucent turquoise shoulder give the current depth.
    float3 liquid = lerp(float3(.18,.43,.68), float3(.40,.86,.89), crossSection);
    float swell = RingState.z * saturate(1.0 - abs(angle - RingState.w * extent) * 8.0);
    float ripple = sin(tau * (3.0 * angle - FlowRates.y * phase));
    float2 flowUV = float2(2.0 * angle - FlowRates.z * phase + .08 * ripple,
        saturate(.12 + .76 * crossSection + .065 * ripple));
    float4 sample = SampleWater(flowUV);
    float foam = sample.r * sample.a;
    float softness = max(.13, fwidth(foam) * .55);
    float caustic = smoothstep(.36 - softness, .56 + softness, foam);
    float waterLight = saturate(.78 * caustic + .32 * swell);
    // A small meniscus travels with the fill boundary, even when the foam is dark.
    if (!full) {
        float lip = saturate(1.0 - abs(angle - fill) * 100.0);
        waterLight = max(waterLight, .65 * lip);
    }
    liquid = lerp(liquid, float3(.89,.99,.97), waterLight);
    return extent > 0 ? Over(liquid, band * .95, result) * tint.a : float4(0,0,0,0);
