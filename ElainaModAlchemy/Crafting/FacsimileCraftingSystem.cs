using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent.Achievements;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.item;

namespace 伊蕾娜.ElainaModAlchemy.Crafting;

/// <summary>
/// 只扩展正常合成栏。既不修改/复制 Recipe，也不改动 AlchemyRecipe，
/// 不对其他系统直接调用的材料查询或 Recipe.Create 注入虚构材料。
/// </summary>
public sealed class FacsimileCraftingSystem : ModSystem
{
    private readonly Dictionary<Recipe, FacsimileRecipe> recipes = new();
    private readonly Dictionary<Recipe, FacsimileMaterialRule.Quote> quotes = new();
    private Dictionary<int, int> ownedItems;
    private List<Item> consumedItems;
    private Func<Player, Recipe, bool> meetsTiles;
    private Func<Player, Recipe, bool> meetsEnvironment;
    private int dustType;
    private Recipe activeRecipe;
    private FacsimileCraftingInventory activeInventory;

    public override void Load()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        // 私有字段仅在加载时解析；刷新配方的热路径不使用反射。
        ownedItems = (Dictionary<int, int>)(typeof(Recipe).GetField("_ownedItems", flags)?.GetValue(null)
            ?? throw new MissingFieldException(typeof(Recipe).FullName, "_ownedItems"));
        consumedItems = (List<Item>)(typeof(RecipeLoader).GetField("ConsumedItems", flags)?.GetValue(null)
            ?? throw new MissingFieldException(typeof(RecipeLoader).FullName, "ConsumedItems"));
        meetsTiles = GetRequirementCheck("PlayerMeetsTileRequirements");
        meetsEnvironment = GetRequirementCheck("PlayerMeetsEnvironmentConditions");

        IL_Recipe.FindRecipes += PatchMaterialCheck;
        On_Recipe.FindRecipes += FindRecipes;
        On_Main.CraftItem += CraftItem;
        On_Recipe.Create += ConsumeMaterials;
    }

    private static Func<Player, Recipe, bool> GetRequirementCheck(string name)
    {
        MethodInfo method = typeof(Recipe).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(Recipe).FullName, name);
        return method.CreateDelegate<Func<Player, Recipe, bool>>();
    }

    public override void PostSetupRecipes()
    {
        dustType = ModContent.ItemType<AshenFacsimileDust>();
        recipes.Clear();
        quotes.Clear();
        for (int i = 0; i < Recipe.numRecipes; i++)
        {
            Recipe recipe = Main.recipe[i];
            FacsimileRecipe cached = FacsimileRecipe.Build(recipe, dustType);
            if (cached != null)
                recipes.Add(recipe, cached);
        }
    }

    public override void Unload()
    {
        IL_Recipe.FindRecipes -= PatchMaterialCheck;
        On_Recipe.FindRecipes -= FindRecipes;
        On_Main.CraftItem -= CraftItem;
        On_Recipe.Create -= ConsumeMaterials;
        recipes.Clear();
        quotes.Clear();
        activeRecipe = null;
        activeInventory = null;
        ownedItems = null;
        consumedItems = null;
        meetsTiles = null;
        meetsEnvironment = null;
    }

    private void PatchMaterialCheck(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(i => i.MatchCall<Recipe>(nameof(Recipe.CollectedEnoughItemsToCraftRecipeNew))))
            throw new InvalidOperationException("Ashen Facsimile Dust: normal crafting material check was not found.");
        cursor.Remove();
        cursor.EmitDelegate<Func<Recipe, bool>>(HasMaterials);
    }

    private void FindRecipes(On_Recipe.orig_FindRecipes orig, bool canDelayCheck)
    {
        if (!canDelayCheck)
            quotes.Clear();
        orig(canDelayCheck);
    }

    private bool HasMaterials(Recipe recipe)
    {
        if (Recipe.CollectedEnoughItemsToCraftRecipeNew(recipe))
            return true;
        if (dustType <= 0 || !ownedItems.TryGetValue(dustType, out int dust) || dust <= 0 ||
            !recipes.TryGetValue(recipe, out FacsimileRecipe cached) ||
            !FacsimileMaterialRule.TryQuote(cached.Requirements, ownedItems, dustType, out var quote))
            return false;
        quotes[recipe] = quote;
        return true;
    }

    private void CraftItem(On_Main.orig_CraftItem orig, Recipe recipe)
    {
        // 原料齐全的配方和独立制作系统保持原路径，包括它们的所有扣料钩子。
        if (!quotes.ContainsKey(recipe) || !recipes.TryGetValue(recipe, out FacsimileRecipe cached))
        {
            orig(recipe);
            return;
        }

        Player player = Main.LocalPlayer;
        if (recipe.Disabled || !meetsTiles(player, recipe) || !meetsEnvironment(player, recipe) ||
            !RecipeLoader.RecipeAvailable(recipe))
        {
            Recipe.FindRecipes();
            return;
        }
        if (Main.mouseItem.stack > 0 && !ItemLoader.CanStack(Main.mouseItem, recipe.createItem))
            return;

        var inventory = new FacsimileCraftingInventory(player);
        if (!FacsimileMaterialRule.TryQuote(cached.Requirements, inventory.Counts, dustType, out var quote))
        {
            Recipe.FindRecipes();
            return;
        }
        if (quote.Dust == 0)
        {
            orig(recipe);
            return;
        }
        if (!inventory.TryPlan(recipe, cached, dustType, out _) || !inventory.IsCurrent())
        {
            Recipe.FindRecipes();
            return;
        }

        Recipe previousRecipe = activeRecipe;
        FacsimileCraftingInventory previousInventory = activeInventory;
        activeRecipe = recipe;
        activeInventory = inventory;
        try
        {
            // 仍由原版生成产物、随机前缀、OnCraft/OnCreate、堆叠、提示和声音。
            orig(recipe);
        }
        catch (StaleFacsimilePlanException)
        {
            // Create 返回 void；必须中断整个 CraftItem，不能只跳过扣料后继续给成品。
            Recipe.FindRecipes();
        }
        finally
        {
            activeRecipe = previousRecipe;
            activeInventory = previousInventory;
        }
    }

    private void ConsumeMaterials(On_Recipe.orig_Create orig, Recipe recipe)
    {
        if (!ReferenceEquals(activeRecipe, recipe) || activeInventory == null)
        {
            orig(recipe);
            return;
        }

        FacsimileCraftingInventory inventory = activeInventory;
        activeInventory = null; // 回调若触发嵌套制作，不得重复提交同一计划。
        if (!inventory.IsCurrent())
            throw new StaleFacsimilePlanException();
        inventory.Commit(consumedItems);
        AchievementsHelper.NotifyItemCraft(recipe);
        AchievementsHelper.NotifyItemPickup(Main.LocalPlayer, recipe.createItem);
        Recipe.FindRecipes();
    }

    internal bool TryGetQuote(Recipe recipe, out FacsimileMaterialRule.Quote quote) => quotes.TryGetValue(recipe, out quote);

    private sealed class StaleFacsimilePlanException : Exception { }
}
