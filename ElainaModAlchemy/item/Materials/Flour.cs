using Terraria;

namespace 伊蕾娜.ElainaModAlchemy.item.Materials;

/// <summary>面粉：通过商店购买的炼金食材。</summary>
public sealed class Flour : AlchemyItem
{
    public override string EntryId => "flour";

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.value = Item.buyPrice(silver: 1);
    }
}
