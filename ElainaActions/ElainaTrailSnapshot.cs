using System;
using System.Collections.Generic;

namespace 伊蕾娜.ElainaActions;

/// <summary>不可变的拖尾几何与外观；队列执行、动作切换后都不再读取玩家姿势。</summary>
public sealed class ElainaTrailSnapshot
{
    private readonly Vector2[] points;
    private readonly float[] widths;
    private readonly Vector2 attachPoint;
    private readonly float attachRotation;
    private readonly Color color;
    private readonly float maxWidth, endWidth, opacity;
    private readonly bool debugPoint, useBloom, behindPlayer;

    public ElainaTrailSnapshot(Vector2[] points, Vector2 attachPoint, float attachRotation, float actionProgress,
        Color color, float maxWidth, float endWidth, float? fadeInEnd = null, float? fadeOutStart = null,
        float[] widths = null, bool debugPoint = false, bool useBloom = true, bool behindPlayer = false)
    {
        this.points = (Vector2[])points.Clone();
        this.widths = widths == null ? null : (float[])widths.Clone();
        this.attachPoint = attachPoint;
        this.attachRotation = attachRotation;
        this.color = color;
        this.maxWidth = maxWidth;
        this.endWidth = endWidth;
        this.debugPoint = debugPoint;
        this.useBloom = useBloom;
        this.behindPlayer = behindPlayer;
        opacity = ElainaTrailVisualStyle.GetActionAlpha(actionProgress, fadeInEnd, fadeOutStart);
    }

    public bool Visible => opacity > 0f && points.Length >= 2;

    public void RequestDraw(float remainingOpacity = 1f)
    {
        float alpha = opacity * remainingOpacity;
        if (!Visible || alpha <= 0f)
            return;
        ElainaTrailVisualStyle.RequestTrail(() =>
            ElainaTrailVisualStyle.DrawTrailOpacity(points, widths, attachPoint, attachRotation,
                color, maxWidth, endWidth, alpha, debugPoint: debugPoint), useBloom, behindPlayer);
    }
}

/// <summary>游戏帧驱动的短收尾。停更时不老化，重复绘制也不推进时间。</summary>
internal sealed class ElainaTrailAfterimage
{
    internal const int Lifetime = 8;
    private readonly ElainaTrailSnapshot[] trails;
    private int age;
    public int PlayerIndex { get; }
    public float Opacity => ElainaTrailVisualStyle.SmoothOpacity(1f - age / (float)Lifetime);

    public ElainaTrailAfterimage(int playerIndex, List<ElainaTrailSnapshot> snapshots)
    {
        PlayerIndex = playerIndex;
        trails = snapshots.ToArray();
    }

    public bool Update() => ++age < Lifetime;

    public void RequestDraw()
    {
        float opacity = Opacity;
        foreach (ElainaTrailSnapshot trail in trails)
            trail.RequestDraw(opacity);
    }
}
