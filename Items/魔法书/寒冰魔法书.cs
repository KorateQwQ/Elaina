using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace 伊蕾娜.Items.魔法书
{
    public class 寒冰魔法书 : MagicBook
    {
        public override int Frame => 9;
        public override int Skill => 3;
        public override void UseItemFrame(Player player)
        {
            UseBook(player);
            player.itemLocation -= player.direction > 0 ? new Vector2(25, 0) : new Vector2(-25, 0);
            Lighting.AddLight(player.itemLocation, 77 / 255f, 179 / 255f, 189 / 255f);

            Vector2 move = player.direction > 0 ? new Vector2(20, -30) : new Vector2(-40, -30);
            for (int i = 0; i < 9; i++)
            {
                Dust d = Dust.NewDustDirect(player.itemLocation + move, 8, 8, DustID.IceTorch, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 0, Color.White, MathHelper.Lerp(0.2f, 2.5f, player.itemAnimation / 30f));
            }
        }
        public override void AddRecipes()
        {
            //Condition freeze = new("Mods.伊蕾娜.Conditions.Freeze", () => !Main.LocalPlayer.SKP().IfLearnSkill[3]);

            Recipe recipe = Recipe.Create(Item.type) // CreateRecipe(ItemID.Cauldron,1)
                .AddIngredient(ItemID.FrostCore, 15) //寒霜核15
                .AddIngredient(ItemID.Shiverthorn, 50) //寒颤棘*50
                .AddIngredient(ItemID.IceBlock, 100) //冰雪块*100
                .AddIngredient(ItemID.SpellTome);//魔法书*1
                //.AddCondition(IsElaina, freeze);
            recipe.Register();
        }
    }
}
