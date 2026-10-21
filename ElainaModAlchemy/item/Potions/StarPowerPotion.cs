using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;
using 伊蕾娜.ElainaModAlchemy.Gameplay;
using 伊蕾娜.ElainaModAlchemy.item.Recipes;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

public sealed class StarPowerPotion : AlchemyPotion
{
    public override string EntryId => "starpower";
    public override AlchemyResearchRequirement ResearchRequirement => new(
        AlchemyProgressionRules.RequiredLevel(EntryId), true, ModContent.ItemType<StarPowerPotionRecipe>());
    protected override int PotionBuffType => ModContent.BuffType<StarPowerBuff>();
}
