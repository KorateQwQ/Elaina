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

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>New alchemy progression belongs to the character, independent of the legacy alchemy system.</summary>
public sealed class AlchemyProgressPlayer : ModPlayer
{
    private readonly HashSet<string> _unlocked = new(StringComparer.Ordinal);
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public int MaximumLevel => AlchemyProgressionRules.MaximumLevel;
    public int ExperienceToNextLevel => AlchemyProgressionRules.ExperienceToNextLevel(Level);
    public IReadOnlySet<string> Unlocked => _unlocked;
    public bool IsReady => Player is { active: true, dead: false, ghost: false };

    public override void Initialize()
    {
        Level = 1;
        Experience = 0;
        _unlocked.Clear();
    }

    public override void SaveData(TagCompound tag)
    {
        tag["Version"] = 1;
        tag["Level"] = Level;
        tag["Experience"] = Experience;
        tag["Unlocked"] = _unlocked.OrderBy(id => id, StringComparer.Ordinal).ToList();
    }

    public override void LoadData(TagCompound tag)
        => Apply(tag.GetInt("Level"), tag.GetInt("Experience"), tag.GetList<string>("Unlocked"));

    private void Apply(int level, int experience, IEnumerable<string> unlocked)
    {
        Level = Math.Clamp(level, 1, MaximumLevel);
        Experience = Level == MaximumLevel ? 0 : Math.Clamp(experience, 0, ExperienceToNextLevel - 1);
        _unlocked.Clear();
        foreach (string id in unlocked.Take(AlchemyCatalog.Recipes.Count))
            if (id != null && AlchemyCatalog.Recipes.Any(r => r.Id == id)) _unlocked.Add(id);
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
        string name = requirement.RecipeItemType > 0 ? Lang.GetItemNameValue(requirement.RecipeItemType) : "尚未登记的配方";
        bool can = IsReady && !IsUnlocked(id) && Level >= requiredLevel && hasRecipe;
        string hint = IsUnlocked(id) ? "已研究" : !IsReady ? "当前无法进行研究" : Level < requiredLevel ? $"需要炼金等级 {requiredLevel}"
            : !configured ? "此配方物品尚未登记" : !hasRecipe ? $"需要 {name} × {required}" : requirement.RequiresRecipe
                ? requirement.ConsumeRecipe ? "研究会消耗所需配方" : "背包中持有配方即可研究" : "达到等级，可直接研究";
        return new(requiredLevel, requirement.RequiresRecipe, configured, name, count, required,
            requirement.ConsumeRecipe, can, hint);
    }

    public bool TryResearch(string id, out string message)
    {
        message = "无法研究此配方";
        if (Main.dedServ || Player.whoAmI != Main.myPlayer || !IsReady || AlchemyCraftingService.FindEntry(id) == null) return false;
        var status = ResearchStatus(id);
        if (!status.CanResearch) { message = status.Hint; return false; }
        var requirement = RequirementFor(id);
        if (requirement.RequiresRecipe && requirement.ConsumeRecipe)
        {
            int needed = Math.Max(1, requirement.RecipeStack);
            if (AlchemyInventory.Count(Player, requirement.RecipeItemType) < needed) { message = "背包中的配方不足"; return false; }
            for (int i = 0; i < needed; i++) Player.ConsumeItem(requirement.RecipeItemType);
        }
        _unlocked.Add(id);
        message = "已研究「" + AlchemyCatalog.GetRecipe(id).Name + "」";
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
            int take = (int)Math.Min(remaining, ExperienceToNextLevel - Experience);
            Experience += take;
            remaining -= take;
            added += take;
            if (Experience >= ExperienceToNextLevel) { Level++; Experience = 0; }
        }
        return added;
    }

    public void ResetProgress() { Level = 1; Experience = 0; _unlocked.Clear(); }

}
