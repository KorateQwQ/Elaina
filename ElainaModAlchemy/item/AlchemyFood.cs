using Terraria.ID;

namespace 伊蕾娜.ElainaModAlchemy.item;

/// <summary>炼金料理共用原版食用、饱食 Buff 与消耗流程。</summary>
public abstract class AlchemyFood : AlchemyItem
{
    protected abstract int FoodBuffType { get; }
    protected virtual int DurationTicks => 5 * 60 * 60;

    public override void SetDefaults()
    {
        base.SetDefaults();
        // 目录图标是单帧；不启用要求三帧图集的 IsFood（食物盘）绘制。
        Item.consumable = true;
        Item.useStyle = ItemUseStyleID.EatFood;
        Item.useTime = 17;
        Item.useAnimation = 17;
        Item.UseSound = SoundID.Item2;
        Item.useTurn = true;
        Item.buffType = FoodBuffType;
        Item.buffTime = DurationTicks;
    }
}
