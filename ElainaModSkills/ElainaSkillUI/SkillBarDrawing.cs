using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

// Shared by the live SUI controls and the offline FNA preview.
internal static class SkillBarDrawing
{
    public const float SealReleaseDuration = .7f;
    public static bool HasSealOverlay(bool sealedState, float releaseAge) =>
        sealedState || releaseAge >= 0 && releaseAge < SealReleaseDuration;

    public static float SealTone(bool sealedState, float releaseAge)
    {
        if (sealedState) return 1;
        if (releaseAge < 0) return 0;
        float t = Math.Clamp(releaseAge / SealReleaseDuration / .45f, 0, 1);
        return 1 - t * t * (3 - 2 * t);
    }

    public static float SealReleasePulse(float age)
    {
        if (age < 0 || age >= .5f) return 0;
        if (age < .06f) return MathHelper.Lerp(.6f, 1, age / .06f);
        return MathF.Pow(1 - (age - .06f) / .44f, 2);
    }

    /// <summary>HTML封印参考的四角束缚、钥匙孔及解封动画；不绘制提示浮字。</summary>
    public static void DrawSealState(SpriteBatch batch, StateArt art, Vector2 center,
        float seconds, float denyAge = 9, float releaseAge = -1, float opacity = 1)
    {
        float p = releaseAge >= 0 ? Math.Clamp(releaseAge / SealReleaseDuration, 0, 1) : 0;
        float fade = (1 - p) * opacity;
        float breath = .5f + .5f * MathF.Sin(seconds * MathHelper.TwoPi / 3.6f);
        float deny = Math.Clamp(1 - denyAge / .38f, 0, 1);
        float shake = 1.8f * MathF.Sin(denyAge * MathHelper.TwoPi * 9) * deny;
        float retract = 1 - MathF.Pow(1 - Math.Clamp(p / .6f, 0, 1), 3);
        Color edge = new(222, 205, 240), keyhole = new(236, 224, 250);
        for (int i = 0; i < 4; i++)
        {
            Vector2 direction = new(i < 2 ? -1 : 1, i % 2 == 0 ? -1 : 1);
            Vector2 corner = center + direction * 21.5f;
            Vector2 start = Vector2.Lerp(center + direction * 8, corner, retract);
            if (retract < .999f)
            {
                Line(batch, art, start, corner, 3, new Color(202, 178, 237) * (.1f * fade));
                Line(batch, art, start, corner, 1, new Color(205, 186, 230) * ((.5f + .5f * deny) * fade));
            }
            Glow(batch, art, corner, new Vector2(9), new Color(196, 150, 255, 0) * ((.25f + .45f * deny) * fade));
            Diamond(batch, art, corner, new Vector2(3.5f, 5), edge * (.85f * fade));
        }
        float scale = 1 + .55f * (1 - MathF.Pow(1 - p, 3));
        float alpha = MathF.Pow(1 - p, 1.5f) * opacity;
        Vector2 c = center + new Vector2(shake, 0);
        Glow(batch, art, c, new Vector2(34), Color.Black * (.55f * alpha));
        Glow(batch, art, c, new Vector2(30 * scale), new Color(196, 150, 255, 0) * ((.22f + .16f * breath + .55f * deny) * alpha));
        Diamond(batch, art, c, new Vector2(19 * scale), edge * alpha);
        Diamond(batch, art, c, new Vector2(16.4f * scale), Ink * (.96f * alpha));
        Diamond(batch, art, c, new Vector2(10.5f * scale), new Color(150, 128, 182) * (.7f * alpha));
        Diamond(batch, art, c, new Vector2(8.9f * scale), Ink * alpha);
        Diamond(batch, art, c + new Vector2(0, -1.4f * scale), new Vector2(4.4f * scale), keyhole * alpha);
        Line(batch, art, c + new Vector2(0, -.8f * scale), c + new Vector2(0, 3.4f * scale), 1.4f * scale, keyhole * alpha);
        if (releaseAge >= 0)
        {
            float radius = 9.5f + 26 * (1 - MathF.Pow(1 - p, 2));
            Glow(batch, art, center, new Vector2(44 + 30 * p), new Color(214, 180, 255, 0) * (.9f * SealReleasePulse(releaseAge) * opacity));
            for (int i = 0; i < 4; i++)
            {
                float angle = -MathHelper.PiOver2 + i * MathHelper.PiOver2;
                Vector2 a = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                Vector2 b = center + new Vector2(MathF.Cos(angle + MathHelper.PiOver2), MathF.Sin(angle + MathHelper.PiOver2)) * radius;
                Line(batch, art, a, b, 1, new Color(241, 226, 255, 0) * (.85f * fade));
            }
        }
    }
    public const int Width = 646;
    public const int Height = 94;
    private const int Tile = 192;
    private const float ArrivalDuration = .45f;
    private static readonly Vector2[] Orbit = CreateOrbit();

