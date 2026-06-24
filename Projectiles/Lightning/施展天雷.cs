using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;

namespace 伊蕾娜.Projectiles.Lightning
{
    public class 施展天雷 : ModProjectile
    {
        public Asset<Texture2D> texture;
        public Asset<Texture2D> texture2;
        public Asset<Texture2D> texture3;

        int time = 0;
        public override void Load()
        {

            base.Load();
        }
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            if (texture == null) texture = Mod.Assets.Request<Texture2D>("Projectiles/渐变");
            if (texture2 == null) texture2 = Mod.Assets.Request<Texture2D>("Projectiles/光线2");
            if (texture3 == null) texture3 = Mod.Assets.Request<Texture2D>("Projectiles/Lightning/雷粒子");
            Projectile.timeLeft = 30;
            Projectile.width = 256;
            Projectile.width = 256;
            Projectile.tileCollide = false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            var player = Main.player[Projectile.owner];
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);

            Vector2 scale = new(0.012f, 0.1f);
            Vector2 origin = texture.Size() * 0.5f;//除3相当于以图片中心为position.除以X,Y,此时为从上往下，从左往右的帧图
            Rectangle sourceRectangle = new(0, 0, 256, 256);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
            float 渐变 = MathHelper.Lerp(1, 0.1f, (float)(Math.Pow(Projectile.timeLeft, 2) / Math.Pow(30f, 2)));
            scale *= 渐变;
            Vector2 toward = new(0, MathHelper.Lerp(-300, -5f, (float)(Math.Pow(Projectile.timeLeft, 2) / Math.Pow(30f, 2))));
            toward = toward.RotatedBy(Projectile.rotation);
            for (int k = 0; k < 4; k++)
            {
                for (int i = 0; i < 100; i++)
                {
                    Vector2 eachtoward = toward.RotatedBy(0.012 * i + k * Math.PI / 2);
                    float eachrotation = (float)(Projectile.rotation + 0.012 * i + k * Math.PI / 2);
                    Main.spriteBatch.Draw(texture.Value, player.itemLocation + eachtoward - Main.screenPosition, sourceRectangle,
                    new Color(166, 235, 255, 255), eachrotation, origin, scale, SpriteEffects.None, 0f);
                }
            }
            scale = new Vector2(0.05f, 0.05f);
            scale *= 渐变;
            origin = texture2.Size() * 0.5f;//除3相当于以图片中心为position.除以X,Y,此时为从上往下，从左往右的帧图
            sourceRectangle = new Rectangle(0, 0, 500, 514);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
            toward *= MathHelper.Lerp(3, 1f, (float)(Math.Pow(Projectile.timeLeft, 2) / Math.Pow(30f, 2)));
            for (int k = 0; k < 4; k++)
            {
                for (int i = 0; i < 20; i++)
                {
                    Vector2 eachtoward = toward.RotatedBy(k * Math.PI / 2 - 0.2);
                    float eachrotation = (float)(Projectile.rotation + k * Math.PI / 2 - 0.2);
                    eachrotation += (float)Math.PI / 2;
                    Vector2 move = eachtoward;
                    move.Normalize();
                    Main.spriteBatch.Draw(texture2.Value, player.itemLocation + eachtoward / 5 + move * i * 10 - Main.screenPosition, sourceRectangle,
                    new Color(166, 235, 255, 255), eachrotation, origin, scale * 5, SpriteEffects.None, 0f);
                }
            }
            scale = new Vector2(5f, 5f);
            scale /= 渐变;
            Color c = new(166, 235, 255, 70);
            //c *= 1/渐变;
            origin = texture3.Size() * 0.5f;//除3相当于以图片中心为position.除以X,Y,此时为从上往下，从左往右的帧图
            origin.Y /= 6;
            int startY = 200 * Projectile.frame;//每一帧的起始坐标Y
            sourceRectangle = new Rectangle(0, startY, 200, 200);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            Main.spriteBatch.Draw(texture3.Value, player.itemLocation - Main.screenPosition, sourceRectangle,
                    Color.White, 0, origin, 3, SpriteEffects.None, 0f);
            //Main.spriteBatch.Draw(texture2.Value, Projectile.Center + toward  - Main.screenPosition, sourceRectangle,
            //new Color(166, 235, 255, 255), Projectile.rotation, origin, 1, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public override void OnKill(int timeLeft)
        {
        }
        public override void AI()
        {

            //ModContent.GetInstance<天黑控制>().startDark = 0;
            //if(Main.myPlayer == Projectile.owner)
            {
                if (Projectile.ai[0] == 0 && !Main.player[Main.myPlayer].GetModPlayer<天雷modplayer>().雷云 && Main.player[Main.myPlayer].GetModPlayer<天雷modplayer>().疲劳值 <= 0)
                {
                    Main.player[Main.myPlayer].GetModPlayer<天雷modplayer>().雷云 = true;
                    ModContent.GetInstance<天黑控制>().startDark = 300;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.NewProjectileDirect(null, Projectile.position, Vector2.Zero, ModContent.ProjectileType<雷云>(), 0, 1, Projectile.owner, Main.player[Main.myPlayer].HasBuff<Buffs.雷引buff>()?1:0);

                    }
                    Projectile.ai[0] = 2;

                }
                else if (Projectile.ai[0] == 1)
                {
                    Main.player[Main.myPlayer].GetModPlayer<天雷modplayer>().雷云 = false;
                    ModContent.GetInstance<天黑控制>().startDark = 0;
                    ModContent.GetInstance<天黑控制>().endDark = 300;
                    Projectile.ai[0] = 2;
                }
            }

            //Projectile.NewProjectileDirect(null, Projectile.position, Vector2.Zero, ModContent.ProjectileType<雷云>(), 0, 1, Projectile.owner);

            Main.player[Projectile.owner].GetModPlayer<天雷modplayer>().雷击距离 = new Vector3(600, 600, 20);
            Main.player[Projectile.owner].itemTime = 30;
            Main.player[Projectile.owner].itemAnimation = 30;
            time++;
            Projectile.rotation += MathHelper.Lerp(0.02f, 0.2f, (float)(Math.Pow(Projectile.timeLeft, 2) / Math.Pow(30f, 2)));
            float 渐变 = MathHelper.Lerp(1000, 0, (float)(Math.Pow(Projectile.timeLeft, 2) / Math.Pow(30f, 2)));
            Lighting.AddLight(Projectile.Center, 渐变, 渐变, 渐变);

            for (int i = 0; i < 30; i++)
            {
                Vector2 v = Main.rand.NextVector2Unit(0);
                v *= 5;
                //Dust d = Dust.NewDustDirect(Projectile.Center, 0, 0, 16,
                //0, -60, 0, Color.White,3f);
                //d.noGravity = true;
                //d.velocity = v;

            }
        }
    }
}
