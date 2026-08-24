using System;
using KL.Drawing;
using ReLogic.Graphics;
using Terraria.GameContent;

namespace 伊蕾娜.ElainaModSkills.UICore;

/// <summary>
/// 星图 UI 绘制工具箱:程序化纹理、着色器缓存、SpriteBatch 状态切换与常用绘制原语。
/// 所有纹理在首次绘制时于主线程惰性生成,不依赖外部美术资源。
/// </summary>
public static class UIDrawKit
{
    private const string EffectRoot = "伊蕾娜/ElainaModSkills/UICore/Effects/";

    // ---------- 程序化纹理 ----------
    private static Texture2D _pixel;
    private static Texture2D _glowDot;
    private static Texture2D _softRing;
    private static Texture2D _star4;
    private static Texture2D _diamondFill;

    /// <summary>1x1 白像素,可靠的线段/矩形/着色器四边形载体。</summary>
    public static Texture2D Pixel { get { EnsureTextures(); return _pixel; } }

    /// <summary>128x128 径向柔光点。</summary>
    public static Texture2D GlowDot { get { EnsureTextures(); return _glowDot; } }

    /// <summary>128x128 柔边圆环。</summary>
    public static Texture2D SoftRing { get { EnsureTextures(); return _softRing; } }

    /// <summary>128x128 四芒星光斑。</summary>
    public static Texture2D Star4 { get { EnsureTextures(); return _star4; } }

    /// <summary>64x64 抗锯齿菱形填充。</summary>
    public static Texture2D DiamondFill { get { EnsureTextures(); return _diamondFill; } }

    // ---------- 着色器 ----------
    private static Effect _nebula;
    private static Effect _glassCard;
    private static Effect _starGlow;
    private static Effect _constellationLine;
    private static Effect _radialCooldown;
    private static Effect _starfieldPlaque;
    private static Effect _sigilRing;
    private static bool _effectLoadFailed;

    public static Effect Nebula => TryGetEffect(ref _nebula, "NebulaBackground");
    public static Effect GlassCard => TryGetEffect(ref _glassCard, "GlassCard");
    public static Effect StarGlow => TryGetEffect(ref _starGlow, "StarGlow");
    public static Effect ConstellationLine => TryGetEffect(ref _constellationLine, "ConstellationLine");
    public static Effect RadialCooldown => TryGetEffect(ref _radialCooldown, "RadialCooldown");
    public static Effect StarfieldPlaque => TryGetEffect(ref _starfieldPlaque, "StarfieldPlaque");
    public static Effect SigilRing => TryGetEffect(ref _sigilRing, "SigilRing");

    // ---------- 字体 ----------
    public static DynamicSpriteFont TitleFont => FontManager.HarmonyOS_Sans_SC?.Value ?? FontAssets.MouseText.Value;
    public static DynamicSpriteFont NumberFont => FontManager.LoliFont?.Value ?? FontAssets.MouseText.Value;

    private static Effect TryGetEffect(ref Effect cache, string name)
    {
        if (cache != null || _effectLoadFailed) return cache;
        try
        {
            cache = ModContent.Request<Effect>(EffectRoot + name, AssetRequestMode.ImmediateLoad).Value;
        }
        catch (Exception e)
        {
            _effectLoadFailed = true;
            Log($"UICore: 加载着色器 {name} 失败,退化为基础绘制。{e.Message}");
        }
        return cache;
    }

