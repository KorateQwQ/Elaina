using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using 伊蕾娜.Mounts;
using 伊蕾娜.System;

namespace 伊蕾娜.Items
{
    public class 扫帚坐骑 : ModItem
    {
        public override LocalizedText Tooltip => Main.gameMenu ? base.Tooltip : Main.LocalPlayer.EMP().Elaina ?
                base.Tooltip : Language.GetText("Mods.伊蕾娜.Items." + GetType().Name + ".Warn");

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 34;
            Item.rare = ItemRarityID.Pink;
            Item.mountType = ModContent.MountType<扫帚>();
        }
        public override bool CanEquipAccessory(Player player, int slot, bool modded) => player.EMP().Elaina;
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddRecipeGroup(RecipeGroupID.Wood, 10)//任意木头10
                .AddIngredient(ItemID.Hay, 20)
                .AddTile(TileID.WorkBenches)//工作台
                .AddCondition(ElainaSystem.IsElaina)
                .Register();
        }

    }
}
