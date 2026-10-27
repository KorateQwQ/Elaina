using static 伊蕾娜.Localization.UIText;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

internal enum ConstellationDiscoveryKind { Appeared, Unsealed }

internal readonly record struct ConstellationDiscoveryEvent(string Id, ConstellationDiscoveryKind Kind);

/// <summary>
/// 记录每个技能上次通知时的状态。未显现→待研习/待启封为“显现”，待启封→待研习为“启封”。
/// 第一次观察（新角色或旧存档）只记录不通知；新加入的技能同样静默记录。
/// </summary>
internal sealed class ConstellationDiscoveryTracker
{
    internal readonly Dictionary<string, string> Known = new(StringComparer.Ordinal);
    internal readonly HashSet<string> Fresh = new(StringComparer.Ordinal);
    internal bool Initialized;

    internal void Clear()
    {
        Known.Clear();
        Fresh.Clear();
        Initialized = false;
    }

    internal List<ConstellationDiscoveryEvent> Observe(IEnumerable<(string Id, string Status)> statuses)
    {
        var events = new List<ConstellationDiscoveryEvent>();
        foreach (var (id, status) in statuses)
        {
            bool had = Known.TryGetValue(id, out string previous);
            Known[id] = status;
            if (status is "hidden" or "learned") { Fresh.Remove(id); continue; }
            if (!Initialized || !had) continue;
            if (previous == "hidden") events.Add(new(id, ConstellationDiscoveryKind.Appeared));
            else if (previous == "locked" && status == "ready") events.Add(new(id, ConstellationDiscoveryKind.Unsealed));
            else continue;
            Fresh.Add(id);
        }
        Initialized = true;
        return events;
    }
}

internal sealed class ConstellationToast(ConstellationDiscoveryKind kind)
{
    internal readonly ConstellationDiscoveryKind Kind = kind;
    internal readonly List<string> Ids = [];
    internal float Age;
}

/// <summary>同类变化合并为一条，依次播放：淡入 0.25 秒、停留 2.6 秒、淡出 0.4 秒；悬停时保持显示。</summary>
internal sealed class ConstellationToastQueue
{
    internal const float FadeIn = .25f, Hold = 2.6f, FadeOut = .4f;
    private readonly List<ConstellationToast> _pending = [];
    internal ConstellationToast Current { get; private set; }
    internal int PendingCount => _pending.Count;

    internal void Push(IEnumerable<ConstellationDiscoveryEvent> events)
    {
        foreach (var e in events)
        {
            // Merge into a waiting toast of the same kind; the one on screen keeps its content.
            var toast = _pending.FirstOrDefault(t => t.Kind == e.Kind);
            if (toast == null) _pending.Add(toast = new(e.Kind));
            if (!toast.Ids.Contains(e.Id)) toast.Ids.Add(e.Id);
        }
    }

    internal void Clear()
    {
        _pending.Clear();
        Current = null;
    }

    internal void Advance(float seconds, bool hovered)
    {
        if (Current == null)
        {
            if (_pending.Count == 0) return;
            Current = _pending[0];
            _pending.RemoveAt(0);
        }
        // Hovering keeps it fully shown, including one that has started to fade out.
        if (hovered && Current.Age >= FadeIn) { Current.Age = FadeIn; return; }
        Current.Age += Math.Max(0, seconds);
        if (Current.Age >= FadeIn + Hold + FadeOut) Current = null;
    }

    internal static float Alpha(ConstellationToast toast) => toast == null ? 0
        : toast.Age < FadeIn ? toast.Age / FadeIn
        : Math.Clamp(1 - (toast.Age - FadeIn - Hold) / FadeOut, 0, 1);

    /// <summary>入场时自下而上滑入的位移，单位为设计像素。</summary>
    internal static float Rise(ConstellationToast toast)
    {
        float t = toast == null ? 1 : Math.Min(1, toast.Age / FadeIn);
        return 10 * (1 - t) * (1 - t);
    }
}

internal static class ConstellationToastPainter
{
    internal const int Width = 264, Height = 70;
    private const float Pad = 14, IconSize = 40;

    internal static Rectangle Bounds(Rectangle dock) => new(dock.Right - Width, dock.Bottom - Height, Width, Height);

