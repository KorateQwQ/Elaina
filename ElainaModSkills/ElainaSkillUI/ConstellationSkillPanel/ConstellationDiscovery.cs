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
    internal const int Width = 236, Height = 50;

    internal static Rectangle Bounds(Rectangle dock) => new(dock.Right - Width, dock.Bottom - Height, Width, Height);

    internal static void Paint(ConstellationDrawing d, ConstellationState state, ConstellationToast toast, string key, bool hover)
    {
        var nodes = toast.Ids.Select(state.Find).Where(n => n != null).ToArray();
        if (nodes.Length == 0) return;
        float a = ConstellationToastQueue.Alpha(toast);
        var old = d.Origin;
        d.Origin += new Vector2(0, ConstellationToastQueue.Rise(toast) * d.Scale);
        try
        {
            d.Image("Glow", -20, -16, Width + 40, Height + 32, ConstellationDrawing.Lavender * (.2f * a));
            d.Box(0, 0, Width, Height, (hover ? new Color(52, 42, 64) : new Color(29, 25, 40)) * (.93f * a));
            d.Frame(0, 0, Width, Height, ConstellationDrawing.Lavender * ((hover ? .8f : .5f) * a));
            d.Box(0, 0, 2, Height, ConstellationDrawing.Gold * (.85f * a));

            // Up to three icons fanned to the left; the first is drawn last, on top.
            int shown = Math.Min(3, nodes.Length);
            float size = shown == 1 ? 34 : 28, step = 13, iconsWidth = size + step * (shown - 1);
            for (int i = shown - 1; i >= 0; i--)
                Icon(d, state, nodes[i], new Vector2(12 + size / 2 + step * i, Height / 2f), size, a);

            float x = 12 + iconsWidth + 10, width = Width - x - 10;
            string title = LocalizeUI(toast.Kind == ConstellationDiscoveryKind.Appeared ? "DiscoveryAppeared" : "DiscoveryUnsealed");
            string hint = LocalizeUI("DiscoveryOpen", key);
            float hintWidth = d.Measure(hint, 9);
            d.Spark(new Vector2(x + 4, 15), 8, ConstellationDrawing.Gold * a);
            d.FittedText(title, x + 12, 8, width - hintWidth - 18, 12, ConstellationDrawing.Gold * a, serif: true, spacing: 1.5f);
            d.Text(hint, Width - 10, 10, 9, ConstellationDrawing.Muted2 * a, 1);

            var first = nodes[0];
            string subtitle = nodes.Length == 1
                ? $"{state.Name(first)} · {ConstellationState.StatusName(state.Status(first))}"
                : LocalizeUI("DiscoveryMany", state.Name(first), nodes.Length);
            d.FittedText(subtitle, x, 28, width, 12, ConstellationDrawing.Ink * a);
        }
        finally { d.Origin = old; }
    }

    private static void Icon(ConstellationDrawing d, ConstellationState state, ConstellationNode node, Vector2 center, float size, float a)
    {
        d.Dot(center, size / 2, ConstellationDrawing.NodePlate * a);
        if (state.Discovered(node)) d.SkillIcon(node, center, size, Color.White * a, round: true);
        else d.Text("?", center.X, center.Y - size * .36f, size * .62f, ConstellationDrawing.Lavender * a, .5f);
        d.Ring(center, size / 2, size / 2, ConstellationDrawing.Gold * (.7f * a), 1.2f);
    }
}
