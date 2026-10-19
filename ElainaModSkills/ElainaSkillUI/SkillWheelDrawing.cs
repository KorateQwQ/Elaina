using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

// Shared by the live SUI wheel and the offline FNA preview.
internal static class SkillWheelDrawing
{
    public const int MaxItems = 8;
    // 环带半宽需容纳图标对角（≈35.4）放大 1.1 倍后再悬停外移 4px。
    public const float DeadZone = 30, BandHalf = 46, ScreenMargin = 24, IconSize = 50;
    public const float RingOpen = .2f, FadeIn = .14f, FadeOut = .16f, FocusFade = .12f;
    public const float ItemStagger = .016f, ItemOpen = .24f, ItemFade = .16f, ItemClose = .14f, ChosenClose = .34f;

    private static readonly Color BandColor = new(22, 18, 31), EdgeColor = new(197, 172, 220), DotColor = new(214, 194, 238),
        Pale = new(243, 230, 255), GlowColor = new(216, 184, 255), HubOuter = new(31, 26, 43), HubInner = new(56, 45, 70),
        HubEdge = new(209, 183, 221), Highlight = new(200, 166, 240), Shade = new(8, 6, 14);

    // Alpha-zero tints add light under the premultiplied batch; 先铺 Shade 暗底再叠光，亮背景下也不会过曝。
    private static readonly Color StarLight = new(222, 196, 255, 0), DiamondLight = new(236, 220, 255, 0);

    /// <summary>暗底浓度取光强的一部分：叠光后在任何背景上都不暗于原背景，只在亮光处收住过曝。</summary>
    private static float ShadeFor(float light) => Math.Min(.5f, light * .45f);

    public static float RadiusFor(int count) => count <= 2 ? 94 : count <= 4 ? 106 : count <= 6 ? 120 : 132;
    public static float HubRadiusFor(int count) => Math.Max(44, RadiusFor(count) - 50);

    /// <summary>顺时针、十二点方向为 0；两个术式时分列左右，与技能栏的阅读方向一致。</summary>
    public static float AngleOf(int index, int count) => count == 2
        ? (index == 0 ? MathHelper.TwoPi * .75f : MathHelper.PiOver2)
        : index * MathHelper.TwoPi / count;

    public static Vector2 Direction(float angle) => new(MathF.Sin(angle), -MathF.Cos(angle));

