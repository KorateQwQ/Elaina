using Terraria;
using Terraria.ID;

namespace 伊蕾娜.ElainaModAlchemy.item;

/// <summary>炼金药剂的共同饮用设置。当前药剂尚未配置配方和解锁方式。</summary>
public abstract class AlchemyPotion : AlchemyItem
{
    public override string LocalizationCategory => "Alchemy.Items";

    protected abstract int PotionBuffType { get; }
    protected virtual int DurationTicks => 5 * 60 * 60;

    public override bool IsRecipeUnlocked(Player player) => false;

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 30;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.useStyle = ItemUseStyleID.DrinkLiquid;
        Item.useTime = 17;
        Item.useAnimation = 17;
        Item.UseSound = SoundID.Item3;
        Item.useTurn = true;
        Item.rare = ItemRarityID.Blue;
        Item.buffType = PotionBuffType;
        Item.buffTime = DurationTicks;
    }
}
