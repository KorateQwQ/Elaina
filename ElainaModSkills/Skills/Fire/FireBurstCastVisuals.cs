using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

/// <summary>炎爆领域施法演出用到的正式贴图（均已预乘）。</summary>
public readonly record struct FireBurstCastTextures(Texture2D GlowDot, Texture2D Star4, Texture2D Flare, Texture2D Line);

/// <summary>
/// 施法时间轴（设计帧，动画速度为 1 时每帧对应一个游戏帧）：
/// 0～Launch 举杖聚火 → Launch～Arrive 火种升空 → Arrive～Compress 天空成环 → Compress～Fall 悬停压缩 → Fall～Impact 坠落 → Impact 爆燃。
/// </summary>
public readonly record struct FireBurstCastTimeline(float Total, float Launch, float Arrive, float Compress, float Fall, float Impact);

/// <summary>螺旋汇入目标的火星。</summary>
public struct FireBurstCastMote
{
    public float Spawn, Angle, Radius, Life, Swirl;
    public bool Line;
}

/// <summary>从领域圆心沿径向散开的十字星或点状星光。</summary>
public struct FireBurstImpactParticle
{
    public float Delay, Life, Angle, Speed, Size, Phase;
    public bool Star;
}

public sealed class FireBurstImpactParts(FireBurstImpactParticle[] particles)
{
    public readonly FireBurstImpactParticle[] Particles = particles;
}

/// <summary>某一帧施法演出的状态，位置均为屏幕坐标。</summary>
public struct FireBurstCastFrame
{
    public float Time;
    public Vector2 WandTip, Seed;
    /// <summary>火种拖尾路径，由头到尾（见 SeedTrailPath）。</summary>
    public Vector2[] SeedTrail;
    public FireBurstCastMote[] WandMotes, SkyMotes;
    public Vector2 Sky;
}

/// <summary>
/// 炎爆领域的施法演出与落地爆燃。只依赖 XNA，游戏中的 Action_FireBurst、FireBurstImpactDust 与离线预览 Tools/FireBurstPreview（--cast）共用。
/// 施法：火星螺旋汇入杖尖成火核 → 火核挑向领域中心上方，杖尖留下较小的施法光效 → 火种在空中长大，两道倾斜火环绕它旋转
/// → 压缩变白 → 连同火环坠向玩家脚下。爆燃：白闪、领在火环前的冲击波细环与从圆心向外散开的十字星及点状星光。
/// 全部用 NonPremultiplied；FireBurstCast Shader 将预乘贴图采样还原为直通 RGB，顶点 alpha 控制覆盖度与淡出。
/// HDR 进入同层 Bloom。可调参数为表达式属性或方法内局部变量，支持热重载。
/// </summary>
public static class FireBurstCastVisuals
{
    public static void SetIntensity(Effect effect, float intensity)
    {
        effect.Parameters["Intensity"].SetValue(intensity);
        effect.CurrentTechnique.Passes[0].Apply();
    }

    // ---- 时间轴与几何（热重载后下次施放生效；Total 决定动作总帧数）----
    public static FireBurstCastTimeline Timeline => new(Total: 60, Launch: 12, Arrive: 22, Compress: 34, Fall: 38, Impact: 44);
    /// <summary>火种悬停在领域中心上方的高度（像素）。</summary>
    public static float SkyHeight => 220f;
    /// <summary>爆燃持续帧数（覆盖全部星光的寿命）。</summary>
    public static int ImpactDuration => 90;

    public static Color Deep => new(150, 28, 8);
    public static Color Red => new(255, 80, 20);
    public static Color Orange => new(255, 150, 65);
    public static Color Yellow => new(255, 205, 90);
    public static Color White => new(255, 245, 215);

    // ShotLineSPA：244×102，亮线长约 240、粗约 7 像素；只取中间的亮线带，柔光交给 Bloom。
    // 仅保留亮线带；Shader 将灰边亮度计入覆盖度，避免亮背景出现矩形暗框。
    private static readonly Rectangle LineBand = new(0, 47, 244, 9);
    private static readonly Vector2 LineOrigin = new(122, 4.5f);
    private const float LineLength = 240f, LineWidth = 7f;
    // GlowDot 可见直径约为缩放 × 64 像素
    private const float GlowDiameter = 64f;

