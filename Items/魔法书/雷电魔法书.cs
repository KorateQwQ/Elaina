using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace 伊蕾娜.Items.魔法书
{
    public class 雷电魔法书 : MagicBook
    {
        public override int Frame => 7;
        public override int Skill => 5;
        public override void UseItemFrame(Player player)
        {
            UseBook(player);
            player.itemLocation -= player.direction > 0 ? new Vector2(25, 0) : new Vector2(-25, 0);
            Lighting.AddLight(player.itemLocation, 77 / 255f, 179 / 255f, 189 / 255f);

            Vector2 move = player.direction > 0 ? new Vector2(20, -30) : new Vector2(-40, -30);
            for (int i = 0; i < 9; i++)
            {
                Dust d = Dust.NewDustDirect(player.itemLocation + move, 8, 8, DustID.YellowStarDust, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 0, Color.White, MathHelper.Lerp(0.2f, 2.5f, player.itemAnimation / 30f));
                d.noGravity = true;
            }
        }
    }
}
