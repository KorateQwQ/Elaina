using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.ElainaModAlchemy.Purification;

/// <summary>净土保护坐标（包括空格），按列合并区间。只在转换/生长/物块更新时查询，不轮询世界。</summary>
public sealed partial class PurificationProtectionSystem : ModSystem
{
    private readonly record struct Span(int Start, int End);
    private static readonly object gate = new(); // 困难模式地形生成可能从工作线程查询。
    private static readonly Dictionary<int, List<Span>> columns = new();

    internal static bool IsProtected(int x, int y)
    {
        lock (gate)
        {
            if (!columns.TryGetValue(x, out var spans)) return false;
            int lo = 0, hi = spans.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                Span span = spans[mid];
                if (y < span.Start) hi = mid - 1;
                else if (y > span.End) lo = mid + 1;
                else return true;
            }
            return false;
        }
    }

    internal static void ProtectRow(int centerX, int y)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        for (int x = centerX - 1; x <= centerX + 1; x++) AddSpan(x, y, y);
    }

    private static void AddSpan(int x, int start, int end)
    {
        if (x < 1 || x >= Main.maxTilesX - 1 || start > end) return;
        start = Math.Max(1, start); end = Math.Min(Main.maxTilesY - 2, end);
        if (start > end) return;
        lock (gate)
        {
            if (!columns.TryGetValue(x, out var spans)) columns[x] = spans = new();
            int index = 0;
            while (index < spans.Count && spans[index].End < start - 1) index++;
            int first = index;
            while (index < spans.Count && spans[index].Start <= end + 1)
            {
                start = Math.Min(start, spans[index].Start);
                end = Math.Max(end, spans[index].End);
                index++;
            }
            if (index > first) spans.RemoveRange(first, index - first);
            spans.Insert(first, new Span(start, end));
        }
    }

    private static int[] Snapshot()
    {
        lock (gate)
        {
            var result = new List<int>();
            foreach (int x in columns.Keys.OrderBy(x => x))
                foreach (Span span in columns[x]) { result.Add(x); result.Add(span.Start); result.Add(span.End); }
            return result.ToArray();
        }
    }

    public override void SaveWorldData(TagCompound tag)
    {
        int[] spans = Snapshot();
        if (spans.Length > 0) tag["PurifiedColumnsV1"] = spans;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        ClearWorld();
        int[] spans = tag.GetIntArray("PurifiedColumnsV1");
        for (int i = 0; i + 2 < spans.Length; i += 3) AddSpan(spans[i], spans[i + 1], spans[i + 2]);
    }

    public override void ClearWorld() { lock (gate) columns.Clear(); }
}
