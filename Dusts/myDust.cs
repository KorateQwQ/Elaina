using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.Dusts
{
    public class myDust : ModDust
    {
        static Asset<Texture2D> texture;
        float randScale = 1;
        float randWidth = 1;
        float startScale = 1;
        bool spawn = false;
        public override void OnSpawn(Dust dust)
        {
            //dust.rotation = Main.rand.NextFloat(-(float)Math.PI, (float)Math.PI);
            randScale = Main.rand.NextFloat(0.6f, 1.2f);
            randWidth = Main.rand.NextFloat(1.0f, 1.5f);
            base.OnSpawn(dust);
        }
        public override Color? GetAlpha(Dust dust, Color lightColor)
        {
            if (texture == null)
            {
                texture = Mod.Assets.Request<Texture2D>("Dusts/myDust");
            }

            dust.color.A = 0;
            for(int i = 0; i < 5; i++)
            {
                Main.spriteBatch.Draw(texture.Value, dust.position - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()), dust.color, dust.rotation, texture.Size() * 0.5f,  new Vector2(dust.scale ,0.15f), SpriteEffects.None, 0);

            }


            return base.GetAlpha(dust, lightColor);
        }
        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;
            dust.rotation = dust.velocity.ToRotation();
            //dust.velocity *= 0.95f;
            dust.velocity.Y += 0.4f;


            //dust.velocity -= Vector2.Normalize(dust.velocity)*0.7f;
            //dust.scale -= 0.04f;
            dust.scale -= dust.fadeIn;
            if (dust.scale < 0.05) dust.active = false;
            return false;

        }
    }
}
