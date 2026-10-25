using System;
using KL.Drawing;
using ReLogic.Graphics;
using Terraria.GameContent;

namespace 伊蕾娜.ElainaModSkills.UICore;

/// <summary>
/// 星图 UI 绘制工具箱：预制纹理、着色器缓存与常用绘制原语。
/// 固定图案由 Tools/FixedTextureBake 离线生成，运行时由资源系统加载。
/// </summary>
public static class UIDrawKit
{
    private const string EffectRoot = "伊蕾娜/ElainaModSkills/UICore/Effects/";

    // ---------- 预制纹理 ----------
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
    public static DynamicSpriteFont TitleFont => FontManager.NotoSerifSC?.Value ?? FontAssets.MouseText.Value;
    public static DynamicSpriteFont NumberFont => FontManager.NotoSerifSC?.Value ?? FontAssets.MouseText.Value;

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
        _glowDot = ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/UICore/Assets/GlowDot", AssetRequestMode.ImmediateLoad).Value;
        _softRing = ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/UICore/Assets/SoftRing", AssetRequestMode.ImmediateLoad).Value;
        _star4 = ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/UICore/Assets/Star4", AssetRequestMode.ImmediateLoad).Value;
        _diamondFill = ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/UICore/Assets/DiamondFill", AssetRequestMode.ImmediateLoad).Value;
        _pixel = ModContent.Request<Texture2D>("伊蕾娜/Effects/Tex/Baked/WhitePixel", AssetRequestMode.ImmediateLoad).Value;
    }

    public static void Unload()
    {
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
