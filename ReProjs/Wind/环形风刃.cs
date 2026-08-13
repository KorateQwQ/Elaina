using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;
using Terraria.ModLoader;
using 伊蕾娜.Managers;

namespace 伊蕾娜.ReProjs.Wind
{
    internal class 环形风刃 : ElainaProj
    {
        Vector2[] 坐标组 = new Vector2[220];
        float MaxTime = 60;
        float start = 0;
        float defWidth = 150;
        float defHeight = 50;
        static Asset<Texture2D> 风刃1;
        static Asset<Texture2D> 风刃2;
        static Asset<Texture2D> 风刃3;

        int 图层 = 0;

        public override void SetDefaults()
        {
            if(风刃1 == null)
            {
                风刃1 = Mod.Assets.Request<Texture2D>("ReProjs/Wind/风刃1", AssetRequestMode.ImmediateLoad);
                风刃2 = Mod.Assets.Request<Texture2D>("ReProjs/Wind/风刃2", AssetRequestMode.ImmediateLoad);
                风刃3 = Mod.Assets.Request<Texture2D>("ReProjs/Wind/Extra_209", AssetRequestMode.ImmediateLoad);

            }
            MaxTime = Main.rand.Next(20, 40);
            Projectile.timeLeft = (int)MaxTime;
            Projectile.rotation = Main.rand.NextFloat(-3.14f / 2f, 3.14f / 2f);
            start = Main.rand.NextFloat(-3.14f, 3.14f);
            defWidth = Main.rand.NextFloat(100, 150);
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.width = 1;
            Projectile.height = 1;
            //Projectile.hide = true;
            base.SetDefaults();
        }
        public override void AI()
        {
            if (Projectile.velocity != Vector2.Zero) Projectile.rotation = Projectile.velocity.ToRotation() + 3.14f / 2;
            if (Projectile.ai[0] != 0) defWidth = Projectile.ai[0];
            if (Projectile.ai[1] != 0) defHeight = Projectile.ai[1];
            float startRot = start + MathHelper.Lerp(4, 0, Projectile.timeLeft / MaxTime);
            float eachRot = 0.02f;
            float width = MathHelper.Lerp(defWidth * 2, defWidth, Projectile.timeLeft / MaxTime);
            float height = defHeight;

            for (int i = 0; i < 坐标组.Length; i++)
            {
                坐标组[i] = Projectile.Center + new Vector2((float)Math.Cos(startRot + eachRot * i) * width, (float)Math.Sin(startRot + eachRot * i) * height);

            }
            base.AI();
        }
        public override bool PreDraw(ref Color lightColFor)
        {
            Color color1 = Fire;

            if (Projectile.ai[2] != (float)伊蕾娜.HitType.HitByWind)
            {
                color1 = 伊蕾娜.SkillDamageColor((伊蕾娜.HitType)Projectile.ai[2]);
            }
            else color1 = new(255, 160, 239);
            color1 = new(255, 160, 239);
            float alpha = MathHelper.Lerp(0, 1, Projectile.timeLeft / MaxTime);
            Texture2D tex = 风刃3.Value;// TextureAssets.Extra[98].Value;// TextureAssets.Projectile[Projectile.type].Value;
            color1.A = 0;
            Color color2 = color1 * 0.8f;
            Color color3 = color1;
            color3.A = 255;
            color2.A = 255;
            //Projectile.knockBack = 1;
            if (图层 == 0)//底层
            {
                //DrawManager.圆环顶点绘制shader(风刃1.Value, Projectile.Center, 坐标组, Projectile.rotation, new Color(167, 50, 249, 0) * 0, new Color(167, 50, 249, 255) * 0.2f, width: 80, alpha, 10, 0);
                if (Projectile.knockBack == 1) DrawManager.圆环顶点绘制shader(风刃2.Value, Projectile.Center, 坐标组, Projectile.rotation, color3, color3, width: 60, alpha, 1, 1);
                else
                {
                    DrawManager.圆环顶点绘制shader(风刃1.Value, Projectile.Center, 坐标组, Projectile.rotation, color1 * 0, color2, width: 40, alpha, 1, 0);
                    DrawManager.圆环顶点绘制shader(tex, Projectile.Center, 坐标组, Projectile.rotation, color1 * 0, color2, width: 60, alpha, 1, 1);
                }
                //DrawManager.圆环顶点绘制shader(tex, Projectile.Center, 坐标组, Projectile.rotation, new Color(167, 50, 249, 55)*0, color2, width: 80, alpha, 1, 0);

                图层 = 1;
            }
            else
            {
                if (Projectile.knockBack == 1) DrawManager.圆环顶点绘制shader(风刃2.Value, Projectile.Center, 坐标组, Projectile.rotation, color3, color3, width: 60, alpha, 1, 1);
                else
                {
                    DrawManager.圆环顶点绘制shader(风刃1.Value, Projectile.Center, 坐标组, Projectile.rotation, color1, color2*0, width: 40, alpha, 10, 1);
                    DrawManager.圆环顶点绘制shader(tex, Projectile.Center, 坐标组, Projectile.rotation, color1, color1 * 0f, width: 90, alpha, 20, 0);
                }
                //DrawManager.圆环顶点绘制shader(tex, Projectile.Center, 坐标组, Projectile.rotation, new Color(167, 50, 249, 55), color2*0, width: 80, alpha, 1, 0);

                图层 = 0;
            }
            //DrawManager.圆环顶点绘制shader(tex, Projectile.Center, 坐标组, Projectile.rotation, new Color(167, 50, 249, 55), new Color(167, 50, 249, 55), width: 80, alpha, 1, 0);

            return false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
            base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
        }

    }
}