    private static void EnsureTextures()
    {
        if (_pixel != null) return;
        GraphicsDevice gd = Main.instance.GraphicsDevice;

        _pixel = new Texture2D(gd, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _glowDot = BuildTexture(128, (x, y) =>
        {
            float d = Dist01(x, y, 128);
            float v = MathF.Pow(Math.Clamp(1f - d, 0f, 1f), 2.4f);
            return v;
        });

        _softRing = BuildTexture(128, (x, y) =>
        {
            float d = Dist01(x, y, 128);
            float sigma = 0.09f;
            float delta = (d - 0.78f) / sigma;
            return MathF.Exp(-delta * delta);
        });

        _star4 = BuildTexture(128, (x, y) =>
        {
            float nx = Math.Abs(x - 63.5f) / 64f;
            float ny = Math.Abs(y - 63.5f) / 64f;
            float armH = MathF.Pow(1f - Math.Clamp(ny, 0f, 1f), 9f) * MathF.Pow(1f - Math.Clamp(nx, 0f, 1f), 2.2f);
            float armV = MathF.Pow(1f - Math.Clamp(nx, 0f, 1f), 9f) * MathF.Pow(1f - Math.Clamp(ny, 0f, 1f), 2.2f);
            float core = MathF.Pow(Math.Clamp(1f - MathF.Sqrt(nx * nx + ny * ny) * 1.6f, 0f, 1f), 3f);
            return Math.Clamp(MathF.Max(MathF.Max(armH, armV), core), 0f, 1f);
        });

        _diamondFill = BuildTexture(64, (x, y) =>
        {
            float nx = Math.Abs(x - 31.5f) / 32f;
            float ny = Math.Abs(y - 31.5f) / 32f;
            float d = nx + ny;
            return Math.Clamp((0.98f - d) / 0.06f, 0f, 1f);
        });
    }

    private static float Dist01(int x, int y, int size)
    {
        float half = (size - 1) * 0.5f;
        float dx = (x - half) / half;
        float dy = (y - half) / half;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static Texture2D BuildTexture(int size, Func<int, int, float> intensity)
    {
        var tex = new Texture2D(Main.instance.GraphicsDevice, size, size);
        var data = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float v = Math.Clamp(intensity(x, y), 0f, 1f);
                data[y * size + x] = Color.White * v;
            }
        tex.SetData(data);
        return tex;
    }

    public static void Unload()
    {
        Texture2D[] textures = { _pixel, _glowDot, _softRing, _star4, _diamondFill };
        Main.QueueMainThreadAction(() =>
        {
            foreach (var tex in textures) tex?.Dispose();
        });
        _pixel = _glowDot = _softRing = _star4 = _diamondFill = null;
        _nebula = _glassCard = _starGlow = _constellationLine = _radialCooldown = null;
        _starfieldPlaque = _sigilRing = null;
        _effectLoadFailed = false;
    }

    // ---------- SpriteBatch 状态 ----------

    public static void Restart(SpriteBatch sb, BlendState blend = null, Effect effect = null,
        SamplerState sampler = null, Matrix? matrix = null)
    {
        sb.End();
        sb.Begin(SpriteSortMode.Immediate, blend ?? BlendState.AlphaBlend,
            sampler ?? SamplerState.LinearClamp, DepthStencilState.None,
            RasterizerState.CullNone, effect, matrix ?? Main.UIScaleMatrix);
    }

    public static void RestoreVanilla(SpriteBatch sb)
    {
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
    }

    // ---------- 绘制原语 ----------

    public static void DrawLine(SpriteBatch sb, Vector2 a, Vector2 b, float thickness, Color color)
    {
        Vector2 d = b - a;
        float len = d.Length();
        if (len < 0.5f) return;
        float rot = MathF.Atan2(d.Y, d.X);
        sb.Draw(Pixel, a, null, color, rot, new Vector2(0f, 0.5f), new Vector2(len, thickness), SpriteEffects.None, 0f);
    }

    /// <summary>三层叠加的发光线段(需在 Additive 或 AlphaBlend 下调用)。</summary>
    public static void DrawGlowLine(SpriteBatch sb, Vector2 a, Vector2 b, float coreWidth, Color color)
    {
        DrawLine(sb, a, b, coreWidth * 3.4f, color * 0.16f);
        DrawLine(sb, a, b, coreWidth * 1.8f, color * 0.38f);
        DrawLine(sb, a, b, coreWidth, color);
    }

    /// <summary>菱形线框,radius 为中心到顶点的距离。</summary>
    public static void DrawDiamondFrame(SpriteBatch sb, Vector2 center, float radius, float thickness, Color color)
    {
        Vector2 top = center + new Vector2(0f, -radius);
        Vector2 right = center + new Vector2(radius, 0f);
        Vector2 bottom = center + new Vector2(0f, radius);
        Vector2 left = center + new Vector2(-radius, 0f);
        DrawLine(sb, top, right, thickness, color);
        DrawLine(sb, right, bottom, thickness, color);
        DrawLine(sb, bottom, left, thickness, color);
        DrawLine(sb, left, top, thickness, color);
    }

    /// <summary>菱形填充,radius 为中心到顶点的距离。</summary>
    public static void DrawDiamond(SpriteBatch sb, Vector2 center, float radius, Color color)
    {
        float scale = radius * 2f / DiamondFill.Width;
        sb.Draw(DiamondFill, center, null, color, 0f,
            new Vector2(DiamondFill.Width * 0.5f), scale, SpriteEffects.None, 0f);
    }

    public static void DrawGlow(SpriteBatch sb, Vector2 center, float radius, Color color)
    {
        float scale = radius * 2f / GlowDot.Width;
        sb.Draw(GlowDot, center, null, color, 0f,
            new Vector2(GlowDot.Width * 0.5f), scale, SpriteEffects.None, 0f);
    }

    public static void DrawRing(SpriteBatch sb, Vector2 center, float radius, Color color)
    {
        float scale = radius * 2f / (SoftRing.Width * 0.78f);
        sb.Draw(SoftRing, center, null, color, 0f,
            new Vector2(SoftRing.Width * 0.5f), scale, SpriteEffects.None, 0f);
    }

    public static void DrawSparkle(SpriteBatch sb, Vector2 center, float size, float rotation, Color color)
    {
        float scale = size / Star4.Width;
        sb.Draw(Star4, center, null, color, rotation,
            new Vector2(Star4.Width * 0.5f), scale, SpriteEffects.None, 0f);
    }

    public static void DrawRect(SpriteBatch sb, Rectangle rect, Color color)
    {
        sb.Draw(Pixel, rect, color);
    }

    public static void DrawRectOutline(SpriteBatch sb, Rectangle rect, float thickness, Color color)
    {
        Vector2 tl = new(rect.X, rect.Y);
        Vector2 tr = new(rect.Right, rect.Y);
        Vector2 br = new(rect.Right, rect.Bottom);
        Vector2 bl = new(rect.X, rect.Bottom);
        DrawLine(sb, tl, tr, thickness, color);
        DrawLine(sb, tr, br, thickness, color);
        DrawLine(sb, br, bl, thickness, color);
        DrawLine(sb, bl, tl, thickness, color);
    }

    // ---------- 文本 ----------

    /// <summary>anchor:(0,0) 左上对齐,(0.5,0.5) 居中,(1,0) 右上对齐……</summary>
    public static void DrawText(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 pos,
        Color color, float scale = 1f, Vector2 anchor = default, float shadow = 1.6f)
    {
        if (string.IsNullOrEmpty(text)) return;
        Vector2 measure = font.MeasureString(text);
        Vector2 origin = measure * anchor;
        if (shadow > 0f)
        {
            sb.DrawString(font, text, pos + new Vector2(shadow * scale), Color.Black * 0.75f * (color.A / 255f),
                0f, origin, scale, SpriteEffects.None, 0f);
        }
        sb.DrawString(font, text, pos, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    public static Vector2 MeasureText(DynamicSpriteFont font, string text, float scale = 1f)
    {
        if (string.IsNullOrEmpty(text)) return Vector2.Zero;
        return font.MeasureString(text) * scale;
    }
}
