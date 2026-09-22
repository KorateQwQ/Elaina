using Terraria;
using Terraria.ID;

namespace 伊蕾娜.ElainaModAlchemy.item;

/// <summary>炼金造物；仅为常规工作台合成补料，不参与炼金配方的替代判定。</summary>
public sealed class AshenFacsimileDust : AlchemyItem
{
    public override string LocalizationCategory => "Alchemy.Items";

    // 尚未设计获取配方，与现有炼金药剂保持一致。
    public override bool IsRecipeUnlocked(Player player) => false;

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 30;
        Item.maxStack = Item.CommonMaxStack;
        Item.rare = ItemRarityID.Blue;
    }
}
