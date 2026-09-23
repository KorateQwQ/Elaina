using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace 伊蕾娜.ElainaModAlchemy;

/// <summary>
/// 新炼金系统的配方数据；产物类型由所属的 AlchemyItem 决定。
/// 不注册为原版 Recipe，也不负责消耗材料或生成物品。
/// </summary>
public sealed class AlchemyRecipe
{
    public IReadOnlyList<AlchemyIngredient> Ingredients { get; }
    public int ResultStack { get; }

    public AlchemyRecipe(int resultStack, params AlchemyIngredient[] ingredients)
    {
        if (resultStack <= 0)
            throw new ArgumentOutOfRangeException(nameof(resultStack));
        ArgumentNullException.ThrowIfNull(ingredients);
        if (ingredients.Length == 0)
            throw new ArgumentException("炼金配方至少需要一项材料。", nameof(ingredients));

        // Merge equivalent rows while retaining the first alternative's preference.
        var normalized = new List<AlchemyIngredient>();
        var indices = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (AlchemyIngredient ingredient in ingredients)
        {
            ArgumentNullException.ThrowIfNull(ingredient);
            string key = string.Join(",", ingredient.ItemTypes.OrderBy(type => type));
            if (indices.TryGetValue(key, out int index))
                normalized[index] = new AlchemyIngredient(normalized[index].ItemTypes.ToArray(),
                    checked(normalized[index].Stack + ingredient.Stack));
            else
            {
                indices.Add(key, normalized.Count);
                normalized.Add(ingredient);
            }
        }

        Ingredients = normalized.AsReadOnly();
        ResultStack = resultStack;
    }

    /// <summary>
    /// 检查传入的材料槽是否足够，支持同类材料分散在多个槽中；不会消耗物品。
    /// 调用方应传入实际允许参与炼金的槽位，每个槽位只传入一次。
    /// </summary>
    public bool HasIngredients(IReadOnlyList<Item> materials) => TryPlan(materials, 1, out _);

    /// <summary>
    /// Creates a slot-level consumption plan without changing any item. A residual
    /// flow graph prevents overlapping alternatives from spending the same stack
    /// twice, and can redirect an earlier choice to satisfy a later, stricter row.
    /// </summary>
    public bool TryPlan(IReadOnlyList<Item> materials, int batches, out int[] consume)
    {
        ArgumentNullException.ThrowIfNull(materials);
        consume = null;
        if (batches < 1 || batches > 99) return false;
        int[] types = Ingredients.SelectMany(i => i.ItemTypes).Distinct().ToArray();
        int rowStart = 1, typeStart = rowStart + Ingredients.Count, sink = typeStart + types.Length;
        var capacity = new long[sink + 1, sink + 1];
        var available = new long[types.Length];
        var seen = new HashSet<Item>(ReferenceEqualityComparer.Instance);
        foreach (Item item in materials)
        {
            if (item == null || item.IsAir || !seen.Add(item)) continue;
            int index = Array.IndexOf(types, item.type);
            if (index >= 0) available[index] += item.stack;
        }
        long required = 0;
        for (int row = 0; row < Ingredients.Count; row++)
        {
            var ingredient = Ingredients[row];
            long amount = (long)ingredient.Stack * batches;
            required += amount;
            capacity[0, rowStart + row] = amount;
            foreach (int type in ingredient.ItemTypes)
                capacity[rowStart + row, typeStart + Array.IndexOf(types, type)] = amount;
        }
        for (int i = 0; i < types.Length; i++) capacity[typeStart + i, sink] = available[i];
        long flow = 0;
        var parents = new int[sink + 1];
        var queue = new Queue<int>();
        while (flow < required)
        {
            Array.Fill(parents, -1);
            parents[0] = 0;
            queue.Clear();
            queue.Enqueue(0);
            while (queue.Count > 0 && parents[sink] < 0)
            {
                int from = queue.Dequeue();
                for (int to = 1; to <= sink; to++)
                    if (parents[to] < 0 && capacity[from, to] > 0)
                    {
                        parents[to] = from;
                        queue.Enqueue(to);
                    }
            }
            if (parents[sink] < 0) return false;
            long amount = required - flow;
            for (int to = sink; to != 0; to = parents[to]) amount = Math.Min(amount, capacity[parents[to], to]);
            for (int to = sink; to != 0; to = parents[to])
            {
                capacity[parents[to], to] -= amount;
                capacity[to, parents[to]] += amount;
            }
            flow += amount;
        }
        consume = new int[materials.Count];
        seen.Clear();
        for (int slot = 0; slot < materials.Count; slot++)
        {
            Item item = materials[slot];
            if (item == null || item.IsAir || !seen.Add(item)) continue;
            int index = Array.IndexOf(types, item.type);
            if (index < 0) continue;
            int amount = (int)Math.Min(item.stack, capacity[sink, typeStart + index]);
            consume[slot] = amount;
            capacity[sink, typeStart + index] -= amount;
        }
        return true;
    }
}
