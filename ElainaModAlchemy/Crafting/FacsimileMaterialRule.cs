using System;
using System.Collections.Generic;

namespace 伊蕾娜.ElainaModAlchemy.Crafting;

internal enum FacsimilePriority
{
    Materials,
    Dust
}

internal enum FacsimileCraftRoute
{
    Original,
    Dust,
    None
}

/// <summary>与游戏/UI无关的补料规则。此规则不用于 AlchemyRecipe。</summary>
internal static class FacsimileMaterialRule
{
    internal readonly record struct Requirement(int Stack, int[] AcceptedTypes);
    internal readonly record struct Quote(int Dust);

    /// <summary>合成栏右键无论缺料与否都每种只用1件、其余全用灰赝尘；炼金手记只补缺口。</summary>
    internal static FacsimilePriority PriorityFor(bool alchemy) =>
        alchemy ? FacsimilePriority.Materials : FacsimilePriority.Dust;

    /// <summary>合成栏左键只用原料、右键使用灰赝尘；炼金手记没有左右键之分，仍只在缺料时补料。</summary>
    internal static FacsimileCraftRoute ChooseRoute(bool alchemy, bool rightClick, bool hasOriginals)
    {
        if (alchemy)
            return hasOriginals ? FacsimileCraftRoute.Original : FacsimileCraftRoute.Dust;
        if (rightClick)
            return FacsimileCraftRoute.Dust;
        return hasOriginals ? FacsimileCraftRoute.Original : FacsimileCraftRoute.None;
    }

    internal static bool HasOriginals(IReadOnlyList<Requirement> requirements, IReadOnlyDictionary<int, int> owned)
    {
        foreach (Requirement requirement in requirements)
        {
            long available = 0;
            foreach (int type in requirement.AcceptedTypes)
            {
                if (owned.TryGetValue(type, out int count) && count > 0)
                    available += count;
            }
            if (available < requirement.Stack)
                return false;
        }
        return true;
    }

    // 一个材料类型只属于一条需求。重复需求应预先合并；相交而不等价的配方组交回原版。
    internal static bool TryQuote(IReadOnlyList<Requirement> requirements,
        IReadOnlyDictionary<int, int> owned, int dustType, FacsimilePriority priority, out Quote quote)
    {
        quote = default;
        if (requirements.Count == 0)
            return false;

        // 唯一门槛：每条需求至少有1件真实材料，缺口全部可由灰赝尘补上。
        long dustNeeded = 0;
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
            dustNeeded += priority == FacsimilePriority.Dust
                ? Math.Max(0, requirement.Stack - 1)
                : requirement.Stack - supplied;
        }

        if (dustNeeded > int.MaxValue)
            return false;
        quote = new Quote((int)dustNeeded);
        return dustNeeded == 0 || owned.TryGetValue(dustType, out int dust) && dust >= dustNeeded;
    }
}