    public static void DrawRack(SpriteBatch batch, Texture2D rack, Vector2 position) =>
        batch.Draw(rack, position - new Vector2(24), null, Color.White, 0,
            Vector2.Zero, .5f, SpriteEffects.None, 0);

    private static void Ornament(SpriteBatch batch, Texture2D atlas, int tile, Vector2 center, Color color) =>
        batch.Draw(atlas, center, new Rectangle(tile * Tile, 0, Tile, Tile), color,
            0, new Vector2(Tile / 2), .5f, SpriteEffects.None, 0);

    public static void DrawSlotBackground(SpriteBatch batch, Texture2D atlas,
        Vector2 center, bool selected, bool occupied, float hover, float seconds, float opacity = 1)
    {
        Ornament(batch, atlas, selected ? 2 : 4, center, Color.White * opacity);
        if (selected)
            Ornament(batch, atlas, 2, center, new Color(255, 235, 255, 0) * (2.2f * ArrivalPulse(seconds) * opacity));
        if (hover > 0 && !selected) Ornament(batch, atlas, 5, center, Color.White * (hover * opacity));
        if (!occupied)
            Ornament(batch, atlas, 3, center, (selected ? new Color(218, 197, 238) :
                Color.Lerp(new Color(177, 154, 195) * (113f / 255f), new Color(224, 203, 238), hover)) * opacity);
    }

    public static void DrawSlotFrame(SpriteBatch batch, Texture2D atlas,
        Vector2 center, bool selected, bool occupied, float hover, float seconds, float opacity = 1)
    {
        if (!selected)
        {
            DrawRestingCorners(batch, atlas, center,
                Color.White * (MathHelper.Lerp(occupied ? .48f : .26f, .72f, hover) * opacity));
            return;
        }
        float pulse = ArrivalPulse(seconds);
        Ornament(batch, atlas, 1, center, Color.White * ((.84f + .16f * pulse) * opacity));
        if (pulse > 0)
        {
            // Alpha-zero tints add light under the existing premultiplied batch.
            Ornament(batch, atlas, 1, center, new Color(245, 222, 255, 0) * (.9f * pulse * opacity));
            float progress = Math.Clamp(seconds / ArrivalDuration, 0, 1);
            float spread = 1 - MathF.Pow(1 - progress, 3);
            batch.Draw(atlas, center, new Rectangle(0, 0, Tile, Tile),
                new Color(223, 192, 255, 0) * (.65f * pulse * opacity), 0, new Vector2(Tile / 2),
                .5f * (1 + .16f * spread), SpriteEffects.None, 0);

        }

        // CSS conic-gradient: clockwise from twelve o'clock, one revolution in 2.8 seconds.
        float rotation = seconds / 2.8f * 360;
        for (int i = 0; i < Orbit.Length - 1; i++)
        {
            Vector2 a = Orbit[i], b = Orbit[i + 1], middle = (a + b) * .5f;
            float angle = (MathHelper.ToDegrees(MathF.Atan2(middle.X, -middle.Y)) - rotation) % 360;
            if (angle < 0) angle += 360;
            Color color = OrbitColor(angle) * opacity;
            if (color.A == 0) continue;
            Vector2 delta = b - a;
            batch.Draw(atlas, center + a, new Rectangle(0, Tile, 4, 8), color,
                MathF.Atan2(delta.Y, delta.X), new Vector2(0, 4),
                new Vector2(delta.Length() / 4, .25f), SpriteEffects.None, 0);
        }
        DrawSelectionMarkers(batch, atlas, center, seconds, pulse, opacity);
    }