    #region 生成

    public static FireBurstCastMote[] CreateMotes(Random random, float from, float to, int perFrame, float minRadius, float maxRadius)
    {
        int count = Math.Max(0, (int)MathF.Ceiling(to - from)) * perFrame;
        var motes = new FireBurstCastMote[count];
        float F(float min, float max) => min + (float)random.NextDouble() * (max - min);
        for (int i = 0; i < count; i++)
            motes[i] = new FireBurstCastMote
            {
                Spawn = from + i / perFrame + F(0, 1),
                Angle = F(0, MathHelper.TwoPi),
                Radius = F(minRadius, maxRadius),
                Life = F(12, 21),
                Swirl = (random.Next(2) == 0 ? -1 : 1) * F(0.7f, 1.6f),
                Line = random.NextDouble() < 0.35,
            };
        return motes;
    }

    /// <summary>杖尖聚火（0～Launch）与天空聚火（Arrive 前后）的火星。</summary>
    public static void CreateCastMotes(Random random, out FireBurstCastMote[] wand, out FireBurstCastMote[] sky)
    {
        FireBurstCastTimeline tl = Timeline;
        wand = CreateMotes(random, 2, tl.Launch, 3, 60, 130);
        sky = CreateMotes(random, tl.Arrive - 2, tl.Compress + 2, 4, 110, 260);
    }

    public static FireBurstImpactParts CreateImpact(Random random)
    {
        int count = 72;
        float F(float min, float max) => min + (float)random.NextDouble() * (max - min);
        var particles = new FireBurstImpactParticle[count];
        for (int i = 0; i < count; i++)
        {
            bool star = i % 3 == 0;
            particles[i] = new FireBurstImpactParticle
            {
                // 分扇区随机取角度，两种星光均覆盖完整圆周；随机速度使它们散成云而非整齐的一圈。
                Angle = MathHelper.TwoPi * (i + F(0, 1)) / count,
                Delay = F(0, 3), Life = F(38, 76), Speed = F(5, 14),
                Size = star ? F(6, 11) : F(3, 6), Phase = F(0, MathHelper.TwoPi), Star = star,
            };
        }
        return new FireBurstImpactParts(particles);
    }

    #endregion

    #region 运动

    /// <summary>从领域圆心出发，保持径向并逐渐减速；age 为粒子出生后的帧数。</summary>
    public static Vector2 ImpactParticlePosition(in FireBurstImpactParticle particle, Vector2 center, float age)
    {
        float damping = 0.965f;
        float travel = (1 - MathF.Pow(damping, Math.Max(0, age))) / (1 - damping);
        return center + Dir(particle.Angle) * (particle.Speed * travel);
    }

    /// <summary>火种的世界/屏幕位置（与传入坐标系一致）：从挑起时的杖尖升到天空点，悬停，再坠向落点（玩家脚下）。</summary>
    public static Vector2 SeedPosition(float t, Vector2 launchTip, Vector2 sky, Vector2 ground)
    {
        FireBurstCastTimeline tl = Timeline;
        float arc = 18f;       // 升空时向前的弧度（像素）
        float bob = 2f;        // 悬停时的上下浮动（像素）
        if (t < tl.Arrive)
        {
            float k = OutCubic(Math.Clamp((t - tl.Launch) / (tl.Arrive - tl.Launch), 0, 1));
            float side = launchTip.X >= sky.X ? 1 : -1;   // 向杖尖所在一侧（身前）鼓出
            return Vector2.Lerp(launchTip, sky, k) + new Vector2(MathF.Sin(k * MathHelper.Pi) * arc * side, 0);
        }
        if (t < tl.Fall) return sky + new Vector2(0, MathF.Sin(t * 0.2f) * bob);
        float f = Math.Clamp((t - tl.Fall) / (tl.Impact - tl.Fall), 0, 1);
        return Vector2.Lerp(sky, ground + new Vector2(0, -4), f * f * f);
    }

