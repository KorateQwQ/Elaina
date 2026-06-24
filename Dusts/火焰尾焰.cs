using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.Dusts
{
    public class 火焰尾焰 : ModDust
    {
        static Asset<Texture2D> texture;
        float alpha = 0f;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }
        public override void OnSpawn(Dust dust)
        {
            dust.frame = new Rectangle(Main.rand.Next(2) * 100, Main.rand.Next(2) * 100, 100, 100);
            dust.rotation = Main.rand.NextFloat(-(float)Math.PI, (float)Math.PI);
            base.OnSpawn(dust);
        }
        public override Color? GetAlpha(Dust dust, Color lightColor)
        {
            if (texture == null)
            {
                texture = Mod.Assets.Request<Texture2D>("Dusts/火焰尾焰");
            }
            Color c = dust.color;
            //c.A = 10;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, 
            SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            Main.spriteBatch.Draw(texture.Value, dust.position - Main.screenPosition, dust.frame, c, dust.rotation, new Vector2(50, 50),dust.scale, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.Transform);
            return base.GetAlpha(dust, lightColor);
        }
        //public override string Texture => null;
        public override bool Update(Dust dust)
        {   if (dust.scale > 1) dust.scale = 1;
            dust.position += dust.velocity;
            //dust.velocity -= Vector2.Normalize(dust.velocity)*0.7f;
            dust.scale -= 0.01f;
            //dust.rotation += 0.05f;

            if (dust.scale >= 0.8f)
            {
                // (byte)MathHelper.Lerp(0, 255, (1 - dust.scale) / 0.2f);
                dust.color = new Color(249, 207, 55);// Color.Lerp(new Color(249, 207, 55), new Color(210,85,0), (1 - dust.scale) / 0.2f);
                dust.color.A = (byte)MathHelper.Lerp(0, 255, (1 - dust.scale) / 0.2f);
                //Main.NewText(dust.color.A);
            }
            else if (dust.scale > 0.5f)
            {
                //dust.color = new Color(210, 85, 0);
                dust.color = Color.Lerp(new Color(249, 207, 55), new Color(210, 85, 0), (0.8f - dust.scale) / 0.2f);
                //dust.color.A = 255;
                if(dust.scale>0.6) dust.color = Color.Lerp(new Color(228, 64, 50), new Color(249, 207, 55), (0.6f - dust.scale) / 0.1f);
            }
            else
            {
                //dust.color.A = (byte)MathHelper.Lerp(0, 255, dust.scale / 0.7f);
               dust.color = Color.Lerp(new Color(0, 0, 0), new Color(228, 64, 50), dust.scale / 0.7f);
            }
            if (dust.scale < 0.8)
            {
                dust.color.A = (byte)MathHelper.Lerp(0, 255, dust.scale / 0.8f);
            }
                //Main.NewText(dust.scale);
                // dust.color = Color.Lerp()
                if (dust.scale < 0.1f) dust.active = false;
            return false;
        }
    }
}
