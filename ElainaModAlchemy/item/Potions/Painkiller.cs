using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;
using 伊蕾娜.ElainaModAlchemy.Gameplay;
using 伊蕾娜.ElainaModAlchemy.item.Recipes;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

public sealed class Painkiller : AlchemyPotion
{
    public override string EntryId => "painkiller";
    public override AlchemyResearchRequirement ResearchRequirement => new(
        AlchemyProgressionRules.RequiredLevel(EntryId), true, ModContent.ItemType<PainkillerRecipe>());
    protected override int PotionBuffType => ModContent.BuffType<PainkillerBuff>();
    protected override int DurationTicks => 60 * 60;
}
