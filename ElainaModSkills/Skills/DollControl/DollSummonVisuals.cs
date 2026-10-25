using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

public enum DollSparkKind : byte { Flare, Star, Mote }

/// <summary>召唤/收回星屑的一颗粒子；位置为相对爆发中心的偏移。</summary>
public struct DollSpark
{
    public DollSparkKind Kind;
    public Vector2 Offset, Velocity;
    public float Age, Life, Size, Rotation, Spin, Hue, Rise;
}

/// <summary>
/// 玩偶召唤与收回的共用表现（游戏和 FNA 预览共用，不依赖 Terraria）：
/// 出现时从星芯弹出并泛白，收回时鼓起、收缩泛白后化作星屑；星屑配色沿用魔杖星光的粉紫。
/// 粒子在游戏中由 <c>DollSummonDust</c> 以独立寿命托管，并走 Projectiles.After 同层 Bloom。
/// </summary>
public static class DollSummonVisuals
{
    public const int AppearTicks = 22;
    public const int VanishTicks = 26;
    /// <summary>四只依次出现/收回的间隔。</summary>
    public const int StaggerTicks = 6;
    /// <summary>收回时在剩余这么多 tick 时爆出星屑，与玩偶缩成星点的瞬间衔接。</summary>
    public const int VanishBurstLead = 8;
    /// <summary>两帧之间位移超过此距离视为瞬移（远距离回到主人身边），播放消散与重新出现。</summary>
    public const float TeleportJump = 480f;

    // 粉白主调、少量淡紫：游戏中 ReColor 把 rgb 抬到 HDR，主色在强通道溢出成 Bloom，弱通道仍低于 1，白天也保持色相。
    private static readonly Color Pink = new(255, 118, 218), Violet = new(206, 138, 255), Core = new(255, 224, 250);

    private static float Clamp01(float t) => MathHelper.Clamp(t, 0, 1);

    /// <summary>
    /// 预乘色：rgb 按亮度缩放，alpha 为覆盖度（按覆盖度压暗背后画面，即暗底）。
    /// cover = 1 为完全遮盖的“正常”绘制，越小越接近叠加；贴图本身也是预乘的，因此柔边线性过渡，不会发灰。
    /// </summary>
    private static Color Glow(Color color, float strength, float cover = 1)
    {
        strength = Clamp01(strength);
        return new Color((int)(color.R * strength), (int)(color.G * strength), (int)(color.B * strength),
            (int)(255 * strength * Clamp01(cover)));
    }

    private static float EaseOutBack(float t)
    {
        t = Clamp01(t) - 1;
        const float c = 2.2f;
        return 1 + (c + 1) * t * t * t + c * t * t;
    }

    /// <summary>
    /// 玩偶本体的缩放、泛白和透明度。appearAge &lt; 0 表示尚未轮到出现（隐藏）；vanishAge &lt; 0 表示未在收回。
    /// </summary>
    public static void Appearance(float appearAge, float vanishAge, out Vector2 scale, out float flash, out float alpha, out Vector2 offset)
    {
        scale = Vector2.One; flash = 0; alpha = 1; offset = Vector2.Zero;
        if (vanishAge >= 0)
        {
            float t = Clamp01(vanishAge / VanishTicks);
            // 先轻轻鼓起，再加速收成一点；越到后面越白、越往上飘。
            float swell = 1 + .1f * MathF.Sin(Clamp01(t / .35f) * MathHelper.Pi);
            float shrink = t < .3f ? 1 : 1 - MathF.Pow((t - .3f) / .7f, 1.6f);
            scale = new Vector2(swell * shrink * (1 - .25f * t), swell * shrink * (1 + .2f * t));
            flash = Clamp01(t * 1.6f);
            alpha = 1 - Clamp01((t - .85f) / .15f);
            offset = new Vector2(0, -12f * t * t);
            return;
        }
        if (appearAge < 0) { alpha = 0; scale = Vector2.Zero; return; }
        float a = Clamp01(appearAge / AppearTicks);
        float pop = EaseOutBack(a);
        // 弹出时先纵向拉长再回弹，带一点卡通感。
        float squash = MathF.Sin(a * MathHelper.Pi) * (1 - a);
        scale = new Vector2(pop * (1 - .18f * squash), pop * (1 + .22f * squash));
        flash = MathF.Pow(1 - a, 2f);
        alpha = Clamp01(a * 4f);
    }

    /// <summary>一次爆发的粒子数（星芒、星屑、光粒）。</summary>
    public static int BurstCount(bool vanish) => vanish ? 19 : 15;

