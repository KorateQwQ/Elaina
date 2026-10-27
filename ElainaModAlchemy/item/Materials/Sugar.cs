using Terraria;

namespace 伊蕾娜.ElainaModAlchemy.item.Materials;

/// <summary>糖：通过商店购买的炼金食材。</summary>
public sealed class Sugar : AlchemyItem
{
    public override string EntryId => "sugar";

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.value = Item.buyPrice(silver: 1);
    }
}
