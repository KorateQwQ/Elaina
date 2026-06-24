using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;

namespace 伊蕾娜.Projectiles.Flame
{
    public class 地面燃烧proj : ModProjectile
    {
        Asset<Texture2D> texture;
        int life = 0;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {   
            if(texture == null)texture = Mod.Assets.Request<Texture2D>("Projectiles/光线2");
            Projectile.penetrate = -1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;//瓷砖碰撞
            Projectile.friendly = true;
            //projectile.timeLeft=30;
            //projectile.extraUpdates=1;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.damage = 20;
            Projectile.timeLeft = 300;
            Projectile.knockBack = 2;
            //Projectile.CritChance = (int)Main.player[Projectile.owner].GetTotalCritChance(DamageClass.Magic);

            //Projectile.extraUpdates = ;
            Projectile.alpha = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;

        }
        public override bool PreDraw(ref Color lightColor)
        {
            Asset<Texture2D> texture2 = Mod.Assets.Request<Texture2D>("Projectiles/Flame/地面燃烧proj");
            float 渐变 = 60 - (float)Main.GameUpdateCount % 120;//60到-60
            float 渐变2 = 1;
            if (Projectile.timeLeft < 60) 渐变2 = MathHelper.Lerp(0, 1, Projectile.timeLeft / 60f);
            
            Vector2 origin = texture.Size() * 0.5f;//除3相当于以图片中心为position.除以X,Y,此时为从上往下，从左往右的帧图
            Vector2 scale = new(0.1f, 1f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            Color c = new Color(252, 134, 60)* 渐变2;
            c.A = (byte)MathHelper.Lerp(150, 255, (float)Math.Abs(渐变) / 60f);
            //   Main.NewText(Projectile.timeLeft);
            Main.spriteBatch.Draw(texture.Value, Projectile.position - Main.screenPosition, null, c, 0, origin, scale, SpriteEffects.None, 0f);
            Vector2 scale2 = new(1f, 1.3f);
            if (Projectile.frameCounter++ > 3)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = Main.rand.Next(32);
            }
            Rectangle rectangle = new(Projectile.frame, 20 * Projectile.frame, 2, 20);
            for (int i = 0; i < 32; i++)
            {
                int startx = (Projectile.frame + i) % 32;
                rectangle = new Rectangle(startx, 20 * Projectile.frame, 2, 20);
                Main.spriteBatch.Draw(texture2.Value, Projectile.position + new Vector2(i, 32) - Main.screenPosition, rectangle, c, 0, texture2.Size() * 0.5f, scale2, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texture2.Value, Projectile.position + new Vector2(i, 42) - Main.screenPosition, rectangle, c * 0.5f, 0, texture2.Size() * 0.5f, scale2 * 1.3f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texture2.Value, Projectile.position + new Vector2(i, 22) - Main.screenPosition, rectangle, c * 0.5f, 0, texture2.Size() * 0.5f, scale2 * 0.7f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texture2.Value, Projectile.position + new Vector2(i, 32) - Main.screenPosition, rectangle, c, 0, texture2.Size() * 0.5f, scale2, SpriteEffects.FlipHorizontally, 0f);
                Main.spriteBatch.Draw(texture2.Value, Projectile.position + new Vector2(i, 42) - Main.screenPosition, rectangle, c * 0.5f, 0, texture2.Size() * 0.5f, scale2 * 1.3f, SpriteEffects.FlipHorizontally, 0f);

            }


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public override void AI()
        {
            life++;
            if (Projectile.wet) Projectile.Kill();
            if (Main.raining&&Main.myPlayer==Projectile.owner)
            {
                foreach (var rain in Main.rain)
                {
                    if (rain.active)
                    {
                        if ((rain.position - Projectile.position).Length() < 25)
                        {   
                            if(Projectile.timeLeft>50) Projectile.timeLeft -= 10;
                        }
                    }
                }
            }
            base.AI();
        }
        public override void OnKill(int timeLeft)
        {
            if (life >= 250)
            {
                Tile tile = Main.tile[(int)(Projectile.position.X / 16), (int)(Projectile.position.Y / 16)];
                if(TileID.Sets.IsATreeTrunk[tile.TileType] | tile.TileType == 323)
                {
                    WorldGen.KillTile((int)(Projectile.position.X / 16), (int)(Projectile.position.Y / 16), false, false, false);
                    if (!tile.HasTile && Main.netMode != NetmodeID.SinglePlayer)
                    {
                        NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, (int)(Projectile.position.X / 16), (int)(Projectile.position.Y / 16), 0f, 0, 0, 0);
                    }
                }
            }
            base.OnKill(timeLeft);
        }

    }
}
