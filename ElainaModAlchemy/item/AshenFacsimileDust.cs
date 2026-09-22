using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Crafting;

namespace 伊蕾娜.ElainaModAlchemy.item;

/// <summary>炼金造物；仅为常规工作台合成补料，不参与炼金配方的替代判定。</summary>
public sealed class AshenFacsimileDust : AlchemyItem
{
    public override string LocalizationCategory => "Alchemy.Items";

    // 尚未设计获取配方，与现有炼金药剂保持一致。
    public override bool IsRecipeUnlocked(Player player) => false;

    public override bool CanRightClick() => true;

    public override bool ConsumeItem(Player player) => false;

    public override void RightClick(Player player)
    {
        player.GetModPlayer<FacsimileCraftingPlayer>().TogglePriority();
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        bool preferDust = Main.LocalPlayer.active &&
            Main.LocalPlayer.GetModPlayer<FacsimileCraftingPlayer>().PreferDust;
        tooltips.Add(new TooltipLine(Mod, "FacsimileMode", Language.GetTextValue(
            preferDust
                ? "Mods.伊蕾娜.Alchemy.Crafting.FacsimileModeDust"
                : "Mods.伊蕾娜.Alchemy.Crafting.FacsimileModeMaterials")));
    }

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 30;
        Item.maxStack = Item.CommonMaxStack;
        Item.rare = ItemRarityID.Blue;
    }
}
