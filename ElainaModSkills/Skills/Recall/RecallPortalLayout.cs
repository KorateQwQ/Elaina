using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

/// <summary>从固定锚点排布用于绘制、交互和传送的阵心；按网络配对键排序，不依赖弹幕槽位或到包顺序。</summary>
internal static class RecallPortalLayout
{
    internal const float Radius = 160f;
    internal const float HorizontalSpacing = Radius * 2 + 24;
    internal const float VerticalSpacing = Radius * .6f + 24;

    internal sealed class Entry(int owner, int pair, int side, Vector2 anchor)
    {
        internal readonly int Owner = owner, Pair = pair, Side = side;
        internal readonly Vector2 Anchor = anchor;
        internal Vector2 Center = anchor;
    }

    internal static void Arrange(List<Entry> entries, float worldWidth)
    {
        entries.Sort((a, b) =>
        {
            int order = a.Owner.CompareTo(b.Owner);
            if (order == 0) order = a.Pair.CompareTo(b.Pair);
            return order == 0 ? a.Side.CompareTo(b.Side) : order;
        });
        for (int i = 0; i < entries.Count; i++)
        {
            Vector2 anchor = entries[i].Anchor;
            entries[i].Center = anchor;
            if (Free(anchor, entries, i)) continue;
            // 优先向左右挪到最近的空位；世界边缘一整行放不下时才向上换行。
            for (int row = 0; row <= i; row++)
            {
                float y = anchor.Y - row * VerticalSpacing;
                float left = anchor.X, right = anchor.X;
                for (int pass = 0; pass <= i; pass++)
                {
                    bool moved = false;
                    for (int j = 0; j < i; j++)
                    {
                        Vector2 taken = entries[j].Center;
                        if (Math.Abs(y - taken.Y) >= VerticalSpacing) continue;
                        if (Math.Abs(left - taken.X) < HorizontalSpacing)
                        { left = taken.X - HorizontalSpacing; moved = true; }
                        if (Math.Abs(right - taken.X) < HorizontalSpacing)
                        { right = taken.X + HorizontalSpacing; moved = true; }
                    }
                    if (!moved) break;
                }
                bool canLeft = left >= Radius, canRight = right <= worldWidth - Radius;
                if (!canLeft && !canRight) continue;
                float x = canRight && (!canLeft || right - anchor.X <= anchor.X - left) ? right : left;
                entries[i].Center = new Vector2(x, y);
                break;
            }
        }
    }

    private static bool Free(Vector2 point, List<Entry> entries, int count)
    {
        for (int i = 0; i < count; i++)
            if (Math.Abs(point.X - entries[i].Center.X) < HorizontalSpacing &&
                Math.Abs(point.Y - entries[i].Center.Y) < VerticalSpacing) return false;
        return true;
    }
}
