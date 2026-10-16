using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

// Shared by the live SUI wheel and the offline FNA preview.
internal static class SkillWheelDrawing
{
    public const int MaxItems = 8;
    public const float DeadZone = 30, BandHalf = 40, ScreenMargin = 24, IconSize = 50;
    public const float RingOpen = .2f, FadeIn = .14f, FadeOut = .16f;
    public const float ItemStagger = .016f, ItemOpen = .24f, ItemFade = .16f, ItemClose = .14f, ChosenClose = .26f;

    private static readonly Color BandColor = new(22, 18, 31), EdgeColor = new(197, 172, 220), DotColor = new(205, 182, 230),
        Pale = new(243, 230, 255), GlowColor = new(216, 184, 255), HubOuter = new(31, 26, 43), HubInner = new(56, 45, 70),
        HubEdge = new(209, 183, 221), NameColor = new(241, 230, 251), Muted = new(165, 147, 182), Highlight = new(200, 166, 240);

    public static float RadiusFor(int count) => count <= 2 ? 86 : count <= 4 ? 98 : count <= 6 ? 112 : 124;
    public static float HubRadiusFor(int count) => Math.Max(40, RadiusFor(count) - 46);

    /// <summary>顺时针、十二点方向为 0；两个术式时分列左右，与技能栏的阅读方向一致。</summary>
    public static float AngleOf(int index, int count) => count == 2
        ? (index == 0 ? MathHelper.TwoPi * .75f : MathHelper.PiOver2)
        : index * MathHelper.TwoPi / count;

