using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.GameContent;

namespace 伊蕾娜.Projectiles.Healing
{
    public class 旋转 : ModProjectile
    {
        Asset<Texture2D> texture;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            Main.projFrames[Projectile.type] = 30;
            Projectile.ignoreWater = true;//无视水
            Projectile.friendly = false;//可以攻击敌人
            Projectile.ownerHitCheck = false;
            Projectile.penetrate = -1; // 穿透数量
            Projectile.tileCollide = false;//瓷砖碰撞
                                           //projectile.timeLeft=500;
            Projectile.width = 65;
            Projectile.height = 110;
            Projectile.hide = true;

        }

        public override bool PreDraw(ref Color lightColor)
        {
            texture = TextureAssets.Projectile[Projectile.type];

            Effect DrawEffect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/Draw", AssetRequestMode.ImmediateLoad).Value;

            Player player = Main.player[Projectile.owner];
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, DrawEffect, Main.Transform);
            Vector2 origin = texture.Size() * 0.5f;//除3相当于以图片中心为position
            origin.Y /= 30;//动图还要除以帧数
            int frameHeight = texture.Height() / Main.projFrames[Projectile.type];//图片总高度除以帧数，得到每张图的高度
            int startY = frameHeight * Projectile.frame;//每一帧的起始坐标Y
            Rectangle sourceRectangle = new(0, startY, texture.Width(), frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片

            SpriteEffects spriteEffects = SpriteEffects.None;//调整图片方向，当弹幕方向不是1时水平翻转图片
            Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition, sourceRectangle, new Color(255, 255, 255, 255), Projectile.rotation, origin, 1.5f, spriteEffects, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            player.heldProj = Projectile.whoAmI;

            var p = Main.player[(int)Projectile.ai[0]];
            Projectile.Center = p.MountedCenter + new Vector2(0f, -80f);
            if (Main.GameUpdateCount % 50 == 0)
            {
                if (!player.CheckMana(player.HeldItem, (int)(10*player.manaCost), false)) Projectile.timeLeft = 30;
                else
                {
                    Projectile.timeLeft = 60;
                }
            }
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 4 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;
            }
            if (player.channel)
            {
                //Projectile.timeLeft = 60;
                if (Projectile.frame == 13)
                {
                    Projectile.frame = 5;
                }
            }
            if (Projectile.frame >= 30 | !p.active | p.dead)
            {
                Projectile.Kill();
            }

        }
    }
}
