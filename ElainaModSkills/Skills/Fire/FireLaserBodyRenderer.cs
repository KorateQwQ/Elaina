using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

/// <summary>
/// 火焰射线主体：沿射线的一个四边形，由 FireLaserBody.fx 投影成横躺的火焰圆柱，
/// 叠层与火焰龙卷相同（色带噪声底 + Additive 火纹/高亮碎片 + 外层暗色条带烧边）。
/// 游戏与 FNA 预览共用；只负责显示，不改变射线长度与判定。
/// </summary>
internal sealed class FireLaserBodyRenderer : IDisposable
{
    internal const string WaterTexture = "KL/Effects/Tex/水波";
    internal const string WindTexture = "KL/Effects/Tex/Wind/wind1";
    internal const string CellTexture = "KL/Effects/Tex/cellnoise";
    internal const string BandTexture = "KL/Effects/Tex/Wind/Eff_Noise_705";
    internal const string PerlinTexture = "KL/Effects/Tex/PerlinX";
    internal const string DisplaceTexture = "KL/Effects/Tex/Noise/Eff_Noise_20";
    internal const string RampTexture = "伊蕾娜/ElainaModSkills/Skills/Fire/FireTornadoFireRamp";

    private readonly GraphicsDevice device;
    private readonly Effect effect;
    private readonly Func<string, Texture2D> loadTexture;
    private readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[4];
    private readonly Texture2D[] textures = new Texture2D[TextureCount];
    private readonly Texture[] oldTextures = new Texture[TextureCount];
    private readonly SamplerState[] oldSamplers = new SamplerState[TextureCount];
    private const int TextureCount = 7;

    internal FireLaserBodyRenderer(GraphicsDevice device, Effect material, Func<string, Texture2D> loadTexture)
    {
        this.device = device;
        this.loadTexture = loadTexture;
        effect = material.Clone();
    }

