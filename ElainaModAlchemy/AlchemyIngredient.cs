using System;

namespace 伊蕾娜.ElainaModAlchemy;

/// <summary>一项炼金材料，使用原版或模组物品的类型 ID。</summary>
public sealed class AlchemyIngredient
{
    public int ItemType { get; }
    public int Stack { get; }

    public AlchemyIngredient(int itemType, int stack = 1)
    {
        if (itemType <= 0)
            throw new ArgumentOutOfRangeException(nameof(itemType));
        if (stack <= 0)
            throw new ArgumentOutOfRangeException(nameof(stack));

        ItemType = itemType;
        Stack = stack;
    }
}
