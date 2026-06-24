using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using 伊蕾娜.Items;
using 伊蕾娜.ReProjs;
using 伊蕾娜.炼金;

namespace 伊蕾娜
{

    public partial class ElainaModplayer
    {
        public class Light : ModProjectile
        {
            Asset<Texture2D> texture;
            Vector2 lightPosition;
            public override void SetDefaults()
            {
                Main.projFrames[Projectile.type] = 4;
                Projectile.damage = 0;
                Projectile.tileCollide = false;
                Projectile.timeLeft = 300;
                //Projectile.hide = true;
            }
            public override bool PreDraw(ref Color lightColor)
            {
                var p = Main.player[Projectile.owner];
                var player = p;
                float itemLocation = p.Center.Y - 10f;
                if (p.mount.Active)
                    itemLocation -= p.mount.HeightBoost / 2;
                //if (itemLocation == p.itemLocation.Y)
                if (p.itemAnimation == 0)
                {
                    //if (p.itemAnimation == 0)
                    {
                        if (player.mount.Active)
                            itemLocation -= player.mount.HeightBoost / 2;

                        //Projectile.Center =  new Vector2(player.MountedCenter.X - 10 + player.gravDir * player.direction * 15, itemLocation + player.gfxOffY);
                        Projectile.rotation = -MathF.PI / 2f + player.direction * 0.7f + player.fullRotation;
                        player.bodyFrame.Y = 56 * 2;
                        Main.player[Projectile.owner].itemLocation = Projectile.Center;
                        player.itemRotation = Projectile.rotation;
                        float rot = Projectile.rotation;

                        int eff = Math.Abs(rot) < MathF.PI / 2f ? 2 : 0;
                        /*Main.spriteBatch.Draw(TextureAssets.Projectile[ModContent.ProjectileType<Wand>()].Value, (player.Center.Floor() + new Vector2(0, p.gfxOffY-8f).Floor()).Floor() - Main.screenPosition,
                            null, lightColor, rot, new Vector2(0, 0), 1f, (SpriteEffects)eff, 0);*/
                    }

                    if (texture == null)
                        texture = Mod.Assets.Request<Texture2D>("Light");
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
                    Vector2 position = p.itemLocation - Main.screenPosition;
                    Vector2 move = new Vector2(24, -35 ).Floor();
                    if (p.direction < 0)
                        move = new Vector2(-7, -35).Floor();


                    position = position + move;
                    Vector2 move2 = p.itemLocation + move - p.Center;
                    //position.RotatedBy(p.fullRotation,p.Center);
                    //move = position - p.Center;
                    move2 = move2.RotatedBy(p.fullRotation);
                    Vector2 origin = texture.Size() * 0.5f;
                    origin.Y /= 4;
                    float rotation = Main.rand.NextFloat(-0.7f, 0.7f);
                    //if (p.mount.Active) Main.NewText(p.fullRotation+" "+ move);
                    int frameHeight = texture.Height() / Main.projFrames[Projectile.type];//图片总高度除以帧数，得到每张图的高度
                    int startY = frameHeight * Projectile.frame;//每一帧的起始坐标Y
                    Rectangle sourceRectangle = new(0, startY, texture.Width(), frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
                    position = new Vector2((int)position.X, (int)position.Y);

                    Main.EntitySpriteDraw(texture.Value,
                    p.Center.Floor() + move2.Floor() - Main.screenPosition,
                    sourceRectangle,
                    Color.White,
                    0,
                    origin,
                    1f, // Scale.
                    SpriteEffects.None, // SpriteEffects.
                    0 // 'Layer'. This is always 0 in Terraria.
                    );

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                        DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                }

                return false;
            }
            public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
            {
                overWiresUI.Add(index);
            }
            public override void AI()
            {
                var p = Main.player[Projectile.owner].GetModPlayer<ElainaModplayer>();
                var player = Main.player[Projectile.owner];
                float itemLocation = player.MountedCenter.Y - 10f;
                //player.heldProj = Projectile.whoAmI;
                if (player.itemAnimation == 0)
                {
                    if (player.mount.Active)
                        itemLocation -= player.mount.HeightBoost / 2;
                    if (player.itemLocation.Y == itemLocation)
                    {
                       // Main.NewText(6);
                    }
                    else
                    {
                        //Main.NewText(player.itemLocation + " " + itemLocation);
                    }
                    Lighting.AddLight(Projectile.Center, 5, 5, 5);

                    Projectile.Center = new Vector2(player.MountedCenter.X - 10 + player.gravDir * player.direction * 15, itemLocation + player.gfxOffY);
                    Projectile.rotation = -MathF.PI / 2f + player.direction * 0.5f + player.fullRotation;
                    player.bodyFrame.Y = 56 * 2;
                    Main.player[Projectile.owner].itemLocation = Projectile.Center;
                    player.itemRotation = Projectile.rotation;
                }




                p.onLight = true;
                //Main.NewText(Projectile.timeLeft+"678");
                var p2 = player.GetModPlayer<炼金modplayer>();
                if (p2.搅拌中 || p2.炼制中 || p.isACritter || !IsWand(player.HeldItem.type) | !player.active | player.dead)
                {   //p.selectedItem指的是123456快捷键+" "+p.HeldItem指的是具体item且为手上的
                    //进入炼制或者切换武器时关闭灯光
                    Projectile.Kill();
                }
                if (Main.myPlayer == Main.player[Projectile.owner].whoAmI)
                {
                    //再次中键关闭灯光
                    if (Main.mouseMiddle && Main.mouseMiddleRelease && Projectile.timeLeft < 290)
                        Projectile.Kill();
                }
                if (Projectile.timeLeft < 150)
                    Projectile.timeLeft++;
                Projectile.frameCounter++;
                if (Projectile.frameCounter % 6 == 0)
                {
                    Projectile.frame += 1;
                    Projectile.frameCounter = 0;
                }
                if (Projectile.frame >= 4)
                {
                    Projectile.frame = 0;
                }
                if (Projectile.ai[0]++ > 300)
                {
                    Projectile.ai[0] = 0f;
                    if (!player.CheckMana(player.HeldItem, (int)(10 * player.manaCost), true))
                        Projectile.Kill();
                }

                player.manaRegenDelay = 2;

            }
            public override void OnKill(int timeLeft)
            {
                var p = Main.player[Projectile.owner].GetModPlayer<ElainaModplayer>();
                p.onLight = false;
            }

        }

    }
}
