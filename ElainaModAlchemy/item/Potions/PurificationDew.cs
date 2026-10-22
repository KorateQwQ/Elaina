using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Gameplay;
using 伊蕾娜.ElainaModAlchemy.item.Recipes;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

/// <summary>净土露滴：炼金物品，具体效果待后续实现。</summary>
public sealed class PurificationDew : AlchemyItem
{
    public override string EntryId => "isolation";
    public override AlchemyResearchRequirement ResearchRequirement => new(
        AlchemyProgressionRules.RequiredLevel(EntryId), true, ModContent.ItemType<PurificationDewRecipe>());
}
