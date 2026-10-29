using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.System.Experience;

public enum LevelUpParticleKind : byte { Streak, Ray, Mote }

/// <summary>升级特效的一颗粒子；位置为相对施放时脚底点的偏移。</summary>
public struct LevelUpParticle
{
    public LevelUpParticleKind Kind;
    public Vector2 Offset, Velocity;
    public float Age, Life, Size, Length, Rotation, Spin, Hue, Phase;
    public bool Sparkle;
}

/// <summary>GlowDot、Star4、StarFlare 星芒、KL 光线、ThinRing、MarkGlow，均为预制资源。</summary>
public readonly record struct LevelUpTextures(Texture2D Dot, Texture2D Sparkle, Texture2D Flare, Texture2D Line,
    Texture2D Ring, Texture2D Glow);

/// <summary>
/// 角色升级特效（游戏与 FNA 预览共用，不依赖 Terraria），定稿见 Tools/LevelUpPreview 的 A 方案：
/// 脚下展开扁平星环，光环自脚底升到头顶，头顶亮起魔杖同款星芒；
/// 粒子为显迹尘同款柔光点与四芒闪粒，以及向上冲的细光线和星芒甩出的短射线。
/// 本体由 <c>LevelUpEffect</c> 弹幕跟随玩家绘制，粒子由 <c>LevelUpDust</c> 以独立寿命托管；
/// 脚下法阵走 Projectiles.After（玩家之下），上升光环、星芒与粒子走 OverPlayers.After（玩家之上），均为同层 Bloom。
/// 每层先以 shadow 模式画一遍纯黑垫底压暗背景，再画正常光效，白天亮背景上不会叠成纯白。
/// </summary>
public static class LevelUpVisuals
{
    public const int BodyTicks = 90;
    /// <summary>头顶星芒相对脚底点的高度。</summary>
    public const float HeadHeight = 62f;
    public const int StreakCount = 14, MoteCount = 24, RayCount = 10;
    public const int FlareStart = 37;

    // 淡紫星光（B 方案），亮芯近白，不用暖金；游戏中 ReColor 抬到 HDR，强通道溢出成 Bloom，白天也保持色相。
    private static readonly Color Moon = new(205, 180, 255), MoonCore = new(248, 242, 255), Silver = new(190, 170, 245),
        Pale = new(215, 185, 255), Lavender = new(165, 130, 240);

    /// <summary>黑色垫底相对光效亮度的不透明度。</summary>
    public const float ShadowOpacity = .7f;
    /// <summary>黑色垫底相对光效的尺寸，收在亮芯下面，不在光效外缘露出黑边。</summary>
    public const float ShadowScale = .7f;
    private static bool shadowPass;

    private static float Clamp01(float t) => MathHelper.Clamp(t, 0, 1);
    private static float EaseOut(float t) { t = 1 - Clamp01(t); return 1 - t * t * t; }
    private static float EaseInOut(float t)
    {
        t = Clamp01(t);
        return t < .5f ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2;
    }

    /// <summary>
    /// 预乘色：rgb 按亮度缩放，alpha 为覆盖度（同玩偶星屑，cover 越小越接近叠加）。
    /// 垫底时为纯黑覆盖；纯叠加的大柔光（cover 为 0）不垫底，否则亮背景上会出现灰色暗圈。
    /// </summary>
    private static Color Glow(Color color, float strength, float cover = 1)
    {
        strength = Clamp01(strength);
        if (shadowPass) return new Color(0, 0, 0, cover > 0 ? (int)(255 * strength * ShadowOpacity) : 0);
        return new Color((int)(color.R * strength), (int)(color.G * strength), (int)(color.B * strength),
            (int)(255 * strength * Clamp01(cover)));
    }

    private static void Draw(SpriteBatch batch, Texture2D texture, Vector2 at, Color color, float rotation, Vector2 size)
    {
        if (shadowPass) size *= ShadowScale;
        DrawExact(batch, texture, at, color, rotation, size);
    }

