using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent;

namespace 伊蕾娜.Projectiles.Lightning
{
    public class 击地粒子 : ModProjectile
    {
        Asset<Texture2D> texture;
        public override void Load()
        {
            base.Load();
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 10;
        }
        public override void SetDefaults()
        {
            if (texture == null) texture = TextureAssets.Projectile[Projectile.type];

            Projectile.friendly = false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            var player = Main.player[Projectile.owner];
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);

            //Texture2D texture = mod.GetTexture("C"+(int)projectile.ai[0]);
            Vector2 origin = texture.Size() * 0.5f;//除3相当于以图片中心为position.除以X,Y,此时为从上往下，从左往右的帧图
            origin.Y /= 10;
            Rectangle sourceRectangle = new(0, texture.Height() / Main.projFrames[Projectile.type] * Projectile.frame, texture.Width(), texture.Height() / Main.projFrames[Projectile.type]);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
            SpriteEffects spriteEffects = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;//调整图片方向，当弹幕方向不是1时水平翻转图片	
            float 大小 = MathHelper.Lerp(1f, 2.5f, Projectile.ai[1] / 5);
            Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition, sourceRectangle, new Color(255, 255, 255, 255), 0, origin, 大小, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 4 == 0)
            {
                Projectile.frame += 1;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 10)
            {
                Projectile.Kill();
                Projectile.frame = 0;
            }
        }
    }
}
