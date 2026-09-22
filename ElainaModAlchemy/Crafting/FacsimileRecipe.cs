using System;
using System.Collections.Generic;
using Terraria;

namespace 伊蕾娜.ElainaModAlchemy.Crafting;

/// <summary>加载配方后缓存可替换类型，不在合成列表刷新时遍历所有 RecipeGroup。</summary>
internal sealed class FacsimileRecipe
{
    internal readonly FacsimileMaterialRule.Requirement[] Requirements;
    internal readonly int[][] TypesByIngredient;
    internal readonly int[] RequirementIndexByIngredient;

    private FacsimileRecipe(FacsimileMaterialRule.Requirement[] requirements, int[][] typesByIngredient,
        int[] requirementIndexByIngredient)
    {
        Requirements = requirements;
        TypesByIngredient = typesByIngredient;
        RequirementIndexByIngredient = requirementIndexByIngredient;
    }

    internal static FacsimileRecipe Build(Recipe recipe, int dustType)
    {
        if (recipe.requiredItem.Count == 0 || recipe.createItem.type == dustType)
            return null;

        var requirements = new List<FacsimileMaterialRule.Requirement>();
        var byIngredient = new int[recipe.requiredItem.Count][];
        var requirementIndex = new int[recipe.requiredItem.Count];
        var owners = new Dictionary<int, int>();
        for (int i = 0; i < recipe.requiredItem.Count; i++)
        {
            Item item = recipe.requiredItem[i];
            if (item.IsAir || item.stack <= 0)
                return null;
            var types = new HashSet<int> { item.type };
            foreach (int groupId in recipe.acceptedGroups)
            {
                RecipeGroup group = RecipeGroup.recipeGroups[groupId];
                if (group.ValidItems.Contains(item.type))
                    types.UnionWith(group.ValidItems);
            }

            if (types.Contains(dustType))
                return null;
            int owner = -1;
            foreach (int type in types)
            {
                if (!owners.TryGetValue(type, out int existing))
                    continue;
                if (owner >= 0 && owner != existing)
                    return null;
                owner = existing;
            }

            int[] accepted = new int[types.Count];
            types.CopyTo(accepted);
            Array.Sort(accepted);
            byIngredient[i] = accepted;
            if (owner >= 0)
            {
                requirementIndex[i] = owner;
                var previous = requirements[owner];
                if (!types.SetEquals(previous.AcceptedTypes) || (long)previous.Stack + item.stack > int.MaxValue)
                    return null;
                requirements[owner] = previous with { Stack = previous.Stack + item.stack };
            }
            else
            {
                requirementIndex[i] = requirements.Count;
                foreach (int type in accepted)
                    owners.Add(type, requirements.Count);
                requirements.Add(new(item.stack, accepted));
            }
        }

        return new FacsimileRecipe(requirements.ToArray(), byIngredient, requirementIndex);
    }
}
