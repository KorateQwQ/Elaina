using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace 伊蕾娜.Items.魔法书
{

    public class 火焰魔法书 : MagicBook
    {
        public override int Frame => 6;
        public override int Skill => 2;
        public override void UseItemFrame(Player player)
        {
            UseBook(player);
            Lighting.AddLight(player.itemLocation, 77 / 255f, 179 / 255f, 189 / 255f);
            Vector2 move = player.direction > 0 ? new Vector2(20, -30) : new Vector2(-20, -30);
            for (int i = 0; i < 9; i++)
            {
                Dust d = Dust.NewDustDirect(player.itemLocation + move, 8, 8, DustID.Flare, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 0, Color.White, MathHelper.Lerp(0.2f, 2.5f, player.itemAnimation / 30f));
                d.noGravity = true;
            }
        }
        public override void AddRecipes()
        {
            //Condition flame = new("Mods.伊蕾娜.Conditions.Flame", () => !Main.LocalPlayer.SKP().IfLearnSkill[2]);

            Recipe recipe = Recipe.Create(Item.type)// CreateRecipe(ItemID.Cauldron,1)
                .AddIngredient(ItemID.HellButterfly, 15)//地狱蝴蝶15
                .AddIngredient(ItemID.Fireblossom, 50)//火焰花50
                .AddIngredient(ItemID.Hellstone, 50)//狱石50
                .AddIngredient(ItemID.SpellTome)//魔法书*1
                //.AddCondition(IsElaina, flame)
                .Register();
        }
    }
}
