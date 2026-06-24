using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.Projectiles.Flame
{
    public class FlameOnhit : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
            //DisplayName.SetDefault("魔力飞弹"); // Set the projectile name to Example Flail Ball
        }
        public override void SetDefaults()
        {

            //projectile.ignoreWater = true;//无视水
            //projectile.friendly = true;//可以攻击敌人
            Projectile.penetrate = -1; // 穿透数量
            Projectile.tileCollide = false;//瓷砖碰撞
                                           //projectile.timeLeft=30;
            Projectile.width = 130;
            Projectile.height = 140;
            //projectile.damage=50;
            Projectile.timeLeft = 300;
            Projectile.scale = 1f;

        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];//读取主人位置
            Projectile.rotation = Projectile.ai[0];
            //if(projectile.ai[0]<1)p=player.position;
            //else player.position=p;
            //

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 7 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 6)
            {
                Projectile.frame = 0;
                Projectile.Kill();
                return;

            }
        }
    }
}
