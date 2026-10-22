using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

/// <summary>沿现有射线路径绘制水球吸水材质；仅生成显示顶点，不修改轨迹。</summary>
internal sealed class WaterLaserFlowRenderer : IDisposable
{
    private readonly GraphicsDevice device;
    private readonly Effect effect;
    private readonly Func<string, Texture2D> loadTexture;
    private readonly SamplerState trailSampler = new()
    {
        Filter = TextureFilter.Linear,
        AddressU = TextureAddressMode.Wrap,
        AddressV = TextureAddressMode.Clamp
    };
    private VertexPositionColorTexture[] vertices = Array.Empty<VertexPositionColorTexture>();
    private float[] distances = Array.Empty<float>();

    internal WaterLaserFlowRenderer(GraphicsDevice device, Effect material, Func<string, Texture2D> loadTexture)
    {
        this.device = device;
        this.loadTexture = loadTexture;
        effect = material.Clone();
    }

    // SpriteBatch must be ended by the caller. Input points remain in world coordinates.
    internal void Draw(Vector2[] points, Vector2 camera, float seconds, Matrix transform)
    {
        if (points.Length < 2) return;
        if (distances.Length != points.Length)
        {
            distances = new float[points.Length];
            vertices = new VertexPositionColorTexture[points.Length * 2];
        }
        distances[0] = 0;
        for (int i = 1; i < points.Length; i++)
            distances[i] = distances[i - 1] + Vector2.Distance(points[i - 1], points[i]);
        float length = distances[^1];
        if (length <= .01f) return;

        Texture2D water = loadTexture("伊蕾娜/ElainaModSkills/Skills/Water/Eff_Splash_322_288");
        Texture2D noise = loadTexture("KL/Effects/Tex/Noise/Eff_Noise_DHL_01");
        Texture2D glow = loadTexture("KL/Effects/Tex/Trail/LightTrail");
        BlendState blend = device.BlendState;
        DepthStencilState depth = device.DepthStencilState;
        RasterizerState rasterizer = device.RasterizerState;
        SamplerState sampler0 = device.SamplerStates[0], sampler1 = device.SamplerStates[1];
        Texture texture0 = device.Textures[0], texture1 = device.Textures[1];
        try
        {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            effect.Parameters["Transform"].SetValue(transform * Matrix.CreateOrthographicOffCenter(
                0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
            effect.Parameters["Time"].SetValue(seconds);
            // 保持吸水的白沫与浅蓝水体，射线纹理以吸水的 6 倍速度向末端冲刷。
            effect.Parameters["NoiseStrength"].SetValue(.025f);
            effect.Parameters["TextureRepeat"].SetValue(1f);
            effect.Parameters["TextureScrollSpeed"].SetValue(5.6f);
            effect.Parameters["NoiseScrollSpeed"].SetValue(.3f);
            effect.Parameters["FoamThreshold"].SetValue(.42f);
            effect.Parameters["FoamSoftness"].SetValue(.12f);
            effect.Parameters["FoamSpread"].SetValue(.035f);
            effect.Parameters["UseTextureAlpha"].SetValue(true);
            effect.Parameters["MaterialColorStrength"].SetValue(0f);
            for (int layer = 0; layer < 3; layer++)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    Vector2 tangent = points[Math.Min(i + 1, points.Length - 1)] - points[Math.Max(i - 1, 0)];
                    Vector2 normal = tangent.LengthSquared() > .0001f
                        ? Vector2.Normalize(new Vector2(-tangent.Y, tangent.X)) : Vector2.UnitY;
                    float distance = distances[i];
                    // 固定像素长度的收口与 UV，碰撞截短时水纹不会被拉伸或加速。
                    float entrance = MathHelper.SmoothStep(0f, 1f, Math.Min(distance / 48f, 1f));
                    float exit = MathHelper.SmoothStep(0f, 1f, Math.Min((length - distance) / 12f, 1f));
                    float halfWidth = MathHelper.Lerp(9f, 14f, entrance) * MathHelper.Lerp(.7f, 1f, exit);
                    float opacity = MathHelper.Lerp(.7f, 1f, entrance) * MathHelper.Lerp(.75f, 1f, exit);
                    Color color = layer switch
                    {
                        0 => new Color(128, 191, 255, 0) * (opacity * .12f),
                        1 => new Color(128, 191, 255) * (opacity * .78f),
                        _ => new Color(245, 253, 255) * Math.Min(opacity * 1.15f, 1f)
                    };
                    if (layer == 0) halfWidth *= 1.65f;
                    Vector2 position = points[i] - camera;
                    float u = distance / 180f;
                    vertices[i * 2] = new(new Vector3(position - normal * halfWidth, 0), color, new Vector2(u, 0));
                    vertices[i * 2 + 1] = new(new Vector3(position + normal * halfWidth, 0), color, new Vector2(u, 1));
                }
                effect.Parameters["Layer"].SetValue((float)layer);
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.Textures[0] = layer == 0 ? glow : water;
                    device.Textures[1] = noise;
                    device.SamplerStates[0] = layer == 0 ? SamplerState.LinearClamp : trailSampler;
                    device.SamplerStates[1] = SamplerState.LinearWrap;
                    device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
                }
            }
        }
        finally
        {
            device.BlendState = blend;
            device.DepthStencilState = depth;
            device.RasterizerState = rasterizer;
            device.SamplerStates[0] = sampler0;
            device.SamplerStates[1] = sampler1;
            device.Textures[0] = texture0;
            device.Textures[1] = texture1;
        }
    }

    public void Dispose()
    {
        effect.Dispose();
        trailSampler.Dispose();
        // Textures are owned by ModContent / the preview cache.
    }
}