    /// <summary>SpriteBatch 必须已由调用方结束。start 为世界坐标，主体从此处沿 direction 延伸 length。</summary>
    internal void Draw(Vector2 start, Vector2 direction, float length, float seconds, Vector2 camera, Matrix transform)
    {
        // 可调参数都写成局部变量，热重载修改方法体即可生效。
        float halfWidth = 28f;              // 条带半宽（像素），需容纳外壳半径与轮廓起伏
        Vector2 radius = new(.76f, .64f);   // 暗色外壳 / 火焰内芯半径（占半宽）
        float period = 130f;                // 沿轴一次纹理重复的像素长度
        Vector3 taper = new(60f, 100f, 30f); // 起点收拢 / 末端燃尽 / 起点燃起长度
        float startRadius = .3f;            // 杖尖处的半径比例
        Vector3 displace = new(.13f, .55f, 2.2f); // 轮廓起伏幅度、沿轴缩放、流速
        float fadePixels = 1.5f;
        // 每层：绕轴旋转（圈/秒）、沿轴流动（周期/秒）、螺旋倾斜、纹理缩放
        Vector4 fireFlow = new(.7f, 2.0f, 1.0f, .6f);
        Vector4 streakFlow = new(.4f, 3.2f, .5f, .8f);
        Vector4 sparkFlow = new(.25f, 1.5f, 1.4f, .8f);
        Vector4 bandFlow = new(.45f, 1.2f, 1.0f, .4f);
        Vector4 stoneFlow = new(.8f, 1.7f, 1.0f, 1.0f);
        float rampGain = .75f;
        float edgeShade = .55f;
        float coreHeat = .15f;
        Vector3 streakColor = new Vector3(1f, .31f, .08f) * 1.2f;
        Vector3 sparkColor = new Vector3(1f, .37f, .1f) * 2.0f;
        float sparkThreshold = .5f;
        float coreGlow = .6f;
        Vector3 bandColor = new(36 / 255f, 13 / 255f, 4 / 255f);
        float bandOpacity = .9f;
        float bandThreshold = .42f;
        Vector3 stoneColor = new(20 / 255f, 10 / 255f, 0f);
        float stoneThreshold = .47f;
        float stoneEdgeWidth = .05f;
        float edgeWidth = .028f;
        Vector3 edgeColor = new(3f, 1f, .18f);
        float endBurn = 1f;

        if (length <= 1f) return;
        direction = direction.LengthSquared() > 1e-5f ? Vector2.Normalize(direction) : Vector2.UnitX;
        Vector2 side = new Vector2(-direction.Y, direction.X) * halfWidth;
        Vector2 a = start - camera, b = a + direction * length;
        vertices[0] = new(new Vector3(a - side, 0), Color.White, new Vector2(0, 0));
        vertices[1] = new(new Vector3(a + side, 0), Color.White, new Vector2(0, 1));
        vertices[2] = new(new Vector3(b - side, 0), Color.White, new Vector2(length, 0));
        vertices[3] = new(new Vector3(b + side, 0), Color.White, new Vector2(length, 1));

        BlendState oldBlend = device.BlendState;
        DepthStencilState oldDepth = device.DepthStencilState;
        RasterizerState oldRasterizer = device.RasterizerState;
        for (int i = 0; i < TextureCount; i++)
        {
            oldSamplers[i] = device.SamplerStates[i];
            oldTextures[i] = device.Textures[i];
        }
        try
        {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            EffectParameterCollection p = effect.Parameters;
            p["Transform"].SetValue(transform * Matrix.CreateOrthographicOffCenter(
                0, device.Viewport.Width, device.Viewport.Height, 0, -1, 1));
            p["Time"].SetValue(seconds);
            p["Length"].SetValue(length);
            p["HalfWidth"].SetValue(halfWidth);
            p["Period"].SetValue(period);
            p["Radius"].SetValue(radius);
            p["Taper"].SetValue(taper);
            p["StartRadius"].SetValue(startRadius);
            p["Displace"].SetValue(displace);
            p["FadePixels"].SetValue(fadePixels);
            p["FireFlow"].SetValue(fireFlow);
            p["StreakFlow"].SetValue(streakFlow);
            p["SparkFlow"].SetValue(sparkFlow);
            p["BandFlow"].SetValue(bandFlow);
            p["StoneFlow"].SetValue(stoneFlow);
            p["RampGain"].SetValue(rampGain);
            p["EdgeShade"].SetValue(edgeShade);
            p["CoreHeat"].SetValue(coreHeat);
            p["StreakColor"].SetValue(streakColor);
            p["SparkColor"].SetValue(sparkColor);
            p["SparkThreshold"].SetValue(sparkThreshold);
            p["CoreGlow"].SetValue(coreGlow);
            p["BandColor"].SetValue(bandColor);
            p["BandOpacity"].SetValue(bandOpacity);
            p["BandThreshold"].SetValue(bandThreshold);
            p["StoneColor"].SetValue(stoneColor);
            p["StoneThreshold"].SetValue(stoneThreshold);
            p["StoneEdgeWidth"].SetValue(stoneEdgeWidth);
            p["EdgeWidth"].SetValue(edgeWidth);
            p["EdgeColor"].SetValue(edgeColor);
            p["EndBurn"].SetValue(endBurn);
            // 槽位顺序与 FireLaserBody.fx 的 register(s0~s6) 一致；色带用 Clamp，其余平铺。
            textures[0] = loadTexture(WaterTexture);
            textures[1] = loadTexture(WindTexture);
            textures[2] = loadTexture(CellTexture);
            textures[3] = loadTexture(BandTexture);
            textures[4] = loadTexture(PerlinTexture);
            textures[5] = loadTexture(DisplaceTexture);
            textures[6] = loadTexture(RampTexture);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                for (int i = 0; i < TextureCount; i++)
                {
                    device.Textures[i] = textures[i];
                    device.SamplerStates[i] = i == TextureCount - 1 ? SamplerState.LinearClamp : SamplerState.LinearWrap;
                }
                device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, 2);
            }
        }
        finally
        {
            device.BlendState = oldBlend;
            device.DepthStencilState = oldDepth;
            device.RasterizerState = oldRasterizer;
            for (int i = 0; i < TextureCount; i++)
            {
                device.SamplerStates[i] = oldSamplers[i];
                device.Textures[i] = oldTextures[i];
                oldTextures[i] = null;
            }
        }
    }

    public void Dispose()
    {
        effect.Dispose();
        // 贴图归 ModContent / 预览缓存管理。
    }
}
