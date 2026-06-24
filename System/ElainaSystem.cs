using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.System
{
    public class ElainaSystem : ModSystem
    {
        public static readonly Condition IsElaina = new("Mods.伊蕾娜.IsElaina", () => Main.LocalPlayer.EMP().Elaina);
        public override void AddRecipes()
        {
            Recipe recipe = Recipe.Create(ItemID.Cauldron)// 大锅
            .AddRecipeGroup(RecipeGroupID.IronBar, 10)
            .AddIngredient(ItemID.Campfire, 1)
            .AddCondition(IsElaina);
            recipe.Register();

            recipe = Recipe.Create(ItemID.SpellTome)// 魔法书制作
            .AddIngredient(ItemID.Book, 1)//书1
            .AddIngredient(ItemID.FallenStar, 10)//十个坠落之星
            .AddCondition(IsElaina);
            recipe.Register();
        }
    }
}
