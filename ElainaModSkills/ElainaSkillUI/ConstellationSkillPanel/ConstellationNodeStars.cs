using System;
using System.Collections.Generic;
using System.Linq;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// Distance is measured outward from the node ring, Angle in degrees (screen space, +y down),
// From is the star this one is linked from; the first star has no link.
internal readonly record struct ConstellationStar(float Distance, float Angle, float Size, int From);

// A port of constOf in Tools/SkillFramePreview/perch.html. Each node draws a small constellation
// outside its ring; the pattern and its rotation/mirror come from an FNV-1a hash of the node id, so
// they stay fixed between sessions. Stars avoid the butterfly perch, the caption below and links.
internal static class ConstellationNodeStars
{
    private static readonly ConstellationStar[][] Shapes =
    [
        // Hook: five stars curve along the ring, with a branch from the bright second star.
        [new(7, -62, 1.3f, -1), new(13, -28, 3.8f, 0), new(7.5f, 6, 1.5f, 1), new(12, 38, 2.6f, 2),
         new(6, 66, 1.2f, 3), new(21, -8, 1.1f, 1)],
        // Belt: three brightening stars in a row, with one distant star.
        [new(9, -82, 1.2f, -1), new(9, -58, 2.2f, 0), new(10, -34, 3.6f, 1), new(9, -8, 1.4f, 2),
         new(22, -42, 1.1f, 2)],
        // Pendant: a hanging chain, bright in the middle, with a branch outward.
        [new(6, -18, 1.2f, -1), new(11, 8, 3.4f, 0), new(7, 34, 1.4f, 1), new(12, 56, 2.3f, 2),
         new(21, 18, 1.1f, 1)],
        // Pair: two bright stars with a small dot between them.
        [new(11, -50, 3.0f, -1), new(7, -18, 1.2f, 0), new(12, 16, 3.6f, 1), new(6, 44, 1.1f, 2)],
    ];
    // The butterfly perch on the upper left and the level badge straight below. The preview keeps
    // 66..114 clear for a small "Lv" label; the panel's 60px badge box needs 50..130.
    private static readonly (double Low, double High)[] Blocked =
        [(ConstellationDrawing.PerchAngle - 35, ConstellationDrawing.PerchAngle + 33), (50, 130)];
    private const double LinkClearance = 14;
    private static readonly Dictionary<string, (double[] Links, ConstellationStar[] Stars)> Cache = [];

    internal static ConstellationStar[] For(ConstellationNode node, IEnumerable<ConstellationNode> skills)
    {
        // Directions from this node to every prerequisite and dependent it is linked with.
        var links = new List<double>();
        string[] prereqs = node.prereqs;
        foreach (var other in skills)
            if (other != node && (prereqs.Contains(other.id) || other.prereqs.Contains(node.id)))
                links.Add(Math.Atan2(other.y - node.y, other.x - node.x) * 180 / Math.PI);
        string id = node.id;
        // Layout editing moves nodes, so a cached placement is only reused for the same link directions.
        if (Cache.TryGetValue(id, out var cached) && cached.Links.SequenceEqual(links)) return cached.Stars;
        var stars = Place(id, links);
        Cache[id] = (links.ToArray(), stars);
        return stars;
    }

    internal static bool IsFree(double angle, IReadOnlyList<double> links)
    {
        double a = Normalize(angle);
        foreach (var (low, high) in Blocked)
            if (a >= low && a <= high) return false;
        foreach (double link in links)
            if (Math.Abs(Normalize(angle - link)) <= LinkClearance) return false;
        return true;
    }

    private static ConstellationStar[] Place(string id, IReadOnlyList<double> links)
    {
        uint hash = Hash(id);
        var shape = Shapes[hash % Shapes.Length];
        // Cluster candidates by where they sit, so the many right-hand placements do not dominate.
        var groups = new List<(int Key, List<double[]> Items)>();
        foreach (double scale in new[] { 1, .8, .66 })
        foreach (int mirror in new[] { 1, -1 })
        for (int center = -180; center < 180; center += 6)
        {
            double[] angles = Rotate(shape, center, mirror, scale);
            if (!angles.All(a => IsFree(a, links))) continue;
            int key = (int)Math.Floor(Normalize((angles.Min() + angles.Max()) / 2) / 60 + .5);
            int index = groups.FindIndex(g => g.Key == key);
            if (index < 0) { groups.Add((key, [])); index = groups.Count - 1; }
            groups[index].Items.Add(angles);
        }
        if (groups.Count == 0) return shape;
        // Same order as Object.values in the preview: non-negative keys ascending, then the rest as inserted.
        var ordered = groups.Where(g => g.Key >= 0).OrderBy(g => g.Key).Concat(groups.Where(g => g.Key < 0)).ToList();
        var group = ordered[(int)((hash >> 3) % (uint)ordered.Count)].Items;
        double[] chosen = group[(int)((hash >> 8) % (uint)group.Count)];
        var result = new ConstellationStar[shape.Length];
        for (int i = 0; i < shape.Length; i++) result[i] = shape[i] with { Angle = (float)chosen[i] };
        return result;
    }

    private static double[] Rotate(ConstellationStar[] shape, double center, int mirror, double scale)
    {
        double min = shape.Min(s => (double)s.Angle), max = shape.Max(s => (double)s.Angle), mid = (min + max) / 2;
        return shape.Select(s => center + mirror * (s.Angle - mid) * scale).ToArray();
    }

    private static double Normalize(double angle) => (angle + 540) % 360 - 180;

    private static uint Hash(string text)
    {
        uint hash = 2166136261;
        foreach (char c in text) hash = unchecked((hash ^ c) * 16777619);
        return hash;
    }
}