    private static void DrawSelectionMarkers(SpriteBatch batch, Texture2D atlas,
        Vector2 center, float seconds, float pulse, float opacity)
    {
        float breath = .5f + .5f * MathF.Sin(seconds * MathHelper.TwoPi / 3.6f);
        Vector2 starPosition = center + new Vector2(0, -37.5f);
        Vector2 diamondPosition = center + new Vector2(0, 34.5f);
        float starScale = 1 + .25f * pulse;
        DrawMarker(batch, atlas, 1, starPosition, new Color(255, 255, 255, 0) * ((.55f + .25f * breath + pulse) * opacity), starScale);
        DrawMarker(batch, atlas, 0, starPosition, Color.White * opacity, starScale);
        DrawMarker(batch, atlas, 0, starPosition, new Color(255, 241, 255, 0) * (.45f * pulse * opacity), starScale);

        // A short delayed response below the slot reinforces the main star's arrival.
        float reveal = MathHelper.SmoothStep(0, 1, Math.Clamp((seconds - .065f) / .10f, 0, 1)) * opacity;
        float diamondPulse = seconds < .10f ? 0 : ArrivalPulse(seconds - .10f);
        DrawMarker(batch, atlas, 3, diamondPosition,
            new Color(255, 255, 255, 0) * reveal * (.35f + .15f * breath + .5f * diamondPulse), 1);
        DrawMarker(batch, atlas, 2, diamondPosition, Color.White * (.82f * reveal), 1);
        DrawMarker(batch, atlas, 2, diamondPosition,
            new Color(255, 241, 255, 0) * (.35f * diamondPulse * reveal), 1);
    }

    private static void DrawMarker(SpriteBatch batch, Texture2D atlas, int marker,
        Vector2 position, Color color, float scale) =>
        batch.Draw(atlas, position, new Rectangle(16 + marker * 64, 192, 64, 64),
            color, 0, new Vector2(32), .5f * scale, SpriteEffects.None, 0);

    private static void DrawRestingCorners(SpriteBatch batch, Texture2D atlas, Vector2 center, Color color)
    {
        // Crop four 12px patches from the existing 2x frame; retain the full tile for arrival pulses.
        const int near = 36;
        const int far = 132;
        const int cornerSize = 24;
        for (int row = 0; row < 2; row++)
        for (int column = 0; column < 2; column++)
        {
            int x = column == 0 ? near : far;
            int y = row == 0 ? near : far;
            batch.Draw(atlas, center + new Vector2(x - Tile / 2, y - Tile / 2) * .5f,
                new Rectangle(x, y, cornerSize, cornerSize), color,
                0, Vector2.Zero, .5f, SpriteEffects.None, 0);
        }
    }

    /// <summary>冷却叠加层所需的实心纹理：1x1 白像素、径向柔光点、抗锯齿菱形。</summary>
    public readonly record struct StateArt(Texture2D Pixel, Texture2D Glow, Texture2D Diamond);

    /// <summary>
    /// 冷却与层数叠加层：扫描指针标出恢复进度；层数 ≤ <see cref="MaxPips"/> 时用菱形刻度，否则用角标；
    /// 只有无层可用时才在中心显示倒计时，避免可释放的技能被大号数字盖住。
    /// </summary>
    public static void DrawSkillState(SpriteBatch batch, StateArt art, DynamicSpriteFont font,
        Vector2 center, float cooldown, float remaining, int stack, int maxStack, bool showNumber)
    {
        bool recovering = cooldown > 0 && remaining > 0;
        float progress = recovering ? 1 - remaining : 0;
        if (recovering) DrawSweepHand(batch, art, center, progress, stack > 0);
        if (maxStack > MaxPips) DrawChargeBadge(batch, art, font, center, stack);
        else if (maxStack > 1) DrawChargePips(batch, art, center, stack, maxStack, progress);
        if (recovering && stack <= 0 && showNumber) DrawCountdown(batch, art, font, center, cooldown);
    }

