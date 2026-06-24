using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;
using Terraria.Map;
using Terraria.ModLoader;
using 伊蕾娜.Managers;

namespace 伊蕾娜.ReProjs.Water
{
    internal class testBall : ModProjectile
    {
        Vector2[] 坐标组 = new Vector2[220];

        Vector2[][] 坐标组2 = new Vector2[33][]; 
        public override void SetDefaults()
        {
            坐标组 = new Vector2[100];
            for(int i = 0; i < 坐标组2.Length; i++) 
                坐标组2[i] = new Vector2[33];
            base.SetDefaults();
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D waterNoi = Mod.Assets.Request<Texture2D>("ReProjs/Water/waterNoi", AssetRequestMode.ImmediateLoad).Value;

            //DrawManager.圆环顶点绘制shader(waterNoi, Projectile.Center, 坐标组, Projectile.rotation, new Color(77,81,255,255), new Color(77, 81, 255, 255), 
            //width: 80, 1, 1, 1);
            float width = 220;
            float height = 50;
            for (int i = 0; i < 坐标组.Length; i++)
            {
                //坐标组[i] = Projectile.Center + new Vector2((float)Math.Cos(0 + (Math.PI*2f/坐标组.Length)* i) * width, (float)Math.Sin(0 + (Math.PI * 2f / 坐标组.Length) * i) * height);

            }
            //DrawManager.圆环顶点绘制shader(waterNoi, Projectile.Center, 坐标组, Projectile.rotation, new Color(77, 81, 255, 255), new Color(77, 81, 255, 255),
            //width: 80, 1, 1, 1);
            //new Color(77, 81, 255, 255)


            确定球体坐标(Main.MouseWorld, 250);


            DrawManager.球体shader(waterNoi, 坐标组2, Projectile.rotation, new Color(77, 81, 255, 255), new Color(77, 81, 255, 255), width: 80, 1, 1, 1,
                DrawManager.FrameTime(0, 10, 3640));
            //DrawManager.任意矩形shader(waterNoi, 坐标组2, Projectile.rotation, new Color(77, 81, 255, 255), new Color(77, 81, 255, 255),width: 80, 1, 1, 1,DrawManager.FrameTime(0,1,120));
            return false;
        }
        void 确定球体坐标(Vector2 center, float width)
        {
            for (int i = 0; i < 坐标组2.Length; i++)
            {
                //if (i != (坐标组2.Length - 1)) continue;
                float height = 20;
                int mid = 坐标组2.Length / 2;
                float scaleW = MathHelper.Lerp(1, 0.4f, Math.Abs(mid - i) / (float)mid);
                float scaleY = MathHelper.Lerp(0, 0.99f, Math.Abs(mid - i) / (float)mid);
                //if (scaleY > 0.99) scaleY = 0.90f;

                float eachHeightY = (width) * scaleY;
                float eachWidthY = (height) * scaleW;

                float eachWidthOfCir = (float)Math.Sqrt((double)(width * width - eachHeightY * eachHeightY));
                Vector2 层数 = new Vector2(0, eachHeightY * ((mid - i) > 0 ? 1 : -1));
                if (i == mid) 层数.Y = 0;
                //Main.NewText(i + " "+ eachHeightY + " " + 层数.Y);
                for (int j = 0; j < 坐标组2[i].Length; j++)
                {
                    int mid2 = 坐标组2[j].Length / 2;
                    int index = j / 2 + ((j%2==0)?0:1);
                    float step = MathHelper.Lerp( mid2 / 2f, 0, Math.Abs(mid2 - index) / (float)mid2)*((j%2==0)?1:-1);
                    //坐标组2[i][j] = Main.MouseWorld+new Vector2(i*10, j*10);
                    Vector2 result = center + 层数 + new Vector2((float)Math.Cos(0 + (Math.PI * 2f / 坐标组2[i].Length) * j) * eachWidthOfCir, (float)Math.Sin(0 + (Math.PI * 2f / 坐标组2[i].Length) * j) * eachWidthY);
                    float sW = width*0.1f;
                    float sH = 1f;
                    if (i == (坐标组2.Length - 1))
                    {
                        //if(j == 0) 
                        //坐标组2[i][j] = center + 层数 + new Vector2((float)Math.Cos(1.5f*Math.PI + (Math.PI * 2f / 坐标组2[i].Length) * step) * eachWidthOfCir, (float)Math.Sin(1.5f * Math.PI + (Math.PI * 2f / 坐标组2[i].Length) * step) * eachWidthY);
                        坐标组2[i][j] = center + 层数 + new Vector2((float)Math.Cos( (Math.PI * 2f / 坐标组2[i].Length)*j ) * sW, (float)Math.Sin(0 + (Math.PI * 2f / 坐标组2[i].Length) * j) * sH);
                        continue;
                    }
                    else if(i == 0)
                    {
                        //坐标组2[i][j] = center + 层数 + new Vector2((float)Math.Cos( (Math.PI * 2f / 坐标组2[i].Length) * (j / 2f)) * eachWidthOfCir, (float)Math.Sin( (Math.PI * 2f / 坐标组2[i].Length) * (j / 2f)) * eachWidthY);
                        //坐标组2[i][j] = center + 层数 + new Vector2((float)Math.Cos(-1.5f * Math.PI + (Math.PI * 2f / 坐标组2[i].Length) * step) * eachWidthOfCir, (float)Math.Sin(-1.5f * Math.PI + (Math.PI * 2f / 坐标组2[i].Length) * step) * eachWidthY);
                        坐标组2[i][j] = center + 层数 + new Vector2((float)Math.Cos( (Math.PI * 2f / 坐标组2[i].Length) * j) * sW, (float)Math.Sin(0 + (Math.PI * 2f / 坐标组2[i].Length) * j) * sH);

                        continue;

                    }
                    坐标组2[i][j] = result;
                }
            }
        }
        #region 存档