    /// <summary>按顺序生成一次爆发中的第 index 颗：首颗为星芒，其后为星形星屑与柔光粒。</summary>
    public static DollSpark Create(Random random, int index, bool vanish)
    {
        float F() => (float)random.NextDouble();
        if (index == 0)
            return new DollSpark { Kind = DollSparkKind.Flare, Life = vanish ? 30 : 24, Size = vanish ? 1.1f : 1f, Hue = .2f };
        DollSparkKind kind = index <= (vanish ? 6 : 4) ? DollSparkKind.Star : DollSparkKind.Mote;
        float angle = F() * MathHelper.TwoPi;
        float speed = (kind == DollSparkKind.Star ? 2.6f : 1.8f) + F() * 2.2f;
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        return new DollSpark
        {
            Kind = kind,
            Offset = direction * (vanish ? 4f : 8f + F() * 6f),
            Velocity = direction * speed * (vanish ? 1.15f : 1f),
            Life = (kind == DollSparkKind.Star ? 30 : 26) + F() * 22 + (vanish ? 10 : 0),
            Size = .75f + F() * .5f,
            Rotation = (F() - .5f) * .5f,
            Spin = (F() - .5f) * .06f,
            Hue = F(),
            // 收回的星屑向上飘散，像化成星光离去；召唤时则略微下落。
            Rise = vanish ? -.045f - F() * .03f : .015f,
        };
    }

    public static bool Step(ref DollSpark spark)
    {
        spark.Age++;
        if (spark.Kind != DollSparkKind.Flare)
        {
            spark.Offset += spark.Velocity;
            spark.Velocity *= .89f;
            spark.Velocity.Y += spark.Rise;
            spark.Rotation += spark.Spin;
        }
        return spark.Age < spark.Life;
    }

    /// <summary>星芒当前亮度（供光照使用）。</summary>
    public static float FlareStrength(in DollSpark spark)
    {
        float t = Clamp01(spark.Age / spark.Life);
        return MathF.Sin(Clamp01(t * 6f) * MathHelper.PiOver2) * MathF.Pow(1 - t, 1.4f);
    }

    /// <summary>
    /// 以预乘 AlphaBlend 绘制（与魔力飞弹本体同理：写入覆盖度、先压暗背景再上色），亮背景上不会叠加爆白。
    /// 游戏中再由 ReColor 抬到 HDR 供 Bloom；只有很小的星芯接近白色。
    /// </summary>
    public static void Draw(SpriteBatch batch, Texture2D dot, Texture2D sparkle, Texture2D flare, Texture2D line,
        Vector2 center, in DollSpark spark)
    {
        float t = Clamp01(spark.Age / spark.Life);
        Vector2 at = center + spark.Offset;
        Color tint = Color.Lerp(Pink, Violet, spark.Hue);
        switch (spark.Kind)
        {
            case DollSparkKind.Flare:
            {
                // 魔杖同款：四芒星 + 正交光线，快速张开后收束。
                float strength = FlareStrength(spark);
                float open = MathF.Sin(Clamp01(t * 4f) * MathHelper.PiOver2);
                Vector2 size = new Vector2(96f, 66f) * spark.Size * (.35f + .65f * open) * (1 - .45f * t);
                Draw(batch, dot, at, Glow(tint, .45f * strength, .3f), 0, new Vector2(70f * spark.Size));
                Draw(batch, flare, at, Glow(Color.Lerp(tint, Core, .35f), strength), 0, size);
                Draw(batch, line, at, Glow(tint, .8f * strength), 0, new Vector2(size.X * .95f, 22f * spark.Size));
                Draw(batch, line, at, Glow(tint, .8f * strength), MathHelper.PiOver2, new Vector2(size.Y * .8f, 22f * spark.Size));
                Draw(batch, dot, at, Glow(Core, strength), 0, new Vector2(14f * spark.Size));
                break;
            }
            case DollSparkKind.Star:
            {
                float alpha = Clamp01(t / .08f) * MathF.Pow(1 - t, 1.3f);
                float twinkle = .6f + .4f * MathF.Sin(spark.Age * .5f + spark.Hue * 9f);
                Draw(batch, dot, at, Glow(tint, .4f * alpha), 0, new Vector2(22f * spark.Size));
                Draw(batch, sparkle, at, Glow(Color.Lerp(tint, Core, .6f), alpha * twinkle), spark.Rotation,
                    new Vector2(30f * spark.Size * (.7f + .3f * twinkle)));
                break;
            }
            default:
            {
                float alpha = Clamp01(t / .1f) * MathF.Pow(1 - t, 1.5f);
                Draw(batch, dot, at, Glow(tint, .45f * alpha), 0, new Vector2(18f * spark.Size));
                Draw(batch, dot, at, Glow(Core, alpha), 0, new Vector2(5f * spark.Size));
                break;
            }
        }
    }

    private static void Draw(SpriteBatch batch, Texture2D texture, Vector2 at, Color color, float rotation, Vector2 size)
        => batch.Draw(texture, at, null, color, rotation, new Vector2(texture.Width, texture.Height) * .5f,
            new Vector2(size.X / texture.Width, size.Y / texture.Height), SpriteEffects.None, 0);
}
