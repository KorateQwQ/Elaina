using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

/// <summary>
/// 绕火柱旋转的一橙一黑两条缎带；与水球飞行水带相同，按空间深度拆成火柱后方和前方两次绘制。
/// 游戏与 FNA 预览共用；只生成显示顶点，不改变射线长度与判定。
/// </summary>
internal sealed class FireLaserRibbonRenderer : IDisposable
{
    internal const string RibbonTexture = "KL/Effects/Tex/Trail/Eff_Trail_2014";
    internal const string NoiseTexture = "KL/Effects/Tex/Noise/Eff_Noise_DHL_01";
    internal const string DissolveTexture = "KL/Effects/Tex/PerlinX";

    private readonly GraphicsDevice device;
    private readonly Effect effect;
    private readonly Func<string, Texture2D> loadTexture;
    private readonly SamplerState sampler = new()
    {
        Filter = TextureFilter.Linear,
        AddressU = TextureAddressMode.Wrap,
        AddressV = TextureAddressMode.Clamp
    };
    private VertexPositionColorTexture[] vertices = Array.Empty<VertexPositionColorTexture>();
    private Vector3[] points = Array.Empty<Vector3>();
    private float[] distances = Array.Empty<float>();
    private float[] depths = Array.Empty<float>();

    private readonly record struct Ribbon(Color Tint, float Intensity, float Coverage, float HalfWidth, float Phase,
        float DissolveThreshold, float DissolveEdgeWidth, Vector3 DissolveEdgeColor);

    internal FireLaserRibbonRenderer(GraphicsDevice device, Effect material, Func<string, Texture2D> loadTexture)
    {
        this.device = device;
        this.loadTexture = loadTexture;
        effect = material.Clone();
    }

