using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;


namespace 伊蕾娜.Projectiles
{
    public class 武器流光 : ModProjectile
    {
        public Vector2[] oldposition;
        Asset<Texture2D> texture;
        Asset<Texture2D> texture2;
        Asset<Texture2D> texture3;
        Effect DefaultEffect2;
        Asset<Texture2D> MainColor;
        Asset<Texture2D> MainShape;
        Asset<Texture2D> MaskColor;

        public override void SetStaticDefaults()
        {
            Projectile.tileCollide = false;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 15;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;

        }
        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
        }
        public override bool PreDraw(ref Color lightColor)
        {

            return false;
        }
        public override void PostDraw(Color lightColor)
        {
            var player = Main.player[Projectile.owner];
            if (texture2 == null)
                texture2 = Mod.Assets.Request<Texture2D>("Projectiles/灰度渐变");
            if (texture3 == null)
                texture3 = Mod.Assets.Request<Texture2D>("Projectiles/Extra_98");
            if (MainColor == null)
                MainColor = Mod.Assets.Request<Texture2D>("Projectiles/粉色拖尾");
            if (MainShape == null)
                MainShape = Mod.Assets.Request<Texture2D>("Projectiles/拖尾");
            if (MaskColor == null)
                MaskColor = Mod.Assets.Request<Texture2D>("Projectiles/渐变");
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            Vector2 origin2 = texture2.Size() * 0.5f;//除3相当于以图片中心为position
            Vector2 scale2 = new(0.15f, 0.15f);
            float 渐变 = 30 - (float)Main.GameUpdateCount % 60;//60到 -60
            if (渐变 < 0)
                渐变 = -渐变;

            float 渐变2 = MathHelper.Lerp(0.5f, 0.7f, 渐变 / 30);
            float 渐变3 = MathHelper.Lerp(0f, 1f, Projectile.timeLeft / 30f);
            scale2 *= 渐变2;
            Color c = new(231, 110, 49);
            int chooseSkill = 0;
            
            switch (chooseSkill)
            {
                case 0:
                    c = new Color(255, 160, 239);
                    break;
                case 1:
                    c = new(156, 220, 254, 255);
                    MainColor = Mod.Assets.Request<Texture2D>("Projectiles/颜色3");
                    break;
                case 2:
                    c = new(231, 110, 49, 255);
                    MainColor = Mod.Assets.Request<Texture2D>("Projectiles/颜色");
                    break;
                case 3:
                    c = new(220, 160, 171, 255);
                    break;
                case 4:
                    c = new(220, 160, 171, 255);
                    MainColor = Mod.Assets.Request<Texture2D>("Projectiles/粉色拖尾");
                    break;
                case 5:
                    c = new(156, 220, 254, 255);
                    MainColor = Mod.Assets.Request<Texture2D>("Projectiles/颜色3");
                    break;
                default:
                    c = new Color(255, 160, 239);
                    break;
            }

            Vector2 Center = player.MountedCenter + (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
            if (player.direction < 0)
                Center = player.MountedCenter - (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
            float length = 15;
            c.A = 200;
            Vector2 scale3 = new Vector2(0.6f, 10f) * 渐变2;
            if (Projectile.timeLeft < 30)
            {
                scale2 *= 渐变3;
                scale3 *= 渐变3;
            }
            Color color2 = c;
            color2.A = 100;
            for (int i = 0; i < length; i++)
            {
                float scalezoom = MathHelper.Lerp(0, 1.5f, i / length);
                float colorzoom = MathHelper.Lerp(1, 0, i / length);
                float scalezoom2 = MathHelper.Lerp(0.5f, 0.0f, (i / length));
                Vector2 scale4 = scale3;
                Main.spriteBatch.Draw(texture3.Value, Center - Main.screenPosition + new Vector2(i * 3, 0), new Rectangle(0, 0, texture3.Width(), texture3.Height()),
                color2, 3.1415f / 2f, texture3.Size() * 0.5f, scale4 * scalezoom2, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texture3.Value, Center - Main.screenPosition + new Vector2(-i * 3, 0), new Rectangle(0, 0, texture3.Width(), texture3.Height()),
                color2, 3.1415f / 2f, texture3.Size() * 0.5f, scale4 * scalezoom2, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texture2.Value, Center - Main.screenPosition, new Rectangle(0, 0, texture2.Width(), texture2.Height()),
                c * colorzoom, 0, origin2, scale2 * scalezoom, SpriteEffects.None, 0f);

            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            //var player = Main.player[Projectile.owner];
            Vector2[] 坐标组;
            float 长度 = Projectile.ai[0];
            if (Projectile.ai[0] > 30)
            {
                长度 = 100;
                坐标组 = smooth(Projectile.oldPos, (int)长度);
            }
            else
                坐标组 = Projectile.oldPos;


            //Effect DefaultEffect2 = player.GetModPlayer<ElainaModplayer>().DefaultEffect;
            Effect DefaultEffect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/tail").Value;



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
                //if (DefaultEffect != null)
                {
                    DefaultEffect.Parameters["uTransform"].SetValue(model * projection);
                    DefaultEffect.Parameters["uTime"].SetValue(-(float)Main.GameUpdateCount * 0.03f);
                    Main.graphics.GraphicsDevice.Textures[0] = MainColor.Value;
                    Main.graphics.GraphicsDevice.Textures[1] = MainShape.Value;
                    Main.graphics.GraphicsDevice.Textures[2] = MaskColor.Value;

                    Main.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.PointWrap;
                    Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.PointWrap;

                    DefaultEffect.CurrentTechnique.Passes[0].Apply();


                    Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0, triangleList.Count / 3);

                    Main.graphics.GraphicsDevice.RasterizerState = originalState;
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
            //overWiresUI.Add(index);

        }

        public void 画切线(Vector2 position, float rotation, float length, float 渐变率)
        {
            rotation = rotation + (float)Math.PI / 4;
            Vector2 move = Vector2.One;
            move = move.RotatedBy(rotation);
            Vector2 move2 = move;
            move2 = move2.RotatedBy(Math.PI / 2);
            int 纹路坐标X = (int)MathHelper.Lerp((int)texture.Size().X, 0f, 渐变率);
            if (length > 0)
            {
                for (int i = 0; i < length / 2f; i++)
                {
                    Color c = new(177, 254, 254, 255);
                    int 纹路坐标Y位移 = (int)MathHelper.Lerp(0f, (int)texture.Size().Y / 2, i / (length / 2));
                    //c *= 渐变率;
                    //c.A *= (byte)MathHelper.Lerp(0,1,length / (length / 2f));
                    Color blue = new(77, 168, 253, 100);
                    Color flame = new(255, 104, 98, 255);
                    Color white = new(225, 255, 255, 255);
                    flame.A = (byte)MathHelper.Lerp(100, 0, i / (length / 2f));
                    c = Color.Lerp(white, flame, i / (length / 2f));// (float)(Math.Sqrt(i) / Math.Sqrt((length / 2f))));
                    int 纹路坐标Ya = (int)texture.Size().Y / 2 - 纹路坐标Y位移;
                    int 纹路坐标Yb = (int)texture.Size().Y / 2 + 纹路坐标Y位移;
                    Main.EntitySpriteDraw(texture.Value, position - Main.screenPosition + i * move,
                        new Rectangle(纹路坐标X, 纹路坐标Ya, 1, 1),
                            flame, rotation, new Vector2(1, 1), 2 * 渐变率, SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(texture.Value, position - Main.screenPosition - i * move,
                        new Rectangle(纹路坐标X, 纹路坐标Yb, 1, 1),
                           flame, rotation, new Vector2(1, 1), 2 * 渐变率, SpriteEffects.None, 0);
                }
            }

        }
        public override void OnKill(int timeLeft)
        {
            Main.player[Projectile.owner].GetModPlayer<ElainaModplayer>().武器流光 = false;
        }
        public override void AI()
        {

            Projectile.ai[0]++;
            var player = Main.player[Projectile.owner];
            if (!player.GetModPlayer<ElainaModplayer>().Elaina)
                player.GetModPlayer<ElainaModplayer>().Elaina = true;
            //if(player.itemTime>0) 

            texture = Mod.Assets.Request<Texture2D>("Projectiles/拖尾");
            if (player.itemTime > 0 || player.GetModPlayer<ElainaModplayer>().onLight)
            {
                Projectile.timeLeft = Main.player[Projectile.owner].itemTime + 30;
                if (oldposition == null)
                    oldposition = new Vector2[20];
                Projectile.Center = player.MountedCenter + (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
                if (player.direction < 0)
                    Projectile.Center = player.MountedCenter - (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
            }
            else
            {
                //Projectile.Kill();
            }
            {
            }
            if (player.dead)
                Projectile.Kill();

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
