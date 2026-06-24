using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.System;

namespace 伊蕾娜.Projectiles.Lightning
{
    public class 雷击点 : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.penetrate = -1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;//瓷砖碰撞
            Projectile.friendly = false;
            //projectile.timeLeft=30;
            //projectile.extraUpdates=1;
            Projectile.width = 100;
            Projectile.height = 30;
            Projectile.damage = 20;
            Projectile.timeLeft = 2000;
            Projectile.knockBack = 2;
            Projectile.extraUpdates = 10;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            float 大小 = MathHelper.Lerp(1f, 2.5f, Projectile.ai[0] / 5f);
            if (大小 < 1.7f)
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    //Main.NewText(123);
                    Projectile.NewProjectile(null, Projectile.Center, Vector2.Zero, ModContent.ProjectileType<击地粒子>(), 0, 0, Projectile.owner, Projectile.ai[0]);
                }
                Projectile.Kill();
                return true;
            }
            Projectile.velocity = oldVelocity;
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            var player = Main.player[Projectile.owner];
            if (Main.myPlayer == Projectile.owner)
            {
                float 伤害系数 = player.GetTotalDamage(DamageClass.Magic).Additive;
                int 大小 = (int)MathHelper.Lerp(1f, 3f, Projectile.ai[0] / 5);
                Projectile proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<雷击>(), (int)(6666 * 大小 * 伤害系数* EXPmodplayer.DamageScale ), 0, Projectile.owner, Main.rand.Next(1, 3), Projectile.ai[0]);
                //proj.CritChance += Main.player[Projectile.owner].HeldItem.crit;
                //Main.NewText(proj.CritChance);
            }
            base.OnKill(timeLeft);
        }
        public override void AI()
        {
            var player = Main.player[Projectile.owner];
            if (Projectile.Center.Y - player.Center.Y > 550)
            {
                Projectile.Kill();
            }
        }
    }
}
