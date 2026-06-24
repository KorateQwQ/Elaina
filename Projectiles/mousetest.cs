using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace 伊蕾娜.Projectiles
{
    public class mousetest : ModProjectile
    {
        Asset<Texture2D> texture;
        public override void SetDefaults()
        {
            Projectile.damage = 0;
            Projectile.tileCollide = false;
            //Projectile.timeLeft = 300;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 30;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void AI()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            if (texture == null)
                texture = Mod.Assets.Request<Texture2D>("Projectiles/形状3.2");
            var player = Main.player[Projectile.owner];
            if (Main.myPlayer == player.whoAmI)
            {
                Projectile.ai[0]++;
                var projectileToMouse = Main.MouseWorld - Projectile.Center;
                //Projectile.velocity = projectileToMouse;
                if (Projectile.ai[1] < 256)
                    Projectile.ai[1]++;
                else
                    Projectile.ai[1] = 0;
                Projectile.velocity = Vector2.Normalize(Main.MouseWorld - Projectile.position);
                Projectile.position = Main.MouseWorld;
                for (int i = Projectile.oldPos.Length - 1; i > 0; --i)
                    Projectile.oldRot[i] = Projectile.oldRot[i - 1];
                Projectile.oldRot[0] = Projectile.rotation;
            }
            else
                Projectile.Kill();
        }

        public void drawLongLine()
        {
            //Asset<Texture2D> texture = Mod.Assets.Request<Texture2D>("Projectiles/光线2");
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 scale = new(0.01f, 0.006f);
            float step = texture.Size().X / 10;
            int startX = 0;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            for (int i = 1; i < 20; i++)
            {
                float rotation = 0f;
                rotation = (Projectile.oldPos[i] - Projectile.oldPos[i - 1]).ToRotation();
                float 渐变值 = MathHelper.Lerp(20f, 10f, i / 20f);
                float alpha = 255f * (渐变值 * 0.05f);
                Color c = Projectile.GetFairyQueenWeaponsColor(0f);//new Color(88, 149, 255);
                c.A = (byte)alpha;
                Vector2 eachScale = scale * 渐变值;
                float distance = (Projectile.oldPos[i] - Projectile.oldPos[i - 1]).Length();
                if (distance < texture.Size().X * eachScale.X / 3 && distance > 0)
                {
                    //Main.NewText(distance + "xxx " + (texture.Size().X * eachScale.X / 3)+" "+rotation);
                    //eachScale.X *= MathHelper.Lerp(0f, 1f, distance / (texture.Size().X * eachScale.X / 2));

                }
                if (Projectile.oldPos[i] != Projectile.oldPos[i - 1])
                    Main.EntitySpriteDraw(texture.Value, Projectile.oldPos[i] - Main.screenPosition, new Rectangle(0, 0, (int)texture.Size().X, (int)texture.Size().Y),
                        c, rotation, origin, eachScale, SpriteEffects.None, 0);
                startX += (int)step;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin();
        }
        public float 二次渐进率(float 总长度, float 最高值, float 当前长度)//半径长度为右零点，从最高值开始衰减到0
        {
            float a = 1;
            a = -最高值 / (总长度 * 总长度);
            //Main.NewText(a+" "+半径长度+" "+当前长度+" "+(a * 当前长度*当前长度 + 最高值));
            return a * (当前长度 * 当前长度) + 最高值;
        }
        public float 从切线向圆周描绘直线距离(double 半径, double 位置)
        {
            return (float)Math.Sqrt(半径 * 半径 - 位置 * 位置);
        }
        public void 画切线(Vector2 position, float rotation, float length, float 渐变率)
        {
            //Asset<Texture2D> texture = Mod.Assets.Request<Texture2D>("Projectiles/形状");
            rotation = rotation + (float)Math.PI / 4;
            Vector2 move = Vector2.One;
            move = move.RotatedBy(rotation);
            Vector2 move2 = move;
            move2 = move2.RotatedBy(Math.PI / 2);
            //Main.NewText(rotation+" "+move);
            //Main.NewText(渐变率);
            int 纹路坐标X = (int)MathHelper.Lerp((int)texture.Size().X, 0f, 渐变率);
            for (int i = 0; i < length / 2f; i++)
            {
                Color c = new(177, 254, 254, 255);
                int 纹路坐标Y位移 = (int)MathHelper.Lerp(0f, (int)texture.Size().Y / 2, i / (length / 2));
                //c.A =(byte) MathHelper.Lerp(50f, 0f, i / (length / 2));
                //c.R = (byte)MathHelper.Lerp(250f, 0f, i / (length / 2));
                //c.G = (byte)MathHelper.Lerp(250f, 0f, i / (length / 2));
                //c.B = (byte)MathHelper.Lerp(250f, 0f, i / (length / 2));
                //c.A= (byte)MathHelper.Lerp(155f, 0f, (float)(Math.Sqrt(i)) / (length / 2));//线性渐变
                //
                //c.A = (byte)二次渐进率(length / 2, 155f, i);
                //Main.NewText(c.A);
                //
                c *= 渐变率;
                float eachLength = 从切线向圆周描绘直线距离(length / 2, i);
                if (渐变率 == 0.95f)//绘制头部圆弧
                {
                    for (int j = 0; j < eachLength; j++)
                    {
                        Color c2 = c;
                        c2.A = (byte)二次渐进率(eachLength, c.A, j);
                        /*Main.EntitySpriteDraw(texture.Value, position - Main.screenPosition + i * move+j*move2, 
                        new Rectangle((int)texture.Size().X / 2, (int)texture.Size().Y / 2, 1, 1),
                        c2, move2.ToRotation(), new Vector2(1, 1), 1, SpriteEffects.None, 0);
                        Main.EntitySpriteDraw(texture.Value, position - Main.screenPosition - i * move + j * move2,
                        new Rectangle((int)texture.Size().X / 2, , 1, 1),
                        c2, move2.ToRotation(), new Vector2(1, 1), 1, SpriteEffects.None, 0);*/
                    }
                }
                else
                {
                    /*for (int j = 0; j < eachLength; j++)
                    {
                        Color c2 = c;
                        c2.A = (byte)二次渐进率(eachLength, c.A, j);
                        Main.EntitySpriteDraw(texture.Value, position - Main.screenPosition + i * move + j * move2,
                        new Rectangle((int)texture.Size().X / 2, (int)texture.Size().Y / 2, 1, 1),
                        c2, move2.ToRotation(), new Vector2(1, 1), 1, SpriteEffects.None, 0);
                        Main.EntitySpriteDraw(texture.Value, position - Main.screenPosition - i * move + j * move2,
                        new Rectangle((int)texture.Size().X / 2, (int)texture.Size().Y / 2, 1, 1),
                        c2, move2.ToRotation(), new Vector2(1, 1), 1, SpriteEffects.None, 0);
                    }*/
                }
                int 纹路坐标Ya = (int)texture.Size().Y / 2 - 纹路坐标Y位移;
                int 纹路坐标Yb = (int)texture.Size().Y / 2 + 纹路坐标Y位移;
                /*if (纹路坐标Ya + (int)Projectile.ai[1] > (int)texture.Size().Y)
                {
                    纹路坐标Ya = 纹路坐标Ya + (int)Projectile.ai[1] - (int)texture.Size().Y;
                }
                else 纹路坐标Ya = 纹路坐标Ya + (int)Projectile.ai[1];

                if (纹路坐标Yb + (int)Projectile.ai[1] > (int)texture.Size().Y)
                {
                    纹路坐标Yb = 纹路坐标Yb + (int)Projectile.ai[1] - (int)texture.Size().Y;
                }
                else 纹路坐标Yb = 纹路坐标Yb + (int)Projectile.ai[1];*/
                Main.EntitySpriteDraw(texture.Value, position - Main.screenPosition + i * move,
                    new Rectangle(纹路坐标X, 纹路坐标Ya, 1, 1),
                        c, rotation, new Vector2(1, 1), 4, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(texture.Value, position - Main.screenPosition - i * move,
                    new Rectangle(纹路坐标X, 纹路坐标Yb, 1, 1),
                        c, rotation, new Vector2(1, 1), 4, SpriteEffects.None, 0);
            }
        }

        public Vector2[] smooth(Vector2[] vecs, int extraLength)//平滑处理，增加标记的坐标点
        {
            int l = vecs.Length;
            extraLength += l;

            Vector2[] scVecs = new Vector2[extraLength];
            for (int n = 0; n < extraLength; n++)
            {
                float t = n / (float)extraLength;
                float k = (l - 1) * t;
                int i = (int)k;
                float vk = k % 1;
                if (i == 0)
                {
                    scVecs[n] = Vector2.CatmullRom(2 * vecs[0] - vecs[1], vecs[0], vecs[1], vecs[2], vk);
                }
                else if (i == l - 2)
                {
                    scVecs[n] = Vector2.CatmullRom(vecs[l - 3], vecs[l - 2], vecs[l - 1], 2 * vecs[l - 1] - vecs[l - 2], vk);
                }
                else
                {
                    scVecs[n] = Vector2.CatmullRom(vecs[i - 1], vecs[i], vecs[i + 1], vecs[i + 2], vk);
                }
            }
            return scVecs;
        }
        public void 补齐两切线矩形间隔(float 此切线半径, Vector2 此切线中心坐标, float 此切线渐变率,
            float 下一切线半径, Vector2 下一切线中心坐标, float 下一切线渐变率)
        {
            float rotation = (此切线中心坐标 - 下一切线中心坐标).ToRotation();
            Vector2 move2 = 下一切线中心坐标 - 此切线中心坐标;
            rotation = rotation + (float)Math.PI / 4;
            Vector2 move = Vector2.One;
            move = move.RotatedBy(rotation);

            float 需要补齐切线数量 = (此切线中心坐标 - 下一切线中心坐标).Length();
            //Main.NewText(需要补齐切线数量);

            for (float i = 0; i < 需要补齐切线数量; i++)
            {
                int 纹路坐标X = (int)MathHelper.Lerp((int)texture.Size().X, 0f, 此切线渐变率 - i * ((此切线渐变率 - 下一切线渐变率) / 需要补齐切线数量));
                float 补切线长度 = (int)MathHelper.Lerp(此切线半径, 下一切线半径, i / 需要补齐切线数量);
                Color c = new(69, 0, 220, 255);
                c *= MathHelper.Lerp(此切线渐变率, 下一切线渐变率, i / 需要补齐切线数量);
                for (float j = 0; j < 补切线长度; j++)
                {

                    int 纹路坐标Y = (int)MathHelper.Lerp(0f, (int)texture.Size().Y / 2, i / 补切线长度);
                    Main.EntitySpriteDraw(texture.Value, 此切线中心坐标 - Main.screenPosition + j * move + i * move2,
                    new Rectangle(纹路坐标X, (int)texture.Size().Y / 2 - 纹路坐标Y, 1, 1),
                        c, rotation, new Vector2(1, 1), 4, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(texture.Value, 此切线中心坐标 - Main.screenPosition - j * move + i * move2,
                        new Rectangle(纹路坐标X, (int)texture.Size().Y / 2 + 纹路坐标Y, 1, 1),
                            c, rotation, new Vector2(1, 1), 4, SpriteEffects.None, 0);
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (texture == null)
                texture = Mod.Assets.Request<Texture2D>("Projectiles/形状3.2");

            //
            //Asset<Texture2D> texture = Mod.Assets.Request<Texture2D>("Projectiles/mousetest");
            Vector2 origin = texture.Size() * 0.5f;
            Vector2[] 坐标组;
            float 长度 = Projectile.ai[0];
            if (Projectile.ai[0] > 20)
            {
                长度 = 200;
            }
            坐标组 = smooth(Projectile.oldPos, (int)长度);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            for (float i = 1; i < 长度; i++)
            {

                float 渐变率 = MathHelper.Lerp(1, 0f, i / 长度);
                Vector2 eachScale = new Vector2(1, 1) * MathHelper.Lerp(20f, 1f, i / 长度);
                float rotation = (坐标组[(int)i] - 坐标组[(int)i - 1]).ToRotation();
                float length = MathHelper.Lerp(25f, 1f, i / 长度);
                //画切线(坐标组[(int)i], rotation, length, 渐变率);
                if (i + 1 < 长度)//此切线还有后续切线，若两切线距离过远则补齐差值部分
                {
                    if ((坐标组[(int)i] - 坐标组[(int)i + 1]).Length() > 10f && (坐标组[(int)i] - 坐标组[(int)i + 1]).Length() < 50f)
                    {
                        float 下一切线length = MathHelper.Lerp(100f, 1f, (i + 1) / 长度);
                        float 下一切线渐变率 = MathHelper.Lerp(1, 0f, i + 1 / 长度);
                        //补齐两切线矩形间隔((length / 2), 坐标组[(int)i], 渐变率, (下一切线length / 2), 坐标组[(int)i + 1], 下一切线渐变率);
                    }

                }
                Color c = new(69, 0, 220, 255);

                c.A = (byte)(渐变率 * 255);
                //Main.EntitySpriteDraw(texture.Value, 坐标组[i] - Main.screenPosition, new Rectangle((int)texture.Size().X/2, (int)texture.Size().Y/2, 1, 1),
                //c, rotation, new Vector2(1, 1), 3, SpriteEffects.None, 0);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);


            /*Asset<Texture2D> texture = TextureAssets.Extra[98];
            Vector2 origin = texture.Size() * 0.5f;
            float 渐变值 = Utils.GetLerpValue(15f, 30f, Projectile.timeLeft, clamped: true) * 
                Utils.GetLerpValue(240f, 200f, Projectile.timeLeft, clamped: true) * 
                (1f + 0.2f * (float)Math.Cos(Main.GlobalTimeWrappedHourly % 30f / 0.5f * ((float)Math.PI * 2f) * 3f)) * 0.8f;
            Color color = Projectile.GetFairyQueenWeaponsColor(0f);
            color *= 渐变值;

            Vector2 scale= new Vector2(0.5f, 5f) * 渐变值*0.4f;
            Vector2 scale2 = new Vector2(0.5f, 2f) * 渐变值 * 0.4f;
            Main.EntitySpriteDraw(texture.Value, Projectile.Center-Main.screenPosition, null, color, 
                (float)Math.PI / 2f, origin, scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(
                texture.Value, Projectile.Center - Main.screenPosition, null, color,
                0f, origin, scale2, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(
                texture.Value, Projectile.Center - Main.screenPosition, null, color*0.5f,
                (float)Math.PI / 2f, origin, scale * 0.6f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(
                texture.Value, Projectile.Center - Main.screenPosition, null, color*0.5f,
                0f, origin, scale2 * 0.6f, SpriteEffects.None, 0);*/
            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            var player = Main.player[Projectile.owner];

            Vector2[] 坐标组;
            float 长度 = Projectile.ai[0];
            if (Projectile.ai[0] > 30)
            {
                长度 = 100;
                坐标组 = smooth(Projectile.oldPos, (int)长度);
            }
            else
                坐标组 = Projectile.oldPos;


            Effect DefaultEffect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/tail").Value;
            Asset<Texture2D> MainColor = Mod.Assets.Request<Texture2D>("Projectiles/颜色3");
            Asset<Texture2D> MainShape = Mod.Assets.Request<Texture2D>("Projectiles/拖尾");
            Asset<Texture2D> MaskColor = Mod.Assets.Request<Texture2D>("Projectiles/渐变");

            // 把所有的点都生成出来，按照顺序
            List<CustomVertexInfo> bars = new();
            List<CustomVertexInfo> triangleList = new();
            for (int i = 1; i < 坐标组.Length; ++i)
            {
                if (坐标组[i] == Vector2.Zero)
                    break;
                //spriteBatch.Draw(Main.magicPixel, projectile.oldPos[i] - Main.screenPosition,
                //    new Rectangle(0, 0, 1, 1), Color.White, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);

                float width = MathHelper.Lerp(5f, 1f, (float)i / 坐标组.Length);

                var normalDir = 坐标组[i - 1] - 坐标组[i];
                normalDir = Vector2.Normalize(new Vector2(-normalDir.Y, normalDir.X));
                var factor = i / (float)坐标组.Length; //(float)坐标组.Length;
                var color = new Color(180, 255, 255, 255);//Color.Lerp(Color.White,Color.Red , factor);//Projectile.GetFairyQueenWeaponsColor(0f)//从头部到尾部渐变颜色
                var w = MathHelper.Lerp(1f, 0.05f, factor);//从头部到尾部越来越透明
                var trans = Main.GameViewMatrix != null ? Main.GameViewMatrix.TransformationMatrix : Matrix.Identity;
                bars.Add(new CustomVertexInfo(坐标组[i] + normalDir * width * trans.M11, color, new Vector3(factor, 1, w)));//最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
                bars.Add(new CustomVertexInfo(坐标组[i] + normalDir * -width * trans.M11, color, new Vector3(factor, 0, w))); //(float)Math.Sqrt(factor)
            }
            //Main.NewText("123");


            if (bars.Count > 2)
            {

                // 按照顺序连接三角形
                triangleList.Add(bars[0]);

                var vertex = new CustomVertexInfo((bars[0].Position + bars[1].Position) * 0.5f + Vector2.Normalize(Projectile.velocity) * 5, Color.White,
                    new Vector3(0, 0.5f, 1));
                triangleList.Add(bars[1]);
                triangleList.Add(vertex);
                for (int i = 0; i < bars.Count - 2; i += 2)
                {
                    triangleList.Add(bars[i]);
                    triangleList.Add(bars[i + 2]);
                    triangleList.Add(bars[i + 1]);

                    triangleList.Add(bars[i + 1]);
                    triangleList.Add(bars[i + 2]);
                    triangleList.Add(bars[i + 3]);
                }


                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
                RasterizerState originalState = Main.graphics.GraphicsDevice.RasterizerState;

                // 干掉注释掉就可以只显示三角形栅格
                /*RasterizerState rasterizerState = new RasterizerState();
                rasterizerState.CullMode = CullMode.None;
                rasterizerState.FillMode = FillMode.WireFrame;
                Main.graphics.GraphicsDevice.RasterizerState = rasterizerState;*/

                var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
                var model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) * Main.Transform;

                // 把变换和所需信息丢给shader

                //
                DefaultEffect.Parameters["uTransform"].SetValue(model * projection);
                DefaultEffect.Parameters["uTime"].SetValue(-(float)Main.GameUpdateCount * 0.03f);

                Main.graphics.GraphicsDevice.Textures[0] = MainColor.Value;
                Main.graphics.GraphicsDevice.Textures[1] = MainShape.Value;
                Main.graphics.GraphicsDevice.Textures[2] = MaskColor.Value;

                Main.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
                Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.PointWrap;
                Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.PointWrap;
                //Main.graphics.GraphicsDevice.Textures[0] = Main.magicPixel;
                //Main.graphics.GraphicsDevice.Textures[1] = Main.magicPixel;
                //Main.graphics.GraphicsDevice.Textures[2] = Main.magicPixel;

                DefaultEffect.CurrentTechnique.Passes[0].Apply();


                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0, triangleList.Count / 3);

                Main.graphics.GraphicsDevice.RasterizerState = originalState;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }

        }
        // 自定义顶点数据结构，注意这个结构体里面的顺序需要和shader里面的数据相同
        private struct CustomVertexInfo : IVertexType
        {
            private static VertexDeclaration _vertexDeclaration = new(
            [
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
            ]);
            public Vector2 Position;
            public Color Color;
            public Vector3 TexCoord;

            public CustomVertexInfo(Vector2 position, Color color, Vector3 texCoord)
            {
                Position = position;
                Color = color;
                TexCoord = texCoord;
            }

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    return _vertexDeclaration;
                }
            }
        }
    }
}
