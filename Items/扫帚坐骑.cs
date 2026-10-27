using System.Collections.Generic;
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
        public override bool CanUseItem(Player player) => BroomAccessSystem.CanRide(player);

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 34;
            Item.rare = ItemRarityID.Pink;
            Item.mountType = ModContent.MountType<扫帚>();
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // 与魔杖一致：非伊蕾娜只看到另一个名字。
            if (Main.LocalPlayer.EMP().Elaina) return;
            tooltips.RemoveAll(line => line.Mod == "Terraria" && line.Name.StartsWith("Tooltip"));
            TooltipLine name = tooltips.Find(line => line.Mod == "Terraria" && line.Name == "ItemName");
            if (name != null) name.Text = GTV("Broom.CantUse");
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