    private static void DrawExact(SpriteBatch batch, Texture2D texture, Vector2 at, Color color, float rotation, Vector2 size)
        => batch.Draw(texture, at, null, color, rotation, new Vector2(texture.Width, texture.Height) * .5f,
            new Vector2(size.X / texture.Width, size.Y / texture.Height), SpriteEffects.None, 0);

    /// <summary>ThinRing 的环半径为贴图半宽的 120/128；按透视压扁为椭圆。垫底不缩放，否则黑环会缩进光环内侧。</summary>
    private static void Ring(SpriteBatch batch, in LevelUpTextures tex, Vector2 center, float radius, float squash, Color color)
    {
        if (radius <= 0) return;
        float width = radius * 2 * 128f / 120f;
        DrawExact(batch, tex.Ring, center, color, 0, new Vector2(width, width * squash));
    }

    /// <summary>柔光点：光晕 + 亮芯（显迹尘粉尘同款）。</summary>
    private static void DotMote(SpriteBatch batch, in LevelUpTextures tex, Vector2 at, float size, Color tint, float alpha)
    {
        Draw(batch, tex.Dot, at, Glow(tint, .5f * alpha, .3f), 0, new Vector2(size * 14f));
        Draw(batch, tex.Dot, at, Glow(MoonCore, alpha), 0, new Vector2(size * 4.5f));
    }

    /// <summary>头顶星芒的亮度（供光照使用）。</summary>
    public static float FlareStrength(float age)
    {
        float f = Clamp01((age - FlareStart) / 27f);
        if (f <= 0 || f >= 1) return 0;
        return MathF.Sin(MathF.Min(1, f * 1.6f) * MathHelper.PiOver2) * MathF.Pow(1 - f, .6f);
    }

    /// <summary>脚下星环的亮度（供光照使用）。</summary>
    public static float CircleStrength(float age) => EaseOut(age / 17f) * (1 - Clamp01((age - 51) / 33f));

    /// <summary>脚下法阵：星环与环上光点，画在玩家之下。feet 为脚底点（屏幕坐标）。</summary>
    public static void DrawCircle(SpriteBatch batch, in LevelUpTextures tex, Vector2 feet, float age, bool shadow = false)
    {
        shadowPass = shadow;
        try { CircleCore(batch, tex, feet, age); }
        finally { shadowPass = false; }
    }

    /// <summary>上升光环与头顶星芒，画在玩家之上。feet 为脚底点（屏幕坐标）。</summary>
    public static void DrawAura(SpriteBatch batch, in LevelUpTextures tex, Vector2 feet, float age, bool shadow = false)
    {
        shadowPass = shadow;
        try { AuraCore(batch, tex, feet, age); }
        finally { shadowPass = false; }
    }

    /// <summary>以预乘 AlphaBlend 绘制一颗粒子；origin 为放出时的脚底点（屏幕坐标）。</summary>
    public static void Draw(SpriteBatch batch, in LevelUpTextures tex, Vector2 origin, in LevelUpParticle p, bool shadow = false)
    {
        shadowPass = shadow;
        try { ParticleCore(batch, tex, origin, p); }
        finally { shadowPass = false; }
    }

