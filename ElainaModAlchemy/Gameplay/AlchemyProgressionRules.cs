using System;
using 伊蕾娜.ElainaModAlchemy.UI;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>Provisional balance values. Replace these functions when final progression is designed.</summary>
public static class AlchemyProgressionRules
{
    public const int MaximumLevel = 20;
    public static int ExperienceToNextLevel(int level) => level >= MaximumLevel ? 0 : 100 + 50 * (Math.Clamp(level, 1, MaximumLevel) - 1);
    public static int ExperiencePerBatch(string entryId) => 10 + 5 * AlchemyCatalog.GetRecipe(entryId).Rarity;
    public static int RequiredLevel(string entryId) => entryId switch
    {
        "painkiller" or "stew" or "brulee" => 3,
        "bloodlust" or "focus" => 10,
        "starpower" or "isolation" => 8,
        "resonance" => 5,
        "featherlight" or "rain" => 4,
        "dropper" or "shimmer" => 12,
        "mimic" => 6,
        "trace" or "beef" or "potato" => 2,
        _ => 1
    };
}

/// <summary>A level requirement, a recipe item, or both. No physical recipe items are assigned yet.</summary>
public sealed record AlchemyResearchRequirement(int RequiredLevel = 1, bool RequiresRecipe = false,
    int RecipeItemType = 0, int RecipeStack = 1, bool ConsumeRecipe = true);
