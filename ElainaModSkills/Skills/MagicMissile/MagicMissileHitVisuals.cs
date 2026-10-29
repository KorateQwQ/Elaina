using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

public enum MagicMissileHitPartKind
{
    Flash,
    Glow,
    Ring,
    Burst,
    Needle,
    Spark,
    Mote,
    Petal,
}

/// <summary>命中特效中的一个构件。age 从 1 开始，Delay 之后的第 1 帧即完整出现。</summary>
public struct MagicMissileHitPart
{
    public MagicMissileHitPartKind Kind;
    public int Delay, Life;
    // 起点相对命中点的偏移
    public Vector2 Offset;
    // 运动方向或贴图角度
    public float Angle, Speed;
    // 尺寸（或光环像素半径、星点长度）的起止
    public float From, To;
    // 闪光的拉伸、细线/火花的长度与粗细
    public Vector2 Shape;
    public float Hdr, Phase, Wander;
    // 花瓣序号
    public int Frame;
    public bool Flip;
    public Color Color;
}

/// <summary>命中特效用到的正式贴图（均已预乘）。</summary>
public readonly record struct MagicMissileHitTextures(Texture2D Flare, Texture2D GlowDot, Texture2D Ring, Texture2D BurstDust,
    Texture2D Line, Texture2D[] Petals);

/// <summary>
/// 魔法飞弹命中特效的构成与绘制。只依赖 XNA，游戏中的 MagicMissileHitDust 与离线预览 Tools/MagicMissileHitPreview 共用。
/// 三层：KL BurstDust 多刺爆点（朝穿透方向，8 帧从伸出到碎裂）表达方向与冲击；ShotLineSPA 细长针线与短火花快速射出；
/// 拉长的 GlowDot 星点减速后随机飘散，持续最久。第五击花印爆发改为一圈爆点，加溅射半径光环与散开的花瓣。
/// 全部用 AlphaBlend，顶点色 alpha 为覆盖度：叠加亮色的同时压掉一部分背景，白天不至于被叠成纯白；亮度经 ReColor HDR 进入同层 Bloom。
/// </summary>
public static class MagicMissileHitVisuals
{
    public static readonly Color Pink = new(255, 120, 239), Violet = new(205, 110, 255), White = new(255, 240, 255),
        Warm = new(255, 232, 196);

    /// <summary>花印爆发的溅射光环半径，与 MagicMissile 的溅射判定半径一致。</summary>
    public const float BurstRingRadius = 164f;

    // BurstCircle 在缩放 1 时光环半径约 40 像素
    private const float RingTextureRadius = 40f;
    // BurstDust：2×4 帧，每帧 128×64，尖刺朝 +X；以根部为原点
    private static readonly Vector2 BurstOrigin = new(24, 32);
    // ShotLineSPA：244×102，亮线长约 240、粗约 7 像素；上下大片低透明柔光叠多了会糊成一团，只取中间的亮线带
    private const float LineLength = 240f, LineWidth = 7f;
    private static readonly Rectangle LineBand = new(0, 43, 244, 17);
    private static readonly Vector2 LineOrigin = new(122, 8.5f);
    private const float NeedleDamping = 0.7f, SparkDamping = 0.86f, MoteDamping = 0.93f, PetalDamping = 0.88f;

    // Flo_1~5 不透明像素重心相对贴图中心的方向，花瓣沿此方向散开
    private static readonly Vector2[] PetalDirections =
    [
        new(-0.038f, -0.999f), new(-0.983f, -0.185f), new(-0.585f, 0.811f), new(0.545f, 0.839f), new(0.98f, -0.199f),
    ];

