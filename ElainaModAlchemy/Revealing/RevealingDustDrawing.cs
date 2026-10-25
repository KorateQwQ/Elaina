using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModAlchemy.Revealing;

/// <summary>显迹尘所需贴图：MarkGlow（RGB 衰减）、GlowDot/Star4（项目烘焙）、KL 四芒星与光线（魔杖星光同款）。</summary>
public readonly record struct RevealingDustTextures(Texture2D Glow, Texture2D Dot, Texture2D Sparkle, Texture2D Flare, Texture2D Line);

/// <summary>游戏和 FNA 预览共用的金色粉尘与圆心星光；粉尘在出手后短时间飞散，星光随区域持续。
/// 所有 tint 的 alpha=0，在 AlphaBlend 批次中以叠加方式发光，不切换混合状态。</summary>
public static class RevealingDustDrawing
{
    public const int BurstTicks = 180;
    public const int ParticleCount = 300;
    /// <summary>每隔若干颗粉尘取一颗提交光照，形成随粉尘向外扫开的照明。</summary>
    public const int LightStride = 6;
    public const int CenterFadeTicks = 180;
    public static readonly Vector3 CenterLightColor = new(1.05f, .78f, .36f);
    public static readonly Vector3 MoteLightColor = new(.55f, .34f, .09f);

    private static float Noise(int value)
    {
        uint x = unchecked((uint)value * 747796405u + 2891336453u);
        x = ((x >> (int)((x >> 28) + 4)) ^ x) * 277803737u;
        return ((x >> 22) ^ x) / (float)uint.MaxValue;
    }

    private static float Smooth(float t)
    {
        t = MathHelper.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }

    private static void Draw(SpriteBatch batch, Texture2D texture, Vector2 at, Color color, float rotation, Vector2 size)
        => batch.Draw(texture, at, null, color, rotation, new Vector2(texture.Width, texture.Height) * .5f,
            new Vector2(size.X / texture.Width, size.Y / texture.Height), SpriteEffects.None, 0);

    /// <summary>第 i 颗粉尘在 age 时的状态；绘制与光照共用，保证光随可见粉尘移动。</summary>
    public static bool Mote(int i, float age, int seed, Vector2 center, out Vector2 at, out float alpha, out float size)
    {
        int key = i * 13 + seed * 977;
        float life = 105f + Noise(key + 6) * 75f;
        at = center; alpha = 0; size = 0;
        if (age < 0 || age >= life) return false;
        float angle = Noise(key) * MathHelper.TwoPi;
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        float reach = 1000f * (.12f + .88f * MathF.Pow(Noise(key + 1), .6f));
        float travel = 50f + Noise(key + 7) * 35f;
        float progress = MathHelper.Clamp(age / travel, 0, 1);
        float spread = 1f - MathF.Pow(1f - progress, 3f);
        at = center + direction * (6 + reach * spread);
        // 飞散减速后像粉尘一样缓慢下沉、左右飘。
        float settle = Math.Max(0, age - travel * .6f);
        at.Y += settle * settle * .002f * (.6f + Noise(key + 8));
        at.X += MathF.Sin(age * .05f + Noise(key + 2) * 6.28f) * 7f * progress;
        float twinkle = .72f + .28f * MathF.Sin(age * .21f + i * 2.3f);
        alpha = MathHelper.Clamp(age / 5f, 0, 1) * Smooth((life - age) / (life * .45f)) * twinkle;
        size = .8f + Noise(key + 4);
        return alpha > 0;
    }

    public static void Draw(SpriteBatch batch, in RevealingDustTextures tex, Vector2 center, float age, int seed)
    {
        if (age < 0 || age >= BurstTicks) return;
        for (int i = 0; i < ParticleCount; i++)
        {
            if (!Mote(i, age, seed, center, out Vector2 at, out float alpha, out float size)) continue;
            int key = i * 13 + seed * 977;
            Color gold = Color.Lerp(new Color(255, 146, 38, 0), new Color(255, 224, 136, 0), Noise(key + 5));
            Draw(batch, tex.Dot, at, gold * (alpha * .5f), 0, new Vector2(size * 28f));
            Draw(batch, tex.Dot, at, new Color(255, 240, 196, 0) * alpha, 0, new Vector2(size * 9f));
            if (i % 4 != 0) continue;
            // 少量星形闪粒：尖锐的闪烁节奏，缓慢自转。
            float flash = MathF.Pow(.5f + .5f * MathF.Sin(age * .16f + i * 1.7f), 3f);
            float star = size * (24f + 16f * flash);
            Draw(batch, tex.Sparkle, at, new Color(255, 228, 156, 0) * (alpha * (.45f + .55f * flash)),
                Noise(key + 9) * .6f - .3f + age * .008f, new Vector2(star));
        }
    }

    /// <summary>圆心亮度：出手闪亮后回落到稳定值，轻微呼吸，区域结束前淡出。光照与星光共用。</summary>
    public static float CenterStrength(float age, float duration)
    {
        if (age < 0 || age >= duration) return 0;
        float flash = .55f * MathF.Exp(-age / 28f);
        float intro = MathHelper.Clamp(age / 8f, 0, 1);
        float outro = MathHelper.Clamp((duration - age) / CenterFadeTicks, 0, 1);
        float breathe = .93f + .07f * MathF.Sin(age * .045f);
        return (breathe + flash) * intro * outro;
    }