    // Matches the term tooltip's notebook note: dotted inner frame with corner stars, serif heading.
    internal static void Paint(ConstellationDrawing d, ConstellationState state, ConstellationToast toast, bool hover)
    {
        var nodes = toast.Ids.Select(state.Find).Where(n => n != null).ToArray();
        if (nodes.Length == 0) return;
        float a = ConstellationToastQueue.Alpha(toast);
        var old = d.Origin;
        d.Origin += new Vector2(0, ConstellationToastQueue.Rise(toast) * d.Scale);
        try
        {
            d.Shadow(0, 6, Width, Height, 4, Color.Black * (.5f * a), 12);
            d.RoundRect(0, 0, Width, Height, 3, (hover ? new Color(40, 33, 52) : new Color(26, 22, 36)) * (.97f * a),
                ConstellationDrawing.Lavender * ((hover ? .55f : .3f) * a), 1);
            float l = 5.5f, r = Width - 5.5f, t = 5.5f, b = Height - 5.5f;
            Color dots = ConstellationDrawing.DotColor * (.8f * a);
            d.Dots(l + 8, t, r - 8, t, dots); d.Dots(l + 8, b, r - 8, b, dots);
            d.Dots(l, t + 8, l, b - 8, dots); d.Dots(r, t + 8, r, b - 8, dots);
            foreach (var corner in new[] { new Vector2(l, t), new Vector2(r, t), new Vector2(r, b), new Vector2(l, b) })
                d.CrossStar(corner, new Vector2(6 * .976f, 6), ConstellationDrawing.Lavender * (.8f * a), .28f);

            // Up to three icons fanned to the left; the first is drawn last, on top.
            int shown = Math.Min(3, nodes.Length);
            float size = shown == 1 ? IconSize : 32, step = 12, iconsWidth = size + step * (shown - 1);
            var firstCenter = new Vector2(Pad + 2 + size / 2, Height / 2f);
            // A brief halo while the toast enters, settling to a faint glow.
            float halo = .18f + .5f * Math.Clamp(1 - toast.Age / 1.2f, 0, 1);
            d.Image("Glow", firstCenter.X - size, firstCenter.Y - size, size * 2, size * 2, ConstellationDrawing.Gold * (halo * a));
            for (int i = shown - 1; i >= 0; i--)
                Icon(d, state, nodes[i], firstCenter + new Vector2(step * i, 0), size, a);

            float x = Pad + 2 + iconsWidth + 12, width = Width - Pad - x;
            string title = LocalizeUI(toast.Kind == ConstellationDiscoveryKind.Appeared ? "DiscoveryAppeared" : "DiscoveryUnsealed");
            d.Spark(new Vector2(x + 4, 18), 8, ConstellationDrawing.Gold * a);
            d.FittedText(title, x + 12, 11, width - 12, 12, ConstellationDrawing.Gold * a, serif: true, spacing: 1.5f);
            d.Dots(x, 30, Width - Pad, 30, ConstellationDrawing.DotColor * a);

            var first = nodes[0];
            if (nodes.Length == 1)
            {
                d.FittedText(state.Name(first), x, 35, width, 14, ConstellationDrawing.Ink * a, serif: true, spacing: .5f);
                d.FittedText(StatusLine(state, first), x, 52, width, 9.5f, ConstellationDrawing.Muted * a, spacing: .5f);
            }
            else d.FittedText(LocalizeUI("DiscoveryMany", state.Name(first), nodes.Length), x, 41, width, 13,
                ConstellationDrawing.Ink * a, serif: true, spacing: .5f);
        }
        finally { d.Origin = old; }
    }

    private static string StatusLine(ConstellationState state, ConstellationNode node) => state.Status(node) switch
    {
        "locked" => LocalizeUI("DiscoveryLocked"),
        "ready" => LocalizeUI("DiscoveryReady"),
        var status => ConstellationState.StatusName(status)
    };

    private static void Icon(ConstellationDrawing d, ConstellationState state, ConstellationNode node, Vector2 center, float size, float a)
    {
        d.Dot(center, size / 2, ConstellationDrawing.NodePlate * a);
        if (state.Discovered(node)) d.SkillIcon(node, center, size, Color.White * a, round: true);
        else d.Text("?", center.X, center.Y - size * .36f, size * .62f, ConstellationDrawing.Lavender * a, .5f);
        d.Ring(center, size / 2, size / 2, ConstellationDrawing.Gold * (.75f * a), 1.2f);
    }
}