    /// <summary>
    /// 火种拖尾路径：沿火种轨迹回溯一小段时间，由头到尾。升空时随弧线弯曲，悬停后尾巴收回火种，坠落时从悬停点拉下。
    /// seedAt 返回某设计帧的火种位置（坐标系与绘制一致）。
    /// </summary>
    public static Vector2[] SeedTrailPath(float t, Func<float, Vector2> seedAt)
    {
        FireBurstCastTimeline tl = Timeline;
        float riseTicks = 4f, fallTicks = 3f;   // 回溯的设计帧数
        int samples = 24;
        if (t < tl.Launch || t >= tl.Impact) return null;
        float from = Math.Max(tl.Launch, t - (t >= tl.Fall ? fallTicks : riseTicks));
        var path = new Vector2[samples];
        for (int i = 0; i < samples; i++)
            path[i] = seedAt(t - (t - from) * i / (samples - 1));
        return path;
    }

    #endregion

    #region 绘制：施法

    /// <summary>
    /// 绘制施法阶段（爆燃前）。要求调用方以 NonPremultiplied + Immediate 开始 SpriteBatch；setHdr 设置 FireBurstCast Shader 倍率。
    /// </summary>
    public static void DrawCast(SpriteBatch batch, in FireBurstCastTextures tex, in FireBurstCastFrame frame, Action<float> setHdr)
    {
        FireBurstCastTimeline tl = Timeline;
        float t = frame.Time;
        if (t >= tl.Impact) return;

        // ---- 杖尖 ----
        float castGlowSize = 10f, castGlowPulse = 0.18f;              // 升空后的施法光效
        float castGlowFadeIn = 4f;                                     // 火核离杖后，施法光效从无到有的帧数
        float castGlowFadeStart = tl.Fall + 3f, castGlowFadeTicks = 5f;

        DrawMotes(batch, tex, frame.WandMotes, t, frame.WandTip, setHdr);
        if (t < tl.Launch)
            DrawCore(batch, tex, frame.WandTip, WandCoreSize(t), 0, t * 0.04f, 1, setHdr);
        else
        {
            // 从零淡入：火核刚离杖时杖尖不另冒一颗亮点，否则看起来像火核原地缩小
            float fadeIn = SmoothStep((t - tl.Launch) / castGlowFadeIn);
            float fadeOut = 1 - Math.Clamp((t - castGlowFadeStart) / castGlowFadeTicks, 0, 1);
            float size = castGlowSize * (1 + castGlowPulse * MathF.Sin(t * 0.45f)) * fadeIn;
            DrawCore(batch, tex, frame.WandTip, size, 0, t * 0.04f, 0.85f * fadeIn * fadeOut, setHdr);
        }

        // ---- 天空火种 ----
        float ringFadeIn = 8f;             // 火环出现的帧数（从 Arrive - 4 开始）
        float ringCloseTicks = 2f;         // 落地前火环收拢的帧数

        DrawMotes(batch, tex, frame.SkyMotes, t, frame.Sky, setHdr);
        if (t < tl.Launch) return;
        Vector2 p = frame.Seed;
        float seedSize = SeedSize(t);
        float hot = Math.Clamp((t - tl.Compress) / (tl.Fall - tl.Compress), 0, 1);   // 压缩变白

        // 升空与坠落时的拖尾：沿轨迹收细
        bool falling = t >= tl.Fall;
        float riseTrailWidth = 7f, fallTrailWidth = 10f;   // 头部宽度（像素），向尾部收到零
        DrawTaperTrail(batch, tex, frame.SeedTrail, falling ? fallTrailWidth : riseTrailWidth, falling ? Yellow : Orange,
            falling ? 2f : 1.6f, 120, setHdr);

        float ringAlpha = Math.Clamp((t - (tl.Arrive - 4)) / ringFadeIn, 0, 1)
            * (t < tl.Impact - ringCloseTicks ? 1 : Math.Clamp((tl.Impact - t) / ringCloseTicks, 0, 1));
        DrawOrbitRings(batch, tex, p, t >= tl.Fall ? 26 : seedSize, t, ringAlpha, setHdr);
        DrawCore(batch, tex, p, seedSize, hot, t * 0.04f, 1, setHdr);
    }

