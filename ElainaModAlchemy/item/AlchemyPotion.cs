using Terraria;
using Terraria.ID;

namespace 伊蕾娜.ElainaModAlchemy.item;

/// <summary>炼金药剂的共同饮用设置；尚未配置 Buff 的药剂可以正常注册，但暂时不可饮用。</summary>
public abstract class AlchemyPotion : AlchemyItem
{
    /// <summary>0 表示效果尚未实现。配置有效 Buff 后，自动启用饮用与消耗。</summary>
    protected virtual int PotionBuffType => 0;
    protected virtual int DurationTicks => 5 * 60 * 60;

    public override void SetDefaults()
    {
        base.SetDefaults();
        int buffType = PotionBuffType;
        if (buffType <= 0) return;

        // Preserve the established defaults of the existing drinkable potions.
        Item.width = 20;
        Item.height = 30;
        Item.rare = ItemRarityID.Blue;
        Item.consumable = true;
        Item.useStyle = ItemUseStyleID.DrinkLiquid;
        Item.useTime = 17;
        Item.useAnimation = 17;
        Item.UseSound = SoundID.Item3;
        Item.useTurn = true;
        Item.buffType = buffType;
        Item.buffTime = DurationTicks;
    }

    public override bool CanUseItem(Player player) => PotionBuffType > 0;
}
