using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.Projectiles.Flame
{
    public class 火球 : ModProjectile
    {
        bool onhit = false;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
            //DisplayName.SetDefault("火球");
        }
        public override void SetDefaults()
        {

            Projectile.ignoreWater = false;//无视水
            Projectile.friendly = true;//可以攻击敌人	
            Projectile.penetrate = -1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;//瓷砖碰撞
            Projectile.timeLeft = 300;
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.scale = 0.8f;
        }
        public override void OnKill(int timeLeft)
        {
            var Flame = Main.player[Projectile.owner].GetModPlayer<FlameModplayer>();
            Flame.exp += 2 + Flame.熟练度;
            Flame.lvUp();
            for (int i = 0; i < 100; i++)
            {
                Vector2 v = Projectile.velocity * 2f + new Vector2(Main.rand.NextFloatDirection() * 40f, Main.rand.NextFloatDirection() * 40f);
                v = Vector2.Normalize(v) * 3;
                Dust d = Dust.NewDustDirect(Projectile.position, 40, 40, 55,
                -v.X, -v.Y, 0, Color.White, 0.5f);
                d.noLight = false;
                d.noGravity = false;

            }
            base.OnKill(timeLeft);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return true;
        }
        public override void AI()
        {
            DrawOffsetX = -16;
            Lighting.AddLight(Projectile.Center, 227 / 100, 0, 36 / 100);
            //Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi / 2;
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frame++;
            }
            if (Projectile.frame >= 3)
            {
                Projectile.frame = 0;
            }
            //Dust d = Dust.NewDustDirect(Projectile.position, 0, 0, 55,
               // -0, 0, 0, Color.White, 0.5f);
            //d.noLight = false;
            if (Projectile.wet) Projectile.Kill();
            Vector2 velocity =new(10, 0);
            /*for (int i = 0; i < 3; i++)
            {

                //d.alpha = 0;
            }
            Vector2 v = -velocity * 0.2f + (velocity.RotatedBy(Math.PI / 2) * (Main.rand.Next(0, 2) == 1 ? 1 : -1) * Main.rand.NextFloat(0, 0.1f));
            Dust d = Dust.NewDustDirect(Projectile.position, 50, 50, ModContent.DustType<火焰尾焰>(),
            v.X, v.Y, 255, Color.White, 1f);
            d.noGravity = true;
            d.velocity = v;
            //Main.NewText(v.Y);
            Projectile.velocity = Vector2.Zero;
            //Projectile.timeLeft++;
            //Projectile.Kill();*/
            base.AI();
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 180);
            if (!onhit)
            {
                onhit = true;
                Projectile.timeLeft = 5;
            }
            base.OnHitNPC(target, hit, damageDone);
        }

    }
}
