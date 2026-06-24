using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.Projectiles.Lightning
{
    public class 准备天雷 : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
        public override void AI()
        {
            var player = Main.player[Projectile.owner];
            if (player.channel)
            {
                player.itemTime = 2;
                player.itemAnimation = 2;
                Projectile.timeLeft = 2;
            }
            if (Main.rand.Next(0, 50) > 10)
            {
                Projectile.NewProjectileDirect(null, Main.MouseWorld + Main.rand.NextVector2Circular(100, 100), Vector2.Zero, ModContent.ProjectileType<雷粒子>(), 0, 1, Projectile.owner);
            }
            base.AI();
        }

    }
}
