using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;
using 伊蕾娜.ElainaModAlchemy.Gameplay;
using 伊蕾娜.ElainaModAlchemy.item.Recipes;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

public sealed class FeatherlightPotion : AlchemyPotion
{
    public override string EntryId => "featherlight";
    public override AlchemyResearchRequirement ResearchRequirement => new(
        AlchemyProgressionRules.RequiredLevel(EntryId), true, ModContent.ItemType<FeatherlightPotionRecipe>());
    protected override int PotionBuffType => ModContent.BuffType<FeatherlightBuff>();
}
