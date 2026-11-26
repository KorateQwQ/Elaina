float4x4 MatrixTransform;
float2 Center;
float2 HalfSize;
float Corner;
float HalfWidth;
float Mode;
float2 Rotation;
float2 ArcStart;
float2 ArcEnd;
float Sweep;

// Exact coverage of a half-plane through a square pixel. Along a diagonal the
// projected pixel footprint is trapezoidal, not the uniform ramp of fwidth.
float PixelBelow(float d, float2 gradient) {
    float2 g = abs(gradient);
    float h = (g.x + g.y) * .5;
    if (d <= -h) return 0;
    if (d >= h) return 1;
    if (min(g.x, g.y) < .00001) return saturate(.5 + d / max(g.x + g.y, .00001));
    float4 q = max(float4(d + h, d + (g.x - g.y) * .5, d + (g.y - g.x) * .5, d - h), 0);
    return saturate(dot(q * q, float4(1, -1, -1, 1)) / (2 * g.x * g.y));
}

struct VSOut { float4 Position : POSITION0; float4 Color : COLOR0; float2 Local : TEXCOORD0; };
VSOut VS(float4 position : POSITION0, float4 color : COLOR0) {
    VSOut o;
    o.Position = mul(position, MatrixTransform);
    o.Color = color;
    float2 p = position.xy - Center;
    o.Local = float2(dot(p, Rotation), dot(p, float2(-Rotation.y, Rotation.x)));
    return o;
}
float4 PS(VSOut i) : COLOR0 {
    float2 p = i.Local;
    float distance;
    if (Mode < .5) {
        float2 q = abs(p) - HalfSize + Corner;
        distance = length(max(q, 0)) + min(max(q.x, q.y), 0) - Corner;
    } else if (Mode < 1.5) {
        float k0 = length(p / HalfSize);
        float k1 = length(p / (HalfSize * HalfSize));
        distance = k0 < .00001 ? -min(HalfSize.x, HalfSize.y) : k0 * (k0 - 1) / max(k1, .00001);
    } else {
        distance = (dot(abs(p) / HalfSize, float2(1, 1)) - 1) * min(HalfSize.x, HalfSize.y);
    }
    float footprint = max(fwidth(distance), .0001);
    float coverage = HalfWidth < 0 ? saturate(.5 - distance / footprint)
        : saturate(.5 + (HalfWidth - distance) / footprint) - saturate(.5 + (-HalfWidth - distance) / footprint);
    if (Mode < .5 && Corner == 0 && HalfWidth < 0) {
        // Integrate both sides of a thin strip, including widths below one device pixel.
        float2 gx = float2(ddx(p.x), ddy(p.x)), gy = float2(ddx(p.y), ddy(p.y));
        float x = PixelBelow(HalfSize.x - p.x, gx) - PixelBelow(-HalfSize.x - p.x, gx);
        float y = PixelBelow(HalfSize.y - p.y, gy) - PixelBelow(-HalfSize.y - p.y, gy);
        coverage = x * y;
    }
    if (Sweep < 6.28318) {
        // Signed distances to endpoint rays. The minor sector is their intersection,
        // the major sector their union. Derivatives retain coverage at both caps.
        float start = ArcStart.x * p.y - ArcStart.y * p.x;
        float end = p.x * ArcEnd.y - p.y * ArcEnd.x;
        float a = saturate(.5 + start / max(fwidth(start), .0001));
        float b = saturate(.5 + end / max(fwidth(end), .0001));
        coverage *= Sweep <= 3.141593 ? saturate(a + b - 1) : saturate(a + b);
    }
    return i.Color * coverage;
}
technique UIShape {
    pass Apply { VertexShader = compile vs_3_0 VS(); PixelShader = compile ps_3_0 PS(); }
}