    /// <summary>杖尖聚火火核的尺寸（像素），Launch 前有效；火种离杖时从 WandCoreSize(Launch) 接续。</summary>
    public static float WandCoreSize(float t)
    {
        float from = 2f, min = 4f, max = 20f;
        if (t < from) return 0;
        float launch = Timeline.Launch, u = t - from;
        return MathHelper.Lerp(min, max, SmoothStep(u / (launch - from))) * (1 + 0.12f * MathF.Sin(0.05f * u * u));
    }

    /// <summary>
    /// 天空火种的尺寸（像素），Launch 起有效：从离杖时的火核尺寸连续长大到 Compress 时的最大值（到达天空后叠加渐快的脉动），
    /// 悬停压缩到一定比例，坠落时固定。
    /// </summary>
    public static float SeedSize(float t)
    {
        FireBurstCastTimeline tl = Timeline;
        float maxSize = 34f, fallSize = 22f;
        float compressTo = 0.65f;          // 悬停压缩到的比例
        if (t >= tl.Fall) return fallSize;
        float grow = SmoothStep((t - tl.Launch) / (tl.Compress - tl.Launch));
        float u = Math.Max(0, t - tl.Arrive);
        float size = MathHelper.Lerp(WandCoreSize(tl.Launch), maxSize, grow) * (1 + 0.12f * MathF.Sin(0.02f * u * u));
        if (t >= tl.Compress) size *= MathHelper.Lerp(1, compressTo, (t - tl.Compress) / (tl.Fall - tl.Compress));
        return size;
    }

    /// <summary>火核：低覆盖度的红色外晕 + 亮芯 + 星芒，压缩时亮芯转白。</summary>
    public static void DrawCore(SpriteBatch batch, in FireBurstCastTextures tex, Vector2 at, float size, float hot, float rotation, float intensity,
        Action<float> setHdr)
    {
        if (size <= 0 || intensity <= 0) return;
        float haloScale = 3.2f, haloHdr = 1.1f;
        float coreScale = 1.4f, coreHdr = 1.6f, hotHdr = 0.35f;
        float flareScale = 4.2f, flareHdr = 2.6f;
        // T_StartFlare001 的十字臂约占满贴图宽度
        Sprite(batch, tex.GlowDot, null, at, Red, 0, Center(tex.GlowDot), new Vector2(size * haloScale / GlowDiameter), haloHdr, 100,
            intensity, setHdr);
        Sprite(batch, tex.GlowDot, null, at, hot > 0.5f ? White : Yellow, 0, Center(tex.GlowDot), new Vector2(size * coreScale / GlowDiameter),
            coreHdr + hot * hotHdr, 255, intensity, setHdr);
        Sprite(batch, tex.Flare, null, at, Orange, rotation + MathHelper.PiOver4, Center(tex.Flare),
            new Vector2(size * flareScale / tex.Flare.Width), flareHdr, 255, intensity, setHdr);
    }

    /// <summary>两道倾斜、各自旋转的火环，环上各有一颗光点绕行。</summary>
    public static void DrawOrbitRings(SpriteBatch batch, in FireBurstCastTextures tex, Vector2 at, float size, float t, float alpha,
        Action<float> setHdr)
    {
        if (alpha <= 0) return;
        float ringHdr = 4.5f, ringWidth = 3f, dotSize = 12f, dotHdr = 1.8f;
        Span<(float Tilt, float Speed, float Scale)> rings = [(0.55f, 0.11f, 1f), (-0.7f, -0.08f, 1.25f)];
        foreach (var ring in rings)
        {
            float rx = size * 1.7f * ring.Scale + 14, ry = rx * (0.28f + 0.1f * MathF.Sin(t * ring.Speed * 3));
            DrawEllipse(batch, tex, at, rx, ry, ring.Tilt, ringWidth, Orange, ringHdr, 255, alpha, null, setHdr);
            float th = t * ring.Speed * 4;
            Vector2 dot = at + Rotate(new Vector2(MathF.Cos(th) * rx, MathF.Sin(th) * ry), ring.Tilt);
            Sprite(batch, tex.GlowDot, null, dot, Yellow, 0, Center(tex.GlowDot), new Vector2(dotSize / GlowDiameter), dotHdr, 255, alpha, setHdr);
        }
    }

