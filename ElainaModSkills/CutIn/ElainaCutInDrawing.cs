using System;

namespace 伊蕾娜.ElainaModSkills.CutIn;

/// <summary>
/// 奥义特写的绘制。整条特写带用顶点网格画在物理屏幕像素上：
/// 带底渐变、速度线、光晕都靠顶点色；星芒、细圆环、带边亮线用项目已有的预制贴图；人像帧用 Tools/CutInBake 烘焙的预乘 PNG。
/// 带内的内容（人像、星点、速度线、闪白、杖尖星芒）按带的两条边裁剪成多边形后绘制，所以人像保持竖直、带子斜着切。
/// 所有颜色为预乘值，AlphaBlend 混合。只依赖 FNA，游戏接入在 <see cref="ElainaCutInSystem"/>，离线预览在 Tools/CutInPreview。
/// </summary>
internal static class ElainaCutInDrawing
{
    // ---- 构图（热重载后立即生效）----
    private static float AngleDegrees => 8f;              // 带子倾角：左低右高
    private static float BandSourceWidth => 200f;          // 带宽（垂直于带方向），以源帧像素计
    // 源帧中对齐到带中心线的点，以眼睛为准（带宽 200 时带内只有锚点上下各 100 源像素，放不下杖头）：
    // 冲击前 1-13 帧眼睛在源图 y 108-212，冲击后 14-21 帧在 y 184-238。切换被冲击闪白盖住。
    // 改带宽或倾角后须重新核对：眼睛上沿 ≥ 锚点 y - 带宽/2 + tan(倾角)·132（左眼相对锚点的横距）。
    private static Vector2 PortraitAnchorIntro => new(470, 180);
    private static Vector2 PortraitAnchorStrike => new(470, 240);
    private static float PortraitAlong => .06f;            // 锚点沿带方向相对屏幕中心的偏移（屏宽比例）
    private static Vector2 WandTip => new(268, 490);       // 定格帧杖头（源帧像素），冲击星芒中心
    private static float PlayerClearance => 200f;           // 带下沿与玩家头顶的间距（UI 缩放 1 时的像素）

    /// <summary>预乘贴图：白像素、PortalLine 线条横截面、StarFlare 星芒、ThinRing 细圆环、当前人像帧（未加载为 null）。</summary>
    internal readonly record struct Textures(Texture2D White, Texture2D Line, Texture2D Star, Texture2D Ring, Texture2D Portrait);

    /// <summary>
    /// 一次绘制的输入：屏幕物理尺寸、UI 缩放、玩家头顶屏幕纵坐标，以及播放时间、冲击时间（未冲击为负）、退场开始时间（未冲击为正无穷）、退场时长，单位秒。
    /// </summary>
    internal readonly record struct Frame(float Width, float Height, float UIScale, float PlayerHeadY,
        float Time, float StrikeTime, float ExitStart, float ExitDuration);

    private static readonly List<VertexPositionColorTexture> Vertices = new();
    private static Texture2D batchTexture;
    private static GraphicsDevice batchDevice;
    private static BasicEffect batchEffect;
    private static Texture2D lineTexture;

    private readonly record struct StarSeed(float U, float V, float Size, float Phase, float Speed);
    private readonly record struct StreakSeed(float U, float V, float Length, float Speed, float Alpha);
    private readonly record struct OuterSeed(float U, float Offset, float Length, float Width, float Speed);

    private static readonly StarSeed[] Stars = CreateStars();
    private static readonly StreakSeed[] Streaks = CreateStreaks();
    private static readonly OuterSeed[] Outers = CreateOuters();

    private static StarSeed[] CreateStars()
    {
        var r = new Random(7);
        var a = new StarSeed[70];
        for (int i = 0; i < a.Length; i++)
            a[i] = new(r.NextSingle(), r.NextSingle() * 2 - 1, .5f + r.NextSingle() * 1.2f, r.NextSingle() * 6.28f, .6f + r.NextSingle() * .8f);
        return a;
    }

