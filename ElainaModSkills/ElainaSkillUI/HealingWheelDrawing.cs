using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using static 伊蕾娜.ElainaModSkills.ElainaSkillUI.SkillWheelDrawing;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI;

/// <summary>Native healing-wheel layout shared by the SUI body and the FNA preview.</summary>
internal static class HealingWheelDrawing
{
    public const int MaxTargets = 8;
    public const float HalfBand = 54;
    public static float Radius(int count) => count > 6 ? 148 : 136;
    public static float OuterRadius(int count) => Radius(count) + HalfBand;
    public static float HubRadius(int count) => Radius(count) - HalfBand - 5;
    public static Vector2 TargetPosition(Vector2 center, int slot, int count)
        => center + Direction(AngleOf(slot, count)) * Radius(count);

    // -1 = no selection in the hub. Beyond the hub, direction selects at any distance.
    public static int Pick(Vector2 offset, int count)
    {
        if (count <= 0) return -1;
        if (offset.LengthSquared() <= (Radius(count) - HalfBand) * (Radius(count) - HalfBand)) return -1;
        if (count == 2) return offset.X < 0 ? 0 : 1;
        float angle = MathF.Atan2(offset.X, -offset.Y);
        if (angle < 0) angle += MathHelper.TwoPi;
        return (int)MathF.Floor(angle / (MathHelper.TwoPi / count) + .5f) % count;
    }

    public readonly record struct Target(int Index, string Name, int Life, int MaxLife, bool Self);
    public readonly record struct Labels(string Target, string Self, string Critical, string Full, string Missing, string Controls, string Cancel);
    public delegate void HeadPainter(int playerIndex, Vector2 center, float scale, float alpha);

    public static void ConfigureFrame(View frame, int count)
    {
        frame.Count = count;
        frame.Radius = Radius(count);
        frame.HalfBand = HalfBand;
        frame.HubRadius = HubRadius(count);
    }