    private static void CircleCore(SpriteBatch batch, in LevelUpTextures tex, Vector2 feet, float age)
    {
        if (age < 0 || age >= BodyTicks) return;
        float open = EaseOut(age / 17f), fade = 1 - Clamp01((age - 51) / 33f);
        float r = 34 * open;
        Draw(batch, tex.Glow, feet, Glow(Lavender, .45f * fade, 0), 0, new Vector2(110, 34) * open);
        Draw(batch, tex.Glow, feet, Glow(Moon, .3f * fade, 0), 0, new Vector2(60, 18) * open);
        Ring(batch, tex, feet, r, .28f, Glow(Silver, .85f * fade, .6f));
        Ring(batch, tex, feet, r * .72f, .28f, Glow(Lavender, .6f * fade, .6f));

        // 环上 8 个绕行光点，隔一个叠一颗闪烁的四芒星。
        float seconds = age / 60f;
        for (int i = 0; i < 8; i++)
        {
            float angle = i / 8f * MathHelper.TwoPi + seconds * 1.6f;
            Vector2 at = feet + new Vector2(MathF.Cos(angle), MathF.Sin(angle) * .28f) * r * .86f;
            DotMote(batch, tex, at, .9f, Moon, .9f * fade);
            if (i % 2 != 0) continue;
            float twinkle = MathF.Pow(.5f + .5f * MathF.Sin(seconds * 9 + i * 1.7f), 3);
            Draw(batch, tex.Sparkle, at, Glow(Pale, (.4f + .6f * twinkle) * fade), seconds * .8f, new Vector2(10 + 8 * twinkle));
        }
    }

    private static void AuraCore(SpriteBatch batch, in LevelUpTextures tex, Vector2 feet, float age)
    {
        if (age < 0 || age >= BodyTicks) return;
        // 自脚底升到头顶的光环。
        float up = Clamp01((age - 5) / 37f);
        if (up > 0 && up < 1)
        {
            Vector2 at = feet + new Vector2(0, -HeadHeight * EaseInOut(up));
            float radius = MathHelper.Lerp(30, 16, up), a = MathF.Sin(up * MathHelper.Pi);
            Draw(batch, tex.Glow, at, Glow(Moon, .35f * a, 0), 0, new Vector2(radius * 3, radius * 1.1f));
            Ring(batch, tex, at, radius, .3f, Glow(MoonCore, .9f * a, .7f));
        }

        // 头顶星芒：魔杖星光同款形状（纯白 StarFlare），四芒星 + 正交光线 + 斜向副星 + 亮芯。
        float strength = FlareStrength(age);
        if (strength > 0)
        {
            float f = Clamp01((age - FlareStart) / 27f);
            float k = .35f + .65f * EaseOut(MathF.Min(1, f * 2.2f));
            Vector2 at = feet + new Vector2(0, -HeadHeight);
            Draw(batch, tex.Glow, at, Glow(Lavender, .35f * strength, 0), 0, new Vector2(60 * k));
            Draw(batch, tex.Flare, at, Glow(Moon, strength), 0, new Vector2(74, 56) * k);
            Draw(batch, tex.Line, at, Glow(Moon, .5f * strength), 0, new Vector2(64, 18) * k);
            Draw(batch, tex.Line, at, Glow(Moon, .5f * strength), MathHelper.PiOver2, new Vector2(36, 18) * k);
            Draw(batch, tex.Sparkle, at, Glow(Pale, .55f * strength), MathHelper.PiOver4, new Vector2(28 * k));
            Draw(batch, tex.Dot, at, Glow(MoonCore, strength), 0, new Vector2(12 * k));
        }
    }

    private static int Born(int index, int count, int from, int span) => from + index * span / count;

