using System;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.Projectiles.MagicMissile;

namespace 伊蕾娜.ReProjs
{
    public class MissileSpawner : ElainaProj
    {
        private static int[] missiles;
        private static bool[] check;
        private static SoundStyle spawn;
        private static Vector2[] pos;
        private static Vector2 origin = new(51, 50);
        public Vector2 Pos => pos[Index];
        public int Index => (int)Projectile.ai[0];
        public int Target
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public override void Load()
        {
            missiles = [-1, -1, -1, -1, -1];
            check = new bool[5];
            spawn = new SoundStyle("伊蕾娜/Projectiles/MagicMissile/生成", 1, SoundType.Sound)
            {
                Volume = 0.1f,
                MaxInstances = 5,
                Pitch = 0,
                PitchVariance = 0.3f,
                SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
                PlayOnlyIfFocused = true,
            };
            float rot = MathF.PI / 4f, r = 0;
            pos = new Vector2[5];
            int i = 0;
            while (i < 5)
            {
                pos[i++] = r.ToRotationVector2() * 75;
                r -= rot;
            }
        }
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 14;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.damage = 50;
            Projectile.alpha = 0;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (++Projectile.frameCounter % 3 == 0)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame > 14)
                {
                    Projectile.frame = 11;
                }
                if (Projectile.frame == 8)
                {
                    Spawn(Projectile.GetSource_FromAI(), player, (int)Projectile.ai[0]);
                }
            }
            Projectile.Center = player.Center + Pos + new Vector2(0, player.gfxOffY);
            if (player.whoAmI == Main.myPlayer)//索敌
            {
                Projectile.ai[1] = Projectile.FindTargetWithLineOfSight(1000f);
                Projectile.netUpdate = true;
            }
            NPC target = null;
            if (Target >= 0)
                target = Main.npc[Target];
            if (target != null && target.active && !target.friendly && !target.dontTakeDamage && Projectile.frame > 8)
            {
                Vector2 targetVec = target.Center - Projectile.Center;
                targetVec.Normalize();
                targetVec *= 15f;
                if (player.CheckMana(player.HeldItem, (int)(5 * player.manaCost), true))
                {
                    SoundEngine.PlaySound(生成, Projectile.Center);
                    player.manaRegenDelay = 90;
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(),
                        Projectile.position, targetVec, ModContent.ProjectileType<魔力飞弹>(),
                        player.MagicDamage(player.EXP().GetMagicMissleDamage()), 2, player.whoAmI, -1f, 1);
                    Projectile.Kill();
                }
            }
        }
        public override bool ShouldUpdatePosition() => false;
        public override void OnKill(int timeLeft) => missiles[Index] = -1;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.Draw(TextureAssets.Projectile[Type].Value, Projectile.Center - Main.screenPosition,
                new(0, Projectile.frame * 100, 100, 100), Color.White, 0, origin, 1f, 0, 0);
            return false;
        }
        public static void Spawn(IEntitySource source, Player player, int start = -1)
        {
            int type = ModContent.ProjectileType<MissileSpawner>();
            int index = -1;
            bool ignoreCheck = start > -1;
            for (int i = start + 1; i < 5; i++)
            {
                int state = missiles[i];
                if (state == -1)
                {
                    index = i;
                    break;
                }
                Projectile p = Main.projectile[state];
                if ((ignoreCheck || !check[i]) && (!p.active || p.active && p.type != type))
                {
                    index = i;
                    break;
                }
            }
            if (index > -1)
            {
                check[index] = false;
                for (int i = index + 1; i < 5; i++)
                    check[i] = true;
                Vector2 target = pos[index] + player.Center;
                int p = Projectile.NewProjectile(source, target, Vector2.Zero, type, 0, 0, player.whoAmI, index);
                missiles[index] = p;
            }
        }
        public static void Reset()
        {
            missiles = [-1, -1, -1, -1, -1];
            check = new bool[5];
        }
    }
}