    /// <summary>
    /// 目标的位置、透明度、悬停与闪光取自 frame.Items（由宿主按开合动画填好），与技能盘的术式共用同一套运动曲线。
    /// </summary>
    public static void DrawTargets(SpriteBatch batch, in Art art, View frame, Target[] targets,
        in Labels labels, SectorPainter sector, HeadPainter drawHead)
    {
        float alpha = frame.Presence, release = frame.Chosen >= 0 ? frame.Release : -1, lead = 0;
        for (int i = 0; i < frame.Count; i++) lead = Math.Max(lead, frame.Items[i].Hover);

        // 柔光垫在整块目标信息下；松开选中时再亮一下，并从头像向外扩散细环。
        for (int i = 0; i < frame.Count; i++)
        {
            ref WheelItem item = ref frame.Items[i];
            if (item.Opacity > 0)
                Halo(batch, art, item.Position + new Vector2(0, 4), 104, Math.Min(1, item.Hover + .6f * item.Flash) * item.Opacity);
        }
        if (release >= 0) ReleaseRings(sector, frame.Items[frame.Chosen].Position + HeadOffset, release, 24, 60);

        for (int i = 0; i < frame.Count; i++)
        {
            ref WheelItem item = ref frame.Items[i];
            if (item.Opacity <= 0) continue;
            Target target = targets[i];
            Vector2 pos = item.Position;
            // 有目标被指向时其余目标随之退后，悬停切换时两者交叉过渡而不是一帧跳变。
            float dim = Math.Max(0, lead - item.Hover), opacity = item.Opacity;
            float pop = i == frame.Chosen && release >= 0 ? .12f * MathF.Sin(MathF.PI * Math.Min(1, release * 1.6f)) : 0;
            drawHead(target.Index, pos + HeadOffset, 1.35f * (1 + .08f * item.Hover + pop), opacity * (1 - .45f * dim));
            string name = target.Self ? target.Name + " · " + labels.Self : target.Name;
            Color nameColor = Color.Lerp(new Color(220, 216, 232), new Color(255, 242, 255), item.Hover);
            Text(batch, art.Number, name, pos + new Vector2(0, 10), 13, 88, Dim(nameColor, dim) * opacity);
            float life = Math.Clamp(target.Life / (float)Math.Max(1, target.MaxLife), 0, 1);
            Color color = life <= .25f ? new Color(237, 150, 159)
                : life >= 1 ? new Color(158, 212, 193) : new Color(197, 175, 217);
            Vector2 bar = pos + new Vector2(-33, 24);
            batch.Draw(art.State.Pixel, bar, null, new Color(255, 255, 255) * (.09f * opacity), 0,
                Vector2.Zero, new Vector2(66, 3), SpriteEffects.None, 0);
            if (life > 0) batch.Draw(art.State.Pixel, bar, null, Dim(color, dim) * opacity, 0,
                Vector2.Zero, new Vector2(66 * life, 3), SpriteEffects.None, 0);
            Text(batch, art.Number, $"{target.Life} / {target.MaxLife}", pos + new Vector2(0, 37), 11, 88,
                Dim(life <= .25f ? color : new Color(200, 192, 214), dim) * opacity);
        }

        Text(batch, art.Number, labels.Controls, frame.Center + new Vector2(0, OuterRadius(frame.Count) + 30),
            11, OuterRadius(frame.Count) * 2, new Color(236, 225, 247) * alpha);

        // 圆心内容随焦点切换淡入并轻微上浮，与技能盘中心图标的切换节奏一致；关闭时随轮盘一起淡出。
        float fade = Math.Clamp(frame.FocusAge / FocusFade, 0, 1), hub = alpha * fade;
        if (hub <= 0) return;
        Vector2 center = frame.Center + new Vector2(0, 5 * (1 - EaseOutCubic(fade)));
        float width = HubRadius(frame.Count) * 1.65f;
        if (frame.Focus >= 0 && frame.Focus < frame.Count)
        {
            Target target = targets[frame.Focus];
            Halo(batch, art, center + new Vector2(0, -20), 84, .7f * hub);
            Text(batch, art.Number, labels.Target, center + new Vector2(0, -48), 11, width, new Color(184, 168, 206) * hub);
            drawHead(target.Index, center + new Vector2(0, -20), 1.5f * MathHelper.Lerp(.9f, 1, EaseOutCubic(fade)), hub);
            Text(batch, art.Number, target.Name, center + new Vector2(0, 12), 16, width, new Color(245, 237, 255) * hub);
            Text(batch, art.Number, $"{target.Life} / {target.MaxLife}", center + new Vector2(0, 32), 13, width,
                new Color(231, 214, 247) * hub);
            bool critical = target.Life <= target.MaxLife * .25f;
            string status = critical ? labels.Critical : target.Life >= target.MaxLife ? labels.Full
                : string.Format(labels.Missing, target.MaxLife - target.Life);
            Text(batch, art.Number, status, center + new Vector2(0, 50), 10, width,
                (critical ? new Color(240, 165, 176) : new Color(189, 176, 206)) * hub);
        }
        else
        {
            Text(batch, art.Number, labels.Cancel, center, 14, width, new Color(200, 187, 218) * hub);
        }
    }

    private static readonly Vector2 HeadOffset = new(0, -18);

    private static Color Dim(Color color, float amount) => Color.Lerp(color, new Color(120, 112, 136), .6f * amount);

    internal static void Text(SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position,
        float size, float width, Color color)
    {
        // Clamp long player names instead of shrinking them into unreadable text; use the same font to measure and draw.
        float scale = size / Math.Max(1, font.MeasureString("国").X);
        string shown = text;
        while (shown.Length > 1 && font.MeasureString(shown).X * scale > width)
        {
            int length = shown.EndsWith("…", StringComparison.Ordinal) ? shown.Length - 2 : shown.Length - 1;
            if (length > 0 && char.IsHighSurrogate(shown[length - 1])) length--;
            shown = shown[..Math.Max(0, length)] + "…";
        }
        Vector2 origin = font.MeasureString(shown) / 2;
        batch.DrawString(font, shown, position + new Vector2(.7f, 1), Color.Black * (.65f * color.A / 255f),
            0, origin, scale, SpriteEffects.None, 0);
        batch.DrawString(font, shown, position, color, 0, origin, scale, SpriteEffects.None, 0);
    }
}
