using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace 伊蕾娜.Projectiles.Lightning
{
    public class 雷粒子 : ModProjectile
    {
        Asset<Texture2D> texture3;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            if (texture3 == null) texture3 = Mod.Assets.Request<Texture2D>("Projectiles/Lightning/雷粒子");
            Projectile.timeLeft = 300;
            base.SetDefaults();
        }
        public override bool PreDraw(ref Color lightColor)
        {

            Vector2 origin = texture3.Size() * 0.5f;//除3相当于以图片中心为position.除以X,Y,此时为从上往下，从左往右的帧图
            origin.Y /= 6;
            int startY = 200 * Projectile.frame;//每一帧的起始坐标Y
            Rectangle sourceRectangle = new(0, startY, 200, 200);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            Main.spriteBatch.Draw(texture3.Value, Projectile.Center - Main.screenPosition, sourceRectangle,
                    Color.White, 0, origin, 1, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public override void AI()
        {

            //Main.player[Projectile.owner].itemTime = 2;
            //Main.player[Projectile.owner].itemAnimation = 2;
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 6 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 6)
            {
                Projectile.Kill();
                Projectile.frame = 0;
            }
            base.AI();
        }
    }
}
