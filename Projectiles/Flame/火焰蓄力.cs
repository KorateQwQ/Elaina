using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using System;

namespace 伊蕾娜.Projectiles.Flame
{
    public class 火焰蓄力 : ModProjectile
    {
        Asset<Texture2D> texture;
        Vector2 towards = Vector2.Zero;
        public override void Load()
        {
            base.Load();
        }
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
            Main.projFrames[Projectile.type] = 16;
            // DisplayName.SetDefault("火焰蓄力");
        }
        public override void SetDefaults()
        {
            if (texture == null) texture = Mod.Assets.Request<Texture2D>("Projectiles/Flame/火焰蓄力");

            Projectile.penetrate = -1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;//瓷砖碰撞
            Projectile.friendly = true;
            //projectile.timeLeft=30;
            //projectile.extraUpdates=1;
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.damage = 20;
            Projectile.timeLeft = 40;
            Projectile.knockBack = 2;
            //Projectile.extraUpdates = ;
            Projectile.alpha = 0;

        }
        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 origin = texture.Size() * 0.5f;//除3相当于以图片中心为position
            origin.Y /= 16;//动图还要除以帧数
            int frameHeight = texture.Height() / Main.projFrames[Projectile.type];//图片总高度除以帧数，得到每张图的高度
            int startY = frameHeight * Projectile.frame;//每一帧的起始坐标Y
            Rectangle sourceRectangle = new(0, startY, texture.Width(), frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
            Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition, sourceRectangle, new Color(255, 255, 255, 255), Projectile.rotation, origin, 1f, 0, 0f);
            return false;
        }
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 3 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;
                Projectile.netUpdate = true;
            }
            if (Projectile.frame > 16)
            {
                Projectile.Kill();
            }
            Player player = Main.player[Projectile.owner];
            if (Main.myPlayer == player.whoAmI)
            {
                towards = Main.MouseWorld - player.Center;
                towards.Normalize();
            }
            if (Main.player[Projectile.owner].channel)
            {
                player.itemTime = 2;
                player.itemAnimation = 2;
                player.direction = towards.X < 0 ? -1 : 1;//玩家朝向根据鼠标
                Projectile.Center = towards * 60 + player.Center;
                Projectile.rotation = towards.ToRotation();
                Projectile.timeLeft = 40;
                player.itemRotation = (float)Math.Atan2(Projectile.rotation.ToRotationVector2().Y * player.direction, Projectile.rotation.ToRotationVector2().X * player.direction);//武器朝向

            }
            else Projectile.Kill();
        }

    }

}