    public static MagicMissileHitPart[] CreateHit(Random random, float angle)
    {
        var parts = new List<MagicMissileHitPart>();
        float F(float min, float max) => min + (float)random.NextDouble() * (max - min);

        // 星点放最底层，持续最久；普通攻击触发频繁，保持细小、较快散去
        for (int i = 0; i < 7; i++)
        {
            float a = i < 6 ? angle + F(-1.5f, 1.5f) : F(0, MathHelper.TwoPi);
            Mote(parts, a, F(3.5f, 7f), random.Next(1, 4), random.Next(20, 39), i % 4 == 0 ? F(8, 10) : F(4, 7),
                F(0, MathHelper.TwoPi), F(5, 12), i % 3 == 0 ? Violet : Pink);
        }
        for (int i = 0; i < 6; i++)
            Needle(parts, angle + F(-1.05f, 1.05f), F(60, 85), random.Next(7, 10), F(130, 200), F(1f, 1.6f), 1.6f);
        Glow(parts, 4, 0.3f, 0.1f, Warm, 0.8f);
        // 主爆点 + 一侧较小、镜像的副爆点
        Burst(parts, angle, 0, 13, 1.25f, false, 1.8f);
        bool side = random.Next(2) == 0;
        Burst(parts, angle + (side ? 0.55f : -0.55f) + F(-0.1f, 0.1f), 1, 11, 0.8f, side, 1.6f);
        parts.AddRange(CreateContact(angle));
        for (int i = 0; i < 9; i++)
        {
            float a = i < 7 ? angle + F(-1.4f, 1.4f) : angle + MathHelper.Pi + F(-0.6f, 0.6f);
            Spark(parts, a, F(9, 15), random.Next(10, 17), F(12, 24), 1.2f, 1.8f);
        }
        Flash(parts, angle, 3, 0.045f, 2.2f, Warm, new Vector2(1.3f, 0.7f));
        return parts.ToArray();
    }

    public static MagicMissileHitPart[] CreateBurst(Random random, float angle)
    {
        var parts = new List<MagicMissileHitPart>();
        float F(float min, float max) => min + (float)random.NextDouble() * (max - min);

        for (int i = 0; i < 14; i++)
            Mote(parts, F(0, MathHelper.TwoPi), F(4f, 8f), random.Next(1, 4), random.Next(28, 49), i % 4 == 0 ? F(8, 11) : F(5, 8),
                F(0, MathHelper.TwoPi), F(6, 14), i % 3 == 0 ? Violet : Pink);
        Ring(parts, 1, 14, 25, BurstRingRadius, Pink, 0.8f);
        for (int i = 0; i < 12; i++)
            Needle(parts, i * MathHelper.TwoPi / 12 + F(-0.2f, 0.2f), F(60, 85), random.Next(8, 11), F(150, 230), F(1.2f, 1.8f), 1.7f);
        for (int i = 0; i < 5; i++)
            parts.Add(new MagicMissileHitPart { Kind = MagicMissileHitPartKind.Petal, Life = 24, Frame = i, Hdr = 1.1f });
        Glow(parts, 5, 0.4f, 0.12f, Warm, 0.9f);
        // 一圈爆点，朝飞行方向的更大
        float start = F(0, MathHelper.TwoPi);
        for (int k = 0; k < 6; k++)
        {
            float a = start + k * MathHelper.TwoPi / 6 + F(-0.15f, 0.15f);
            float forward = MathF.Cos(MathHelper.WrapAngle(a - angle));
            Burst(parts, a, k % 2, 13, 0.95f + 0.35f * Math.Max(0, forward), k % 2 == 1, 1.6f, 18f);
        }
        for (int i = 0; i < 16; i++)
            Spark(parts, F(0, MathHelper.TwoPi), F(11, 18), random.Next(12, 19), F(14, 28), 1.3f, 1.8f);
        Flash(parts, angle, 5, 0.07f, 2.4f, Warm, new Vector2(1.2f, 0.85f));
        Flash(parts, angle + MathHelper.PiOver4, 4, 0.05f, 2f, Pink, Vector2.One);
        return parts.ToArray();
    }

    /// <summary>接触点的短反向爆点。前向尖刺之外覆盖来向拖尾，不把整场爆炸无限放大。</summary>
    public static MagicMissileHitPart[] CreateContact(float angle)
    {
        var parts = new List<MagicMissileHitPart>();
        Burst(parts, angle + MathHelper.Pi, 0, 9, 0.63f, false, 1.7f);
        Glow(parts, 6, 0.42f, 0.12f, Pink, 1.4f);
        return parts.ToArray();
    }

