using System;
using System.Collections.Generic;

namespace 伊蕾娜.ElainaModAlchemy.UI;

/// <summary>Read-only view of the real inventory and saved alchemy progression.</summary>
public sealed record AlchemyResearchStatus(int RequiredLevel, bool RequiresRecipe, bool RecipeConfigured,
    string RecipeName, int RecipeOwned, int RecipeRequired, bool ConsumeRecipe, bool CanResearch, string Hint);

public sealed record AlchemyNotebookSnapshot
{
    public IReadOnlyDictionary<string, int> Materials { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> Products { get; init; } = new Dictionary<string, int>();
    public IReadOnlySet<string> Unlocked { get; init; } = new HashSet<string>();
    public IReadOnlyDictionary<string, AlchemyResearchStatus> Research { get; init; } = new Dictionary<string, AlchemyResearchStatus>();
    public IReadOnlyDictionary<string, int> MaximumBatches { get; init; } = new Dictionary<string, int>();
    public int Level { get; init; } = 1;
    public int MaximumLevel { get; init; } = 20;
    public int Experience { get; init; }
    public int ExperienceToNextLevel { get; init; } = 100;
    public bool IsBusy { get; init; }
}

public enum AlchemyNotebookAction : byte { Craft, Research, DebugRestock, DebugReset }

public sealed record AlchemyNotebookResult(int Sequence, AlchemyNotebookAction Action, string EntryId,
    bool Success, string Message, int OutputCount = 0, int ExperienceGained = 0);

/// <summary>Production sources transact through game services; offline preview fixtures implement this separately.</summary>
public interface IAlchemyNotebookSource
{
    AlchemyNotebookSnapshot Capture();
    bool Request(AlchemyNotebookAction action, string entryId, int batches = 1);
    AlchemyNotebookResult LastResult { get; }
}
