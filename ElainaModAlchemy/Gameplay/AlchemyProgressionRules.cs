using System;
using 伊蕾娜.ElainaModAlchemy.UI;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>Alchemy progression baseline: routine preparation within each unlocked skill tier.</summary>
public static class AlchemyProgressionRules
{
    public const int MaximumLevel = 20;
    public const int InitialLevelExperience = 100, ExperienceIncreasePerLevel = 50;
    public const int BaseBatchExperience = 10, BatchExperiencePerRarity = 5;

    public static int ExperienceToNextLevel(int level) => level >= MaximumLevel ? 0
        : InitialLevelExperience + ExperienceIncreasePerLevel * (Math.Clamp(level, 1, MaximumLevel) - 1);
    public static int ExperiencePerBatch(string entryId) => BaseBatchExperience
        + BatchExperiencePerRarity * AlchemyCatalog.GetRecipe(entryId).Rarity;
    public static int RequiredLevel(string entryId) => entryId switch
    {
        "rain" => 1,
        // Skill nodes are character Lv.6/10/15/20/25/30/35/40. Lv.5 needs character Lv.25 (after Queen Bee);
        // Lv.8 needs Lv.40 (after the Wall of Flesh), since Lv.7 is already reachable after Deerclops.
        "painkiller" => 5,
        // Featherlight is strong for its stage: Lv.4 needs character Lv.20 (after the Eater of Worlds / Brain of Cthulhu).
        "featherlight" => 4,
        "trace" => 2,
        "dropper" or "mimic" => 3,
        "bloodlust" or "starpower" or "focus" or "resonance" => 8,
        "isolation" => 8,
        _ => 1
    };
}

/// <summary>A level requirement, a physical recipe scroll, or both.</summary>
public sealed record AlchemyResearchRequirement(int RequiredLevel = 1, bool RequiresRecipe = false,
    int RecipeItemType = 0, int RecipeStack = 1, bool ConsumeRecipe = true);
