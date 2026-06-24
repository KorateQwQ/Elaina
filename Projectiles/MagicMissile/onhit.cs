using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.Projectiles.MagicMissile
{
    public class onhit : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
            //DisplayName.SetDefault("魔力飞弹"); // Set the projectile name to Example Flail Ball
        }
        public override void SetDefaults()
        {

            //projectile.ignoreWater = true;//无视水
            //projectile.friendly = true;//可以攻击敌人
            Projectile.penetrate = -1; // 穿透数量
            Projectile.tileCollide = false;//瓷砖碰撞
                                           //projectile.timeLeft=30;
            Projectile.width = 250;
            Projectile.height = 250;
            //projectile.damage=50;
            Projectile.timeLeft = 300;
            Projectile.scale = 1f;

        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];//读取主人位置
            var p2 = player.GetModPlayer<魔力飞弹modplayer>();
            Projectile.rotation = Projectile.ai[0];
            //if(projectile.ai[0]<1)p=player.position;
            //else player.position=p;
            //
            Lighting.AddLight(Projectile.Center, 255f / 200f, 119f / 200f, 215f / 200f);

            Projectile.frameCounter++;
            if (Projectile.frame == 1 && Projectile.frameCounter == 2)
            {
                /*for (int i = 0; i < int.MaxValue / 20f; i++)
				{什么时停卡肉
				}*/
            }
            if (Projectile.frameCounter % 7 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 3)
            {
                Projectile.frame = 0;
                Projectile.Kill();
                return;

            }


        }
    }
}