    /// <summary>SpriteBatch 必须已由调用方结束。start 为世界坐标，缎带从此处沿 direction 延伸 length。</summary>
    /// <param name="foreground">true 画转到火柱前方的部分，false 画后方的部分。</param>
    internal void Draw(Vector2 start, Vector2 direction, float length, float seconds, bool foreground,
        Vector2 camera, Matrix transform)
    {
        // 可调参数都写成局部变量，热重载修改方法体即可生效（静态字段初始化与 const 不会重新执行）。
        const float segmentLength = 6f;
        float turns = 1.35f;            // 整条射线上绕行的圈数
        float minWavelength = 510f;     // 短射线不再压缩成密集小弧
        float rotationSpeed = 10f;      // 绕射线轴旋转的弧度/秒
        float startRadius = 4f;        // 杖尖法阵处的绕行半径
        float orbitRadius = 28f;        // 火柱段的绕行半径，略大于火柱外壳
        float textureLength = 150f;     // 固定路径长度对应一次纹理重复
        float scrollSpeed = 3f;         // 缎带纹理流动速度
        float noiseScrollSpeed = 1.7f;  // 明暗噪声流动速度
        float dissolveScrollSpeed = .3f; // 黑带消融噪声流动速度
        // 颜色、亮度倍率（超过 Bloom 阈值才发光）、覆盖度、半宽、相位、消融阈值、烧边宽度、烧边 HDR 颜色
        Span<Ribbon> ribbons =
        [
            // 偏红的橙带与黄白火柱拉开色相；倍率超过 Bloom 阈值，自身带光晕。
            new(new Color(255, 20, 10), 1.2f, 1f, 7f, 0f, 0.31f, .035f, new Vector3(7f, 2.4f, .4f)),
            // 黑带本体不发光、覆盖度加倍；少量消融，烧边用高倍率橙黄色产生强烈 Bloom。
            new(new Color(56, 12, 9), 1f, 2f, 8f, MathHelper.Pi, .31f, .035f, new Vector3(7f, 2.4f, .4f))
        ];

        if (length <= segmentLength) return;
        direction = direction.LengthSquared() > 1e-5f ? Vector2.Normalize(direction) : Vector2.UnitX;
        Vector2 side = new(-direction.Y, direction.X);
        int segments = Math.Max(2, (int)MathF.Ceiling(length / segmentLength));
        if (points.Length != segments + 1)
        {
            points = new Vector3[segments + 1];
            distances = new float[segments + 1];
            depths = new float[segments + 1];
            vertices = new VertexPositionColorTexture[(segments + 1) * 2];
        }

        BlendState oldBlend = device.BlendState;
        DepthStencilState oldDepth = device.DepthStencilState;
        RasterizerState oldRasterizer = device.RasterizerState;
        SamplerState oldSampler0 = device.SamplerStates[0], oldSampler1 = device.SamplerStates[1], oldSampler2 = device.SamplerStates[2];
        Texture oldTexture0 = device.Textures[0], oldTexture1 = device.Textures[1], oldTexture2 = device.Textures[2];
        try
        {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            effect.Parameters["Transform"].SetValue(transform * Matrix.CreateOrthographicOffCenter(
                0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
            effect.Parameters["Time"].SetValue(seconds);
            effect.Parameters["ScrollSpeed"].SetValue(scrollSpeed);
            effect.Parameters["NoiseScrollSpeed"].SetValue(noiseScrollSpeed);
            effect.Parameters["DissolveScrollSpeed"].SetValue(dissolveScrollSpeed);
            Texture2D dissolve = loadTexture(DissolveTexture);
            Texture2D ribbon = loadTexture(RibbonTexture);
            Texture2D noise = loadTexture(NoiseTexture);
            float k = MathHelper.TwoPi / MathF.Max(length / turns, minWavelength);

            for (int r = 0; r < ribbons.Length; r++)
            {
                Ribbon config = ribbons[r];
                for (int i = 0; i <= segments; i++)
                {
                    float d = length * i / segments;
                    // 从法阵的大半径收拢到火柱半径；正时间项让螺旋纹向射线末端推进。
                    float radius = MathHelper.Lerp(startRadius, orbitRadius, Smooth(d / 90f));
                    float angle = k * d - seconds * rotationSpeed + config.Phase;
                    Vector2 projected = direction * d + side * (MathF.Cos(angle) * radius);
                    points[i] = new Vector3(projected, MathF.Sin(angle) * radius);
                    depths[i] = MathF.Sin(angle);
                    distances[i] = i == 0 ? 0 : distances[i - 1] + Vector2.Distance(
                        new Vector2(points[i - 1].X, points[i - 1].Y), projected);
                }

                for (int i = 0; i <= segments; i++)
                {
                    Vector3 point = points[i];
                    Vector3 difference = points[Math.Min(i + 1, segments)] - points[Math.Max(0, i - 1)];
                    Vector2 tangent = new(difference.X, difference.Y);
                    Vector2 normal = tangent.LengthSquared() > .001f
                        ? Vector2.Normalize(new Vector2(-tangent.Y, tangent.X)) : side;
                    float depth = depths[i];
                    float near = depth * .5f + .5f;
                    float frontWeight = Smooth((depth + .06f) / .12f);
                    float depthWeight = foreground ? frontWeight : 1f - frontWeight;
                    float d = length * i / segments;
                    float tips = Smooth(d / 30f) * Smooth((length - d) / 40f);
                    float halfWidth = config.HalfWidth * tips * MathHelper.Lerp(.7f, 1.2f, near);
                    float opacity = tips * depthWeight * MathHelper.Lerp(.55f, 1f, near);
                    Color color = config.Tint * opacity;
                    Vector2 position = start + new Vector2(point.X, point.Y) - camera;
                    float u = distances[i] / textureLength + r * .5f;
                    vertices[i * 2] = new(new Vector3(position - normal * halfWidth, 0), color, new Vector2(u, 0));
                    vertices[i * 2 + 1] = new(new Vector3(position + normal * halfWidth, 0), color, new Vector2(u, 1));
                }

                effect.Parameters["Intensity"].SetValue(config.Intensity);
                effect.Parameters["Coverage"].SetValue(config.Coverage);
                effect.Parameters["DissolveThreshold"].SetValue(config.DissolveThreshold);
                effect.Parameters["DissolveEdgeWidth"].SetValue(config.DissolveEdgeWidth);
                effect.Parameters["DissolveEdgeColor"].SetValue(config.DissolveEdgeColor);
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.Textures[0] = ribbon;
                    device.Textures[1] = noise;
                    device.Textures[2] = dissolve;
                    device.SamplerStates[0] = sampler;
                    device.SamplerStates[1] = SamplerState.LinearWrap;
                    device.SamplerStates[2] = SamplerState.LinearWrap;
                    device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
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
            device.SamplerStates[2] = oldSampler2;
            device.Textures[0] = oldTexture0;
            device.Textures[1] = oldTexture1;
            device.Textures[2] = oldTexture2;
        }
    }

    private static float Smooth(float value) => MathHelper.SmoothStep(0, 1, MathHelper.Clamp(value, 0, 1));

    public void Dispose()
    {
        effect.Dispose();
        sampler.Dispose();
        // 贴图归 ModContent / 预览缓存管理。
    }
}
