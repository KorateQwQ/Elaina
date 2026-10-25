using Terraria;
using Terraria.ID;

namespace 伊蕾娜.ElainaModAlchemy.item.Curios;

/// <summary>炼金造物；仅为常规工作台合成补料，不参与炼金配方的替代判定。</summary>
public sealed class AshenFacsimileDust : AlchemyItem
{
    public override string EntryId => "mimic";

    public override void SetDefaults()
    {
        Item.width = CatalogEntry.PixelWidth;
        Item.height = CatalogEntry.PixelHeight;
        Item.maxStack = Item.CommonMaxStack;
        Item.rare = ItemRarityID.Blue;
    }
}
