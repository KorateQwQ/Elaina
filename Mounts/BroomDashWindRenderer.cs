using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.Mounts;

/// <summary>Projects a rotating, open-ended air arc into the plane perpendicular to dash motion.</summary>
public sealed class BroomDashWindRenderer : IDisposable
{
    private const int Segments = 48;
    private const int RingLifetime = 28;
    private const float DefaultOpeningAngle = .7f;
    // The former +.12 UV offset on two texture repeats moved features toward decreasing phase.
    private const float DefaultRotationSpeed = -.12f * (MathHelper.TwoPi - DefaultOpeningAngle) / 2f;
    /// <summary>拖尾保留的历史 tick 数（不含当前点）。</summary>
    public const int TrailSamples = 10;
    private const float TrailFade = 14f;

    /// <summary>双环与拖尾都播完所需的 tick 数。</summary>
    public static int Lifetime(int dashTicks) => Math.Max(RingLifetime, dashTicks + (int)TrailFade);
    // Catmull-Rom 每段细分数；细分后的四边形数沿用双环的索引表，不能超过 Segments。
    private const int TrailSubdivisions = 4;
    private const int TrailPoints = TrailSamples * TrailSubdivisions + 1;

    private readonly BasicEffect effect;
    private readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[(Segments + 1) * 2];
    private readonly VertexPositionColorTexture[] trailVertices = new VertexPositionColorTexture[TrailPoints * 2];
    private readonly Vector2[] trailPath = new Vector2[TrailPoints];
    private readonly short[] indices = new short[Segments * 6];

    public BroomDashWindRenderer(GraphicsDevice device)
    {
        effect = new BasicEffect(device)
        {
            TextureEnabled = true,
            VertexColorEnabled = true,
            LightingEnabled = false,
            World = Matrix.Identity,
            View = Matrix.Identity
        };
        for (int i = 0; i < Segments; i++)
        {
            int vertex = i * 2;
            int index = i * 6;
            indices[index] = (short)vertex;
            indices[index + 1] = (short)(vertex + 1);
            indices[index + 2] = (short)(vertex + 2);
            indices[index + 3] = (short)(vertex + 1);
            indices[index + 4] = (short)(vertex + 3);
            indices[index + 5] = (short)(vertex + 2);
        }
    }

    public void Draw(GraphicsDevice device, Texture2D texture, Vector2 origin, Vector2 direction,
        float age, float dashSpeed, Matrix worldToScreen,
        float startAngle = 0f, float rotationSpeed = DefaultRotationSpeed,
        float radiusStart = 63f, float radiusEnd = 116f,
        float depthStart = 26f, float depthEnd = 58f,
        float thicknessStart = 32f, float thicknessEnd = 55f,
        float openingAngle = DefaultOpeningAngle, float tipFraction = .16f)
    {
        if (texture == null || direction.LengthSquared() < .01f) return;
        openingAngle = MathHelper.Clamp(openingAngle, 0f, MathHelper.TwoPi - .01f);
        tipFraction = MathHelper.Clamp(tipFraction, .001f, .5f);
        direction.Normalize();
        Vector2 transverse = new(-direction.Y, direction.X);
        effect.Texture = texture;
        effect.Projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width,
            device.Viewport.Height, 0, 0, 1);

