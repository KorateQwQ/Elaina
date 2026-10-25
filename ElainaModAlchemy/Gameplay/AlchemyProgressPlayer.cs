using static 伊蕾娜.Localization.UIText;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaModAlchemy.item;
using 伊蕾娜.ElainaModAlchemy.UI;
using 伊蕾娜.ElainaModSkills.Skills.Alchemy;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>New alchemy progression belongs to the character, independent of the legacy alchemy system.</summary>
public sealed class AlchemyProgressPlayer : ModPlayer
{
    private readonly HashSet<string> _unlocked = new(StringComparer.Ordinal);
    // Keep earned progress independently of the skill cap, including while skills load or are reset.
    private int _level = 1;
    private int _experience;
    public int Level => Math.Min(_level, MaximumLevel);
    public int Experience => _level >= MaximumLevel ? 0 : _experience;
    public int MaximumLevel => AlchemySkill.GetNotebookMaximumLevel(Player);
    public int ExperienceToNextLevel => Level >= MaximumLevel ? 0 : AlchemyProgressionRules.ExperienceToNextLevel(Level);
    public IReadOnlySet<string> Unlocked => _unlocked;
    public bool IsReady => Player is { active: true, dead: false, ghost: false };

    public override void Initialize()
    {
        _level = 1;
        _experience = 0;
        ResetInitialUnlocks();
    }

    public override void SaveData(TagCompound tag)
    {
        tag["Version"] = 1;
        tag["Level"] = _level;
        tag["Experience"] = _experience;
        tag["Unlocked"] = _unlocked.OrderBy(id => id, StringComparer.Ordinal).ToList();
    }

    public override void LoadData(TagCompound tag)
        => Apply(tag.GetInt("Level"), tag.GetInt("Experience"), tag.GetList<string>("Unlocked"));

    private void Apply(int level, int experience, IEnumerable<string> unlocked)
    {
        _level = Math.Clamp(level, 1, AlchemyProgressionRules.MaximumLevel);
        _experience = _level == AlchemyProgressionRules.MaximumLevel ? 0
            : Math.Clamp(experience, 0, AlchemyProgressionRules.ExperienceToNextLevel(_level) - 1);
        _unlocked.Clear();
        // Filter retired entries before limiting the count, so an older, larger catalog cannot truncate valid research.
        var knownIds = AlchemyCatalog.Recipes.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        foreach (string id in unlocked.Where(knownIds.Contains).Distinct(StringComparer.Ordinal).Take(knownIds.Count))
            _unlocked.Add(id);
        foreach (string id in AlchemyCatalog.InitiallyUnlockedIds) _unlocked.Add(id);
    }

    public bool IsUnlocked(string id) => id != null && _unlocked.Contains(id);

    public static AlchemyResearchRequirement RequirementFor(string id)
    {
        var entry = AlchemyCraftingService.FindEntry(id);
        if (entry?.ItemType > 0 && ItemLoader.GetItem(entry.ItemType) is AlchemyItem item)
            return item.ResearchRequirement;
        return new(AlchemyProgressionRules.RequiredLevel(id));
    }

