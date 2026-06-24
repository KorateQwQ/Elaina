using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.Projectiles.Magicwater;

namespace 伊蕾娜.Dusts
{
    public class 水花粒子 : ModDust
    {
        static Asset<Texture2D> texture;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }
        public override void OnSpawn(Dust dust)
        {
            dust.fadeIn = -1;
            base.OnSpawn(dust);
        }
        public override Color? GetAlpha(Dust dust, Color lightColor)
        {
            if (texture == null)
            {
                texture = Mod.Assets.Request<Texture2D>("Dusts/水花粒子");
            }
            if (dust.fadeIn is >= 0 and <= 1000)
            {
                //Main.NewText(Main.projectile[(int)dust.fadeIn].type+" "+ModContent.ProjectileType<WaterMagicBall>());
                if (Main.projectile[(int)dust.fadeIn].type == ModContent.ProjectileType<WaterMagicBall>())
                {
                    Vector2 len = (Main.projectile[(int)dust.fadeIn].Center - dust.position);
                    if (len.Length() < 100) dust.active = false;
                    else
                    {
                        for (int i = 0; i < 1; i++)
                        {
                            Dust d = Dust.NewDustDirect(dust.position, 5, 5, ModContent.DustType<水花粒子>(),
                            0, 0, 255, Color.White, 2);
                        }
                    }

                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            RasterizerState originalState = Main.graphics.GraphicsDevice.RasterizerState;
            Main.spriteBatch.Draw(texture.Value, dust.position - Main.screenPosition, new Rectangle(0, 0, 50, 50), Color.White, 0, new Vector2(25, 25), 0.5f * dust.scale, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            return base.GetAlpha(dust, lightColor);
        }
        //public override string Texture => null;
        public override bool Update(Dust dust)
        {   
            if(dust.fadeIn is >=0 and <= 1000)
            {
                //Main.NewText(Main.projectile[(int)dust.fadeIn].type+" "+ModContent.ProjectileType<WaterMagicBall>());
                if (Main.projectile[(int)dust.fadeIn].active&&Main.projectile[(int)dust.fadeIn].type== ModContent.ProjectileType<WaterMagicBall>())
                {
                    Vector2 len = (Main.projectile[(int)dust.fadeIn].Center - dust.position);
                    if (len.Length() < 100) dust.active = false;
                    else
                    {
                        Vector2 v = Vector2.Normalize(len);
                        v *=25;
                        dust.velocity = v;
                        dust.scale += 0.1f;
                    }

                }
            }
            dust.position += dust.velocity;
            dust.velocity *= 0.92f;
            dust.scale -= 0.1f;
            if (dust.scale < 0.2f) dust.active = false;
            return false;
        }
    }
}