        BlendState oldBlend = device.BlendState;
        DepthStencilState oldDepth = device.DepthStencilState;
        RasterizerState oldRasterizer = device.RasterizerState;
        SamplerState oldSampler = device.SamplerStates[0];
        Texture oldTexture = device.Textures[0];
        try
        {
            device.BlendState = BlendState.NonPremultiplied;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            device.SamplerStates[0] = SamplerState.LinearWrap;
            if (age >= 0 && age < RingLifetime)
            {
                float rotationAngle = age * rotationSpeed;
                float progress = age / RingLifetime;
                float entrance = MathHelper.Clamp(age / 2f, 0f, 1f);
                float fade = MathF.Pow(1f - progress, .9f) * entrance;
                float radius = MathHelper.Lerp(radiusStart, radiusEnd, MathF.Sqrt(progress));
                float depth = MathHelper.Lerp(depthStart, depthEnd, progress);
                float thickness = MathHelper.Lerp(thicknessStart, thicknessEnd, progress);
                Vector2 center = origin - direction * (22f + age * MathHelper.Min(dashSpeed * .22f, 4.8f));
                // Start with the gap at the bottom, then carry it around the fixed ellipse.
                float bottomAngle = MathF.Atan2(transverse.Y * radius, direction.Y * depth);
                for (int i = 0; i <= Segments; i++)
                {
                    float phase = i / (float)Segments;
                    float angle = bottomAngle + openingAngle * .5f + phase * (MathHelper.TwoPi - openingAngle)
                        + startAngle + rotationAngle;
                    float sine = MathF.Sin(angle);
                    float cosine = MathF.Cos(angle);
                    float tip = MathHelper.SmoothStep(0f, 1f,
                        MathHelper.Clamp(MathF.Min(phase, 1f - phase) / tipFraction, 0f, 1f));
                    Vector2 radial = transverse * sine + direction * cosine;
                    Vector2 point = center + transverse * (sine * radius) + direction * (cosine * depth);
                    float front = .55f + .45f * MathHelper.Clamp(cosine, 0f, 1f);
                    float brightness = fade * front * tip;
                    Color tint = Color.Lerp(new Color(208, 219, 225), Color.White, MathHelper.Clamp(cosine, 0f, 1f) * .65f);
                    // NonPremultiplied：PNG 与顶点 RGB 均不预乘，只以 Alpha 控制消隐。
                    tint.A = (byte)(255f * MathHelper.Clamp(brightness, 0f, 1f));
                    float u = phase * 2f;
                    Vector2 outer = Vector2.Transform(point + radial * (thickness * tip * .5f), worldToScreen);
                    Vector2 inner = Vector2.Transform(point - radial * (thickness * tip * .5f), worldToScreen);
                    vertices[i * 2] = new VertexPositionColorTexture(new Vector3(outer, 0f), tint, new Vector2(u, 0f));
                    vertices[i * 2 + 1] = new VertexPositionColorTexture(new Vector3(inner, 0f), tint, new Vector2(u, 1f));
                }

                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.Textures[0] = texture;
                    device.SamplerStates[0] = SamplerState.LinearWrap;
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length,
                        indices, 0, indices.Length / 3);
                }
            }
        }
        finally
        {
            device.BlendState = oldBlend;
            device.DepthStencilState = oldDepth;
            device.RasterizerState = oldRasterizer;
            device.SamplerStates[0] = oldSampler;
            device.Textures[0] = oldTexture;
        }
    }

    // 与 CoVFX 的 SakuraDashRingRenderer 共用几何思路与原始条纹纹理；双环参数针对扫帚重新调整。
    public void DrawWake(GraphicsDevice device, Texture2D texture, Vector2 origin, Vector2 direction,
        float age, float dashSpeed, Matrix worldToScreen)
    {
        Draw(device, texture, origin - direction * 66f, direction, age, dashSpeed, worldToScreen,
            startAngle: .35f, rotationSpeed: -.135f, radiusStart: 62f, radiusEnd: 96f,
            depthStart: 14f, depthEnd: 23f, thicknessStart: 20f, thicknessEnd: 25f,
            openingAngle: .8f, tipFraction: .16f);
        Draw(device, texture, origin, direction, age, dashSpeed, worldToScreen,
            startAngle: 0f, rotationSpeed: -.135f, radiusStart: 40f, radiusEnd: 67f,
            depthStart: 10f, depthEnd: 20f, thicknessStart: 14f, thicknessEnd: 20f,
            openingAngle: 1.7f, tipFraction: .16f);
    }

    private readonly record struct TrailStyle(float Alpha, float HeadWidth, float TailWidth, float Offset, float OffsetTail,
        float VMin, float VMax, float UScale, float ScrollSpeed, Color TailColor,
        float Wave = 0f, float WavePhase = 0f, float HeadFade = 14f);

    private static readonly Color Haze = new(176, 204, 228), Fiber = new(205, 225, 240);
    // 与人物同宽的气流带，加两条从身体上下缘向后收拢、反相摆动的风线。
    private static readonly TrailStyle[] TrailStyles =
    [
        new(.55f, 44f, 62f, 0f, 0f, .18f, .82f, 190f, 1f, Haze, HeadFade: 16f),
        new(.85f, 4f, 3f, 20f, 9f, .2f, .32f, 120f, 1.6f, Fiber, 3f, 0f),
        new(.8f, 4f, 3f, -20f, -9f, .62f, .74f, 140f, 1.3f, Fiber, 3f, MathF.PI),
    ];

    /// <summary>以骑乘中心为路径的整体气流拖尾。</summary>
    /// <param name="points">逐 tick 采样，最新点在 [0]，最多 <see cref="TrailSamples"/>+1 个。
    /// 冲刺结束后调用方不再追加新点，拖尾留在冲刺直线上向终点收拢。</param>
    public void DrawTrail(GraphicsDevice device, Texture2D texture, ReadOnlySpan<Vector2> points,
        float age, int dashTicks, Matrix worldToScreen)
    {
        if (texture == null || points.Length < 2) return;
        float alpha = MathHelper.Clamp(age / 2f, 0f, 1f) * (age < dashTicks ? 1f
            : MathHelper.Clamp(1f - (age - dashTicks) / TrailFade, 0f, 1f));
        if (alpha <= .001f) return;
        // 收拢：从最旧的一端逐步裁掉采样点。
        if (age >= dashTicks)
        {
            float shrink = MathHelper.Clamp((age - dashTicks) / TrailFade, 0f, 1f);
            points = points[..Math.Max(2, (int)MathF.Round(points.Length * (1f - shrink * .8f)))];
        }
        // Catmull-Rom 插值，转折处不出折角。
        points = points[..Math.Min(points.Length, TrailSamples + 1)];
        int count = 0;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 p0 = points[Math.Max(i - 1, 0)], p1 = points[i], p2 = points[i + 1],
                p3 = points[Math.Min(i + 2, points.Length - 1)];
            for (int s = 0; s < TrailSubdivisions; s++)
                trailPath[count++] = Vector2.CatmullRom(p0, p1, p2, p3, s / (float)TrailSubdivisions);
        }
        trailPath[count++] = points[^1];
        ReadOnlySpan<Vector2> path = trailPath.AsSpan(0, count);

        effect.Texture = texture;
        effect.Projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width,
            device.Viewport.Height, 0, 0, 1);
        BlendState oldBlend = device.BlendState;
        DepthStencilState oldDepth = device.DepthStencilState;
        RasterizerState oldRasterizer = device.RasterizerState;
        SamplerState oldSampler = device.SamplerStates[0];
        Texture oldTexture = device.Textures[0];
        try
        {
            device.BlendState = BlendState.NonPremultiplied;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            foreach (TrailStyle style in TrailStyles)
                Ribbon(device, texture, path, worldToScreen, alpha, age, style);
        }
        finally
        {
            device.BlendState = oldBlend;
            device.DepthStencilState = oldDepth;
            device.RasterizerState = oldRasterizer;
            device.SamplerStates[0] = oldSampler;
            device.Textures[0] = oldTexture;
        }
    }

    private void Ribbon(GraphicsDevice device, Texture2D texture, ReadOnlySpan<Vector2> points, Matrix worldToScreen,
        float alpha, float age, in TrailStyle style)
    {
        float total = 0f;
        for (int i = 1; i < points.Length; i++) total += Vector2.Distance(points[i - 1], points[i]);
        if (total < 1f) return;
        float along = 0f;
        for (int i = 0; i < points.Length; i++)
        {
            if (i > 0) along += Vector2.Distance(points[i - 1], points[i]);
            Vector2 tangent = points[Math.Min(i + 1, points.Length - 1)] - points[Math.Max(i - 1, 0)];
            tangent = tangent.LengthSquared() < .001f ? Vector2.UnitX : Vector2.Normalize(tangent);
            Vector2 normal = new(-tangent.Y, tangent.X);
            float t = along / total;
            // 头部按像素距离淡入，从身体后方长出，不在人物身上堆成一团。
            float head = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(along / style.HeadFade, 0f, 1f));
            float tail = MathF.Pow(1f - t, 1.6f);
            float k = MathHelper.SmoothStep(0f, 1f, t);
            float width = MathHelper.Lerp(style.HeadWidth, style.TailWidth, k);
            float offset = MathHelper.Lerp(style.Offset, style.OffsetTail, k)
                + MathF.Sin(along * .03f + style.WavePhase + age * .45f) * style.Wave * MathF.Min(1f, t * 3f);
            Vector2 center = points[i] + normal * offset;
            Color tint = Color.Lerp(Color.White, style.TailColor, t);
            tint.A = (byte)(255f * MathHelper.Clamp(alpha * style.Alpha * head * tail, 0f, 1f));
            float u = along / style.UScale - age * .11f * style.ScrollSpeed;
            trailVertices[i * 2] = new VertexPositionColorTexture(
                new Vector3(Vector2.Transform(center + normal * width * .5f, worldToScreen), 0f), tint, new Vector2(u, style.VMin));
            trailVertices[i * 2 + 1] = new VertexPositionColorTexture(
                new Vector3(Vector2.Transform(center - normal * width * .5f, worldToScreen), 0f), tint, new Vector2(u, style.VMax));
        }
        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.Textures[0] = texture;
            device.SamplerStates[0] = SamplerState.LinearWrap;
            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, trailVertices, 0, points.Length * 2,
                indices, 0, (points.Length - 1) * 2);
        }
    }

    public void Dispose()
    {
        effect.Dispose();
    }
}
