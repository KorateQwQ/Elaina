using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace 伊蕾娜.Items.魔法书
{
    public class 治疗魔法书 : MagicBook
    {
        public override int Frame => 8;
        public override int Skill => 4;
        public override void UseItemFrame(Player player)
        {
            UseBook(player);
            Lighting.AddLight(player.itemLocation, 77 / 255f, 179 / 255f, 189 / 255f);
            Vector2 move = player.direction > 0 ? new Vector2(20, -30) : new Vector2(-40, -30);
            for (int i = 0; i < 9; i++)
            {
                Dust d = Dust.NewDustDirect(player.itemLocation + move, 8, 8, DustID.GreenFairy, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 0, Color.White, MathHelper.Lerp(0.2f, 1.1f, player.itemAnimation / 30f));
                d.noGravity = true;
            }
        }
        public override void AddRecipes()
        {
            //Condition healing = new("Mods.伊蕾娜.Conditions.Healing", () => !Main.LocalPlayer.SKP().IfLearnSkill[4]);

            Recipe recipe = Recipe.Create(Item.type)// CreateRecipe(ItemID.Cauldron,1)
                .AddIngredient(ItemID.Vine, 50)//藤蔓50
                .AddIngredient(ItemID.JungleSpores, 50)//孢子50
                .AddIngredient(ItemID.Moonglow, 50)//月光草50
                .AddIngredient(ItemID.SpellTome)//魔法书*1
                //.AddCondition(IsElaina, healing)
                .Register();

        }

    }
}