        /*花式两段写法（
                             int mid2 = 坐标组2[j].Length / 2;
                    int index = j / 2 + ((j%2==0)?0:1);
                    float step = MathHelper.Lerp( mid2 / 2f, 0, Math.Abs(mid2 - index) / (float)mid2)*((j%2==0)?1:-1);
                    //坐标组2[i][j] = Main.MouseWorld+new Vector2(i*10, j*10);
                    Vector2 result = center + 层数 + new Vector2((float)Math.Cos(0 + (Math.PI * 2f / 坐标组2[i].Length) * j) * eachWidthOfCir, (float)Math.Sin(0 + (Math.PI * 2f / 坐标组2[i].Length) * j) * eachWidthY);
                    if (i == (坐标组2.Length - 1))
                    {
                        //if(j == 0) 
                        坐标组2[i][j] = center + 层数 + new Vector2((float)Math.Cos(1.5f*Math.PI + (Math.PI * 2f / 坐标组2[i].Length) * step) * eachWidthOfCir, (float)Math.Sin(1.5f * Math.PI + (Math.PI * 2f / 坐标组2[i].Length) * step) * eachWidthY);

                        continue;
                    }
                    else if(i == 0)
                    {
                        //坐标组2[i][j] = center + 层数 + new Vector2((float)Math.Cos( (Math.PI * 2f / 坐标组2[i].Length) * (j / 2f)) * eachWidthOfCir, (float)Math.Sin( (Math.PI * 2f / 坐标组2[i].Length) * (j / 2f)) * eachWidthY);
                        坐标组2[i][j] = center + 层数 + new Vector2((float)Math.Cos(-1.5f * Math.PI + (Math.PI * 2f / 坐标组2[i].Length) * step) * eachWidthOfCir, (float)Math.Sin(-1.5f * Math.PI + (Math.PI * 2f / 坐标组2[i].Length) * step) * eachWidthY);

                        continue;

                    }
         
         */
        void 矩形顶点()
        {
            float width = 220;
            float height = 50;
            for (int i = 0; i < 坐标组2.Length; i++)//非常好矩形条带使我的shader旋转
            {
                width = 220;
                height = 50;
                int mid = 坐标组2.Length / 2;
                float scaleW = MathHelper.Lerp(1, 0, Math.Abs(mid - i) / (float)mid);
                float scaleY = MathHelper.Lerp(0, 1, Math.Abs(mid - i) / (float)mid);

                float eachHeightY = (width) * scaleY;
                Vector2 层数 = new Vector2(0, eachHeightY * ((mid - i) > 0 ? 1 : -1));
                if (i == mid) 层数.Y = 0;
                //Main.NewText(i + " "+ eachHeightY + " " + 层数.Y);
                for (int j = 0; j < 坐标组2[i].Length; j++)
                {

                    //坐标组2[i][j] = Main.MouseWorld+new Vector2(i*10, j*10);
                    坐标组2[i][j] = Projectile.Center + 层数 + new Vector2((float)Math.Cos(0 + (Math.PI * 2f / 坐标组2[i].Length) * j) * width, (float)Math.Sin(0 + (Math.PI * 2f / 坐标组2[i].Length) * j) * height);
                }
            }
        }
        #endregion
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            float startRot =0.3f;
            float eachRot = 0.118f;
            float width =220;
            float height = 50;
            for (int i = 0; i < 坐标组.Length; i++)
            {
                //坐标组[i] = Projectile.Center + new Vector2((float)Math.Cos(startRot + eachRot * i) * width, (float)Math.Sin(startRot + eachRot * i) * height);

            }
            if (Main.mouseLeft)
            {
                player.itemAnimation =  Projectile.timeLeft = 60;
            }
            base.AI();
        }
    }
}
