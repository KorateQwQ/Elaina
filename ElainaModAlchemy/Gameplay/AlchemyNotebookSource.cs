using System;
using System.Collections.Generic;
using Terraria;
using 伊蕾娜.ElainaModAlchemy.UI;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>Live adapter over the same owned-material counts used by vanilla crafting.</summary>
public sealed class AlchemyNotebookSource : IAlchemyNotebookSource
{
    private readonly Player _player;
    private int _sequence;
    public AlchemyNotebookResult LastResult { get; private set; }
    public AlchemyNotebookSource(Player player) => _player = player;
    public bool Request(AlchemyNotebookAction action, string id, int batches = 1)
    {
        if (_player != Main.LocalPlayer || Main.dedServ) return false;
        LastResult = AlchemyCraftingService.Execute(_player, action, id, batches, ++_sequence);
        return true;
    }
    public AlchemyNotebookSnapshot Capture()
    {
        if (_player == null || !_player.active || Main.dedServ) return new();
        var progress = _player.GetModPlayer<AlchemyProgressPlayer>();
        var owned = AlchemyVanillaRecipes.Collect(_player);
        var materials = new Dictionary<string, int>(StringComparer.Ordinal);
        var products = new Dictionary<string, int>(StringComparer.Ordinal);
        var unlocked = new HashSet<string>(progress.Unlocked, StringComparer.Ordinal);
        var research = new Dictionary<string, AlchemyResearchStatus>(StringComparer.Ordinal);
        var maximum = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var material in AlchemyCatalog.Materials) materials[material.Id] = owned.GetValueOrDefault(material.ItemType);
        foreach (var entry in AlchemyCatalog.Recipes)
        {
            products[entry.Id] = owned.GetValueOrDefault(entry.ItemType);
            research[entry.Id] = progress.ResearchStatus(entry.Id);
            maximum[entry.Id] = unlocked.Contains(entry.Id) ? AlchemyVanillaRecipes.MaxBatches(_player, entry.Id) : 0;
        }
        return new AlchemyNotebookSnapshot
        {
            Materials = materials, Products = products, Unlocked = unlocked, Research = research,
            MaximumBatches = maximum, Level = progress.Level, MaximumLevel = progress.MaximumLevel,
            Experience = progress.Experience, ExperienceToNextLevel = progress.ExperienceToNextLevel
        };
    }
}