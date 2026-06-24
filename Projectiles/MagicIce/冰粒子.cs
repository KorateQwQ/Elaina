using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent;

namespace 伊蕾娜.Projectiles.MagicIce
{


    public class 冰粒子 : ModProjectile
    {
        Asset<Texture2D> texture;

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.extraUpdates = 5;
            Projectile.timeLeft = 6000;
            Main.projFrames[Projectile.type] = 36;
            base.SetDefaults();
        }
        public void 冻结矩形区域(int x, int y)
        {
            for (int i = x - 1; i <= x + 1; i++)
            {
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (i > 0 && j > 0)
                    {
                        if (!Main.tile[i, j].HasTile && Main.tile[i, j].LiquidType == 0 && Main.tile[i, j].LiquidAmount > 0)
                        {
                            //Main.NewText(Main.tile[x, y].LiquidAmount);
                            Vector2 position = new(i * 16, j * 16);
                            Vector2 v = Vector2.Normalize(Projectile.Center - position) * 15;
                            //if (Main.tile[i, j].LiquidAmount > 60)
                            {
                                //Main.tile[i, j].TileType = 161;
                                WorldGen.PlaceTile(i, j, 161);
                                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, i, j, 0f, 0, 0, 0);
                                for (int k = 0; k < 30; k++)
                                {
                                    Dust d = Dust.NewDustDirect(position, 16, 16, DustID.IceTorch, 0, -10, 255, Color.White, 2);
                                }

                                if (Main.myPlayer == Projectile.owner)
                                {
                                    if (Main.netMode == 1)
                                        NetMessage.sendWater(i, j);
                                    else Liquid.AddWater(i, j);
                                }
                            }
                        }
                    }
                }
            }
        }
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 15 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 36)
            {
                Projectile.Kill();
                Projectile.frame = 0;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            for (int k = 0; k < 3; k++)
            {
                //Dust d = Dust.NewDustDirect(Projectile.Center, 30, 30, DustID.IceTorch, 0, -10, 255, Color.White, 1);
                //Dust d = Dust.NewDustDirect(Projectile.Center, 30, 30, 43, 0, 0, 0, Color.White, 1);
                //d.noGravity = true;
            }
            base.AI();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (texture == null) texture = TextureAssets.Projectile[Projectile.type];
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            Vector2 origin = texture.Size() / new Vector2(6f, 6f) * 0.5f;//除3相当于以图片中心为position.除以X,Y,此时为从上往下，从左往右的帧图
            int frameWidth = texture.Width() / 6;//图片总高度除以长度，得到每张图的长度
            int frameHeight = texture.Height() / 6;//图片总高度除以高度，得到每张图的高度
            int startX = frameWidth * (Projectile.frame % 6);//每一帧的起始坐标X
            int startY = frameHeight * (Projectile.frame / 6);//每一帧的起始坐标Y
            Rectangle sourceRectangle = new(startX, startY, frameWidth, frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片

            SpriteEffects spriteEffects = SpriteEffects.None;//调整图片方向，当弹幕方向不是1时水平翻转图片
            Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition, sourceRectangle, new Color(54, 253, 253, 255), Projectile.rotation, origin, 3f, spriteEffects, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

    }
}
