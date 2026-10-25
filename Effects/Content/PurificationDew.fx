// Narrow tapered emissive trail. s0 is KL/Effects/Tex/Trail/LightTrail2.
// HDR RGB is composited by the same RequestBloom pipeline as MagicMissile.
sampler Trail : register(s0);
float Time;
float Opacity;
// Uploaded every draw from PurificationDewVisualSettings.Read(), editable through C# Hot Reload.
float4 Motion;       // sway speed, amplitude, spatial frequency, unused
float4 Hue;          // flow speed, offset, saturation, longitudinal span
float4 Shape;        // tip width, body width, taper exponent, crosswise hue span
float4 Intensity;    // ribbon, core, head, ring
float4 HeadShape;    // head position, width, length, core sharpness
float4 RingShape;    // width, height, sharpness, hue span
float RingHueOffset;

float3 Rainbow(float hue)
{
    float3 rgb = saturate(abs(frac(hue + float3(0, 2.0/3.0, 1.0/3.0)) * 6 - 3) - 1);
    return lerp(float3(1, 1, 1), rgb, saturate(Hue.z));
}

float4 Dew(float2 uv : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    float x = (uv.x - .5) * 2;
    float progress = saturate(uv.y / HeadShape.x);
    // Near-zero tip grows into the dew head: the silhouette is never parallel-sided.
    float width = max(.001, Shape.x + Shape.y * pow(progress, max(.01, Shape.z)));
    float sway = sin(progress * Motion.z - Time * Motion.x) * Motion.y * sin(progress * 3.14159);
    float cross = (x - sway) / width;
    float inside = 1 - smoothstep(.82, 1, abs(cross));
    float mask = tex2D(Trail, float2(1 - progress, saturate(.5 + cross * .5))).a;
    float falloff = smoothstep(0, .24, progress) * (1 - smoothstep(HeadShape.x - .01, HeadShape.x + .04, uv.y));
    float ribbon = mask * inside * falloff;
    float core = exp(-cross * cross * max(.01, HeadShape.w)) * pow(progress, 1.5) * falloff;
    float2 tip = float2(x / max(.001, HeadShape.y), (uv.y - HeadShape.x) / max(.001, HeadShape.z));
    float droplet = exp(-dot(tip, tip) * 2);
    float ring = exp(-abs(length(float2(x / max(.001, RingShape.x), (uv.y - HeadShape.x) / max(.001, RingShape.y))) - 1) * max(.01, RingShape.z));
    float phase = Hue.y - Time * Hue.x;
    float3 tint = Rainbow(progress * Hue.w + phase + cross * Shape.w);
    float3 rgb = tint * (ribbon * Intensity.x + core * Intensity.y)
        + lerp(tint, float3(1, .98, 1), .84) * droplet * Intensity.z
        + Rainbow(RingHueOffset + uv.x * RingShape.w + phase) * ring * Intensity.w;
    // Emissive premultiplied RGB, no scene occlusion. Peaks above 1 feed KL's HDR extraction.
    return float4(rgb * Opacity * color.rgb * color.a, 0);
}

technique DewLight
{
    pass RainbowFall { PixelShader = compile ps_3_0 Dew(); }
}
