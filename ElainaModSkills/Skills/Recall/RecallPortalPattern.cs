using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

/// <summary>
/// 回城法阵内部纹路的笔画表。坐标位于地面平面，以法阵半径为单位（y 向屏幕下方即玩家前方），
/// 由绘制方法旋转并压扁投影；首次使用时生成一次。
/// </summary>
internal static class RecallPortalPattern
{
    /// <summary>
    /// Points 中 [Start, Start + Count) 为一条连续折线，描绘进度从 Begin 到 End 时逐段画出（以蓄力时长为 1）。
    /// Layer：0 静止、1 随六芒星正转、2 反转。Rim 为外圈，鼠标悬停时加亮。
    /// </summary>
    public readonly record struct Stroke(int Start, int Count, float Begin, float End, int Layer, float Width, float Alpha, bool Rim = false);

    public const float StarRadius = .66f, NodeRadius = .08f;

    static Vector2[] points;
    static Stroke[] strokes;
    public static ReadOnlySpan<Vector2> Points { get { if (points == null) Build(); return points; } }
    public static ReadOnlySpan<Stroke> Strokes { get { if (strokes == null) Build(); return strokes; } }

    static readonly List<Vector2> pointList = new();
    static readonly List<Stroke> strokeList = new();

    static void Add(IEnumerable<Vector2> line, float begin, float end, int layer, float width, float alpha, bool rim = false)
    {
        int start = pointList.Count;
        pointList.AddRange(line);
        strokeList.Add(new(start, pointList.Count - start, begin, end, layer, width, alpha, rim));
    }

    static Vector2 Polar(float angle, float r) => new(MathF.Cos(angle) * r, MathF.Sin(angle) * r);

    /// <summary>圆弧；sweep 为负时逆时针描绘。</summary>
    static IEnumerable<Vector2> Arc(Vector2 center, float r, float from, float sweep, int segments)
    {
        for (int i = 0; i <= segments; i++) yield return center + Polar(from + sweep * i / segments, r);
    }

    /// <summary>直线细分为等长小段，笔尖的亮度渐变才能只落在最后一小截。</summary>
    static IEnumerable<Vector2> Straight(Vector2 from, Vector2 to, int segments)
    {
        for (int i = 0; i <= segments; i++) yield return Vector2.Lerp(from, to, i / (float)segments);
    }

    /// <summary>
    /// 正多边形：每条边是一条独立笔画，同一时间窗口内从各自的起始顶点同时出发，
    /// 沿同一绕向画到下一个顶点。trim 为两端各截掉的长度（用于避开顶点小圆）。
    /// </summary>
    static void AddPolygon(float r, int sides, float rotation, float trim, int segmentsPerSide,
        float begin, float end, int layer, float width, float alpha)
    {
        for (int i = 0; i < sides; i++)
        {
            Vector2 a = Polar(rotation + i * MathF.Tau / sides, r), b = Polar(rotation + (i + 1) * MathF.Tau / sides, r);
            Vector2 cut = Vector2.Normalize(b - a) * trim;
            Add(Straight(a + cut, b - cut, segmentsPerSide), begin, end, layer, width, alpha);
        }
    }

    /// <summary>顶点小圆内的炼金符号，局部坐标 y 朝外；依次为日、火、月、水、风、土。</summary>
    static IEnumerable<IEnumerable<Vector2>> Symbol(int kind)
    {
        const float s = .045f;
        Vector2[] up = [new(0, s), new(-s * .87f, -s * .5f), new(s * .87f, -s * .5f), new(0, s)];
        Vector2[] down = [new(0, -s), new(s * .87f, s * .5f), new(-s * .87f, s * .5f), new(0, -s)];
        Vector2[] bar = [new(-s * .8f, 0), new(s * .8f, 0)];
        switch (kind)
        {
            case 0:
                yield return Arc(Vector2.Zero, s * .8f, MathF.PI / 2, MathF.Tau, 16);
                yield return Arc(Vector2.Zero, s * .15f, 0, MathF.Tau, 6);
                break;
            case 1: yield return up; break;
            case 2:
                // 外弧向左鼓起，内弧往回收，形成新月。
                var moon = new List<Vector2>(Arc(Vector2.Zero, s * .85f, MathF.PI / 3, MathF.PI * 4 / 3, 12));
                moon.AddRange(Arc(new Vector2(s * .35f, 0), s * .7f, MathF.PI * 5 / 3 - .2f, -MathF.PI * 4 / 3 + .4f, 10));
                moon.Add(moon[0]);
                yield return moon;
                break;
            case 3: yield return down; break;
            case 4: yield return up; yield return bar; break;
            default: yield return down; yield return bar; break;
        }
    }

