using static 伊蕾娜.Localization.UIText;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Gameplay;
using 伊蕾娜.ElainaModAlchemy.UI;

namespace 伊蕾娜.ElainaModAlchemy.item.Recipes;

/// <summary>A physical research scroll. Recipe ingredients remain on the linked catalog entry.</summary>
public abstract class AlchemyRecipeScroll : ModItem
{
    public abstract string EntryId { get; }
    public override string LocalizationCategory => "Alchemy.Items";
    public override string Texture => AlchemyCatalog.RecipeScrollPixelPath;

    public override void SetDefaults()
    {
        Item.width = Item.height = 34;
        Item.maxStack = Item.CommonMaxStack;
        Item.rare = AlchemyCatalog.GetRecipe(EntryId).Rarity;
        Item.value = AlchemyRecipeSources.ScrollValue;
        // Only the notebook research transaction consumes a scroll.
        Item.consumable = false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        var requirement = AlchemyProgressPlayer.RequirementFor(EntryId);
        tooltips.Add(new(Mod, "ResearchLevel", LocalizeUI("RecipeScrollResearchLevel", requirement.RequiredLevel)));
        tooltips.Add(new(Mod, "ResearchUse", requirement.ConsumeRecipe
            ? LocalizeUI("RecipeScrollConsumed", requirement.RecipeStack) : LocalizeUI("ResearchKeepRecipe")));
        if (!Main.dedServ && Main.LocalPlayer is { active: true } player &&
            player.GetModPlayer<AlchemyProgressPlayer>().IsUnlocked(EntryId))
            tooltips.Add(new(Mod, "AlreadyLearned", LocalizeUI("RecipeScrollLearned")) { OverrideColor = new Color(150, 210, 170) });
    }

    public override bool OnPickup(Player player)
    {
        player.GetModPlayer<AlchemyRecipeDiscoveryPlayer>().Observe(Item);
        return true;
    }
}
