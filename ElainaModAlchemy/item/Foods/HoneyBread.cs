using Terraria;
using Terraria.ID;

namespace 伊蕾娜.ElainaModAlchemy.item.Foods;

/// <summary>蜂蜜面包：伊蕾娜获得很满意并恢复 30 生命，其他角色获得吃得好。</summary>
public sealed class HoneyBread : AlchemyFood
{
    public override string EntryId => "bread";
    protected override int FoodBuffType => BuffID.WellFed;

    public override void SetStaticDefaults() => ItemID.Sets.WorksInVoidBag[Type] = true;

    // 原版快捷增益在 CanUseItem 之前比较食物等级；背包和开启的虚空袋均会调用此钩子。
    public override void UpdateInventory(Player player) => UpdateFoodBuff(player);

    public override bool CanUseItem(Player player)
    {
        UpdateFoodBuff(player);
        return !player.dead;
    }

    private void UpdateFoodBuff(Player player) =>
        Item.buffType = player.GetModPlayer<ElainaModplayer>().Elaina ? BuffID.WellFed2 : BuffID.WellFed;

    public override void OnConsumeItem(Player player)
    {
        // 消耗钩子兼容手动食用和快捷增益，只由食用者结算一次额外治疗。
        if (player.whoAmI == Main.myPlayer && !player.dead &&
            player.GetModPlayer<ElainaModplayer>().Elaina && player.statLife < player.statLifeMax2)
            player.Heal(30);
    }
}