    /// <summary>火星沿螺旋汇入目标：越近越亮，三分之一画成指向目标的短线。</summary>
    public static void DrawMotes(SpriteBatch batch, in FireBurstCastTextures tex, FireBurstCastMote[] motes, float t, Vector2 target,
        Action<float> setHdr)
    {
        if (motes == null) return;
        float dotHdr = 2f, lineHdr = 1.8f;
        float lineWidth = 1.8f;                  // 线状火星的粗细（像素）
        float lineMinLength = 10f, lineExtraLength = 36f;   // 线长：刚出现时 min + extra，汇入目标时缩到 min
        foreach (FireBurstCastMote m in motes)
        {
            float a = t - m.Spawn;
            if (a < 0 || a > m.Life) continue;
            float q = a / m.Life;
            float radius = m.Radius * MathF.Pow(1 - q, 1.5f), angle = m.Angle + m.Swirl * q;
            Vector2 at = target + Dir(angle) * radius;
            float fadeIn = Math.Min(1, q * 5), brighten = 0.6f + 0.9f * q;
            float fade = fadeIn * brighten;
            if (m.Line)
            {
                float backAngle = angle - m.Swirl * 0.08f;
                Vector2 back = target + Dir(backAngle) * Math.Min(m.Radius, radius + lineMinLength + lineExtraLength * (1 - q));
                DrawStreak(batch, tex, back, at, lineWidth, Orange, lineHdr, 255, setHdr, fadeIn);
            }
            else
            {
                float size = 9 - 4 * q;
                // 淡入只改变 Alpha，保留橙黄色，不叠加已有背景的 RGB。
                Sprite(batch, tex.GlowDot, null, at, q > 0.6f ? Yellow : Orange, 0, Center(tex.GlowDot), new Vector2(size / GlowDiameter)*0.5f,
                    dotHdr, 255, fade, setHdr);
            }
        }
    }

    #endregion

    #region 绘制：爆燃

    /// <summary>
    /// 绘制落地爆燃的第 n 帧（n 从 1 开始）。ground 为落点，center 为领域中心，均为屏幕坐标；screen 为可见区域（用于裁掉屏幕外的冲击波段）。
    /// 要求调用方以 NonPremultiplied + Immediate 开始 SpriteBatch，并使用 FireBurstCast Shader。
    /// </summary>
    public static void DrawImpact(SpriteBatch batch, in FireBurstCastTextures tex, Vector2 ground, Vector2 center, float domainRadius, int n,
        FireBurstImpactParts parts, Rectangle screen, Action<float> setHdr)
    {
        if (n < 1) return;
        // ---- 可调参数 ----
        float flashPeak = 2f, flashFade = 6f, flashHdr = 1.5f;
        Vector2 flashSize = new(150, 70);
        float shockTicks = 16f, shockReach = 1.12f, shockHdr = 2.4f, shockGlowHdr = 0.8f;
        float shockWidth = 6f, shockGlowWidth = 14f;

        // 白闪：短暂冲顶，随后退去
        float flash = n < flashPeak ? n / flashPeak : Math.Max(0, 1 - (n - flashPeak) / flashFade);
        Sprite(batch, tex.GlowDot, null, ground + new Vector2(0, -6), White, 0, Center(tex.GlowDot),
            flashSize * (1 + n * 0.03f) / GlowDiameter, flashHdr, 255, flash, setHdr);

        // 冲击波细环：领在火环前面扫出屏幕
        float s = Math.Clamp(n / shockTicks, 0, 1);
        if (s < 1)
        {
            float radius = domainRadius * shockReach * OutCubic(s);
            DrawEllipse(batch, tex, center, radius, radius, 0, MathHelper.Lerp(shockWidth, 2, s), White, shockHdr, 120, 1 - s, screen, setHdr);
            DrawEllipse(batch, tex, center, Math.Max(0, radius - 8), Math.Max(0, radius - 8), 0, MathHelper.Lerp(shockGlowWidth, 4, s), Yellow, shockGlowHdr,
                120, (1 - s) * 0.5f, screen, setHdr);
        }

        DrawImpactParticles(batch, tex, center, n, parts, setHdr);
    }

