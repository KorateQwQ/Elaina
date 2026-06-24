using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace 伊蕾娜.Items.魔法书
{

    public class 风魔法书 : MagicBook
    {
        public override int Frame => 12;
        public override int Skill => 6;
        public override void UseItemFrame(Player player)
        {
            UseBook(player);
            Lighting.AddLight(player.itemLocation, 77 / 255f, 179 / 255f, 189 / 255f);
            Vector2 move = player.direction > 0 ? new Vector2(20, -30) : new Vector2(-20, -30);
            for (int i = 0; i < 9; i++)
            {
                Dust d = Dust.NewDustDirect(player.itemLocation + move, 8, 8, DustID.Cloud, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 0, Color.White, MathHelper.Lerp(0.2f, 2.5f, player.itemAnimation / 30f));
                d.noGravity = true;
            }
        }
        public override void AddRecipes()
        {
            //ondition Wind = new("Mods.伊蕾娜.Conditions.Wind", () => !Main.LocalPlayer.SKP().IfLearnSkill[6]);

            Recipe recipe = Recipe.Create(Item.type)// CreateRecipe(ItemID.Cauldron,1)
                .AddIngredient(ItemID.RazorbladeTyphoon, 1)//利刃台风
                .AddIngredient(ItemID.SoulofFright, 50)//恐惧之魂50个
                .AddIngredient(ItemID.SoulofSight, 50)//视觉之魂50
                .AddIngredient(ItemID.SoulofMight, 50)//力量之魂50个
                //.AddCondition(IsElaina, Wind)
                .Register();

        }
    }
}