    private const int MaxPips = 5;
    private const float IconHalf = 25;
    private static readonly Color Ink = new(20, 14, 30);
    private static readonly Color Lilac = new(232, 216, 255);

    private static void DrawSweepHand(SpriteBatch batch, StateArt art, Vector2 center, float progress, bool usable)
    {
        // 与 CDEffect 的扇区边界对齐：十二点方向顺时针；只画外半段，避免划过图标主体。
        float angle = progress * MathHelper.TwoPi;
        Vector2 ray = new(MathF.Sin(angle), -MathF.Cos(angle));
        Vector2 tip = center + ray * (IconHalf / Math.Max(Math.Abs(ray.X), Math.Abs(ray.Y)));
        Color light = new Color(240, 224, 255, 0) * (usable ? .55f : .8f);
        Line(batch, art, Vector2.Lerp(center, tip, .3f), Vector2.Lerp(center, tip, .6f), 1, light * .3f);
        Line(batch, art, Vector2.Lerp(center, tip, .6f), tip, 1.25f, light);
        Glow(batch, art, tip, new Vector2(7), light);
    }

    private static void DrawChargePips(SpriteBatch batch, StateArt art, Vector2 center,
        int stack, int maxStack, float progress)
    {
        const float spacing = 8;
        Vector2 row = center + new Vector2(0, 21);
        Glow(batch, art, row, new Vector2((maxStack - 1) * spacing + 26, 20), Color.Black);
        for (int i = 0; i < maxStack; i++)
        {
            Vector2 pip = row + new Vector2((i - (maxStack - 1) / 2f) * spacing, 0);
            if (i < stack)
            {
                Glow(batch, art, pip, new Vector2(11), new Color(196, 150, 255, 0) * .55f);
                Diamond(batch, art, pip, new Vector2(5, 7), Lilac);
                continue;
            }
            Diamond(batch, art, pip, new Vector2(5, 7), new Color(150, 128, 182) * .75f);
            Diamond(batch, art, pip, new Vector2(3, 4.5f), Ink);
            // 正在恢复的一层由内向外长出。
            if (i == stack && progress > 0)
                Diamond(batch, art, pip, new Vector2(5, 7) * progress, Lilac * (.4f + .4f * progress));
        }
    }

    private static void DrawChargeBadge(SpriteBatch batch, StateArt art, DynamicSpriteFont font,
        Vector2 center, int stack)
    {
        const float scale = 12f / 48f;
        string text = stack.ToString();
        int width = (int)MathF.Ceiling(font.MeasureString(text).X * scale) + 13;
        var chip = new Rectangle((int)center.X + 27 - width, (int)center.Y + 15, width, 12);
        bool empty = stack <= 0;
        batch.Draw(art.Pixel, chip, Ink * .88f);
        Color edge = new Color(176, 150, 214) * (empty ? .35f : .6f);
        batch.Draw(art.Pixel, new Rectangle(chip.X, chip.Y, chip.Width, 1), edge);
        batch.Draw(art.Pixel, new Rectangle(chip.X, chip.Bottom - 1, chip.Width, 1), edge * .6f);
        batch.Draw(art.Pixel, new Rectangle(chip.X, chip.Y, 1, chip.Height), edge * .8f);
        batch.Draw(art.Pixel, new Rectangle(chip.Right - 1, chip.Y, 1, chip.Height), edge * .8f);
        Vector2 pip = new(chip.X + 5, chip.Center.Y);
        Diamond(batch, art, pip, new Vector2(4, 6), empty ? new Color(96, 82, 116) : Lilac);
        OutlinedText(batch, font, text, pip + new Vector2(4, 0),
            empty ? new Color(140, 124, 160) : new Color(246, 238, 255), scale, new Vector2(0, .5f));
    }

    private static void DrawCountdown(SpriteBatch batch, StateArt art, DynamicSpriteFont font,
        Vector2 center, float seconds)
    {
        string text = seconds >= 10 ? $"{MathF.Ceiling(seconds):0}" : $"{seconds:0.0}";
        Glow(batch, art, center, new Vector2(46, 34), Color.Black * .65f);
        OutlinedText(batch, font, text, center, new Color(248, 240, 255), 16f / 48f, new Vector2(.5f));
    }

