using System;
using System.Collections.Generic;

namespace 伊蕾娜.ElainaModAlchemy.Crafting;

internal enum FacsimilePriority
{
    Materials,
    Dust
}

/// <summary>与游戏/UI无关的补料规则。此规则不用于 AlchemyRecipe。</summary>
internal static class FacsimileMaterialRule
{
    internal readonly record struct Requirement(int Stack, int[] AcceptedTypes);
    internal readonly record struct Quote(int Dust, double Fulfillment);

    // 一个材料类型只属于一条需求。重复需求应预先合并；相交而不等价的配方组交回原版。
    internal static bool TryQuote(IReadOnlyList<Requirement> requirements,
        IReadOnlyDictionary<int, int> owned, int dustType, FacsimilePriority priority, out Quote quote)
    {
        quote = default;
        if (requirements.Count == 0)
            return false;

        long dustNeeded = 0;
        double fulfillment = 0;
        for (int i = 0; i < requirements.Count; i++)
        {
            Requirement requirement = requirements[i];
            long available = 0;
            foreach (int type in requirement.AcceptedTypes)
            {
                if (type == dustType)
                    return false; // 粉尘不能替代自己，也不能充当某种材料的首次样本。
                if (owned.TryGetValue(type, out int count) && count > 0)
                    available += count;
            }

            if (requirement.Stack <= 0 || available <= 0)
                return false;
            int supplied = (int)Math.Min(available, requirement.Stack);
            fulfillment += (double)supplied / requirement.Stack;
            dustNeeded += priority == FacsimilePriority.Dust
                ? Math.Max(0, requirement.Stack - 1)
                : requirement.Stack - supplied;
        }

        fulfillment /= requirements.Count;
        if (fulfillment + 1e-12 < 0.5 || dustNeeded > int.MaxValue)
            return false;
        quote = new Quote((int)dustNeeded, fulfillment);
        return dustNeeded == 0 || owned.TryGetValue(dustType, out int dust) && dust >= dustNeeded;
    }
}