    private static StreakSeed[] CreateStreaks()
    {
        var r = new Random(31);
        var a = new StreakSeed[22];
        for (int i = 0; i < a.Length; i++)
            a[i] = new(r.NextSingle(), r.NextSingle() * 2 - 1, .08f + r.NextSingle() * .22f, 1.6f + r.NextSingle() * 1.8f, .25f + r.NextSingle() * .45f);
        return a;
    }

    private static OuterSeed[] CreateOuters()
    {
        var r = new Random(97);
        var a = new OuterSeed[8];
        for (int i = 0; i < a.Length; i++)
            a[i] = new(r.NextSingle(), (r.NextSingle() < .5f ? -1 : 1) * (.62f + r.NextSingle() * .55f), .12f + r.NextSingle() * .25f,
                .5f + r.NextSingle() * 1.5f, 2.2f + r.NextSingle() * 1.5f);
        return a;
    }

    // ---- 缓动 ----
    private static float Clamp01(float x) => Math.Clamp(x, 0f, 1f);
    private static float Seg(float t, float a, float b) => Clamp01((t - a) / (b - a));
    private static float OutCubic(float x) => 1 - MathF.Pow(1 - x, 3);
    private static float InCubic(float x) => x * x * x;
    private static float OutBack(float x) { const float c1 = 1.5f, c3 = c1 + 1; return 1 + c3 * MathF.Pow(x - 1, 3) + c1 * MathF.Pow(x - 1, 2); }
    private static float Wrap(float x, float m) => (x % m + m) % m;

    // ---- 每次绘制的几何状态 ----
    private static Vector2 bandOrigin, dir, nrm;
    private static float halfT;

    private static Vector2 Band(float x, float y) => bandOrigin + dir * x + nrm * y;

