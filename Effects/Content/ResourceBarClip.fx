// SpriteBatch's logical positions are retained for a shared per-layer rounded mask.
float4x4 MatrixTransform;
float2 BarOrigin;
float2 BarSize;
sampler TextureSampler : register(s0) = sampler_state {
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};
struct VSOut {
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 UV : TEXCOORD0;
    float2 Local : TEXCOORD1;
};
VSOut VS(float4 position : POSITION0, float4 color : COLOR0, float2 uv : TEXCOORD0) {
    VSOut o;
    o.Position = mul(position, MatrixTransform);
    o.Color = color;
    o.UV = uv;
    o.Local = position.xy - BarOrigin;
    return o;
}
float RoundedDistance(float2 local, float2 size) {
    float radius = min(size.x, size.y) * .5;
    float2 q = abs(local - size * .5) - (size * .5 - radius);
    return length(max(q, 0)) + min(max(q.x, q.y), 0) - radius;
}
float4 PS(VSOut i) : COLOR0 {
    // Intersect with the full slot as well: a sub-pixel current fill must not escape its left cap.
    float distance = max(RoundedDistance(i.Local, BarSize), RoundedDistance(i.Local, float2(262, BarSize.y)));
    float aa = max(fwidth(distance), .001);
    float coverage = saturate(.5 - distance / aa);
    // Small petals/stars can occupy less than one screen pixel. Integrate the pixel footprint
    // instead of sampling a large transparent sprite at a single point (which shimmers).
    float2 dx = ddx(i.UV) * .25;
    float2 dy = ddy(i.UV) * .25;
    float4 texel = (tex2D(TextureSampler, i.UV - dx - dy) + tex2D(TextureSampler, i.UV + dx - dy)
                 + tex2D(TextureSampler, i.UV - dx + dy) + tex2D(TextureSampler, i.UV + dx + dy)) * .25;
    return texel * i.Color * coverage;
}
technique RoundedResource {
    pass Apply {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}
