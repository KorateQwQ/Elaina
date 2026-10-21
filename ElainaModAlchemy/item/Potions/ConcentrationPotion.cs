using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;
using 伊蕾娜.ElainaModAlchemy.Gameplay;
using 伊蕾娜.ElainaModAlchemy.item.Recipes;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

public sealed class ConcentrationPotion : AlchemyPotion
{
    public override string EntryId => "focus";
    public override AlchemyResearchRequirement ResearchRequirement => new(
        AlchemyProgressionRules.RequiredLevel(EntryId), true, ModContent.ItemType<ConcentrationPotionRecipe>());
    protected override int PotionBuffType => ModContent.BuffType<ConcentrationBuff>();
}