    /// <summary>调用前须结束 SpriteBatch；设备状态（混合、深度、光栅、采样 0）由本方法设置，之后由调用方重新 Begin 恢复。</summary>
    internal static void Draw(GraphicsDevice device, BasicEffect effect, Textures textures, Frame frame)
    {
        batchDevice = device;
        batchEffect = effect;
        batchTexture = null;
        lineTexture = textures.Line;
        float W = frame.Width, H = frame.Height;
        float t = frame.Time;
        float k = frame.UIScale;
        float s = ElainaCutIn.BaseScale * k;
        float T = BandSourceWidth * s;
        float theta = MathHelper.ToRadians(AngleDegrees);
        dir = new Vector2(MathF.Cos(theta), -MathF.Sin(theta));
        nrm = new Vector2(MathF.Sin(theta), MathF.Cos(theta));
        float L = MathF.Sqrt(W * W + H * H) * 1.2f;

        // 带中心：屏幕水平中点，纵向放在玩家头顶上方（张开回弹约 9%）
        float bandY = frame.PlayerHeadY - PlayerClearance * k - T * .5f * 1.09f / MathF.Cos(theta);

        float strikeAt = frame.StrikeTime;
        float exitStart = frame.ExitStart, exitEnd = exitStart + frame.ExitDuration;
        float sinceStrike = strikeAt >= 0 ? t - strikeAt : -1;
        float open = t < exitStart ? OutBack(Seg(t, .06f, .26f)) : 1 - InCubic(Seg(t, exitStart, exitEnd));
        float dim = MathF.Min(OutCubic(Seg(t, 0, .2f)), 1 - Seg(t, exitStart + .08f, exitEnd));
        float shake = sinceStrike > 0 && sinceStrike < .18f ? (1 - sinceStrike / .18f) * 6 * k : 0;
        Vector2 shakeOffset = new(shake * MathF.Sin(t * 190), shake * MathF.Cos(t * 230));
        bandOrigin = new Vector2(W * .5f, bandY) + shakeOffset;
        halfT = T * .5f * MathF.Max(open, 0);

        device.BlendState = BlendState.AlphaBlend;
        device.DepthStencilState = DepthStencilState.None;
        device.RasterizerState = RasterizerState.CullNone;
        effect.TextureEnabled = true;
        effect.VertexColorEnabled = true;
        effect.World = Matrix.Identity;
        effect.View = Matrix.Identity;
        effect.Projection = Matrix.CreateOrthographicOffCenter(0, W, H, 0, 0, 1);

        Texture2D white = textures.White;

        // 压暗游戏画面
        Quad(white, Vector2.Zero, new Vector2(W, 0), new Vector2(W, H), new Vector2(0, H), new Color(8, 4, 20) * (.42f * dim), clip: false);

        // 外侧细线（入场速度感）
        float outerA = MathF.Min(Seg(t, .04f, .14f), 1 - Seg(t, .5f, .8f))
            + (t > exitStart ? Seg(t, exitStart, exitStart + .1f) * (1 - Seg(t, exitStart + .1f, exitEnd)) : 0);
        if (outerA > 0)
            foreach (OuterSeed o in Outers)
            {
                float x = Wrap(o.U - t * o.Speed, 1.2f) * L - L * .6f;
                float y = o.Offset * T * .5f * MathF.Max(open, .3f);
                BandRect(white, x, y, x + o.Length * L * .4f, y + o.Width * k, new Color(233, 226, 255) * (outerA * .7f), clip: false);
            }

        // 光缝：先从左到右划过一道细线
        float slit = Seg(t, 0, .10f);
        if (slit > 0 && open < .98f && t < exitStart)
            GlowLine(white, -L / 2, -L / 2 + L * OutCubic(slit), 0, 1f, k);

        if (halfT > .5f)
        {
            // 背后的错位副带
            BandRect(white, -L / 2, -halfT - 14 * k * open, L / 2, -halfT - 6 * k * open, new Color(120, 86, 210) * .38f, clip: false);
            BandRect(white, -L / 2, halfT + 8 * k * open, L / 2, halfT + 13 * k * open, new Color(160, 130, 240) * .30f, clip: false);

            // 带底：上 → 中 → 下 三段竖直渐变
            Color top = new(42, 29, 85), mid = new(59, 42, 120), bottom = new(26, 19, 56);
            QuadColors(white, Band(-L / 2, -halfT), Band(L / 2, -halfT), Band(L / 2, 0), Band(-L / 2, 0), top, top, mid, mid, clip: false);
            QuadColors(white, Band(-L / 2, 0), Band(L / 2, 0), Band(L / 2, halfT), Band(-L / 2, halfT), mid, mid, bottom, bottom, clip: false);

            // 星点
            Texture2D star = textures.Star;
            foreach (StarSeed st in Stars)
            {
                float x = Wrap(st.U - t * .06f * st.Speed, 1) * L - L / 2;
                float twinkle = .45f + .55f * MathF.Sin(t * 6 * st.Speed + st.Phase);
                Sprite(star, Band(x, st.V * T * .5f), (2 + st.Size * 3) * k * 1.1f, 0, new Color(232, 224, 255) * (.55f * Math.Max(twinkle, 0)));
            }

            // 沿带方向的速度线：尾部透明 → 头部亮
            foreach (StreakSeed sk in Streaks)
            {
                float x = Wrap(sk.U - t * sk.Speed * .35f, 1.3f) * L - L * .65f;
                float y = sk.V * T * .48f, len = sk.Length * L * .5f;
                Color head = new Color(230, 222, 255) * sk.Alpha;
                QuadColors(white, Band(x, y), Band(x + len, y), Band(x + len, y + 1.5f * k), Band(x, y + 1.5f * k),
                    Color.Transparent, head, head, Color.Transparent, clip: true);
            }

            // 人像：保持竖直，只沿带方向平移
            float enter = 1 - OutCubic(Seg(t, .08f, .42f));
            float exitP = InCubic(Seg(t, exitStart, exitEnd));
            float drift = 26 * s * MathF.Min(1, Math.Max(0, t - .3f) / 1.22f);
            float along = W * .16f * enter - drift - W * .35f * exitP;
            float punch = sinceStrike > 0 ? 1 + .055f * MathF.Exp(-sinceStrike * 9) + .02f * Clamp01(sinceStrike / .3f) : 1;
            float sc = s * punch;
            Vector2 anchor = bandOrigin + dir * (W * PortraitAlong + along);
            Vector2 portraitAnchor = sinceStrike >= 0 ? PortraitAnchorStrike : PortraitAnchorIntro;
            if (textures.Portrait is { } tex)
            {
                Vector2 tl = anchor - portraitAnchor * sc, size = new Vector2(tex.Width, tex.Height) * sc;
                Quad(tex, tl, tl + new Vector2(size.X, 0), tl + size, tl + new Vector2(0, size.Y), Color.White, clip: true);
            }

            // 冲击闪白
            if (sinceStrike > 0 && sinceStrike < .16f)
                BandRect(white, -L / 2, -halfT, L / 2, halfT, new Color(245, 240, 255) * (.85f * (1 - sinceStrike / .16f)), clip: true);

            // 带边缘亮线
            GlowLine(white, -L / 2, L / 2, -halfT - .5f * k, 1f, k);
            GlowLine(white, -L / 2, L / 2, halfT + .5f * k, 1f, k);

            // 冲击：杖头星芒与光环（裁剪在带内，不压到玩家）
            if (sinceStrike > 0 && sinceStrike < .45f && t < exitStart)
            {
                float p = sinceStrike / .45f, e = OutCubic(p);
                Vector2 tip = anchor + (WandTip - portraitAnchor) * sc;
                // 带子放不下杖头时，星芒沿带法向收进带内（保持杖头所在的横向位置），否则会被裁掉看不见
                float across = Vector2.Dot(tip - bandOrigin, nrm), limit = halfT * .7f;
                tip -= nrm * (across - Math.Clamp(across, -limit, limit));
                Sprite(star, tip, (40 + 260 * e) * k * 1.1f, .2f * p, Color.White * (1 - p), clip: true);
                Sprite(star, tip, (20 + 130 * e) * k * 1.1f, MathHelper.PiOver4 + .2f * p, new Color(217, 204, 255) * ((1 - p) * .8f), clip: true);
                // ThinRing 的环半径为贴图半宽的 120/128
                Sprite(textures.Ring, tip, (30 + 340 * e) * k * 128f / 120f, 0, new Color(230, 220, 255) * ((1 - p) * .9f), clip: true);
            }
        }

        // 退场：收拢后残留的光线向右射出
        float exitLine = Seg(t, exitStart + .12f, exitEnd);
        if (exitLine > 0 && exitLine < 1)
            GlowLine(white, -L / 2 + L * InCubic(exitLine), L / 2, 0, 1 - exitLine, k);

        FlushBatch();
        batchDevice = null;
        batchEffect = null;
        batchTexture = null;
        lineTexture = null;
    }