    /// <summary>整个特效持续的帧数。</summary>
    public static int Duration(MagicMissileHitPart[] parts)
    {
        int duration = 0;
        foreach (MagicMissileHitPart part in parts)
            duration = Math.Max(duration, part.Delay + part.Life);
        return duration;
    }

    /// <summary>整个特效在某一帧的照明强度（0~1），用于 Lighting.AddLight。</summary>
    public static float LightStrength(int age, int duration) => duration <= 0 ? 0 : MathF.Pow(1 - Math.Clamp(age / (float)duration, 0, 1), 2);

    #region 构件

    private static void Flash(List<MagicMissileHitPart> parts, float rotation, int life, float size, float hdr, Color color, Vector2 stretch) =>
        parts.Add(new MagicMissileHitPart { Kind = MagicMissileHitPartKind.Flash, Life = life, Angle = rotation, From = size, Hdr = hdr,
            Color = color, Shape = stretch });

    private static void Glow(List<MagicMissileHitPart> parts, int life, float from, float to, Color color, float hdr) =>
        parts.Add(new MagicMissileHitPart { Kind = MagicMissileHitPartKind.Glow, Life = life, From = from, To = to, Color = color, Hdr = hdr });

    private static void Ring(List<MagicMissileHitPart> parts, int delay, int life, float from, float to, Color color, float hdr) =>
        parts.Add(new MagicMissileHitPart { Kind = MagicMissileHitPartKind.Ring, Delay = delay, Life = life, From = from, To = to,
            Color = color, Hdr = hdr });

    // offset：根部沿自身方向离开命中点的距离，多个爆点围成一圈时避免根部叠成实心
    private static void Burst(List<MagicMissileHitPart> parts, float angle, int delay, int life, float scale, bool flip, float hdr, float offset = 0) =>
        parts.Add(new MagicMissileHitPart { Kind = MagicMissileHitPartKind.Burst, Delay = delay, Life = life, Angle = angle, From = scale,
            Flip = flip, Hdr = hdr, Offset = Dir(angle) * offset });

    private static void Needle(List<MagicMissileHitPart> parts, float angle, float speed, int life, float length, float width, float hdr) =>
        parts.Add(new MagicMissileHitPart { Kind = MagicMissileHitPartKind.Needle, Life = life, Angle = angle, Speed = speed,
            Shape = new Vector2(length, width), Hdr = hdr });

    private static void Spark(List<MagicMissileHitPart> parts, float angle, float speed, int life, float length, float width, float hdr) =>
        parts.Add(new MagicMissileHitPart { Kind = MagicMissileHitPartKind.Spark, Life = life, Angle = angle, Speed = speed,
            Shape = new Vector2(length, width), Hdr = hdr });

    private static void Mote(List<MagicMissileHitPart> parts, float angle, float speed, int delay, int life, float length, float phase,
        float wander, Color color) =>
        parts.Add(new MagicMissileHitPart { Kind = MagicMissileHitPartKind.Mote, Delay = delay, Life = life, Angle = angle, Speed = speed,
            From = length, Phase = phase, Wander = wander, Color = color, Hdr = 1.8f });

    #endregion

    #region 绘制

    // 顶点色 alpha（覆盖度）：0 为纯叠加，越大压掉越多背景。闪光、光环是大面积柔光，带覆盖度会在亮背景上压出灰圈
    private static byte Coverage(MagicMissileHitPartKind kind) => kind switch
    {
        MagicMissileHitPartKind.Burst => 200,
        MagicMissileHitPartKind.Needle or MagicMissileHitPartKind.Spark => 120,
        MagicMissileHitPartKind.Mote => 100,
        MagicMissileHitPartKind.Glow => 60,
        MagicMissileHitPartKind.Petal => 150,
        _ => 0,
    };

