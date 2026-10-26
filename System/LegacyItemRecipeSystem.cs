using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.Items;
using 伊蕾娜.Items.accessories;
using 伊蕾娜.Items.消耗品;
using 伊蕾娜.Items.魔法书;

namespace 伊蕾娜.System;

/// <summary>暂时停用旧物品的制作，保留物品类型、存档兼容和原配方，便于以后恢复。</summary>
public sealed class LegacyItemRecipeSystem : ModSystem
{
    public override void PostAddRecipes()
    {
        for (int i = 0; i < Recipe.numRecipes; i++)
        {
            Recipe recipe = Main.recipe[i];
            if (IsLegacyItem(recipe.createItem.ModItem) ||
                // 旧魔法书的基础材料配方；只停用本模组添加的配方。
                (recipe.Mod == Mod && recipe.createItem.type == ItemID.SpellTome))
            {
                recipe.DisableRecipe();
            }
        }
    }

    private static bool IsLegacyItem(ModItem item) => item is
        魔力共享戒指 or
        白河豚项链 or
        MagicBook or
        寒霜药水 or
        焰火药水 or
        潋滟药水 or
        雷引药水 or
        小动物药水 or
        究极魔力药水;
}
