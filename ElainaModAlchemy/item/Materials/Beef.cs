using Terraria;

namespace 伊蕾娜.ElainaModAlchemy.item.Materials;

/// <summary>牛肉：通过商店购买的炼金食材。</summary>
public sealed class Beef : AlchemyItem
{
    public override string EntryId => "beef";

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.value = Item.buyPrice(silver: 5);
    }
}
