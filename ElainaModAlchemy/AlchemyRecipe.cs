using System;
using System.Collections.Generic;
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

        // 合并重复材料，防止同一份材料被多次用于满足不同条目。
        var amounts = new Dictionary<int, int>();
        foreach (AlchemyIngredient ingredient in ingredients)
        {
            ArgumentNullException.ThrowIfNull(ingredient);
            amounts.TryGetValue(ingredient.ItemType, out int amount);
            amounts[ingredient.ItemType] = checked(amount + ingredient.Stack);
        }

        var normalized = new List<AlchemyIngredient>();
        foreach (KeyValuePair<int, int> entry in amounts)
            normalized.Add(new AlchemyIngredient(entry.Key, entry.Value));

        Ingredients = normalized.AsReadOnly();
        ResultStack = resultStack;
    }

    /// <summary>
    /// 检查传入的材料槽是否足够，支持同类材料分散在多个槽中；不会消耗物品。
    /// 调用方应传入实际允许参与炼金的槽位，每个槽位只传入一次。
    /// </summary>
    public bool HasIngredients(IReadOnlyList<Item> materials)
    {
        ArgumentNullException.ThrowIfNull(materials);

        foreach (AlchemyIngredient ingredient in Ingredients)
        {
            long available = 0;
            foreach (Item material in materials)
            {
                if (material != null && !material.IsAir && material.type == ingredient.ItemType)
                    available += material.stack;

                if (available >= ingredient.Stack)
                    break;
            }

            if (available < ingredient.Stack)
                return false;
        }

        return true;
    }
}
