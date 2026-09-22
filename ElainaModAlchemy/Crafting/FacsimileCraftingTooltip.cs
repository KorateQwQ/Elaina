using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Crafting;

public sealed class FacsimileCraftingTooltip : GlobalItem
{
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        if (!Main.playerInventory || !Main.craftingHide || !Main.guideItem.IsAir ||
            Main.focusRecipe < 0 || Main.focusRecipe >= Main.numAvailableRecipes)
            return;
        Recipe recipe = Main.recipe[Main.availableRecipe[Main.focusRecipe]];
        if (item.type != recipe.createItem.type ||
            !ModContent.GetInstance<FacsimileCraftingSystem>().TryGetQuote(recipe, out var quote))
            return;
        tooltips.Add(new TooltipLine(Mod, "FacsimileCost", Language.GetTextValue(
            "Mods.伊蕾娜.Alchemy.Crafting.FacsimileCost", quote.Dust, quote.Fulfillment * 100)));
    }
}
