using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace 伊蕾娜.Projectiles.Healing
{
    public class 治疗施法 : ModProjectile
    {
        Asset<Texture2D> texture;
        Vector2 towards;
        Projectile proj;
        Projectile proj2 = null;
        int i = 0;

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

            Projectile.friendly = false;
            Main.projFrames[Projectile.type] = 15;
            Projectile.ignoreWater = true; //无视水
            Projectile.tileCollide = false; //瓷砖碰撞
            Projectile.width = 92;
            Projectile.height = 50;
            Projectile.hide = true;

        }

        public override bool PreDraw(ref Color lightColor)
        {
            texture = TextureAssets.Projectile[Projectile.type];

            Player player = Main.player[Projectile.owner];
            //if (player.GetModPlayer<Cmodplayer>().chooseItem >= 0|| player.GetModPlayer<Cmodplayer>().controlWeapon) return false;

            Vector2 origin = texture.Size() * 0.5f; //除3相当于以图片中心为position
            origin.Y /= 15; //动图还要除以帧数
            int frameHeight = texture.Height() / Main.projFrames[Projectile.type]; //图片总高度除以帧数，得到每张图的高度
            int startY = frameHeight * Projectile.frame; //每一帧的起始坐标Y
            Rectangle sourceRectangle = new(0, startY, texture.Width(), frameHeight); //坐标x，坐标y，图片长度，图片高度，得到一张完整图片
            SpriteEffects spriteEffects = SpriteEffects.None; //调整图片方向，当弹幕方向不是1时水平翻转图片
            //spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.Transform);
            //Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition, sourceRectangle, new Color(255, 255, 255, 255), base.Projectile.rotation, origin, 1f, spriteEffects, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null,
                Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }

        public override void AI()
        {
            i++;
            //Main.NewText(NPC.downedMoonlord);
            Player player = Main.player[Projectile.owner];
            player.heldProj = Projectile.whoAmI;

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 4 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;
            }

            if (Projectile.frame == 11)
                Projectile.frame = 5;
            //player.MountedCenter
            //Projectile.Center = player.Center + new Vector2(-6, -8) + (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
            //if (player.direction < 0) Projectile.Center = player.Center + new Vector2(2, -8) - (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
            if (player.channel)
            {
                if (Main.GameUpdateCount % 30 == 0)
                {
                    if (!player.CheckMana(1, true))
                        Projectile.timeLeft = 15;
                    else
                    {
                        Projectile.timeLeft = 60;
                    }
                }

                if (Main.myPlayer == player.whoAmI)
                {
                    towards = Main.MouseWorld - player.Center;


                    Projectile.Center = player.MountedCenter +
                                        (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
                    if (player.direction < 0)
                        Projectile.Center = player.MountedCenter -
                                            (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
                    Projectile.direction = player.direction;
                    player.itemTime = 60;
                    player.itemAnimation = 60;
                    player.direction = towards.X < 0 ? -1 : 1; //玩家朝向根据鼠标
                    Projectile.rotation = Vector2.Normalize(towards).ToRotation();
                    player.itemRotation =
                        (float)Math.Atan2(Projectile.rotation.ToRotationVector2().Y * player.direction,
                            Projectile.rotation.ToRotationVector2().X * player.direction); //武器朝向
                    if (Projectile.frame == 4 && Projectile.frameCounter == 1 && Main.myPlayer == Projectile.owner)
                    {
                        foreach (Player p in Main.player)
                        {
                            if (p.active && !p.dead && p.Distance(player.position) < 1000)
                            {
                                proj = Projectile.NewProjectileDirect(null, p.Center + new Vector2(0f, -20f),
                                    Vector2.Zero, ModContent.ProjectileType<旋转>(), 0, Projectile.whoAmI,
                                    Projectile.owner, p.whoAmI);
                                proj2 = Projectile.NewProjectileDirect(null, p.Center + new Vector2(0f, -20f),
                                    Vector2.Zero, ModContent.ProjectileType<治疗彩虹>(), 0, Projectile.whoAmI,
                                    Projectile.owner, p.whoAmI);
                                proj2.tileCollide = false;
                            }
                        }
                    }
                    else if (proj2 != null && !proj2.active && i > 120)
                    {
                        proj.Kill();
                        Projectile.Kill();
                    }
                }
            }
        }
    }
}
