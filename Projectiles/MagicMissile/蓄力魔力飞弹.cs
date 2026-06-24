using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.IO;
using Terraria.Audio;
using Terraria.ModLoader;

namespace 伊蕾娜.Projectiles.MagicMissile
{
    public class 蓄力魔力飞弹 : KLProjectile
    {
        Asset<Texture2D> texture;
        Vector2 towards = Vector2.Zero;
        int 发射速率 = 0;
        Vector2 推动速度 = Vector2.Zero;
        SoundStyle 生成 = (new SoundStyle($"伊蕾娜/Projectiles/MagicMissile/生成", 1, SoundType.Sound)) with
        {
            Volume = 0.1f,
            MaxInstances = 15,
            Pitch = 0,
            PitchVariance = 0.7f,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PlayOnlyIfFocused = true,
        };
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(towards);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            towards = reader.ReadVector2();
        }
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 12;
            // DisplayName.SetDefault("魔力飞弹");
        }

        public override void SetDefaults()
        {

            Projectile.penetrate = -1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;//瓷砖碰撞
            Projectile.friendly = true;
            //projectile.timeLeft=30;
            //projectile.extraUpdates=1;
            Projectile.width = 90;
            Projectile.height = 120;
            Projectile.damage = 20;
            Projectile.timeLeft = 40;
            Projectile.knockBack = 2;
            //Projectile.extraUpdates = ;
            Projectile.alpha = 0;

        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (texture == null)
                texture = Mod.Assets.Request<Texture2D>("Projectiles/MagicMissile/蓄力魔力飞弹");
            Vector2 origin = texture.Size() * 0.5f;//除3相当于以图片中心为position
            origin.Y /= 12;//动图还要除以帧数
            int frameHeight = texture.Height() / Main.projFrames[Projectile.type];//图片总高度除以帧数，得到每张图的高度
            int startY = frameHeight * Projectile.frame;//每一帧的起始坐标Y
            Rectangle sourceRectangle = new(0, startY, texture.Width(), frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
            Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition, sourceRectangle, new Color(255, 255, 255, 255), Projectile.rotation, origin, 1f, 0, 0f);
            return false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }
        public void 水中推动(Player player)
        {
            if (player.wet)
            {
                player.velocity = (player.velocity - towards * 30) / 15f;
                Vector2 v = Vector2.Normalize(player.velocity);
                player.velocity = v * 17.5f;
                //Main.NewText(player.velocity);
            }
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];//读取主人位置
            var p = player.GetModPlayer<魔力飞弹modplayer>();
            Lighting.AddLight(Projectile.Center, 2.5f, 1.1f, 2.1f);
            //动画
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 3 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 12)
            {
                Projectile.frame = 10;
            }
            if (Main.myPlayer == player.whoAmI)
            {
                if (towards != Main.MouseWorld - player.Center)
                {
                    towards = Main.MouseWorld - player.Center;
                    towards.Normalize();
                    Projectile.netUpdate = true;

                }

                if (++发射速率 >= 3 && Projectile.frame >= 10)
                {
                    p.screenPosition = Vector2.One;
                    发射速率 = 0;
                    Vector2 velocity = towards * 30f + new Vector2(Main.rand.NextFloatDirection() * 15f, Main.rand.NextFloatDirection() * 15f);
                    velocity = Vector2.Normalize(velocity) * 25;
                    if (!player.CheckMana(player.HeldItem, (int)(25 * player.manaCost), true))
                        Projectile.Kill();
                    else
                    {
                        player.manaRegenDelay = 60;
                        SoundEngine.PlaySound(生成, Projectile.Center);
                        float 伤害系数 = player.GetTotalDamage(DamageClass.Magic).Additive;
                        var expmodplayer = player.GetModPlayer<EXPmodplayer>();

                        Projectile proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<ElainaModSkills.Skills.MagicMissile.MagicMissile>(), (int)(expmodplayer.GetMagicMissleDamage() * 伤害系数 * 1.5f), (float)伊蕾娜.HitType.HitByMagicMissile, player.whoAmI, -1, 2);
                        proj.rotation = velocity.ToRotation() + MathHelper.ToRadians(-90f);
                    }


                }
            }
            if (Main.myPlayer == Projectile.owner)
            {
                if (!Main.mouseLeft)
                {
                    Projectile.Kill();
                    p.screenPosition = Vector2.Zero;

                }
            }
            {
                player.itemTime = 2;
                player.itemAnimation = 2;
                //player.direction = towards.X < 0 ? -1 : 1;//玩家朝向根据鼠标
                水中推动(player);
                Projectile.Center = towards * 60 + player.Center;
                Projectile.rotation = towards.ToRotation();
                Projectile.timeLeft = 2;
                //player.itemRotation = (float)Math.Atan2(Projectile.rotation.ToRotationVector2().Y * player.direction, Projectile.rotation.ToRotationVector2().X * player.direction);//武器朝向
                player.heldProj = Projectile.whoAmI;
            }


        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.immune[Projectile.owner] = 3;

            base.OnHitNPC(target, hit, damageDone);
        }

    }
}
