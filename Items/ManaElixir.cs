using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.System;

namespace 伊蕾娜.Items;

/// <summary>
/// 由原版魔力药水转换而来的伊蕾娜专用魔力合剂。
/// 同一堆叠中的合剂必须具有相同的独特魔力恢复量。
/// </summary>
public sealed class ManaElixir : ModItem
{
    private const int DefaultRestoreAmount = 50;

    /// <summary>存入药水槽的独特魔力恢复量。该数值在合成时由原料药水决定。</summary>
    public int RestoreAmount { get; private set; } = DefaultRestoreAmount;

    // 临时与究极魔力药水共用贴图，后续可直接替换为 ManaElixir.png。
    public override string Texture => "伊蕾娜/Items/究极魔力药水";

    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.LesserManaPotion);

        // 魔力合剂只为专属药水槽充能，不能进入原版 QuickMana 的候选列表。
        Item.healMana = 0;
        Item.maxStack = Item.CommonMaxStack;
        Item.value = 0;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        TooltipLine itemName = tooltips.Find(
            line => line.Mod == "Terraria" && line.Name == "ItemName");
        if (itemName != null)
        {
            itemName.Text += this.GetLocalization("NameSuffix")
                .WithFormatArgs(RestoreAmount).Value;
        }
    }

    public override bool CanUseItem(Player player)
    {
        return player.GetModPlayer<ElainaManaElixirPlayer>().CanStoreCharge(RestoreAmount);
    }

    public override bool? UseItem(Player player)
    {
        return true;
    }

    public override bool ConsumeItem(Player player)
    {
        return player.GetModPlayer<ElainaManaElixirPlayer>().CanStoreCharge(RestoreAmount);
    }

    public override void OnConsumeItem(Player player)
    {
        // 仅在本地玩家实际消耗一瓶合剂时入槽，避免动画和远端 UseItem 重复充能。
        player.GetModPlayer<ElainaManaElixirPlayer>().TryStoreCharge(RestoreAmount);
    }

    public override bool CanStack(Item source)
    {
        return source.ModItem is ManaElixir sourceElixir
            && RestoreAmount == sourceElixir.RestoreAmount;
    }

    public override bool CanStackInWorld(Item source)
    {
        return CanStack(source);
    }

    public override void SaveData(TagCompound tag)
    {
        if (RestoreAmount != DefaultRestoreAmount)
        {
            tag["restoreAmount"] = RestoreAmount;
        }
    }

    public override void LoadData(TagCompound tag)
    {
        if (tag.TryGet("restoreAmount", out int restoreAmount))
        {
            RestoreAmount = Math.Max(1, restoreAmount);
        }
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(RestoreAmount);
    }

    public override void NetReceive(BinaryReader reader)
    {
        RestoreAmount = Math.Max(1, reader.ReadInt32());
    }

    public override void AddRecipes()
    {
        RegisterVanillaManaPotionRecipe(ItemID.LesserManaPotion);
        RegisterVanillaManaPotionRecipe(ItemID.ManaPotion);
        RegisterVanillaManaPotionRecipe(ItemID.GreaterManaPotion);
        RegisterVanillaManaPotionRecipe(ItemID.SuperManaPotion);
    }

    private void RegisterVanillaManaPotionRecipe(int manaPotionType)
    {
        int restoreAmount = new Item(manaPotionType).healMana;
        Recipe recipe = CreateRecipe()
            .AddIngredient(manaPotionType)
            .AddTile(TileID.CookingPots)
            .AddCondition(ElainaSystem.IsElaina)
            .DisableDecraft();

        // 在注册前设置配方结果实例，使配方预览、连续合成和堆叠检查
        // 都能读取这一档原料对应的恢复量。
        if (recipe.createItem.ModItem is ManaElixir recipeResult)
        {
            recipeResult.RestoreAmount = restoreAmount;
        }
        else
        {
            throw new InvalidOperationException(
                $"Recipe result for {nameof(ManaElixir)} has no matching ModItem instance.");
        }

        recipe.Register();
    }
}