    /// <summary>沿带方向的发光线：顶点色柔光 + PortalLine 横截面亮芯。</summary>
    private static void GlowLine(Texture2D white, float x0, float x1, float y, float alpha, float k)
    {
        if (x1 <= x0 || alpha <= 0) return;
        float glow = 12 * k;
        Color glowColor = new Color(169, 140, 255) * (.45f * alpha);
        QuadColors(white, Band(x0, y - glow), Band(x1, y - glow), Band(x1, y), Band(x0, y), Color.Transparent, Color.Transparent, glowColor, glowColor, clip: false);
        QuadColors(white, Band(x0, y), Band(x1, y), Band(x1, y + glow), Band(x0, y + glow), glowColor, glowColor, Color.Transparent, Color.Transparent, clip: false);
        float core = 1.8f * k + .6f;
        QuadUV(lineTexture, Band(x0, y - core), Band(x1, y - core), Band(x1, y + core), Band(x0, y + core),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), new Color(244, 239, 255) * alpha, clip: false);
    }

    private static void BandRect(Texture2D tex, float x0, float y0, float x1, float y1, Color color, bool clip) =>
        Quad(tex, Band(x0, y0), Band(x1, y0), Band(x1, y1), Band(x0, y1), color, clip);

    private static void Sprite(Texture2D tex, Vector2 center, float halfSize, float rotation, Color color, bool clip = true)
    {
        Vector2 a = new Vector2(MathF.Cos(rotation), MathF.Sin(rotation)) * halfSize;
        Vector2 b = new Vector2(-a.Y, a.X);
        Quad(tex, center - a - b, center + a - b, center + a + b, center - a + b, color, clip);
    }

    private static void Quad(Texture2D tex, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color color, bool clip) =>
        QuadUV(tex, p0, p1, p2, p3, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), color, clip);

    private static void QuadUV(Texture2D tex, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
        Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3, Color color, bool clip) =>
        Polygon(tex, clip, V(p0, uv0, color), V(p1, uv1, color), V(p2, uv2, color), V(p3, uv3, color));

    private static void QuadColors(Texture2D tex, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color c0, Color c1, Color c2, Color c3, bool clip) =>
        Polygon(tex, clip, V(p0, new(0, 0), c0), V(p1, new(1, 0), c1), V(p2, new(1, 1), c2), V(p3, new(0, 1), c3));

    private static VertexPositionColorTexture V(Vector2 p, Vector2 uv, Color c) => new(new Vector3(p, 0), c, uv);

    // ---- 带内裁剪：多边形依次被带的上、下两条边裁切，再按扇形三角化 ----
    private static readonly List<VertexPositionColorTexture> ClipA = new(), ClipB = new();

    private static void Polygon(Texture2D tex, bool clip, params VertexPositionColorTexture[] poly)
    {
        ClipA.Clear();
        ClipA.AddRange(poly);
        if (clip)
        {
            ClipHalfPlane(ClipA, ClipB, +1);
            ClipHalfPlane(ClipB, ClipA, -1);
        }
        if (ClipA.Count < 3) return;
        if (tex != batchTexture) { FlushBatch(); batchTexture = tex; }
        for (int i = 1; i < ClipA.Count - 1; i++)
        {
            Vertices.Add(ClipA[0]);
            Vertices.Add(ClipA[i]);
            Vertices.Add(ClipA[i + 1]);
        }
    }

    /// <summary>保留 side·(带法向距离) ≤ halfT 的部分（Sutherland–Hodgman）。</summary>
    private static void ClipHalfPlane(List<VertexPositionColorTexture> input, List<VertexPositionColorTexture> output, int side)
    {
        output.Clear();
        int n = input.Count;
        for (int i = 0; i < n; i++)
        {
            VertexPositionColorTexture a = input[i], b = input[(i + 1) % n];
            float da = Distance(a, side), db = Distance(b, side);
            if (da <= 0) output.Add(a);
            if ((da <= 0) != (db <= 0))
            {
                float f = da / (da - db);
                output.Add(new VertexPositionColorTexture(
                    Vector3.Lerp(a.Position, b.Position, f),
                    Color.Lerp(a.Color, b.Color, f),
                    Vector2.Lerp(a.TextureCoordinate, b.TextureCoordinate, f)));
            }
        }
    }

    private static float Distance(VertexPositionColorTexture v, int side) =>
        side * Vector2.Dot(new Vector2(v.Position.X, v.Position.Y) - bandOrigin, nrm) - halfT;

    private static void FlushBatch()
    {
        if (Vertices.Count == 0 || batchTexture == null) { Vertices.Clear(); return; }
        batchEffect.Texture = batchTexture;
        batchEffect.CurrentTechnique.Passes[0].Apply();
        batchDevice.SamplerStates[0] = SamplerState.LinearClamp;
        VertexPositionColorTexture[] array = Vertices.ToArray();
        batchDevice.DrawUserPrimitives(PrimitiveType.TriangleList, array, 0, array.Length / 3);
        Vertices.Clear();
    }
}