    /// <summary>本体第 tick 帧应放出的粒子（各类数量固定，随机参数），偏移相对当前脚底点。</summary>
    public static void Emit(Random random, int tick, List<LevelUpParticle> into)
    {
        float F() => (float)random.NextDouble();
        // 自星环边缘向上冲的细光线，沿速度方向拉长。
        for (int i = 0; i < StreakCount; i++)
        {
            if (Born(i, StreakCount, 2, 33) != tick) continue;
            float angle = F() * MathHelper.TwoPi;
            into.Add(new LevelUpParticle
            {
                Kind = LevelUpParticleKind.Streak,
                Offset = new Vector2(MathF.Cos(angle) * 28, MathF.Sin(angle) * 7),
                Velocity = new Vector2(0, -(2.6f + F() * 1.8f)),
                Life = 21 + F() * 18, Length = 22 + F() * 22, Hue = F(),
            });
        }
        // 上浮飘散的柔光点，每三颗一颗四芒闪粒。
        for (int i = 0; i < MoteCount; i++)
        {
            if (Born(i, MoteCount, 6, 33) != tick) continue;
            float angle = F() * MathHelper.TwoPi;
            into.Add(new LevelUpParticle
            {
                Kind = LevelUpParticleKind.Mote,
                Offset = new Vector2(MathF.Cos(angle) * 30, MathF.Sin(angle) * 8),
                Velocity = new Vector2((F() - .5f) * .3f, -(.6f + F() * .75f)),
                Life = 42 + F() * 33, Size = .7f + F() * .6f, Hue = F(), Phase = F() * MathHelper.TwoPi,
                Rotation = (F() - .5f) * .6f, Spin = .008f, Sparkle = i % 3 == 0,
            });
        }
        // 星芒亮起时向四周甩出的短射线。
        for (int i = 0; i < RayCount; i++)
        {
            if (FlareStart + 1 + i % 3 != tick) continue;
            float angle = i / (float)RayCount * MathHelper.TwoPi + (F() - .5f) * .4f;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            into.Add(new LevelUpParticle
            {
                Kind = LevelUpParticleKind.Ray,
                Offset = new Vector2(0, -HeadHeight) + direction * 6,
                Velocity = direction * (2.6f + F() * 1.8f),
                Life = 18 + F() * 11, Length = 14 + F() * 12, Rotation = angle, Hue = F() * .3f,
            });
        }
    }

    public static bool Step(ref LevelUpParticle p)
    {
        p.Age++;
        p.Offset += p.Velocity;
        switch (p.Kind)
        {
            case LevelUpParticleKind.Streak: p.Velocity *= .93f; break;
            case LevelUpParticleKind.Ray: p.Velocity *= .88f; break;
            default:
                p.Velocity.Y *= .985f;
                p.Offset.X += MathF.Sin(p.Age * .083f + p.Phase) * .25f;
                p.Rotation += p.Spin;
                break;
        }
        return p.Age < p.Life;
    }

    /// <summary>粒子的光照亮度。</summary>
    public static float Brightness(in LevelUpParticle p)
    {
        float t = Clamp01(p.Age / p.Life);
        return p.Kind == LevelUpParticleKind.Mote ? Clamp01(p.Age / 5f) * (1 - t * t) : 1 - t;
    }

    private static void ParticleCore(SpriteBatch batch, in LevelUpTextures tex, Vector2 origin, in LevelUpParticle p)
    {
        float t = Clamp01(p.Age / p.Life);
        Vector2 at = origin + p.Offset;
        Color tint = p.Hue < .6f ? Moon : Lavender;
        switch (p.Kind)
        {
            case LevelUpParticleKind.Streak:
            {
                float alpha = MathF.Sin(MathF.Min(1, t * 3) * MathHelper.PiOver2) * (1 - t);
                Draw(batch, tex.Line, at, Glow(tint, alpha, .6f), MathHelper.PiOver2, new Vector2(p.Length * (1 - t * .55f), 10));
                break;
            }
            case LevelUpParticleKind.Ray:
                Draw(batch, tex.Line, at, Glow(Moon, .9f * (1 - t), .6f), p.Rotation, new Vector2(p.Length * (1 - t * .6f), 9));
                break;
            default:
            {
                float seconds = p.Age / 60f;
                float alpha = Clamp01(p.Age / 5f) * (1 - t * t) * (.75f + .25f * MathF.Sin(seconds * 12 + p.Phase));
                DotMote(batch, tex, at, p.Size, tint, alpha);
                if (!p.Sparkle) break;
                float twinkle = MathF.Pow(.5f + .5f * MathF.Sin(seconds * 10 + p.Phase * 1.7f), 3);
                Draw(batch, tex.Sparkle, at, Glow(Pale, alpha * (.45f + .55f * twinkle)), p.Rotation,
                    new Vector2(p.Size * (20 + 14 * twinkle)));
                break;
            }
        }
    }
}
