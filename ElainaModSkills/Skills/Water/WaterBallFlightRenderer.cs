using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

/// <summary>两股绕飞行轴旋转的水带；按空间深度拆到水球前后，共用正式水流材质。</summary>
internal sealed class WaterBallFlightRenderer : IDisposable
{
    private const int Segments = 120;
    private readonly GraphicsDevice device;
    private readonly Effect effect;
    private readonly Func<string, Texture2D> loadTexture;
    private readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[(Segments + 1) * 2];
    private readonly Vector3[] points = new Vector3[Segments + 1];
    private readonly float[] distances = new float[Segments + 1];
    private readonly SamplerState sampler = new()
    {
        Filter = TextureFilter.Linear,
        AddressU = TextureAddressMode.Wrap,
        AddressV = TextureAddressMode.Clamp
    };

    internal WaterBallFlightRenderer(GraphicsDevice device, Effect material, Func<string, Texture2D> loadTexture)
    {
        this.device = device;
        this.loadTexture = loadTexture;
        effect = material.Clone();
    }

    // Caller owns SpriteBatch.End/Begin. Center is camera-relative; seconds is visual age since launch.
    internal void Draw(Vector2 center, float waterRadius, Vector2 stretch, float rotation,
        float seconds, bool foreground, Matrix transform)
    {
        if (seconds <= 0 || waterRadius <= 0) return;
        float turns = 0.55f;             // 单条水带从尾到头的绕行圈数
        float rotationSpeed = 12f;        // 绕飞行轴旋转的弧度/秒
        float ribbonWidth = 16f;         // 满蓄力时的水带宽度
        float tailLength = 4.6f;         // 拖到球心后方的长度（球体基础半径倍数）
        float tailFlare = 0.6f;          // 尾端额外张开半径（球体基础半径倍数，不随发射拉伸放大）
        float noseOrbitScale = .75f;    // 球头旋转半径：锥形小口
        float textureLength =350f;      // 固定路径长度对应一次水纹重复
        float radius = waterRadius * (256f / 255f * .35f);
        float axialRadius = radius * Math.Max(.25f, stretch.X);
        float crossRadius = radius * Math.Max(.35f, stretch.Y);
        float tailOrbit = crossRadius + radius * tailFlare;
        Vector2 forward = new(MathF.Cos(rotation), MathF.Sin(rotation));
        Vector2 side = new(-forward.Y, forward.X);
        float appear = Smooth(seconds / .07f);
        Texture2D water = loadTexture("伊蕾娜/ElainaModSkills/Skills/Water/Eff_Splash_322_288");
        Texture2D noise = loadTexture("KL/Effects/Tex/Noise/Eff_Noise_DHL_01");
        BlendState oldBlend = device.BlendState;
        DepthStencilState oldDepth = device.DepthStencilState;
        RasterizerState oldRasterizer = device.RasterizerState;
        SamplerState oldSampler0 = device.SamplerStates[0], oldSampler1 = device.SamplerStates[1];
        Texture oldTexture0 = device.Textures[0], oldTexture1 = device.Textures[1];
        try
        {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            effect.Parameters["Transform"].SetValue(transform * Matrix.CreateOrthographicOffCenter(
                0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
            effect.Parameters["Time"].SetValue(seconds);
            effect.Parameters["NoiseStrength"].SetValue(.025f);
            effect.Parameters["TextureRepeat"].SetValue(1f);
            // U 从尾端增向球头；Shader 采样 U-Time*Speed，负速度才向尾端流。
            effect.Parameters["TextureScrollSpeed"].SetValue(-2.8f);
            effect.Parameters["NoiseScrollSpeed"].SetValue(-.3f);
            effect.Parameters["FoamThreshold"].SetValue(.46f);
            effect.Parameters["FoamSoftness"].SetValue(.12f);
            effect.Parameters["FoamSpread"].SetValue(.025f);
            effect.Parameters["UseTextureAlpha"].SetValue(true);
            effect.Parameters["MaterialColorStrength"].SetValue(0f);
            for (int stream = 0; stream < 2; stream++)
            {
                for (int i = 0; i <= Segments; i++)
                {
                    float t = i / (float)Segments;
                    float x = MathHelper.Lerp(-radius * tailLength, axialRadius * .92f, t);
                    // t 从尾到头；越远离球体的尾端，绕轴半径越大，形成向后展开的锥形。
                    float orbit = MathHelper.Lerp(tailOrbit, crossRadius * noseOrbitScale, t);
                    // t 从尾到头递增；正时间项让等相位点沿 -t 向尾端传播。
                    float angle = t * MathHelper.TwoPi * turns + seconds * rotationSpeed + stream * MathF.PI;
                    Vector2 projected = forward * x + side * (MathF.Cos(angle) * orbit);
                    points[i] = new Vector3(projected, MathF.Sin(angle) * orbit);
                    distances[i] = i == 0 ? 0 : distances[i - 1] + Vector3.Distance(points[i - 1], points[i]);
                }
                // Covered blue water and white crests; no bloom or additive halo.
                for (int layer = 1; layer <= 2; layer++)
                {
                    for (int i = 0; i <= Segments; i++)
                    {
                        float t = i / (float)Segments;
                        Vector3 point = points[i];
                        Vector3 difference = points[Math.Min(i + 1, Segments)] - points[Math.Max(0, i - 1)];
                        Vector2 tangent = new(difference.X, difference.Y);
                        Vector2 normal = tangent.LengthSquared() > .001f
                            ? Vector2.Normalize(new Vector2(-tangent.Y, tangent.X)) : side;
                        float localOrbit = MathHelper.Lerp(tailOrbit, crossRadius * noseOrbitScale, t);
                        float near = MathHelper.Clamp(point.Z / localOrbit * .5f + .5f, 0, 1);
                        float frontWeight = Smooth((point.Z / localOrbit + .06f) / .12f);
                        float depthWeight = foreground ? frontWeight : 1 - frontWeight;
                        float tips = Smooth(t / .25f) * Smooth((1 - t) / .13f);
                        float halfWidth = ribbonWidth * waterRadius / 200f * .5f * tips
                            * MathHelper.Lerp(.72f, 1.15f, near) * MathHelper.Lerp(1.35f, 1f, t);
                        float opacity = appear * tips * depthWeight * MathHelper.Lerp(.55f, 1f, near);
                        Color color = layer == 1
                            ? new Color(100, 180, 250) * (opacity * .76f)
                            : new Color(239, 251, 255) * (opacity * .92f);
                        Vector2 position = center + new Vector2(point.X, point.Y);
                        float u = distances[i] / textureLength + stream * .5f;
                        vertices[i * 2] = new(new Vector3(position - normal * halfWidth, 0), color, new Vector2(u, 0));
                        vertices[i * 2 + 1] = new(new Vector3(position + normal * halfWidth, 0), color, new Vector2(u, 1));
                    }
                    effect.Parameters["Layer"].SetValue((float)layer);
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        device.Textures[0] = water;
                        device.Textures[1] = noise;
                        device.SamplerStates[0] = sampler;
                        device.SamplerStates[1] = SamplerState.LinearWrap;
                        device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
                    }
                }
            }
        }
        finally
        {
            device.BlendState = oldBlend;
            device.DepthStencilState = oldDepth;
            device.RasterizerState = oldRasterizer;
            device.SamplerStates[0] = oldSampler0;
            device.SamplerStates[1] = oldSampler1;
            device.Textures[0] = oldTexture0;
            device.Textures[1] = oldTexture1;
        }
    }

    private static float Smooth(float value) => MathHelper.SmoothStep(0, 1, MathHelper.Clamp(value, 0, 1));

    public void Dispose()
    {
        effect.Dispose();
        sampler.Dispose();
    }
}