    public static Vector2 Direction(float angle) => new(MathF.Sin(angle), -MathF.Cos(angle));

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.2f, c3 = c1 + 1;
        float u = t - 1;
        return 1 + c3 * u * u * u + c1 * u * u;
    }

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

    public readonly record struct Art(Texture2D Ornaments, SkillBarDrawing.StateArt State, Texture2D Veil, Effect Cooldown,
        DynamicSpriteFont Title, DynamicSpriteFont Body, DynamicSpriteFont Number);

    /// <summary>角度沿用 cos/sin 约定（屏幕 y 向下，-π/2 为正上方）。</summary>
    public delegate void SectorPainter(Vector2 center, float inner, float outer, float start, float end,
        Color fill, float border, Color borderColor);

    public delegate void BatchRestarter(SpriteBatch batch, SpriteSortMode mode, Effect effect);

    public struct WheelItem
    {
        public Vector2 Position;
        public float Opacity, Hover, Flash, Cooldown, Remaining;
        public bool Current, Chosen;
        public int Slot, Stack, MaxStack;
        public Texture2D Icon;
    }

    public sealed class View
    {
        public readonly WheelItem[] Items = new WheelItem[MaxItems];
        public Vector2 Center, Pointer, ScreenSize;
        public int Count, Hovered = -1;
        public bool Open, ShowNumber;
        public float Presence, RingScale = 1, Seconds;
        public string Name, Sub;
    }

    public static void Draw(SpriteBatch batch, in Art art, View view, SectorPainter sector, BatchRestarter restart)
    {
        if (view.Count == 0) return;
        float alpha = view.Presence;
        if (alpha > 0)
        {
            DrawVeil(batch, art.Veil, view, alpha);
            DrawRing(batch, art, view, sector, alpha);
        }
        DrawItems(batch, art, view, restart);
        if (alpha > 0) DrawHub(batch, art, view, sector, alpha);
    }

    public static Color[] VeilPixels(int size)
    {
        // 屏幕对角线为单位半径：中心保持通透，向外逐渐压暗，聚焦轮盘。
        var data = new Color[size * size];
        float half = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = new Vector2(x - half, y - half).Length() / half;
            float a = d < .073f ? MathHelper.Lerp(0, .2f, d / .073f)
                : MathHelper.Lerp(.2f, .72f, Math.Clamp((d - .073f) / .277f, 0, 1));
            data[y * size + x] = new Color(11, 9, 18) * a;
        }
        return data;
    }

    private static void DrawVeil(SpriteBatch batch, Texture2D veil, View view, float alpha)
    {
        float reach = view.ScreenSize.Length();
        batch.Draw(veil, view.Center, null, Color.White * alpha, 0, new Vector2(veil.Width / 2f),
            reach * 2 / veil.Width, SpriteEffects.None, 0);
    }

    private static void DrawRing(SpriteBatch batch, in Art art, View view, SectorPainter sector, float alpha)
    {
        int count = view.Count;
        Vector2 center = view.Center;
        float radius = RadiusFor(count), scale = view.RingScale;
        float r1 = (radius - BandHalf) * scale, r2 = (radius + BandHalf) * scale;
        const float top = -MathHelper.PiOver2, full = MathHelper.TwoPi;
        sector(center, r1, r2, top, top + full, BandColor * (.82f * alpha), 1, EdgeColor * (.22f * alpha));

        float dotRadius = r2 + 9 * scale;
        int dots = (int)(MathHelper.TwoPi * dotRadius / 8);
        for (int i = 0; i < dots; i++)
            batch.Draw(art.State.Pixel, center + Direction(i * MathHelper.TwoPi / dots) * dotRadius, null,
                DotColor * (.4f * alpha), 0, new Vector2(.5f), 1, SpriteEffects.None, 0);

        float half = MathF.PI / count;
        if (count > 1)
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = Direction(AngleOf(i, count) + half);
                Line(batch, art.State.Pixel, center + direction * (r1 + 6), center + direction * (r2 - 6), 1,
                    EdgeColor * (.15f * alpha));
            }

        for (int i = 0; i < count; i++)
        {
            bool on = view.Open && i == view.Hovered;
            Stretch(batch, art.State.Diamond, center + Direction(AngleOf(i, count)) * dotRadius, new Vector2(6, 8),
                (on ? new Color(246, 236, 255) : DotColor * .5f) * alpha);
        }

        if (!view.Open || view.Hovered < 0) return;
        float start = top, end = top + full;
        if (count > 1)
        {
            float angle = AngleOf(view.Hovered, count) + top;
            start = angle - half + .02f;
            end = angle + half - .02f;
        }
        sector(center, r1, r2, start, end, Highlight * (.18f * alpha), 0, Color.Transparent);
        sector(center, r2 - 4, r2 + 4, start, end, GlowColor * (.16f * alpha), 0, Color.Transparent);
        sector(center, r2 - 1, r2 + 1, start, end, Pale * alpha, 0, Color.Transparent);
    }

    private static void DrawHub(SpriteBatch batch, in Art art, View view, SectorPainter sector, float alpha)
    {
        Vector2 center = view.Center;
        float baseHub = HubRadiusFor(view.Count);
        float hub = baseHub * MathHelper.Lerp(.8f, 1, Math.Min(1, view.RingScale));
        const float top = -MathHelper.PiOver2;
        Stretch(batch, art.State.Glow, center, new Vector2((hub + 30) * 2), new Color(173, 121, 206) * (.1f * alpha));
        float bandInner = (RadiusFor(view.Count) - BandHalf) * view.RingScale;
        // 亮色场景下环带与中心之间的缝隙会透出背景，压暗以保持轮廓。
        if (bandInner > hub)
            sector(center, hub, bandInner, top, top + MathHelper.TwoPi, BandColor * (.5f * alpha), 0, Color.Transparent);
        sector(center, 0, hub, top, top + MathHelper.TwoPi, HubOuter * (.91f * alpha), 1, HubEdge * (.21f * alpha));
        Stretch(batch, art.State.Glow, center, new Vector2(hub * 1.9f), HubInner * (.75f * alpha));

        Vector2 offset = view.Pointer - center;
        if (view.Open && offset.Length() >= DeadZone)
        {
            float angle = MathF.Atan2(offset.X, -offset.Y);
            Vector2 needle = center + Direction(angle) * (hub + 3);
            Stretch(batch, art.State.Glow, needle, new Vector2(18), GlowColor * (.45f * alpha));
            batch.Draw(art.State.Diamond, needle, null, Pale * alpha, angle,
                new Vector2(art.State.Diamond.Width, art.State.Diamond.Height) / 2,
                new Vector2(8f / art.State.Diamond.Width, 15f / art.State.Diamond.Height), SpriteEffects.None, 0);
        }

        bool small = baseHub < 56;
        float width = hub * 1.8f;
        Text(batch, art.Title, view.Name, center + new Vector2(0, -6), NameColor * alpha,
            FittedScale(art.Title, view.Name, small ? 13 : 15, width), 1.2f);
        Text(batch, art.Body, view.Sub, center + new Vector2(0, 12), Muted * alpha,
            FittedScale(art.Body, view.Sub, small ? 9 : 10, width), 1);
    }

    private static void DrawItems(SpriteBatch batch, in Art art, View view, BatchRestarter restart)
    {
        for (int i = 0; i < view.Count; i++)
        {
            ref WheelItem item = ref view.Items[i];
            if (item.Opacity <= 0) continue;
            if (item.Chosen && !view.Open)
                Stretch(batch, art.State.Glow, item.Position, new Vector2(92), new Color(236, 214, 255, 0) * (.6f * item.Opacity));
            SkillBarDrawing.DrawSlotBackground(batch, art.Ornaments, item.Position, item.Current, true,
                item.Hover, view.Seconds, item.Opacity);
        }

        for (int i = 0; i < view.Count; i++)
        {
            ref WheelItem item = ref view.Items[i];
            if (item.Icon == null || item.Opacity <= 0) continue;
            art.Cooldown.Parameters["time"]?.SetValue(1 - item.Remaining);
            art.Cooldown.Parameters["flashTime"]?.SetValue(item.Flash);
            art.Cooldown.Parameters["ready"]?.SetValue(item.Stack > 0 ? 1f : 0f);
            restart(batch, SpriteSortMode.Immediate, art.Cooldown);
            float scale = IconSize / Math.Max(item.Icon.Width, item.Icon.Height) * (1 + .08f * item.Hover);
            batch.Draw(item.Icon, item.Position, null, Color.White * item.Opacity, 0,
                new Vector2(item.Icon.Width, item.Icon.Height) / 2, scale, SpriteEffects.None, 0);
        }
        restart(batch, SpriteSortMode.Deferred, null);

        const float numberScale = 11f / 48f;
        for (int i = 0; i < view.Count; i++)
        {
            ref WheelItem item = ref view.Items[i];
            if (item.Opacity <= 0) continue;
            SkillBarDrawing.DrawSlotFrame(batch, art.Ornaments, item.Position, item.Current, true,
                item.Hover, view.Seconds, item.Opacity);
            if (view.Open && item.Opacity >= .99f)
                SkillBarDrawing.DrawSkillState(batch, art.State, art.Number, item.Position, item.Cooldown,
                    item.Remaining, item.Stack, item.MaxStack, view.ShowNumber);
            Text(batch, art.Number, (item.Slot + 1).ToString(), item.Position + new Vector2(-39, -39),
                Color.Lerp(new Color(190, 172, 206), Pale, item.Hover) * item.Opacity, numberScale, 1, Vector2.Zero);
        }
    }

    private static float FittedScale(DynamicSpriteFont font, string text, float pixels, float width)
    {
        float scale = pixels / font.MeasureString("国").X;
        float measured = font.MeasureString(text ?? "").X * scale;
        return measured > width ? scale * width / measured : scale;
    }

    private static void Text(SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position, Color color,
        float scale, float shadow, Vector2? anchor = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        Vector2 origin = font.MeasureString(text) * (anchor ?? new Vector2(.5f));
        batch.DrawString(font, text, position + new Vector2(shadow), Color.Black * (.75f * color.A / 255f),
            0, origin, scale, SpriteEffects.None, 0);
        batch.DrawString(font, text, position, color, 0, origin, scale, SpriteEffects.None, 0);
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