    /// <summary>结尾星光：十字星与圆点从同一圆心向四周散开，减速、轻微闪烁并缩小淡出。</summary>
    public static void DrawImpactParticles(SpriteBatch batch, in FireBurstCastTextures tex, Vector2 center, float n,
        FireBurstImpactParts parts, Action<float> setHdr)
    {
        float starHdr = 1.8f, dotHdr = 2f, shrink = 0.5f, flicker = 0.18f;
        foreach (FireBurstImpactParticle particle in parts.Particles)
        {
            float age = n - particle.Delay;
            if (age < 0 || age >= particle.Life) continue;
            float life = 1 - age / particle.Life;
            Vector2 at = ImpactParticlePosition(particle, center, age);
            float size = particle.Size * (1 - shrink * (1 - life));
            float fade = MathF.Sqrt(life) * (1 - flicker * (0.5f + 0.5f * MathF.Sin(age * 0.3f + particle.Phase)));
            Color color = Color.Lerp(Orange, Yellow, life);
            if (particle.Star)
                Sprite(batch, tex.Star4, null, at, color, 0, Center(tex.Star4), new Vector2(size * 2 / tex.Star4.Width),
                    starHdr, 255, fade, setHdr);
            else
                Sprite(batch, tex.GlowDot, null, at, color, 0, Center(tex.GlowDot), new Vector2(size / GlowDiameter),
                    dotHdr, 255, fade, setHdr);
        }
    }

    /// <summary>爆燃第 n 帧的照明强度（0～1）。</summary>
    public static float ImpactLight(int n) => n < 3 ? 1 : Math.Max(0, 1 - (n - 3) / 40f);

    #endregion

    #region 基础

    // NonPremultiplied 会乘一次 Alpha；只衰减覆盖度，不能再衰减 RGB 导致柔边发灰。
    private static void Sprite(SpriteBatch batch, Texture2D texture, Rectangle? source, Vector2 at, Color color, float rotation, Vector2 origin,
        Vector2 scale, float hdr, byte coverage, float fade, Action<float> setHdr)
    {
        fade = Math.Clamp(fade, 0, 1);
        if (fade <= 0) return;
        setHdr(hdr);
        batch.Draw(texture, at, source, new Color(color.R, color.G, color.B, (byte)(coverage * fade)), rotation, origin, scale,
            SpriteEffects.None, 0);
    }

    /// <summary>ShotLineSPA 亮线带画成的线条，从 from 指向 to。</summary>
    public static void DrawStreak(SpriteBatch batch, in FireBurstCastTextures tex, Vector2 from, Vector2 to, float width, Color color, float hdr,
        byte coverage, Action<float> setHdr, float fade = 1)
    {
        Vector2 d = to - from;
        float length = d.Length();
        if (length < 0.5f) return;
        Sprite(batch, tex.Line, LineBand, (from + to) / 2, color, MathF.Atan2(d.Y, d.X), LineOrigin,
            new Vector2(length * 1.25f / LineLength, width / LineWidth), hdr, coverage, fade, setHdr);
    }

