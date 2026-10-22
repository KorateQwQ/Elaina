using System;
using System.Collections.Generic;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

/// <summary>
/// 回城法阵外圈的花体字笔迹。只求氛围，不要求玩家辨认；首次使用时生成一次，绘制时按进度"书写"。
/// </summary>
internal static class RecallPortalScript
{
    /// <summary>Angle 为相对起点的弧长参数（0..2π，需经 EllipseAngle 换算为椭圆角度）；Radius 为半径比例；Progress 为到达该点时的书写进度 0..1；PenDown 为 false 表示从这里起笔。</summary>
    public readonly record struct Vertex(float Angle, float Radius, float Progress, bool PenDown);

    public const float Baseline = .85f; // 基线所在半径比例
    public const float Unit = .045f;    // 小写字母 x 高度对应的半径比例；升部约 2.1、降部约 -1
    const float Slant = .3f, WordGap = .7f, LiftCost = .4f;
    const int Steps = 3; // 每段 Catmull-Rom 插值数，决定线段数量
    const float Aspect = .3f; // 与 RecallPortalDrawing 的椭圆纵横比一致
    const int TableSize = 256;

    // 文本只为了笔画节奏自然，不需要被读出；不在字形表中的字符会被跳过。
    const string Text = "redi ad domum tuam viator sub luce lunae et stellarum via te ducet in somnis ad limen tuum";

    static Vertex[] path;
    static float[] angles;
    static float perimeter;
    public static ReadOnlySpan<Vertex> Path => path ??= Build();

    /// <summary>
    /// 把均匀的弧长参数换算为椭圆角度。直接按角度排字时，透视压扁的左右两侧会挤成一团；
    /// 按投影后的周长均匀排布，字距在屏幕上保持一致。
    /// </summary>
    public static float EllipseAngle(float s)
    {
        if (angles == null) BuildAngles();
        float f = (s / MathF.Tau % 1 + 1) % 1 * TableSize;
        int i = Math.Min((int)f, TableSize - 1);
        return angles[i] + (angles[i + 1] - angles[i]) * (f - i);
    }

    static void BuildAngles()
    {
        const int samples = 4096;
        var length = new float[samples + 1];
        for (int i = 1; i <= samples; i++)
        {
            float t = (i - .5f) * MathF.Tau / samples;
            length[i] = length[i - 1] + MathF.Sqrt(Sq(MathF.Sin(t)) + Sq(Aspect * MathF.Cos(t))) * MathF.Tau / samples;
        }
        var table = new float[TableSize + 1];
        for (int k = 0, i = 0; k <= TableSize; k++)
        {
            float target = length[samples] * k / TableSize;
            while (i < samples - 1 && length[i + 1] < target) i++;
            float span = length[i + 1] - length[i];
            table[k] = (i + (span > 0 ? Math.Clamp((target - length[i]) / span, 0, 1) : 0)) * MathF.Tau / samples;
        }
        perimeter = length[samples];
        angles = table;
    }

    // 坐标以 x 高度为单位，基线 y = 0、向上为正；每个字母从基线附近入笔，在右侧出笔与下一个字母相连。
    // 连续两个相同的点会形成尖角。Marks 为写完整个单词后再补的 i 点、t 横。
    static readonly Dictionary<char, (float Width, float[] Stroke, float[] Mark)> Glyphs = new()
    {
        ['a'] = (1f, [0, .1f, .45f, .8f, .62f, .95f, .4f, 1, .18f, .75f, .12f, .35f, .28f, .05f, .5f, .3f, .65f, .95f, .65f, .95f, .6f, .35f, .72f, .03f, 1, .2f], null),
        ['b'] = (.95f, [0, .1f, .45f, 1, .7f, 1.8f, .62f, 2.15f, .42f, 2, .32f, 1.3f, .28f, .4f, .35f, .03f, .6f, .25f, .68f, .75f, .5f, .85f, .62f, .72f, .95f, .8f], null),
        ['c'] = (.85f, [0, .1f, .45f, .8f, .62f, .92f, .55f, .98f, .3f, .9f, .15f, .5f, .25f, .08f, .5f, .02f, .85f, .22f], null),
        ['d'] = (1f, [0, .1f, .45f, .8f, .62f, .95f, .4f, 1, .18f, .75f, .12f, .35f, .28f, .05f, .5f, .3f, .72f, 2.1f, .72f, 2.1f, .6f, .4f, .72f, .03f, 1, .2f], null),
        ['e'] = (.8f, [0, .1f, .4f, .45f, .55f, .8f, .4f, .95f, .22f, .7f, .25f, .25f, .45f, .02f, .8f, .2f], null),
        ['h'] = (1.05f, [0, .1f, .45f, 1, .7f, 1.8f, .62f, 2.15f, .42f, 2, .32f, 1.3f, .22f, 0, .22f, 0, .4f, .75f, .6f, .92f, .72f, .7f, .68f, .25f, .78f, .03f, 1.05f, .2f], null),
        ['i'] = (.55f, [0, .1f, .3f, .9f, .3f, .9f, .25f, .25f, .35f, .02f, .55f, .2f], [.36f, 1.45f, .44f, 1.52f]),
        ['l'] = (.8f, [0, .1f, .45f, 1, .65f, 1.8f, .58f, 2.15f, .4f, 2, .3f, 1.3f, .25f, .35f, .38f, .02f, .8f, .2f], null),
        ['m'] = (1.5f, [0, .1f, .2f, .85f, .2f, .85f, .18f, 0, .18f, 0, .35f, .75f, .5f, .9f, .6f, .6f, .58f, 0, .58f, 0, .75f, .75f, .9f, .9f, 1, .6f, .98f, .2f, 1.1f, .02f, 1.5f, .2f], null),
        ['n'] = (1.1f, [0, .1f, .2f, .85f, .2f, .85f, .18f, 0, .18f, 0, .35f, .75f, .52f, .92f, .62f, .65f, .6f, .2f, .7f, .02f, 1.1f, .2f], null),
        ['o'] = (.95f, [0, .1f, .4f, .8f, .55f, .98f, .32f, .95f, .16f, .55f, .25f, .08f, .45f, .05f, .58f, .5f, .55f, .95f, .62f, .78f, .95f, .85f], null),
        ['r'] = (.85f, [0, .1f, .25f, .85f, .38f, .78f, .5f, .92f, .45f, .5f, .42f, .15f, .55f, .02f, .85f, .2f], null),
        ['s'] = (.75f, [0, .1f, .35f, .95f, .35f, .95f, .5f, .55f, .48f, .15f, .3f, .05f, .2f, .18f, .4f, .08f, .75f, .2f], null),
        ['t'] = (.7f, [0, .1f, .32f, 1, .42f, 1.6f, .42f, 1.6f, .3f, .5f, .38f, .02f, .7f, .2f], [.1f, 1, .62f, 1.08f]),
        ['u'] = (1.05f, [0, .1f, .22f, .9f, .22f, .9f, .18f, .3f, .3f, .02f, .5f, .2f, .68f, .9f, .68f, .9f, .62f, .3f, .75f, .02f, 1.05f, .2f], null),
        ['v'] = (.85f, [0, .1f, .2f, .85f, .2f, .85f, .3f, .25f, .42f, .02f, .55f, .4f, .65f, .9f, .6f, .78f, .85f, .82f], null),
        ['y'] = (1f, [0, .1f, .22f, .9f, .22f, .9f, .18f, .3f, .3f, .02f, .5f, .2f, .68f, .9f, .68f, .9f, .55f, -.3f, .4f, -.95f, .22f, -1, .18f, -.75f, .45f, -.3f, .75f, 0, 1, .2f], null),
    };

