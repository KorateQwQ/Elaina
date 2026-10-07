using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

/// <summary>
/// 月露合剂的炼金物品。
/// 饮用后会写入伊蕾娜的专属魔力药水槽。
/// </summary>
public sealed class MoonDewElixir : AlchemyItem
{
    public override string EntryId => "mana";

    public override void SetDefaults()
    {
        Item.width = CatalogEntry.PixelWidth;
        Item.height = CatalogEntry.PixelHeight;
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
        return player.GetModPlayer<ElainaManaElixirPlayer>().CanStoreCharge();
    }

    public override bool? UseItem(Player player) => true;

    public override bool ConsumeItem(Player player)
    {
        return player.GetModPlayer<ElainaManaElixirPlayer>().CanStoreCharge();
    }

    public override void OnConsumeItem(Player player)
    {
        player.GetModPlayer<ElainaManaElixirPlayer>().TryStoreCharge();
    }
}
