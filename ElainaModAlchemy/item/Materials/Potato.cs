using Terraria;

namespace 伊蕾娜.ElainaModAlchemy.item.Materials;

/// <summary>土豆：通过商店购买的炼金食材。</summary>
public sealed class Potato : AlchemyItem
{
    public override string EntryId => "potato";

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.value = Item.buyPrice(silver: 1);
    }
}
