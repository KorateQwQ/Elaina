using static 伊蕾娜.Localization.UIText;
using System;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

internal sealed class LevelUpToast
{
    internal int From, To;
    internal long Points;
    internal float Age;
}

/// <summary>
/// 角色升级提示：在人物头顶上方弹出（0.22 秒），停留 1.4 秒后原地上浮淡出（0.45 秒），不进入技能栏旁的停靠位，
/// 因此不与显现/启封提示排队或重叠。停留期间再次升级会合并为一条，并重新计时。
/// </summary>
internal sealed class LevelUpToastQueue
{
    internal const float Pop = .22f, Hold = 1.4f, Fade = .45f;
    /// <summary>屏幕中的放大倍数，以及卡片中心距屏幕顶部的比例（人物位于 50%）。</summary>
    internal const float CenterScale = 1.35f, CenterY = .28f;
    internal LevelUpToast Current { get; private set; }

    internal void Show(int from, int to, long points)
    {
        if (to <= from) return;
        if (Current == null) { Current = new LevelUpToast { From = from, To = to, Points = Math.Max(0, points) }; return; }
        Current.To = Math.Max(Current.To, to);
        Current.Points += Math.Max(0, points);
        // 停留中回到停留起点；淡入或淡出中按当前透明度续接，避免闪跳。
        Current.Age = Pop * Alpha(Current);
    }

    internal void Clear() => Current = null;

    internal void Advance(float seconds)
    {
        if (Current == null) return;
        Current.Age += Math.Max(0, seconds);
        if (Current.Age >= Pop + Hold + Fade) Current = null;
    }

    internal static float Alpha(LevelUpToast toast) => toast == null ? 0
        : toast.Age < Pop ? toast.Age / Pop
        : Math.Clamp(1 - (toast.Age - Pop - Hold) / Fade, 0, 1);

    /// <summary>弹出时的回弹缩放（乘在 CenterScale 上）。</summary>
    internal static float PopScale(LevelUpToast toast)
    {
        if (toast == null || toast.Age >= Pop) return 1;
        float t = toast.Age / Pop - 1;
        const float c = 1.7f;
        return MathHelper.Lerp(.86f, 1, 1 + (c + 1) * t * t * t + c * t * t);
    }

    /// <summary>纵向位移（设计像素）：自下而上弹入，淡出时继续上浮。</summary>
    internal static float Rise(LevelUpToast toast)
    {
        if (toast == null) return 0;
        if (toast.Age < Pop) { float t = 1 - toast.Age / Pop; return 10 * t * t; }
        float fade = Math.Clamp((toast.Age - Pop - Hold) / Fade, 0, 1);
        return -12 * (1 - (1 - fade) * (1 - fade) * (1 - fade));
    }
}

internal static class LevelUpToastPainter
{
    internal const int Width = ConstellationToastPainter.Width, Height = ConstellationToastPainter.Height;
    private const float Pad = 14, PlateSize = 40;

    /// <summary>与显现提示同款卡片；左侧为等级徽记，右侧为标题、等级变化与研习点。d.Origin 为卡片左上角。</summary>
    internal static void Paint(ConstellationDrawing d, LevelUpToast toast)
    {
        float a = LevelUpToastQueue.Alpha(toast);
        if (a <= 0) return;
        ConstellationToastPainter.Frame(d, a, false);

        var center = new Vector2(Pad + 2 + PlateSize / 2, Height / 2f);
        float halo = .18f + .5f * Math.Clamp(1 - toast.Age / 1.2f, 0, 1);
        d.Image("Glow", center.X - PlateSize, center.Y - PlateSize, PlateSize * 2, PlateSize * 2, ConstellationDrawing.Gold * (halo * a));
        d.Dot(center, PlateSize / 2, ConstellationDrawing.NodePlate * a);
        d.Ring(center, PlateSize / 2, PlateSize / 2, ConstellationDrawing.Gold * (.75f * a), 1.2f);
        d.Text("Lv", center.X, center.Y - 15, 8, ConstellationDrawing.Muted * a, .5f, serif: true);
        d.Text(toast.To.ToString(), center.X, center.Y - 8, 17, ConstellationDrawing.Ink * a, .5f, serif: true);

        float x = Pad + 2 + PlateSize + 12, width = Width - Pad - x;
        d.Spark(new Vector2(x + 4, 18), 8, ConstellationDrawing.Gold * a);
        d.FittedText(LocalizeUI("LevelUpTitle"), x + 12, 11, width - 12, 12, ConstellationDrawing.Gold * a, serif: true, spacing: 1.5f);
        d.Dots(x, 30, Width - Pad, 30, ConstellationDrawing.DotColor * a);
        string range = LocalizeUI("LevelUpRange", toast.From, toast.To);
        if (toast.Points > 0)
        {
            d.FittedText(range, x, 35, width, 14, ConstellationDrawing.Ink * a, serif: true, spacing: .5f);
            d.FittedText(LocalizeUI("LevelUpPoints", toast.Points), x, 52, width, 9.5f, ConstellationDrawing.Muted * a, spacing: .5f);
        }
        else d.FittedText(range, x, 41, width, 14, ConstellationDrawing.Ink * a, serif: true, spacing: .5f);
    }
}