    internal static void OutlinedText(SpriteBatch batch, DynamicSpriteFont font, string text,
        Vector2 position, Color color, float scale, Vector2 anchor)
    {
        Vector2 origin = font.MeasureString(text) * anchor;
        Color outline = Ink * (.9f * color.A / 255f);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * MathHelper.PiOver4;
            batch.DrawString(font, text, position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)), outline,
                0, origin, scale, SpriteEffects.None, 0);
        }
        batch.DrawString(font, text, position, color, 0, origin, scale, SpriteEffects.None, 0);
    }

    private static void Line(SpriteBatch batch, StateArt art, Vector2 a, Vector2 b, float thickness, Color color)
    {
        Vector2 delta = b - a;
        batch.Draw(art.Pixel, a, null, color, MathF.Atan2(delta.Y, delta.X),
            new Vector2(0, .5f), new Vector2(delta.Length(), thickness), SpriteEffects.None, 0);
    }

    private static void Glow(SpriteBatch batch, StateArt art, Vector2 center, Vector2 size, Color color) =>
        Stretch(batch, art.Glow, center, size, color);

    private static void Diamond(SpriteBatch batch, StateArt art, Vector2 center, Vector2 size, Color color) =>
        Stretch(batch, art.Diamond, center, size, color);

    private static void Stretch(SpriteBatch batch, Texture2D texture, Vector2 center, Vector2 size, Color color)
    {
        Vector2 source = new(texture.Width, texture.Height);
        batch.Draw(texture, center, null, color, 0, source / 2, size / source, SpriteEffects.None, 0);
    }

    private static float ArrivalPulse(float seconds)
    {
        if (seconds >= ArrivalDuration) return 0;
        const float attack = .055f;
        if (seconds < attack) return MathHelper.Lerp(.65f, 1, Math.Clamp(seconds / attack, 0, 1));
        float decay = (seconds - attack) / (ArrivalDuration - attack);
        return (1 - decay) * (1 - decay);
    }

    private static Color OrbitColor(float angle)
    {
        if (angle < 235) return Color.Transparent;
        if (angle < 275) return Color.Lerp(Color.Transparent, new Color(179, 142, 211) * (59f / 255), (angle - 235) / 40);
        if (angle < 312) return Color.Lerp(new Color(179, 142, 211) * (59f / 255), new Color(215, 185, 243) * (179f / 255), (angle - 275) / 37);
        if (angle < 342) return Color.Lerp(new Color(215, 185, 243) * (179f / 255), new Color(250, 241, 255), (angle - 312) / 30);
        if (angle < 355) return Color.Lerp(new Color(250, 241, 255), new Color(234, 215, 255), (angle - 342) / 13);
        return Color.Lerp(new Color(234, 215, 255), Color.Transparent, (angle - 355) / 5);
    }

    private static Vector2[] CreateOrbit()
    {
        // The 57px CSS ring has a 1.25px inset stroke and 2.5px outer corner radius.
        var points = new Vector2[225];
        const float half = 28.5f - .625f;
        const float radius = 2.5f - .625f;
        for (int i = 0; i <= 224; i++)
        {
            float angle = i / 224f * MathHelper.TwoPi;
            Vector2 ray = new(MathF.Sin(angle), -MathF.Cos(angle));
            Vector2 hit = ray * (half / Math.Max(Math.Abs(ray.X), Math.Abs(ray.Y)));
            if (Math.Abs(hit.X) > half - radius && Math.Abs(hit.Y) > half - radius)
            {
                Vector2 corner = new(MathF.CopySign(half - radius, ray.X), MathF.CopySign(half - radius, ray.Y));
                float dot = Vector2.Dot(ray, corner);
                hit = ray * (dot + MathF.Sqrt(Math.Max(0, dot * dot - corner.LengthSquared() + radius * radius)));
            }
            points[i] = hit;
        }
        return points;
    }
}
