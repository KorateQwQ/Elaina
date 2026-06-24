using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.Dusts
{
    public class starDust : ModDust
    {
        static Asset<Texture2D> texture;
        public override void OnSpawn(Dust dust)
        {

        }
        public override Color? GetAlpha(Dust dust, Color lightColor)
        {
            if (texture == null)
            {
                texture = Mod.Assets.Request<Texture2D>("Dusts/starDust");
            }

            dust.color.A = 15;
            for (int i = 0; i < 2; i++)
            {
                Main.spriteBatch.Draw(texture.Value, dust.position - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()), dust.color * (dust.alpha/255f)*0.3f, dust.rotation+i*3.14f/2f, texture.Size() * 0.5f, dust.scale, SpriteEffects.None, 0);

            }


            return Color.White;
        }
        public override bool Update(Dust dust)
        {
            Lighting.AddLight(dust.position, 0.5f,0.5f, 0.5f);
            dust.position += dust.velocity*0.5f;
            dust.rotation = dust.velocity.ToRotation();
            dust.scale -= 0.01f;
            if (dust.alpha > 0) dust.alpha--;
            if (dust.scale < 0.1f) dust.active = false;
            return false;
        }
    }
}
