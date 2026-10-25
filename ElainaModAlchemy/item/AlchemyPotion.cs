using Terraria;
using Terraria.ID;

namespace 伊蕾娜.ElainaModAlchemy.item;

/// <summary>炼金药剂的共同饮用设置；尚未配置 Buff 的药剂可以正常注册，但暂时不可饮用。</summary>
public abstract class AlchemyPotion : AlchemyItem
{
    /// <summary>0 表示效果尚未实现。配置有效 Buff 后，自动启用饮用与消耗。</summary>
    protected virtual int PotionBuffType => 0;
    protected virtual int DurationTicks => 4 * 60 * 60;

    public override void SetDefaults()
    {
        base.SetDefaults();
        int buffType = PotionBuffType;
        if (buffType <= 0) return;

        // 与原版愤怒药水一样交给 Item.buffType/buffTime 处理饮用、续时和快捷增益。
        // 尺寸与稀有度保留炼金目录的值。
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
