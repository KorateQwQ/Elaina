using KL.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.Dusts
{
    public class FloDust : KLBasicDust
    {
        public override void OnSpawn(Dust dust)
        {
            base.OnSpawn(dust);
        }

        public override bool PreDraw(Dust dust)
        {
            float alpha = MathHelper.Lerp(1, 0, dust.LifeProgress());
            Vector2 scale = Vector2.One*MathHelper.Lerp(1, 2f, dust.LifeProgress());
            DrawInWorld(MainTexture,dust.position,new Color(255,255,255,0)*alpha,scale:scale);
            DrawInWorld(MainTexture,dust.position,new Color(255,255,255,0)*alpha,scale:scale);
            DrawInWorld(MainTexture,dust.position,new Color(255,255,255,0)*alpha,scale:scale);
            DrawInWorld(MainTexture,dust.position,new Color(255,255,255,0)*alpha,scale:scale);

            return false;
        }

        public override bool Update(Dust dust)
        {
            base.Update(dust);
            return false;

        }
    }
}
