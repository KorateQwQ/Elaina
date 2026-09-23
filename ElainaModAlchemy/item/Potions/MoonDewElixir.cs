using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

/// <summary>
/// 月露合剂的炼金物品。
/// 配方暂未配置；饮用后会像魔力合剂一样写入伊蕾娜的专属魔力药水槽。
/// </summary>
public sealed class MoonDewElixir : AlchemyItem
{
    public override string EntryId => "mana";
    private const int RestoreAmount = 50;

    // AlchemyRecipe 暂为空，避免在材料尚未确定时引入临时配方。

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
        Item.value = 0;
    }

    public override bool CanUseItem(Player player)
    {
        return player.GetModPlayer<ElainaManaElixirPlayer>().CanStoreCharge(RestoreAmount);
    }

    public override bool? UseItem(Player player) => true;

    public override bool ConsumeItem(Player player)
    {
        return player.GetModPlayer<ElainaManaElixirPlayer>().CanStoreCharge(RestoreAmount);
    }

    public override void OnConsumeItem(Player player)
    {
        player.GetModPlayer<ElainaManaElixirPlayer>().TryStoreCharge(RestoreAmount);
    }
}
