using System;
using System.Collections.Generic;
using System.Linq;

namespace 伊蕾娜.ElainaModAlchemy.UI;

/// <summary>Navigation over real inventory/progression snapshots; owns no item stock.</summary>
public sealed class AlchemyNotebookState
{
    public const int MaximumBatches = 99;
    public const string Ready = "ready", Short = "short", Acquire = "acquire", Locked = "locked", Researchable = "researchable";
    private IAlchemyNotebookSource _source;
    private AlchemyNotebookSnapshot _snapshot = new();
    private int _stamp;
    public IReadOnlyDictionary<string, int> Materials => _snapshot.Materials;
    public IReadOnlyDictionary<string, int> Products => _snapshot.Products;
    public string SelectedId { get; private set; } = "mana";
    public string Category { get; private set; } = "potion";
    public int Quantity { get; private set; } = 1;
    public int Revision { get; private set; }
    public int Level => _snapshot.Level;
    public int MaximumLevel => _snapshot.MaximumLevel;
    public int Experience => _snapshot.Experience;
    public int ExperienceToNextLevel => _snapshot.ExperienceToNextLevel;
    public bool IsBusy => _snapshot.IsBusy;
    public int UnlockedCount => _snapshot.Unlocked.Count;
    public AlchemyNotebookResult LastResult => _source?.LastResult;
    public AlchemyCatalogRecipe Current => AlchemyCatalog.GetRecipe(SelectedId);
    public IReadOnlyList<AlchemyCatalogRecipe> Visible => AlchemyCatalog.Recipes.Where(r => r.Category == Category).ToArray();

    public AlchemyNotebookState(IAlchemyNotebookSource source = null) { if (source != null) Bind(source); }
    public void Bind(IAlchemyNotebookSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        SelectedId = "mana"; Category = "potion"; Quantity = 1;
        Refresh();
    }
    public void Clear()
    {
        _source = null; _snapshot = new(); _stamp = 0;
        SelectedId = "mana"; Category = "potion"; Quantity = 1; Revision++;
    }
    public bool Refresh()
    {
        var next = _source?.Capture() ?? new AlchemyNotebookSnapshot();
        var hash = new HashCode();
        hash.Add(next.Level); hash.Add(next.Experience); hash.Add(next.ExperienceToNextLevel); hash.Add(next.MaximumLevel); hash.Add(next.IsBusy);
        foreach (var pair in next.Materials) { hash.Add(pair.Key); hash.Add(pair.Value); }
        foreach (var pair in next.Products) { hash.Add(pair.Key); hash.Add(pair.Value); }
        foreach (string id in next.Unlocked) hash.Add(id);
        foreach (var pair in next.MaximumBatches) { hash.Add(pair.Key); hash.Add(pair.Value); }
        foreach (var pair in next.Research) { hash.Add(pair.Key); hash.Add(pair.Value); }
        int stamp = hash.ToHashCode();
        _snapshot = next;
        if (stamp == _stamp) return false;
        _stamp = stamp; ClampQuantity(); Revision++;
        return true;
    }
    public bool IsUnlocked(AlchemyCatalogRecipe recipe) => recipe != null && _snapshot.Unlocked.Contains(recipe.Id);
    public AlchemyResearchStatus Research(AlchemyCatalogRecipe recipe) =>
        recipe != null && _snapshot.Research.TryGetValue(recipe.Id, out var research) ? research : new(1, false, true, "", 0, 0, false, false, "等待角色数据");
    public string DisplayName(AlchemyCatalogRecipe recipe) => IsUnlocked(recipe) ? recipe.Name : "未解锁的造物";
    public static string StateLabel(string key) => key switch
    { Ready => "可炼制", Short => "缺素材", Acquire => "可取得", Researchable => "可研究", _ => "未解锁" };
    public string StateOf(AlchemyCatalogRecipe recipe) => !IsUnlocked(recipe) ? Research(recipe).CanResearch ? Researchable : Locked
        : !recipe.Craftable ? Acquire : MaxBatches(recipe) > 0 ? Ready : Short;
    public int MaterialCount(string id) => id != null && Materials.TryGetValue(id, out int count) ? count : 0;
    public int Owned(AlchemyCatalogRecipe recipe) => recipe != null && Products.TryGetValue(recipe.Id, out int count) ? count : 0;
    public int Available(AlchemyCatalogIngredient ingredient) => (int)Math.Min(int.MaxValue,
        ingredient.Choices.Distinct(StringComparer.Ordinal).Sum(id => (long)MaterialCount(id)));
    public int MaxBatches(AlchemyCatalogRecipe recipe) => IsUnlocked(recipe) && _snapshot.MaximumBatches.TryGetValue(recipe.Id, out int count)
        ? Math.Clamp(count, 0, MaximumBatches) : 0;
    public bool CanCraft() => !IsBusy && IsUnlocked(Current) && Current.Craftable && MaxBatches(Current) >= Quantity;
    public bool CanResearch() => !IsBusy && !IsUnlocked(Current) && Research(Current).CanResearch;
    public bool TryCraft() => CanCraft() && Request(AlchemyNotebookAction.Craft, Quantity);
    public bool TryResearch() => CanResearch() && Request(AlchemyNotebookAction.Research, 1);
    public bool DebugRestock() => !IsBusy && Current.Craftable && Request(AlchemyNotebookAction.DebugRestock, 1);
    public bool DebugReset() => !IsBusy && Request(AlchemyNotebookAction.DebugReset, 1);
    private bool Request(AlchemyNotebookAction action, int batches)
    {
        bool accepted = _source?.Request(action, SelectedId, batches) == true;
        Refresh();
        return accepted;
    }
    public void Select(string id)
    {
        var recipe = AlchemyCatalog.Recipes.FirstOrDefault(r => r.Id == id);
        if (recipe == null) return;
        if (SelectedId != id) Quantity = 1;
        SelectedId = id; Category = recipe.Category; ClampQuantity(); Revision++;
    }
    public void SetCategory(string id)
    {
        if (!AlchemyCatalog.Categories.Any(c => c.Id == id)) return;
        Category = id;
        if (Current.Category != id) SelectedId = Visible[0].Id;
        Quantity = 1; Revision++;
    }
    public void SetQuantity(int quantity)
    { Quantity = Math.Clamp(quantity, 1, Math.Max(1, MaxBatches(Current))); Revision++; }
    private void ClampQuantity() => Quantity = Math.Clamp(Quantity, 1, Math.Max(1, MaxBatches(Current)));
}