using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Crafting;

public sealed class FacsimileCraftingTooltip : GlobalItem
{
    internal static readonly Color HintColor = new(205, 175, 255);
    internal const string KeyPrefix = "Mods.伊蕾娜.Alchemy.Crafting.";

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        if (!FacsimileCraftingHoverSystem.TryGetHoveredRecipe(item, out Recipe recipe) ||
            !ModContent.GetInstance<FacsimileCraftingSystem>().TryGetQuote(recipe, out var quote) || quote.Dust <= 0)
            return;

        bool preferDust = Main.LocalPlayer.GetModPlayer<FacsimileCraftingPlayer>().PreferDust;
        tooltips.Add(new TooltipLine(Mod, "FacsimileSubstitution", Language.GetTextValue(
            KeyPrefix + (preferDust ? "FacsimileSubstitutionPriority" : "FacsimileSubstitution")))
        {
            OverrideColor = HintColor
        });
        tooltips.Add(new TooltipLine(Mod, "FacsimileCost", Language.GetTextValue(KeyPrefix + "FacsimileCost", quote.Dust))
        {
            OverrideColor = HintColor
        });
    }
}