    static void Build()
    {
        pointList.Clear();
        strokeList.Clear();
        float vertexGap = 2 * MathF.Asin(NodeRadius / (2 * StarRadius));

        // 外圈：外环顺时针、内框逆时针，与花体字同时开始。
        Add(Arc(Vector2.Zero, 1, MathF.PI, MathF.Tau, 96), 0, .22f, 0, 2, .7f, true);
        Add(Arc(Vector2.Zero, .78f, MathF.PI, -MathF.Tau, 96), .04f, .26f, 0, 1, .5f);
        Add(Arc(Vector2.Zero, .75f, 0, MathF.Tau, 96), .08f, .3f, 0, 1, .3f);

        for (int i = 0; i < 6; i++)
        {
            float angle = -MathF.PI / 2 + i * MathF.Tau / 6;
            // 星圆在顶点小圆处断开，避免线条穿过符号。
            Add(Arc(Vector2.Zero, StarRadius, angle + vertexGap, MathF.Tau / 6 - 2 * vertexGap, 14),
                .2f + i * .025f, .3f + i * .025f, 1, 1.5f, .6f);
            for (int k = 1; k <= 3; k++)
            {
                float tick = angle + k * MathF.Tau / 24;
                float length = k == 2 ? .06f : .035f;
                Add([Polar(tick, StarRadius), Polar(tick, StarRadius + length)],
                    .3f + (i * 3 + k) * .008f, .34f + (i * 3 + k) * .008f, 1, 1, .5f);
            }
        }

        // 六芒星：两个三角形先后画出，每个三角形的三条边从三个顶点同时出发；边在顶点小圆处截断。
        for (int triangle = 0; triangle < 2; triangle++)
        {
            float begin = .15f + triangle * .2f;
            AddPolygon(StarRadius, 3, -MathF.PI / 2 + triangle * MathF.Tau / 6, NodeRadius, 24, begin, begin + .3f, 1, 1.5f, .65f);
        }

        // 顶点小圆与炼金符号，符号朝向法阵外侧。
        for (int i = 0; i < 6; i++)
        {
            float angle = -MathF.PI / 2 + i * MathF.Tau / 6;
            Vector2 node = Polar(angle, StarRadius);
            Add(Arc(node, NodeRadius, angle, MathF.Tau, 20), .35f + i * .04f, .45f + i * .04f, 1, 1.2f, .8f);
            Vector2 tangent = new(-MathF.Sin(angle), MathF.Cos(angle)), outward = new(MathF.Cos(angle), MathF.Sin(angle));
            foreach (var line in Symbol(i))
            {
                var placed = new List<Vector2>();
                foreach (Vector2 p in line) placed.Add(node + tangent * p.X + outward * p.Y);
                Add(placed, .45f + i * .04f, .55f + i * .04f, 1, 1, .9f);
            }
        }

        // 反向旋转的虚线环。
        for (int k = 0; k < 24; k++)
            Add(Arc(Vector2.Zero, .5f, k * MathF.Tau / 24, MathF.Tau / 48, 2), .5f + k * .01f, .54f + k * .01f, 2, 1, .5f);

        // 中心：内圆与双方形组成的八芒星，方形四边从四个角同时画出。
        Add(Arc(Vector2.Zero, .3f, -MathF.PI / 2, -MathF.Tau, 48), .3f, .75f, 2, 1.2f, .6f);
        AddPolygon(.3f, 4, -MathF.PI / 2, 0, 12, .45f, .8f, 2, 1, .55f);
        AddPolygon(.3f, 4, -MathF.PI / 4, 0, 12, .53f, .88f, 2, 1, .55f);

        points = pointList.ToArray();
        strokes = strokeList.ToArray();
        // 生成后只保留数组，临时列表释放掉。
        pointList.Clear();
        pointList.TrimExcess();
        strokeList.Clear();
        strokeList.TrimExcess();
    }
}