    /// <summary>圆心星光：柔光底 + 魔杖同款四芒星与正交光线 + 斜向副星 + 缓慢上浮的金色光粒。</summary>
    public static void DrawCenter(SpriteBatch batch, in RevealingDustTextures tex, Vector2 center,
        float age, float duration, int seed)
    {
        float strength = CenterStrength(age, duration);
        if (strength <= 0) return;
        float s = MathHelper.Clamp(strength, 0, 1.2f);
        Draw(batch, tex.Glow, center, new Color(255, 150, 40, 0) * (.26f * s), 0, new Vector2(170f));
        Draw(batch, tex.Glow, center, new Color(255, 194, 90, 0) * (.40f * s), 0, new Vector2(62f));

        // 出手瞬间一次放大后收束的大星芒。
        float intro = age / 40f;
        if (intro < 1)
        {
            float grow = 1f - MathF.Pow(1f - intro, 3f);
            Draw(batch, tex.Flare, center, new Color(255, 214, 130, 0) * ((1 - intro) * (1 - intro)), 0,
                new Vector2(70f + 120f * grow, 52f + 80f * grow));
        }

        float appear = Smooth(age / 18f);
        float pulse = (.86f + .14f * MathF.Sin(age * .06f + seed)) * appear;
        Color starColor = new Color(255, 212, 118, 0) * MathHelper.Clamp(s, 0, 1);
        Draw(batch, tex.Flare, center, starColor, 0, new Vector2(74f, 52f) * pulse);
        Draw(batch, tex.Line, center, starColor * .5f, 0, new Vector2(64f, 20f) * pulse);
        Draw(batch, tex.Line, center, starColor * .5f, MathHelper.PiOver2, new Vector2(34f, 20f) * pulse);
        float diagonal = .75f + .25f * MathF.Sin(age * .09f + seed * 1.3f);
        Draw(batch, tex.Sparkle, center, new Color(255, 232, 170, 0) * (.5f * s * appear), MathHelper.PiOver4,
            new Vector2(30f * diagonal));
        Draw(batch, tex.Dot, center, new Color(255, 248, 220, 0) * MathHelper.Clamp(s, 0, 1), 0, new Vector2(12f));

        float fade = MathHelper.Clamp(strength, 0, 1) * MathHelper.Clamp(age / 40f, 0, 1);
        for (int i = 0; i < 12; i++)
        {
            int key = i * 31 + seed * 577 + 9001;
            float period = 110f + Noise(key) * 90f;
            float t = (age + Noise(key + 1) * period) / period;
            float life = t - MathF.Floor(t);
            int cycle = (int)MathF.Floor(t);
            float lane = (Noise(key + cycle * 7 + 2) - .5f) * 56f;
            Vector2 at = center + new Vector2(lane + MathF.Sin(age * .05f + i * 1.9f) * 5f, 8f - life * 58f);
            float alpha = MathF.Sin(life * MathHelper.Pi) * fade;
            float size = .8f + Noise(key + 3) * .7f;
            Color gold = Color.Lerp(new Color(255, 150, 40, 0), new Color(255, 230, 150, 0), Noise(key + 4));
            Draw(batch, tex.Dot, at, gold * (alpha * .35f), 0, new Vector2(size * 14f));
            Draw(batch, tex.Dot, at, new Color(255, 242, 200, 0) * alpha, 0, new Vector2(size * 4f));
            if (i % 4 == 0)
                Draw(batch, tex.Sparkle, at, new Color(255, 228, 156, 0) * (alpha * .8f), age * .01f, new Vector2(size * 16f));
        }
    }

    public static Color DangerColor(Color color)
    {
        color.R = 255;
        color.G = Math.Max(color.G, (byte)50);
        color.B = Math.Max(color.B, (byte)50);
        return color;
    }

    /// <summary>独立柔光覆盖，不与敌人原贴图相乘，黑色身体也能看到沾染位置。</summary>
    public static void DrawMark(SpriteBatch batch, Texture2D glow, Vector2 center, float targetSize, float tick, int seed)
    {
        float pulse = .92f + .08f * MathF.Sin(tick * .055f + seed * 1.7f);
        float diameter = MathHelper.Clamp(targetSize + 18, 34, 72);
        // Asset stores falloff in RGB (not only alpha). Alpha=0 tint makes an additive spot
        // within the game's existing AlphaBlend batch, without changing render state.
        batch.Draw(glow, center, null, new Color(255, 177, 47, 0) * (.52f * pulse),
            0, new Vector2(glow.Width, glow.Height) * .5f, diameter / glow.Width, SpriteEffects.None, 0);
        batch.Draw(glow, center, null, new Color(255, 226, 142, 0) * (.64f * pulse),
            0, new Vector2(glow.Width, glow.Height) * .5f, 9f / glow.Width, SpriteEffects.None, 0);
    }
}
