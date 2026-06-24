using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using System;
using 伊蕾娜.Dusts;

namespace 伊蕾娜.Projectiles.Magicwater
{
    public class 水牢 : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.penetrate = -1;
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.ownerHitCheck = false;
            base.SetStaticDefaults();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            float scale = MathHelper.Lerp(1, 3f, 1);
            for (int i = 0; i < scale * 5; i++)
            {
                Vector2 dustrandpos = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f);
                Dust d = Dust.NewDustDirect(dustrandpos, 0, 0, ModContent.DustType<水花粒子>(),
                0, 0, 255, Color.White, scale);
                d.noGravity = true;
            }
            return false;
        }
        public override void AI()
        {
            var player = Main.player[Projectile.owner];
            if (Projectile.owner == Main.myPlayer)//同步鼠标位置
            {

                if (Main.mouseLeft)
                {
                    控制武器位置(player);
                    if (Main.GameUpdateCount % 10 == 0)
                    {
                        Projectile proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI("水牢"), Projectile.Center+ Main.rand.NextVector2Circular(100,100), Vector2.Zero, ModContent.ProjectileType<水粒子2>(), 0, 1, player.whoAmI, Projectile.whoAmI);
                    }
                }
            }
            base.AI();
        }
        void 控制武器位置(Player player)
        {
            var towards = Vector2.Normalize(Main.MouseWorld - player.Center);
            player.itemTime = 2;
            player.itemAnimation = 2;
            player.direction = towards.X < 0 ? -1 : 1;//玩家朝向根据鼠标
            Projectile.rotation = towards.ToRotation();
            Projectile.timeLeft = 20;
            player.itemRotation = (float)Math.Atan2(towards.ToRotation().ToRotationVector2().Y * player.direction, towards.ToRotation().ToRotationVector2().X * player.direction);//武器朝向
            Projectile.Center = Main.npc[(int)Projectile.ai[0]].Center;
        }

    }
}
