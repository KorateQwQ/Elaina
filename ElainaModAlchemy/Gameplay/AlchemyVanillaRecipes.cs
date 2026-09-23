using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Crafting;
using 伊蕾娜.ElainaModAlchemy.item;
using 伊蕾娜.ElainaModAlchemy.UI;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>Registers notebook recipes, then reuses Terraria's real collection, availability and CraftItem paths.</summary>
public sealed class AlchemyVanillaRecipes : ModSystem
{
    private static readonly Dictionary<string, Recipe> Recipes = new(StringComparer.Ordinal);
    private static readonly HashSet<Recipe> RecipeSet = new();
    private static readonly Dictionary<string, int> Groups = new(StringComparer.Ordinal);
    private static Action<Player> _collect;
    private static Dictionary<int, int> _owned;
    private static Func<Player, Recipe, bool> _tiles, _environment;
    private static int _contextDepth;

    /// <summary>Future opt-in: both checking and crafting will use the existing dust extension.</summary>
    public static bool AllowFacsimileDust { get; set; } = false;
    internal static bool IsAlchemyRecipe(Recipe recipe) => RecipeSet.Contains(recipe);
    internal static Recipe Get(string id) => id != null && Recipes.TryGetValue(id, out var recipe) ? recipe : null;

    public override void Load()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        _collect = typeof(Recipe).GetMethod("CollectItemsToCraftWithFrom", flags)?.CreateDelegate<Action<Player>>()
            ?? throw new MissingMethodException("Recipe.CollectItemsToCraftWithFrom");
        _owned = (Dictionary<int, int>)typeof(Recipe).GetField("_ownedItems", flags)?.GetValue(null)
            ?? throw new MissingFieldException("Recipe._ownedItems");
        _tiles = typeof(Recipe).GetMethod("PlayerMeetsTileRequirements", flags)?.CreateDelegate<Func<Player, Recipe, bool>>()
            ?? throw new MissingMethodException("Recipe.PlayerMeetsTileRequirements");
        _environment = typeof(Recipe).GetMethod("PlayerMeetsEnvironmentConditions", flags)?.CreateDelegate<Func<Player, Recipe, bool>>()
            ?? throw new MissingMethodException("Recipe.PlayerMeetsEnvironmentConditions");
    }

    private static string GroupKey(IEnumerable<int> types) => string.Join("_", types.OrderBy(type => type));
    public override void AddRecipeGroups()
    {
        Groups.Clear();
        foreach (var entry in AlchemyCatalog.Recipes)
        {
            var definition = AlchemyCraftingService.RecipeFor(entry);
            if (definition == null) continue;
            foreach (var ingredient in definition.Ingredients.Where(i => i.ItemTypes.Count > 1))
            {
                string key = GroupKey(ingredient.ItemTypes);
                if (Groups.ContainsKey(key)) continue;
                int[] types = ingredient.ItemTypes.ToArray();
                Groups[key] = RecipeGroup.RegisterGroup("伊蕾娜:Alchemy_" + key,
                    new RecipeGroup(() => string.Join(" / ", types.Select(Lang.GetItemNameValue)), types));
            }
        }
    }

    public override void AddRecipes()
    {
        Recipes.Clear(); RecipeSet.Clear();
        foreach (var entry in AlchemyCatalog.Recipes)
        {
            var definition = AlchemyCraftingService.RecipeFor(entry);
            if (definition == null) continue;
            var recipe = Recipe.Create(entry.ItemType, definition.ResultStack);
            foreach (var ingredient in definition.Ingredients)
            {
                if (ingredient.ItemTypes.Count > 1) recipe.AddRecipeGroup(Groups[GroupKey(ingredient.ItemTypes)], ingredient.Stack);
                else recipe.AddIngredient(ingredient.ItemType, ingredient.Stack);
            }
            string id = entry.Id;
            recipe.AddCondition(new Condition("Mods.伊蕾娜.Alchemy.OnlyNotebook", () => _contextDepth > 0 &&
                Main.LocalPlayer.GetModPlayer<AlchemyProgressPlayer>().IsUnlocked(id)));
            recipe.Register();
            Recipes.Add(id, recipe); RecipeSet.Add(recipe);
        }
    }

    public static IReadOnlyDictionary<int, int> Collect(Player player)
    {
        if (_collect == null) return new Dictionary<int, int>();
        _collect(player);
        return new Dictionary<int, int>(_owned);
    }

    internal static bool CanCraft(Player player, string id, bool refresh = true)
    {
        Recipe recipe = Get(id);
        if (recipe == null || player != Main.LocalPlayer || !player.active || player.dead || recipe.Disabled ||
            !player.GetModPlayer<AlchemyProgressPlayer>().IsUnlocked(id)) return false;
        if (refresh) _collect(player);
        _contextDepth++;
        try
        {
            return _tiles(player, recipe) && _environment(player, recipe) && RecipeLoader.RecipeAvailable(recipe) &&
                (AllowFacsimileDust ? ModContent.GetInstance<FacsimileCraftingSystem>().HasAlchemyMaterials(recipe)
                    : Recipe.CollectedEnoughItemsToCraftRecipeNew(recipe));
        }
        finally { _contextDepth--; }
    }

    internal static int MaxBatches(Player player, string id)
    {
        if (!CanCraft(player, id, false)) return 0;
        Recipe recipe = Get(id);
        int limit = 99;
        foreach (var item in recipe.requiredItem)
        {
            int key = item.type;
            foreach (int group in recipe.acceptedGroups)
                if (RecipeGroup.recipeGroups[group].ValidItems.Contains(item.type)) key = RecipeGroup.recipeGroups[group].GetGroupFakeItemId();
            limit = Math.Min(limit, _owned.GetValueOrDefault(key) / item.stack);
        }
        // The existing dust extension owns substitution math; do not recreate it here.
        return AllowFacsimileDust
            ? ModContent.GetInstance<FacsimileCraftingSystem>().MaxAlchemyBatches(recipe, 99) : limit;
    }

    internal static Item CraftOne(Player player, string id)
    {
        if (!CanCraft(player, id)) return null;
        Item previousCursor = Main.mouseItem;
        _contextDepth++;
        try
        {
            Main.mouseItem = new Item();
            // Includes Recipe.Create, consumption hooks, chest packets, OnCraft,
            // OnCreated, prefixes and the existing optional facsimile extension.
            Main.CraftItem(Get(id));
            return Main.mouseItem.IsAir ? null : Main.mouseItem;
        }
        finally
        {
            Main.mouseItem = previousCursor;
            _contextDepth--;
            Recipe.FindRecipes();
        }
    }

    public override void Unload()
    {
        Recipes.Clear(); RecipeSet.Clear(); Groups.Clear();
        _collect = null; _owned = null; _tiles = _environment = null;
        _contextDepth = 0; AllowFacsimileDust = false;
    }
}