    /// <summary>绕轮盘中心缩放的绘制矩阵，左乘到宿主的 UI 变换之前。</summary>
    public static Matrix ScaleAround(Vector2 center, float scale) =>
        Matrix.CreateTranslation(-center.X, -center.Y, 0) * Matrix.CreateScale(scale, scale, 1)
        * Matrix.CreateTranslation(center.X, center.Y, 0);

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.2f, c3 = c1 + 1;
        float u = t - 1;
        return 1 + c3 * u * u * u + c1 * u * u;
    }

    internal static float EaseOutCubic(float t) => 1 - MathF.Pow(1 - Math.Clamp(t, 0, 1), 3);

    public static float OpeningRingScale(float seconds) =>
        MathHelper.Lerp(.82f, 1, EaseOutBack(Math.Clamp(seconds / RingOpen, 0, 1)));

    /// <summary>术式从中心附近依次飞出到环带上，返回相对轮盘半径的比例。</summary>
    public static float OpeningExtent(float seconds, int index) =>
        MathHelper.Lerp(.25f, 1, EaseOutBack(Math.Clamp((seconds - index * ItemStagger) / ItemOpen, 0, 1)));

    public static float OpeningOpacity(float seconds, int index) =>
        Math.Clamp((seconds - index * ItemStagger) / ItemFade, 0, 1);

    /// <summary>选中项留在原位淡出，其余项向内收拢。</summary>
    public static (float Extent, float Opacity) ClosingMotion(float closeSeconds, bool chosen, float extent, float opacity)
    {
        if (chosen) return (extent, opacity * (1 - Math.Clamp(closeSeconds / ChosenClose, 0, 1)));
        float t = Math.Clamp(closeSeconds / ItemClose, 0, 1);
        return (MathHelper.Lerp(extent, .6f, t), opacity * (1 - t));
    }

    /// <summary>离开中心死区后按最近方向选中，不要求指针压在图标上。</summary>
    public static int Pick(Vector2 offset, int count)
    {
        if (count == 0 || offset.Length() < DeadZone) return -1;
        float angle = MathF.Atan2(offset.X, -offset.Y);
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            float distance = Math.Abs(MathHelper.WrapAngle(angle - AngleOf(i, count)));
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = i;
        }
        return best;
    }

    public readonly record struct Art(SkillBarDrawing.StateArt State, Effect Cooldown, DynamicSpriteFont Number);

    /// <summary>角度沿用 cos/sin 约定（屏幕 y 向下，-π/2 为正上方）。</summary>
    public delegate void SectorPainter(Vector2 center, float inner, float outer, float start, float end,
        Color fill, float border, Color borderColor);

    public delegate void BatchRestarter(SpriteBatch batch, SpriteSortMode mode, Effect effect);

    public struct WheelItem
    {
        public Vector2 Position;
        public float Opacity, Hover, Flash, Cooldown, Remaining;
        public bool Current;
        public bool Sealed;
        public float SealDeniedAge;
        public int Slot, Stack, MaxStack;
        public Texture2D Icon;
    }

    public sealed class View
    {
        public readonly WheelItem[] Items = new WheelItem[MaxItems];
        public Vector2 Center, Pointer;
        /// <summary>Focus 为中心展示的术式：悬停项，或死区内的当前术式；FocusAge 为其切换后经过的秒数。</summary>
        public int Count, Hovered = -1, Focus = -1, Chosen = -1;
        public bool Open, ShowNumber;
        /// <summary>Release 为松开选中后的反馈进度 0→1，未选中时为 -1。</summary>
        public float Presence, RingScale = 1, Seconds, FocusAge, Release = -1;
        // Optional geometry for the healing target wheel. Zero retains the skill wheel's layout.
        public float Radius, HalfBand, HubRadius;
    }

    public static void Draw(SpriteBatch batch, in Art art, View view, SectorPainter sector, BatchRestarter restart)
    {
        if (view.Count == 0) return;
        float alpha = view.Presence;
        bool releasing = view.Release >= 0 && view.Chosen >= 0;
        if (alpha > 0 || releasing) DrawRing(batch, art, view, sector, alpha);
        DrawItems(batch, art, view, sector, restart);
        if (alpha > 0) DrawHub(batch, art, view, sector, restart, alpha);
    }

    /// <summary>只画环带、星点与法阵，项目由调用方自绘；松开选中后的光波在轮盘淡出后仍会播完。</summary>
    public static void DrawFrame(SpriteBatch batch, in Art art, View view, SectorPainter sector, BatchRestarter restart)
    {
        if (view.Count == 0) return;
        float alpha = view.Presence;
        if (alpha > 0 || view.Release >= 0 && view.Chosen >= 0) DrawRing(batch, art, view, sector, alpha);
        if (alpha > 0) DrawHub(batch, art, view, sector, restart, alpha);
    }

    private static (float Start, float End) SectorSpan(int index, int count)
    {
        const float top = -MathHelper.PiOver2;
        if (count <= 1) return (top, top + MathHelper.TwoPi);
        float half = MathF.PI / count, angle = AngleOf(index, count) + top;
        return (angle - half + .02f, angle + half - .02f);
    }

    private static void DrawRing(SpriteBatch batch, in Art art, View view, SectorPainter sector, float alpha)
    {
        int count = view.Count;
        Vector2 center = view.Center;
        float radius = view.Radius > 0 ? view.Radius : RadiusFor(count), scale = view.RingScale;
        float halfBand = view.HalfBand > 0 ? view.HalfBand : BandHalf;
        float r1 = (radius - halfBand) * scale, r2 = (radius + halfBand) * scale;
        const float top = -MathHelper.PiOver2, full = MathHelper.TwoPi;
        float release = view.Chosen >= 0 ? view.Release : -1;
        float afterglow = release >= 0 ? 1 - release : 0;

        if (alpha > 0)
        {
            sector(center, r1, r2, top, top + full, BandColor * (.88f * alpha), 1, EdgeColor * (.22f * alpha));
            float half = MathF.PI / count;
            if (count > 1)
                for (int i = 0; i < count; i++)
                {
                    Vector2 direction = Direction(AngleOf(i, count) + half);
                    Line(batch, art.State.Pixel, center + direction * (r1 + 6), center + direction * (r2 - 6), 1,
                        EdgeColor * (.15f * alpha));
                }

            // 当前术式：沿环带内沿的一道亮弧，与技能栏顶部星标区分开。
            for (int i = 0; i < count; i++)
            {
                if (!view.Items[i].Current) continue;
                var (start, end) = SectorSpan(i, count);
                sector(center, r1, r1 + 7, start, end, GlowColor * (.13f * alpha), 0, Color.Transparent);
                sector(center, r1, r1 + 2, start, end, Pale * (.75f * alpha), 0, Color.Transparent);
                Sparkle(batch, art, center + Direction(AngleOf(i, count)) * (r1 + 1), 11, alpha, view.Seconds);
            }

            if (view.Open && view.Hovered >= 0)
            {
                var (start, end) = SectorSpan(view.Hovered, count);
                sector(center, r1, r2, start, end, Highlight * (.18f * alpha), 0, Color.Transparent);
                sector(center, r2 - 4, r2 + 4, start, end, GlowColor * (.16f * alpha), 0, Color.Transparent);
                sector(center, r2 - 1, r2 + 1, start, end, Pale * alpha, 0, Color.Transparent);
            }
        }

        if (afterglow > 0)
        {
            // 松开选中：所选扇区闪白后淡出，外沿亮弧向两侧铺开。
            var (start, end) = SectorSpan(view.Chosen, count);
            if (alpha > 0) sector(center, r1, r2, start, end, Pale * (.3f * afterglow * alpha), 0, Color.Transparent);
            float mid = (start + end) / 2, spread = MathHelper.Lerp((end - start) / 2, MathF.PI * .45f, EaseOutCubic(release));
            sector(center, r2 - 1, r2 + 1, mid - spread, mid + spread, Pale * (.8f * afterglow * alpha), 0, Color.Transparent);
        }

        DrawStarField(batch, art, view, center, r2 + 9 * scale, alpha, release);
    }

    /// <summary>
    /// 外圈星点整体缓慢顺时针漂移，另有三道彗尾状亮光沿环追逐；
    /// 松开选中时再从所选方向向两侧推出一道光波。先整圈铺暗底再叠光，避免相邻光晕互相压暗。
    /// </summary>
    private static void DrawStarField(SpriteBatch batch, in Art art, View view, Vector2 center, float dotRadius,
        float alpha, float release)
    {
        int count = view.Count, dots = (int)(MathHelper.TwoPi * dotRadius / 8);
        float drift = view.Seconds * .12f, chase = view.Seconds * 1.15f;
        float chosenAngle = view.Chosen >= 0 ? AngleOf(view.Chosen, count) : 0;
        float wave = release >= 0 ? EaseOutCubic(release) * MathF.PI : 0, waveFade = release >= 0 ? 1 - release : 0;
        if (alpha <= 0 && waveFade <= 0) return;

        // 光波只在波前附近铺暗底，轮盘淡出后不会残留一圈暗点。
        float Burst(float angle) => waveFade <= 0 ? 0
            : MathF.Exp(-MathF.Pow((Math.Abs(MathHelper.WrapAngle(angle - chosenAngle)) - wave) / .2f, 2)) * waveFade;
        float Angle(int i) => i * MathHelper.TwoPi / dots + drift;
        float Comet(float angle)
        {
            float comet = 0;
            for (int k = 0; k < 3; k++)
            {
                // behind > 0 表示星点落在彗头后方（逆时针一侧），亮度沿尾迹二次衰减。
                float behind = MathHelper.WrapAngle(chase + k * MathHelper.TwoPi / 3 - angle);
                comet = Math.Max(comet, behind >= 0 ? MathF.Pow(Math.Max(0, 1 - behind / .9f), 2)
                    : Math.Max(0, 1 + behind / .05f));
            }
            return comet;
        }
        (float Light, float Swell, float Twinkle, float Comet) Dot(int i)
        {
            float angle = Angle(i), comet = Comet(angle), burst = Burst(angle);
            float twinkle = .5f + .5f * MathF.Sin(view.Seconds * 2.2f + i * 2.4f);
            return (alpha * (.16f + .1f * twinkle + .85f * comet) + burst * .9f, Math.Max(comet, burst), twinkle, comet);
        }
        (float Light, float Size) Diamond(int i)
        {
            bool on = view.Open && i == view.Hovered;
            float flare = i == view.Chosen ? waveFade : 0;
            float breath = .5f + .5f * MathF.Sin(view.Seconds * MathHelper.TwoPi / 3.6f + i * 1.3f);
            return ((on ? .85f : .4f + .15f * breath) * alpha + flare, (on ? 30 : 22) + 40 * flare * (1 - flare));
        }

        // 暗底与光晕同尺寸，浓度随光强走：光越亮压得越深，暗淡的星点几乎不压，外圈不会留下黑边。
        for (int i = 0; i < dots; i++)
        {
            var dot = Dot(i);
            Stretch(batch, art.State.Glow, center + Direction(Angle(i)) * dotRadius, new Vector2(8 + 9 * dot.Swell),
                Shade * ShadeFor(dot.Light));
        }
        for (int i = 0; i < count; i++)
        {
            var diamond = Diamond(i);
            Stretch(batch, art.State.Glow, center + Direction(AngleOf(i, count)) * dotRadius, new Vector2(diamond.Size),
                Shade * ShadeFor(diamond.Light));
        }

        for (int i = 0; i < dots; i++)
        {
            var dot = Dot(i);
            float burst = Burst(Angle(i));
            Vector2 position = center + Direction(Angle(i)) * dotRadius;
            Stretch(batch, art.State.Glow, position, new Vector2(8 + 9 * dot.Swell), StarLight * dot.Light);
            batch.Draw(art.State.Pixel, position, null,
                DotColor * Math.Min(1, alpha * (.55f + .25f * dot.Twinkle + .45f * dot.Comet) + burst),
                0, new Vector2(.5f), 1.25f + .75f * dot.Swell, SpriteEffects.None, 0);
        }

        for (int i = 0; i < count; i++)
        {
            bool on = view.Open && i == view.Hovered;
            float flare = i == view.Chosen ? waveFade : 0;
            if (alpha <= 0 && flare <= 0) continue;
            Vector2 position = center + Direction(AngleOf(i, count)) * dotRadius;
            var diamond = Diamond(i);
            Stretch(batch, art.State.Glow, position, new Vector2(diamond.Size), DiamondLight * diamond.Light);
            Color body = Color.Lerp(on ? new Color(250, 244, 255) : DotColor * .85f, new Color(250, 244, 255), flare);
            Stretch(batch, art.State.Diamond, position, on || flare > 0 ? new Vector2(8, 11) : new Vector2(7, 9.5f),
                body * Math.Max(alpha, flare));
        }
    }

    private static void DrawItems(SpriteBatch batch, in Art art, View view, SectorPainter sector, BatchRestarter restart)
    {
        float release = view.Chosen >= 0 ? view.Release : -1;
        for (int i = 0; i < view.Count; i++)
        {
            ref WheelItem item = ref view.Items[i];
            Halo(batch, art, item.Position, 86, item.Hover * item.Opacity);
        }

        if (release >= 0) ReleaseRings(sector, view.Items[view.Chosen].Position, release, 26, 58);

        for (int i = 0; i < view.Count; i++)
        {
            ref WheelItem item = ref view.Items[i];
            if (item.Icon == null || item.Opacity <= 0) continue;
            // 有悬停项时其余术式略微压暗，把视线引向指针所指方向。
            float dim = view.Open && view.Hovered >= 0 && i != view.Hovered ? .38f : 0;
            float pop = i == view.Chosen && release >= 0 ? .14f * MathF.Sin(MathF.PI * Math.Min(1, release * 1.6f)) : 0;
            DrawIcon(batch, art, restart, item, item.Position, IconSize * (1 + .1f * item.Hover + pop),
                Color.Lerp(Color.White, new Color(120, 112, 136), dim) * item.Opacity);
        }
        restart(batch, SpriteSortMode.Deferred, null);

        for (int i = 0; i < view.Count; i++)
        {
            ref WheelItem item = ref view.Items[i];
            if (view.Open && item.Opacity >= .99f)
                SkillBarDrawing.DrawSkillState(batch, art.State, art.Number, item.Position, item.Cooldown,
                    item.Remaining, item.Stack, item.MaxStack, view.ShowNumber && !item.Sealed);
            if (item.Sealed && item.Opacity > 0)
                SkillBarDrawing.DrawSealState(batch, art.State, item.Position, view.Seconds, item.SealDeniedAge, -1, item.Opacity);
        }
    }

    /// <summary>悬停项下的柔光：先铺暗底再叠光，亮背景下也不发白。</summary>
    internal static void Halo(SpriteBatch batch, in Art art, Vector2 position, float size, float strength)
    {
        if (strength <= 0) return;
        Stretch(batch, art.State.Glow, position, new Vector2(size), Shade * (.45f * strength));
        Stretch(batch, art.State.Glow, position, new Vector2(size), StarLight * (.4f * strength));
    }

    /// <summary>松开选中后从所选项向外扩散的两圈细环，release 为 0→1 的反馈进度。</summary>
    internal static void ReleaseRings(SectorPainter sector, Vector2 position, float release, float from, float to)
    {
        for (int k = 0; k < 2; k++)
        {
            float t = Math.Clamp(release * 1.25f - k * .25f, 0, 1);
            if (t <= 0 || t >= 1) continue;
            float ring = MathHelper.Lerp(from, to, EaseOutCubic(t));
            sector(position, ring - .75f, ring + .75f, 0, MathHelper.TwoPi, Pale * (.8f * (1 - t)), 0, Color.Transparent);
        }
    }

    /// <summary>经 CDEffect 绘制图标；调用方负责之后把批次切回 Deferred。</summary>
    private static void DrawIcon(SpriteBatch batch, in Art art, BatchRestarter restart, in WheelItem item,
        Vector2 position, float size, Color color)
    {
        art.Cooldown.Parameters["time"]?.SetValue(1 - item.Remaining);
        art.Cooldown.Parameters["flashTime"]?.SetValue(item.Flash);
        art.Cooldown.Parameters["ready"]?.SetValue(item.Stack > 0 ? 1f : 0f);
        art.Cooldown.Parameters["seal"]?.SetValue(item.Sealed ? 1f : 0f);
        restart(batch, SpriteSortMode.Immediate, art.Cooldown);
        batch.Draw(item.Icon, position, null, color, 0, new Vector2(item.Icon.Width, item.Icon.Height) / 2,
            size / Math.Max(item.Icon.Width, item.Icon.Height), SpriteEffects.None, 0);
    }

    /// <summary>中心是一枚缓慢转动的法阵，阵心放大展示所指术式；指针回到死区时淡显当前术式，表示松开即保留。</summary>
    private static void DrawHub(SpriteBatch batch, in Art art, View view, SectorPainter sector, BatchRestarter restart, float alpha)
    {
        Vector2 center = view.Center;
        float baseHub = view.HubRadius > 0 ? view.HubRadius : HubRadiusFor(view.Count);
        float hub = baseHub * MathHelper.Lerp(.8f, 1, Math.Min(1, view.RingScale));
        const float top = -MathHelper.PiOver2, full = MathHelper.TwoPi;
        Stretch(batch, art.State.Glow, center, new Vector2((hub + 30) * 2), new Color(173, 121, 206) * (.1f * alpha));
        float bandInner = ((view.Radius > 0 ? view.Radius : RadiusFor(view.Count))
            - (view.HalfBand > 0 ? view.HalfBand : BandHalf)) * view.RingScale;
        if (bandInner > hub)
            sector(center, hub, bandInner, top, top + full, BandColor * (.5f * alpha), 0, Color.Transparent);
        sector(center, 0, hub, top, top + full, HubOuter * (.91f * alpha), 1, HubEdge * (.21f * alpha));
        Stretch(batch, art.State.Glow, center, new Vector2(hub * 1.9f), HubInner * (.75f * alpha));

        float circle = hub - 8;
        sector(center, circle - .5f, circle + .5f, top, top + full, EdgeColor * (.2f * alpha), 0, Color.Transparent);
        sector(center, circle * .7f - .5f, circle * .7f + .5f, top, top + full, EdgeColor * (.1f * alpha), 0, Color.Transparent);
        const int runes = 16;
        for (int i = 0; i < runes; i++)
        {
            bool major = i % 4 == 0;
            float angle = i * full / runes + view.Seconds * .3f;
            Vector2 position = center + Direction(angle) * circle;
            batch.Draw(art.State.Diamond, position, null, EdgeColor * ((major ? .5f : .25f) * alpha), angle,
                new Vector2(art.State.Diamond.Width, art.State.Diamond.Height) / 2,
                new Vector2((major ? 4f : 2.5f) / art.State.Diamond.Width, (major ? 7f : 4f) / art.State.Diamond.Height),
                SpriteEffects.None, 0);
        }
        for (int i = 0; i < 6; i++)
        {
            float angle = i * full / 6 - view.Seconds * .45f;
            Stretch(batch, art.State.Diamond, center + Direction(angle) * circle * .7f, new Vector2(2.5f), EdgeColor * (.3f * alpha));
        }

        if (view.Focus >= 0 && view.Focus < view.Count && view.Items[view.Focus].Icon != null)
        {
            ref WheelItem item = ref view.Items[view.Focus];
            bool pointed = view.Focus == view.Hovered || view.Focus == view.Chosen;
            float fade = Math.Clamp(view.FocusAge / FocusFade, 0, 1);
            float size = Math.Min(56, circle * 1.15f) * MathHelper.Lerp(.82f, 1, EaseOutBack(fade));
            float strength = (pointed ? 1 : .42f) * fade * alpha;
            if (pointed)
            {
                Stretch(batch, art.State.Glow, center, new Vector2(size * 1.9f), Shade * (.55f * strength));
                Stretch(batch, art.State.Glow, center, new Vector2(size * 1.9f), StarLight * (.3f * strength));
            }
            DrawIcon(batch, art, restart, item, center, size, (pointed ? Color.White : new Color(170, 160, 186)) * strength);
            restart(batch, SpriteSortMode.Deferred, null);
            if (item.Sealed)
                SkillBarDrawing.DrawSealState(batch, art.State, center, view.Seconds, item.SealDeniedAge, -1, strength);
        }

        // 指针只在确有选中项时出现；治疗盘的取消区比技能盘死区大，不能按固定死区判断。
        Vector2 offset = view.Pointer - center;
        if (view.Open && view.Hovered >= 0 && offset.LengthSquared() > 0)
        {
            float angle = MathF.Atan2(offset.X, -offset.Y);
            Vector2 needle = center + Direction(angle) * (hub + 3);
            Stretch(batch, art.State.Glow, needle, new Vector2(18), GlowColor * (.45f * alpha));
            batch.Draw(art.State.Diamond, needle, null, Pale * alpha, angle,
                new Vector2(art.State.Diamond.Width, art.State.Diamond.Height) / 2,
                new Vector2(8f / art.State.Diamond.Width, 15f / art.State.Diamond.Height), SpriteEffects.None, 0);
        }
    }

    /// <summary>两枚细长菱形交叉成的四芒星，带暗底与缓慢呼吸的柔光。</summary>
    private static void Sparkle(SpriteBatch batch, in Art art, Vector2 position, float size, float alpha, float seconds)
    {
        float breath = .5f + .5f * MathF.Sin(seconds * MathHelper.TwoPi / 2.4f);
        float light = (.45f + .25f * breath) * alpha;
        Stretch(batch, art.State.Glow, position, new Vector2(size * 2), Shade * ShadeFor(light));
        Stretch(batch, art.State.Glow, position, new Vector2(size * 2), StarLight * light);
        Stretch(batch, art.State.Diamond, position, new Vector2(size * .28f, size), Pale * alpha);
        Stretch(batch, art.State.Diamond, position, new Vector2(size, size * .28f), Pale * alpha);
    }

    private static void Line(SpriteBatch batch, Texture2D pixel, Vector2 a, Vector2 b, float thickness, Color color)
    {
        Vector2 delta = b - a;
        batch.Draw(pixel, a, null, color, MathF.Atan2(delta.Y, delta.X),
            new Vector2(0, .5f), new Vector2(delta.Length(), thickness), SpriteEffects.None, 0);
    }

    private static void Stretch(SpriteBatch batch, Texture2D texture, Vector2 center, Vector2 size, Color color)
    {
        Vector2 source = new(texture.Width, texture.Height);
        batch.Draw(texture, center, null, color, 0, source / 2, size / source, SpriteEffects.None, 0);
    }
}