    /// <summary>
    /// 按顺序绘制整个特效。要求调用方以 AlphaBlend + Immediate 开始 SpriteBatch；setHdr 在每次绘制前设置 ReColor 倍率。
    /// center 为屏幕坐标。
    /// </summary>
    public static void Draw(SpriteBatch batch, in MagicMissileHitTextures textures, Vector2 center, MagicMissileHitPart[] parts, int age,
        Action<float> setHdr)
    {
        foreach (MagicMissileHitPart part in parts)
            DrawPart(batch, textures, center, part, age, setHdr);
    }

    private static void DrawPart(SpriteBatch batch, in MagicMissileHitTextures textures, Vector2 center, in MagicMissileHitPart part, int age,
        Action<float> setHdr)
    {
        int n = age - part.Delay;
        if (n < 1 || n > part.Life) return;
        float q = (n - 1f) / part.Life;
        Vector2 at = center + part.Offset;
        byte coverage = Coverage(part.Kind);
        Texture2D line = textures.Line;

        // fade 同时作用于亮度与覆盖度，避免颜色淡出后只剩遮挡、在亮背景上发灰
        void Sprite(Texture2D texture, Rectangle? source, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, float hdr,
            float fade = 1, SpriteEffects effects = SpriteEffects.None)
        {
            setHdr(hdr * fade);
            batch.Draw(texture, position, source, new Color(color.R, color.G, color.B, (byte)(coverage * Math.Clamp(fade, 0, 1))), rotation,
                origin, scale, effects, 0);
        }

        Vector2 LineScale(float length, float width) => new(length / LineLength, width / LineWidth);

        switch (part.Kind)
        {
            case MagicMissileHitPartKind.Flash:
            {
                float s = n == 1 ? 0.8f : 1 - MathF.Pow((n - 2f) / (part.Life - 1f), 0.75f);
                Sprite(textures.Flare, null, at, part.Color, part.Angle, Center(textures.Flare), part.Shape * part.From * s, part.Hdr);
                break;
            }
            case MagicMissileHitPartKind.Glow:
                Sprite(textures.GlowDot, null, at, part.Color, 0, Center(textures.GlowDot), new Vector2(MathHelper.Lerp(part.From, part.To, q)),
                    part.Hdr, MathF.Pow(1 - q, 1.5f));
                break;
            case MagicMissileHitPartKind.Ring:
            {
                float e = 1 - MathF.Pow(1 - q, 3);
                Sprite(textures.Ring, null, at, part.Color, 0, Center(textures.Ring),
                    new Vector2(MathHelper.Lerp(part.From, part.To, e) / RingTextureRadius), part.Hdr, MathF.Pow(1 - q, 1.4f));
                break;
            }
            case MagicMissileHitPartKind.Burst:
            {
                // 8 帧：伸出 → 完整多刺 → 碎裂。只画一层纯色，倍率明显超过阈值，由 Bloom 晕出光感与泛白的中心
                int frame = Math.Clamp((int)(q * 8), 0, 7);
                var source = new Rectangle(frame % 2 * 128, frame / 2 * 64, 128, 64);
                float scale = part.From * (n == 1 ? 0.85f : 1 + 0.1f * q);
                float fade = q < 0.5f ? 1 : 1 - (q - 0.5f) / 0.5f * 0.6f;
                var effects = part.Flip ? SpriteEffects.FlipVertically : SpriteEffects.None;
                Vector2 origin = part.Flip ? new Vector2(BurstOrigin.X, 64 - BurstOrigin.Y) : BurstOrigin;
                Sprite(textures.BurstDust, source, at, Pink, part.Angle, origin, new Vector2(scale), part.Hdr, fade, effects);
                break;
            }
            case MagicMissileHitPartKind.Needle:
            {
                // 头部飞快射出，尾部随后收拢，线从命中点向外抽离
                float head = part.Speed * Travel(NeedleDamping, n);
                float tail = Math.Max(0, head - part.Shape.X * MathF.Pow(1 - q, 0.8f));
                float length = head - tail;
                if (length <= 0.5f) break;
                Vector2 mid = at + Dir(part.Angle) * (head + tail) / 2;
                float width = part.Shape.Y * (1 - 0.5f * q);
                float fade = MathF.Pow(1 - q, 0.7f);
                Sprite(line, LineBand, mid, Pink, part.Angle, LineOrigin, LineScale(length, width * 1.8f), part.Hdr, fade);
                Sprite(line, LineBand, mid, White, part.Angle, LineOrigin, LineScale(length * 0.92f, width * 0.7f), part.Hdr * 1.3f, fade);
                break;
            }
            case MagicMissileHitPartKind.Spark:
            {
                Vector2 position = at + Dir(part.Angle) * (6 + part.Speed * Travel(SparkDamping, n));
                float length = part.Shape.X * (0.35f + 0.65f * MathF.Pow(SparkDamping, n)) * MathF.Pow(1 - q, 0.5f);
                float fade = MathF.Pow(1 - q, 0.8f);
                Sprite(line, LineBand, position, Pink, part.Angle, LineOrigin, LineScale(length, part.Shape.Y * 1.6f), part.Hdr, fade);
                Sprite(line, LineBand, position, White, part.Angle, LineOrigin, LineScale(length * 0.8f, part.Shape.Y * 0.6f), part.Hdr * 1.3f, fade);
                break;
            }
            case MagicMissileHitPartKind.Mote:
            {
                // 顺速度拉长的光点：柔光 + 亮芯，淡入、闪烁、后半段淡出
                Vector2 position = MotePosition(part, at, n);
                Vector2 velocity = position - MotePosition(part, at, n - 1);
                float rotation = velocity.LengthSquared() > 0.0001f ? MathF.Atan2(velocity.Y, velocity.X) : part.Angle;
                float fadeIn = Math.Min(1, n / 3f);
                float fadeOut = q < 0.5f ? 1 : (1 - q) / 0.5f;
                float fade = fadeIn * fadeOut * (0.7f + 0.3f * MathF.Sin(n * 0.35f + part.Phase));
                float stretch = 1 + Math.Min(1.2f, velocity.Length() * 0.35f);
                // GlowDot 可见直径约为缩放 × 64 像素
                var size = new Vector2(part.From * stretch, part.From * 0.45f) / 64f;
                Sprite(textures.GlowDot, null, position, part.Color, rotation, Center(textures.GlowDot), size, part.Hdr, fade);
                Sprite(textures.GlowDot, null, position, White, rotation, Center(textures.GlowDot), size * 0.45f, part.Hdr * 1.6f, fade);
                break;
            }
            case MagicMissileHitPartKind.Petal:
            {
                // 五片花瓣先拼成整朵花，再沿各自方向散开并旋转
                Vector2 position = at + PetalDirections[part.Frame] * 5f * Travel(PetalDamping, n);
                float spin = (part.Frame % 2 == 0 ? 1 : -1) * 0.05f;
                Texture2D petal = textures.Petals[part.Frame];
                Sprite(petal, null, position, White, spin * n, Center(petal), new Vector2(MathHelper.Lerp(1.6f, 2.1f, q)), part.Hdr,
                    MathF.Pow(1 - q, 1.1f));
                break;
            }
        }
    }

    // 星点：沿初速减速散开，叠加逐渐增强的缓慢正弦漂移与轻微上浮
    private static Vector2 MotePosition(in MagicMissileHitPart part, Vector2 at, int n)
    {
        float ramp = Math.Min(1, n / 25f);
        var wander = new Vector2(MathF.Sin(n * 0.06f + part.Phase) - MathF.Sin(part.Phase),
            MathF.Sin(n * 0.045f + part.Phase * 1.7f) - MathF.Sin(part.Phase * 1.7f)) * part.Wander * ramp;
        return at + Dir(part.Angle) * part.Speed * Travel(MoteDamping, n) + wander + new Vector2(0, -0.04f * n);
    }

    private static Vector2 Center(Texture2D texture) => new(texture.Width / 2f, texture.Height / 2f);

    private static Vector2 Dir(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));

    // 每次更新先衰减速度再位移，n 次更新后的累计位移（以初速为单位）
    private static float Travel(float damping, int n) => n <= 0 ? 0 : damping * (1 - MathF.Pow(damping, n)) / (1 - damping);

    #endregion
}
