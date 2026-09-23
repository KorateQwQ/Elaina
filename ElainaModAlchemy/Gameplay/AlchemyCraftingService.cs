using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.UI;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

/// <summary>Local-player notebook operations. Crafting itself belongs to Terraria's Recipe/Main APIs.</summary>
public static class AlchemyCraftingService
{
    public const int MaximumBatches = 99, DebugSupplyBatches = 10;
    public static AlchemyCatalogRecipe FindEntry(string id) => id == null ? null : AlchemyCatalog.Recipes.FirstOrDefault(r => r.Id == id);
    public static AlchemyRecipe RecipeFor(string id) => RecipeFor(FindEntry(id));
    public static AlchemyRecipe RecipeFor(AlchemyCatalogRecipe entry)
    {
        if (entry == null || !entry.Craftable || entry.ItemType <= 0) return null;
        var ingredients = new List<AlchemyIngredient>();
        foreach (var row in entry.Ingredients)
        {
            int[] choices = row.Choices.Select(AlchemyCatalog.ResolveItemType).ToArray();
            if (choices.Length == 0 || choices.Any(type => type <= 0) || row.Count <= 0) return null;
            ingredients.Add(new AlchemyIngredient(choices, row.Count));
        }
        return new AlchemyRecipe(entry.Yield, ingredients.ToArray());
    }

    internal static AlchemyNotebookResult Execute(Player player, AlchemyNotebookAction action, string id, int batches, int sequence)
    {
        AlchemyNotebookResult Fail(string text) => new(sequence, action, id, false, text);
        if (Main.dedServ || player == null || player != Main.LocalPlayer || !player.active || player.dead || player.ghost)
            return Fail("当前无法炼金");
        var entry = FindEntry(id);
        if (entry == null || batches is < 1 or > MaximumBatches) return Fail("炼金操作无效");
        var progress = player.GetModPlayer<AlchemyProgressPlayer>();
        if (action == AlchemyNotebookAction.Research)
        {
            bool unlocked = progress.TryResearch(id, out string message);
            if (unlocked) Recipe.FindRecipes();
            return new(sequence, action, id, unlocked, message);
        }
#if DEBUG
        if (action == AlchemyNotebookAction.DebugReset)
        {
            progress.ResetProgress(); Recipe.FindRecipes();
            return new(sequence, action, id, true, "已重置全部研究、炼金等级和经验");
        }
        if (action == AlchemyNotebookAction.DebugRestock)
        {
            var recipe = AlchemyVanillaRecipes.Get(id);
            if (recipe == null) return Fail("此条目没有制作材料");
            foreach (var material in recipe.requiredItem)
                Give(player, material.type, checked(material.stack * DebugSupplyBatches), "ElainaAlchemyDebug");
            Recipe.FindRecipes();
            return new(sequence, action, id, true, "已补充当前配方的 10 批素材");
        }
#endif
        if (action != AlchemyNotebookAction.Craft) return Fail("此操作不可用");
        if (!progress.IsUnlocked(id)) return Fail("请先研究此配方");
        int completed = 0, output = 0, experience = 0;
        for (int i = 0; i < batches; i++)
        {
            // Vanilla recollects current materials and consumes a batch through CraftItem.
            // Recheck every batch, including external material-provider callbacks.
            Item item = AlchemyVanillaRecipes.CraftOne(player, id);
            if (item == null || item.IsAir) break;
            output += item.stack;
            AlchemyInventory.Give(player, item, new EntitySource_Misc("ElainaAlchemy"));
            experience += progress.AddCraftExperience(id, 1);
            completed++;
        }
        if (completed == 0) return Fail("素材不足或当前不满足制作条件");
        Recipe.FindRecipes();
        string result = $"炼成「{entry.Name}」× {output} · 炼金经验 +{experience}";
        if (completed != batches) result += $"（完成 {completed}/{batches} 批）";
        return new(sequence, action, id, true, result, output, experience);
    }

    private static void Give(Player player, int type, int count, string context)
    {
        while (count > 0)
        {
            var item = new Item(type);
            item.stack = Math.Min(count, item.maxStack);
            count -= item.stack;
            AlchemyInventory.Give(player, item, new EntitySource_Misc(context));
        }
    }
}