    public AlchemyResearchStatus ResearchStatus(string id)
    {
        var requirement = RequirementFor(id);
        int requiredLevel = Math.Max(1, requirement.RequiredLevel);
        bool configured = !requirement.RequiresRecipe || requirement.RecipeItemType > 0;
        int count = requirement.RequiresRecipe && configured ? AlchemyInventory.Count(Player, requirement.RecipeItemType) : 0;
        int required = Math.Max(1, requirement.RecipeStack);
        bool hasRecipe = !requirement.RequiresRecipe || configured && count >= required;
        string name = requirement.RecipeItemType > 0 ? Lang.GetItemNameValue(requirement.RecipeItemType) : LocalizeUI("UnknownRecipe");
        bool can = IsReady && !IsUnlocked(id) && Level >= requiredLevel && hasRecipe;
        string hint = IsUnlocked(id) ? LocalizeUI("Researched") : !IsReady ? LocalizeUI("CannotResearchNow") : Level < requiredLevel ? LocalizeUI("NeedAlchemyLevel", requiredLevel)
            : !configured ? LocalizeUI("RecipeNotConfigured") : !hasRecipe ? LocalizeUI("NeedRecipe", name, required) : requirement.RequiresRecipe
                ? requirement.ConsumeRecipe ? LocalizeUI("ResearchConsumesRecipe") : LocalizeUI("ResearchKeepRecipe") : LocalizeUI("ResearchLevelMet");
        return new(requiredLevel, requirement.RequiresRecipe, configured, name, count, required,
            requirement.ConsumeRecipe, can, hint, requirement.RecipeItemType > 0
                ? ItemLoader.GetItem(requirement.RecipeItemType)?.Texture : null);
    }

    public bool TryResearch(string id, out string message)
    {
        message = LocalizeUI("CannotResearchRecipe");
        if (Main.dedServ || Player.whoAmI != Main.myPlayer || !IsReady || AlchemyCraftingService.FindEntry(id) == null) return false;
        var status = ResearchStatus(id);
        if (!status.CanResearch) { message = status.Hint; return false; }
        var requirement = RequirementFor(id);
        if (requirement.RequiresRecipe && requirement.ConsumeRecipe)
        {
            int needed = Math.Max(1, requirement.RecipeStack);
            if (AlchemyInventory.Count(Player, requirement.RecipeItemType) < needed) { message = LocalizeUI("MissingRecipeInventory"); return false; }
            for (int i = 0; i < needed; i++) Player.ConsumeItem(requirement.RecipeItemType);
        }
        _unlocked.Add(id);
        message = LocalizeUI("ResearchSuccess", AlchemyCatalog.GetRecipe(id).Name);
        return true;
    }

    public int AddCraftExperience(string id, int batches)
    {
        if (batches <= 0 || Level >= MaximumLevel) return 0;
        var entry = AlchemyCraftingService.FindEntry(id);
        if (entry == null) return 0;
        int perBatch = entry.ItemType > 0 && ItemLoader.GetItem(entry.ItemType) is AlchemyItem item
            ? item.CraftExperience : AlchemyProgressionRules.ExperiencePerBatch(id);
        long remaining = Math.Min(int.MaxValue, Math.Max(0L, (long)perBatch * batches));
        int added = 0;
        while (remaining > 0 && Level < MaximumLevel)
        {
            int needed = AlchemyProgressionRules.ExperienceToNextLevel(_level);
            int take = (int)Math.Min(remaining, needed - _experience);
            _experience += take;
            remaining -= take;
            added += take;
            if (_experience >= needed) { _level++; _experience = 0; }
        }
        return added;
    }

#if DEBUG
    public bool DebugLevelUp()
    {
        if (Main.dedServ || Player != Main.LocalPlayer || !IsReady || Level >= MaximumLevel) return false;
        _level++;
        if (_level == MaximumLevel) _experience = 0;
        return true;
    }

    public bool DebugMaxLevel()
    {
        if (Main.dedServ || Player != Main.LocalPlayer || !IsReady || Level >= MaximumLevel) return false;
        _level = MaximumLevel;
        _experience = 0;
        return true;
    }

    public bool DebugUnlockAll()
    {
        if (Main.dedServ || Player != Main.LocalPlayer || !IsReady) return false;
        bool changed = false;
        foreach (var entry in AlchemyCatalog.Recipes) changed |= _unlocked.Add(entry.Id);
        return changed;
    }
#endif

    public void ResetProgress() { _level = 1; _experience = 0; ResetInitialUnlocks(); }

    private void ResetInitialUnlocks()
    {
        _unlocked.Clear();
        foreach (string id in AlchemyCatalog.InitiallyUnlockedIds) _unlocked.Add(id);
    }

}