    /// <summary>
    /// 收细拖尾：沿路径（由头到尾）按弧长等距盖柔光点，宽度、亮度与覆盖度从头部向尾部线性收到零。
    /// 与 DrawEllipse 同样用盖点代替拼线段，弯曲处没有接缝；路径过短时不画。
    /// </summary>
    public static void DrawTaperTrail(SpriteBatch batch, in FireBurstCastTextures tex, Vector2[] path, float headWidth, Color color,
        float hdr, byte coverage, Action<float> setHdr)
    {
        if (path == null || path.Length < 2 || headWidth <= 0) return;
        float stampCoverage = 0.4f;   // 同 DrawEllipse：约 2.5 个点叠在同一处
        float minLength = 4f, minWidth = 0.6f;
        float total = 0;
        for (int i = 1; i < path.Length; i++) total += Vector2.Distance(path[i - 1], path[i]);
        if (total < minLength) return;

        setHdr(hdr);
        Vector2 origin = Center(tex.GlowDot);
        int seg = 0;
        float segStart = 0, segLength = Vector2.Distance(path[0], path[1]);
        for (float s = 0; s <= total;)
        {
            while (seg < path.Length - 2 && segStart + segLength < s)
            {
                segStart += segLength;
                seg++;
                segLength = Vector2.Distance(path[seg], path[seg + 1]);
            }
            Vector2 at = segLength > 0 ? Vector2.Lerp(path[seg], path[seg + 1], Math.Clamp((s - segStart) / segLength, 0, 1)) : path[seg];
            float k = 1 - s / total;
            float width = Math.Max(minWidth, headWidth * k);
            var tint = new Color(color.R, color.G, color.B, (byte)(coverage * stampCoverage * k));
            batch.Draw(tex.GlowDot, at, null, tint, 0, origin, new Vector2(width / GlowDiameter), SpriteEffects.None, 0);
            s += Math.Max(0.75f, width * 0.4f);
        }
    }

    /// <summary>
    /// （倾斜）椭圆细线：沿弧长等距盖柔光点。重叠沿整条线均匀，椭圆两端曲率大处也没有接缝
    /// （平头线段拼接时，相接处要么叠出横向亮痕，要么在转角外侧露出暗点）。clip 非空时跳过可见区域外的部分。
    /// </summary>
    public static void DrawEllipse(SpriteBatch batch, in FireBurstCastTextures tex, Vector2 at, float rx, float ry, float tilt, float width,
        Color color, float hdr, byte coverage, float fade, Rectangle? clip, Action<float> setHdr)
    {
        fade = Math.Clamp(fade, 0, 1);
        if (rx < 1 || fade <= 0 || width <= 0) return;
        float spacing = Math.Max(1f, width * 0.4f);       // 柔光点间距；约 2.5 个点叠在同一处
        float stampCoverage = 0.4f;
        Rectangle bounds = clip ?? default;
        if (clip.HasValue) bounds.Inflate((int)width + 48, (int)width + 48);
        // 按细分折线累计弧长，到达间距处盖点
        float perimeter = MathF.PI * (3 * (rx + ry) - MathF.Sqrt((3 * rx + ry) * (rx + 3 * ry)));
        int steps = Math.Clamp((int)(perimeter / spacing) * 3, 48, 40000);
        setHdr(hdr);
        var tint = new Color(color.R, color.G, color.B, (byte)(coverage * fade * stampCoverage));
        Vector2 origin = Center(tex.GlowDot), scale = new(width / GlowDiameter);
        Vector2 previous = at + Rotate(new Vector2(rx, 0), tilt);
        float travelled = spacing;
        for (int i = 1; i <= steps; i++)
        {
            float a = i * MathHelper.TwoPi / steps;
            Vector2 next = at + Rotate(new Vector2(MathF.Cos(a) * rx, MathF.Sin(a) * ry), tilt);
            travelled += Vector2.Distance(previous, next);
            previous = next;
            if (travelled < spacing) continue;
            travelled -= spacing;
            if (clip.HasValue && !bounds.Contains((int)next.X, (int)next.Y)) continue;
            batch.Draw(tex.GlowDot, next, null, tint, 0, origin, scale, SpriteEffects.None, 0);
        }
    }

    private static Vector2 Rotate(Vector2 v, float angle)
    {
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }

    private static Vector2 Center(Texture2D texture) => new(texture.Width / 2f, texture.Height / 2f);
    private static Vector2 Dir(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));
    private static float OutCubic(float x) => 1 - MathF.Pow(1 - x, 3);
    private static float SmoothStep(float x)
    {
        x = Math.Clamp(x, 0, 1);
        return x * x * (3 - 2 * x);
    }

    #endregion
}
