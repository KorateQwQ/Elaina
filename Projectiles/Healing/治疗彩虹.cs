using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.GameContent;

namespace 伊蕾娜.Projectiles.Healing
{
    public class 治疗彩虹 : ModProjectile
    {
        static Asset<Texture2D> texture;
        static Effect DrawEffect;
        public override void SetStaticDefaults()
        {
            DrawEffect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/Draw", AssetRequestMode.ImmediateLoad).Value;

            Main.projFrames[Projectile.type] = 25;
            Projectile.ignoreWater = true;//无视水
            Projectile.friendly = false;//可以攻击敌人
            Projectile.ownerHitCheck = false;
            Projectile.penetrate = -1; // 穿透数量
            Projectile.tileCollide = false;//瓷砖碰撞
            Projectile.width = 65;
            Projectile.height = 110;
            Projectile.hide = true;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            texture = TextureAssets.Projectile[Projectile.type];

            Player player = Main.player[Projectile.owner];
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, DrawEffect, Main.Transform);
            Vector2 origin = texture.Size() * 0.5f;//除3相当于以图片中心为position
            origin.Y /= 25;//动图还要除以帧数
            int frameHeight = texture.Height() / Main.projFrames[Projectile.type];//图片总高度除以帧数，得到每张图的高度
            int startY = frameHeight * Projectile.frame;//每一帧的起始坐标Y
            Rectangle sourceRectangle = new(0, startY, texture.Width(), frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片

            SpriteEffects spriteEffects = SpriteEffects.None;//调整图片方向，当弹幕方向不是1时水平翻转图片
            //spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.Transform);
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
        public void CleanDebuff(Player player)
        {
            for (int i = 0; i < player.CountBuffs(); i++)
            {
                if (Main.debuff[player.buffType[i]])
                {
                    player.DelBuff(i);
                }
            }
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            var p = Main.player[(int)Projectile.ai[0]];
            Projectile.Center = p.MountedCenter + new Vector2(0f, -20f);
            Projectile.frameCounter++;
            player.heldProj = Projectile.whoAmI;
            Dust d = Dust.NewDustDirect(player.Center + new Vector2(-20f, -80f), 40, 40, DustID.TintableDustLighted,
            0, 10, 0, Color.White, 1.2f);
            if (Main.GameUpdateCount % 30 == 0)
            {
                if (!player.CheckMana(player.HeldItem,(int)(10*player.manaCost), true)) Projectile.timeLeft = 30;
                else
                {
                    player.manaRegenDelay = 60;
                    Projectile.timeLeft = 60;
                    p.HealEffect(20);//回血数字
                    if (p.whoAmI == Main.myPlayer)
                    {
                        if (p.active && !p.dead)
                        {
                            p.statLife += 20;
                            CleanDebuff(p);
                        }
                        else
                        {
                            NetMessage.SendData(MessageID.PlayerLifeMana, -1, p.whoAmI, null, p.whoAmI);
                            Projectile.Kill();
                        }
                        NetMessage.SendData(MessageID.PlayerLifeMana, -1, p.whoAmI, null, p.whoAmI);
                    }
                }
            }
            if (Projectile.frameCounter % 4 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;

            }
            if (player.channel)
            {
                if (Projectile.frame == 18)
                {
                    Projectile.frame = 10;
                }
            }
            if (Projectile.frame >= 25 | !p.active | p.dead)
            {
                Projectile.Kill();
            }
        }
    }
}
