using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;
using 伊蕾娜.ElainaModAlchemy.Gameplay;
using 伊蕾娜.ElainaModAlchemy.item.Recipes;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

public sealed class ResonancePotion : AlchemyPotion
{
    public override string EntryId => "resonance";
    public override AlchemyResearchRequirement ResearchRequirement => new(
        AlchemyProgressionRules.RequiredLevel(EntryId), true, ModContent.ItemType<ResonancePotionRecipe>());
    protected override int PotionBuffType => ModContent.BuffType<ResonanceBuff>();
}
