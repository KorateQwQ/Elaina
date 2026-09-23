using System;
using System.Collections.Generic;
using System.Linq;

namespace 伊蕾娜.ElainaModAlchemy;

/// <summary>一项炼金材料。替代品可合计满足数量；首项用于调试补充。</summary>
public sealed class AlchemyIngredient
{
    public int ItemType => ItemTypes[0];
    public IReadOnlyList<int> ItemTypes { get; }
    public int Stack { get; }

    public AlchemyIngredient(int itemType, int stack = 1) : this(new[] { itemType }, stack) { }

    public AlchemyIngredient(int[] itemTypes, int stack = 1)
    {
        ArgumentNullException.ThrowIfNull(itemTypes);
        if (itemTypes.Length == 0 || itemTypes.Any(type => type <= 0))
            throw new ArgumentOutOfRangeException(nameof(itemTypes));
        if (stack <= 0)
            throw new ArgumentOutOfRangeException(nameof(stack));

        ItemTypes = Array.AsReadOnly(itemTypes.Distinct().ToArray());
        Stack = stack;
    }
}
