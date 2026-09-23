using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent.Achievements;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.item.Curios;
using 伊蕾娜.ElainaModAlchemy.Gameplay;

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
        bool hasOriginalMaterials = Recipe.CollectedEnoughItemsToCraftRecipeNew(recipe);
        if (AlchemyVanillaRecipes.IsAlchemyRecipe(recipe) && !AlchemyVanillaRecipes.AllowFacsimileDust)
        {
            quotes.Remove(recipe);
            return hasOriginalMaterials;
        }
        if (dustType <= 0 || !recipes.TryGetValue(recipe, out FacsimileRecipe cached))
            return hasOriginalMaterials;

        FacsimilePriority priority = GetPriority();
        if (priority == FacsimilePriority.Materials && hasOriginalMaterials)
            return true;

        if (FacsimileMaterialRule.TryQuote(cached.Requirements, ownedItems, dustType, priority, out var quote) &&
            quote.Dust > 0)
        {
            quotes[recipe] = quote;
            return true;
        }

        // 灰赝尘方案不可用时，优先灰赝尘模式仍可回退到完整的原版材料方案。
        return hasOriginalMaterials;
    }

    private void CraftItem(On_Main.orig_CraftItem orig, Recipe recipe)
    {
        if (AlchemyVanillaRecipes.IsAlchemyRecipe(recipe) && !AlchemyVanillaRecipes.AllowFacsimileDust)
        {
            quotes.Remove(recipe);
            orig(recipe);
            return;
        }
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
        FacsimilePriority priority = GetPriority();
        if (!FacsimileMaterialRule.TryQuote(cached.Requirements, inventory.Counts, dustType, priority, out var quote))
        {
            Recipe.FindRecipes();
            return;
        }
        if (quote.Dust == 0)
        {
            orig(recipe);
            return;
        }
        if (!inventory.TryPlan(recipe, cached, dustType, priority, out _) || !inventory.IsCurrent())
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

    internal bool HasAlchemyMaterials(Recipe recipe) => HasMaterials(recipe);
    internal int MaxAlchemyBatches(Recipe recipe, int limit)
    {
        if (!recipes.TryGetValue(recipe, out var cached)) return 1;
        int result = 0;
        for (int count = 1; count <= limit; count++)
        {
            var requirements = new FacsimileMaterialRule.Requirement[cached.Requirements.Length];
            bool originals = true;
            for (int i = 0; i < requirements.Length; i++)
            {
                var original = cached.Requirements[i];
                requirements[i] = new FacsimileMaterialRule.Requirement(checked(original.Stack * count), original.AcceptedTypes);
                long have = 0;
                foreach (int type in original.AcceptedTypes) have += ownedItems.GetValueOrDefault(type);
                if (have < requirements[i].Stack) originals = false;
            }
            if (!originals && !FacsimileMaterialRule.TryQuote(requirements, ownedItems, dustType, GetPriority(), out _)) break;
            result = count;
        }
        return result;
    }

    private static FacsimilePriority GetPriority() => Main.LocalPlayer.active &&
        Main.LocalPlayer.GetModPlayer<FacsimileCraftingPlayer>().PreferDust
            ? FacsimilePriority.Dust
            : FacsimilePriority.Materials;

    private sealed class StaleFacsimilePlanException : Exception { }
}