    static Vertex[] Build()
    {
        // 按单词循环填满整圈：投影周长按 x 高度换算，正前后方的字母宽高比接近 1。
        if (angles == null) BuildAngles();
        float capacity = perimeter * Baseline / Unit;
        string[] words = Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var strokes = new List<List<(float X, float Y)>>();
        float x = 0;
        for (int w = 0; ; w = (w + 1) % words.Length)
        {
            float width = 0;
            foreach (char c in words[w]) if (Glyphs.TryGetValue(c, out var g)) width += g.Width;
            if (width == 0) continue;
            if (x + width > capacity - WordGap && strokes.Count > 0) break;
            var main = new List<(float, float)>();
            var marks = new List<List<(float, float)>>();
            foreach (char c in words[w])
            {
                if (!Glyphs.TryGetValue(c, out var g)) continue;
                for (int i = 0; i < g.Stroke.Length; i += 2) main.Add((x + g.Stroke[i], g.Stroke[i + 1]));
                if (g.Mark != null) marks.Add([(x + g.Mark[0], g.Mark[1]), (x + g.Mark[2], g.Mark[3])]);
                x += g.Width;
            }
            strokes.Add(Spline(main));
            strokes.AddRange(marks);
            x += WordGap;
        }

        // 书写进度按笔迹长度计算，抬笔移动记少量固定开销，让 i 点和 t 横也有短暂停顿。
        float total = 0;
        var lengths = new List<float>();
        foreach (var stroke in strokes)
        {
            total += LiftCost;
            for (int i = 1; i < stroke.Count; i++)
                total += MathF.Sqrt(Sq(stroke[i].X - stroke[i - 1].X) + Sq(stroke[i].Y - stroke[i - 1].Y));
        }
        var result = new List<Vertex>();
        float done = 0;
        foreach (var stroke in strokes)
        {
            done += LiftCost;
            for (int i = 0; i < stroke.Count; i++)
            {
                if (i > 0) done += MathF.Sqrt(Sq(stroke[i].X - stroke[i - 1].X) + Sq(stroke[i].Y - stroke[i - 1].Y));
                var (px, py) = stroke[i];
                result.Add(new((px + Slant * py) / x * MathF.Tau, Baseline + py * Unit, done / total, i > 0));
            }
        }
        return result.ToArray();
    }

    static float Sq(float v) => v * v;

    static List<(float X, float Y)> Spline(List<(float X, float Y)> p)
    {
        var result = new List<(float, float)> { p[0] };
        for (int i = 0; i < p.Count - 1; i++)
        {
            var (a, b, c, d) = (p[Math.Max(i - 1, 0)], p[i], p[i + 1], p[Math.Min(i + 2, p.Count - 1)]);
            if (b == c) continue; // 尖角处的重复点
            for (int s = 1; s <= Steps; s++)
            {
                float t = s / (float)Steps, t2 = t * t, t3 = t2 * t;
                float Blend(float v0, float v1, float v2, float v3) =>
                    .5f * (2 * v1 + (v2 - v0) * t + (2 * v0 - 5 * v1 + 4 * v2 - v3) * t2 + (3 * v1 - v0 - 3 * v2 + v3) * t3);
                result.Add((Blend(a.X, b.X, c.X, d.X), Blend(a.Y, b.Y, c.Y, d.Y)));
            }
        }
        return result;
    }
}
