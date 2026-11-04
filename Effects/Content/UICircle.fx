// Screen-footprint coverage for a centered UI stroke. No fixed texture is generated.
float4x4 MatrixTransform;
float2 Center;
float Radius;
float HalfWidth;

struct VSOut {
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 Local : TEXCOORD0;
};
VSOut VS(float4 position : POSITION0, float4 color : COLOR0) {
    VSOut o;
    o.Position = mul(position, MatrixTransform);
    o.Color = color;
    o.Local = position.xy - Center;
    return o;
}
float4 PS(VSOut i) : COLOR0 {
    float radial = length(i.Local);
    float footprint = max(fwidth(radial), .0001);
    float outer = saturate(.5 + (Radius + HalfWidth - radial) / footprint);
    float inner = Radius > HalfWidth
        ? saturate(.5 + (Radius - HalfWidth - radial) / footprint) : 0;
    // Subtract coverages so sub-pixel strokes retain energy rather than double fading.
    return i.Color * (outer - inner);
}
technique UICircle {
    pass Apply {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}
