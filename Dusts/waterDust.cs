using Homura.Manager;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using 伊蕾娜.Managers;

namespace 伊蕾娜.Dusts
{
    internal class waterDust : ModDust
    {
        static Asset<Texture2D> texture;
        public override void OnSpawn(Dust dust)
        {

        }
        public override Color? GetAlpha(Dust dust, Color lightColor)
        {



            return Color.White;
        }

        public override bool PreDraw(Dust dust)
        {
            if (texture == null)
            {
                texture = Mod.Assets.Request<Texture2D>("Dusts/waterDust");
            }
            Asset<Texture2D> shape = Mod.Assets.Request<Texture2D>("Effects/Tex/voronoi", AssetRequestMode.ImmediateLoad);
            Asset<Texture2D> mask = Mod.Assets.Request<Texture2D>("Effects/Tex/fadeUP", AssetRequestMode.ImmediateLoad);

            dust.endBegin(1,1);
            //1 - (dust.alpha / 60f)
            DrawManager.消融shader(1 - (dust.alpha / 35f), null, shape.Value,useMask:true, mask.Value,
                lineColor: dust.color, lineWidth:0.3f);
            for (int i = 0; i < 3; i++)
            {
                Main.spriteBatch.Draw(texture.Value, dust.position - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()), 
                    dust.color, dust.rotation+3.14f/2 , texture.Size() * 0.5f, new Vector2(dust.scale*1.2f, dust.scale), SpriteEffects.None, 0);

            }
            dust.endBegin();
            return false;
        }

        public override bool Update(Dust dust)
        {
            //Lighting.AddLight(dust.position, 0.5f, 0.5f, 0.5f);
            dust.position += dust.velocity;
            dust.velocity *=0.90f;
            dust.rotation = dust.velocity.ToRotation();
            //dust.scale += 0.01f;
            if (dust.alpha > 0) dust.alpha--;
            if (dust.alpha <= 1f) dust.active = false;

            return false;
        }
    }
}
