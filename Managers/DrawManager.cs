using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace 伊蕾娜.Managers
{
    internal class DrawManager : ModSystem
    {
        static Effect 顶点绘制;
        static Effect 上色;
        static Effect 扰动;
        static Effect 消融;

        static Effect Sphere;
        public static Asset<Texture2D> Perlin;
        public static Asset<Texture2D> PerlinX;

        public override void Load()
        {
            顶点绘制 = Mod.Assets.Request<Effect>("Effects/Content/顶点绘制", AssetRequestMode.ImmediateLoad).Value;
            上色 = Mod.Assets.Request<Effect>("Effects/Content/上色", AssetRequestMode.ImmediateLoad).Value;
            扰动 = Mod.Assets.Request<Effect>("Effects/Content/扰动", AssetRequestMode.ImmediateLoad).Value;
            消融 = Mod.Assets.Request<Effect>("Effects/Content/消融", AssetRequestMode.ImmediateLoad).Value;
            Sphere = Mod.Assets.Request<Effect>("Effects/Content/SpherePerspective", AssetRequestMode.ImmediateLoad).Value;


            Perlin = Mod.Assets.Request<Texture2D>("Effects/Tex/Perlin", AssetRequestMode.ImmediateLoad);
            PerlinX = Mod.Assets.Request<Texture2D>("Effects/Tex/PerlinX", AssetRequestMode.ImmediateLoad);

            base.Load();
        }
        public struct SlashTrailInfo
        {
            public Vector2 startPosition;
            public Vector2[] 坐标组;
            public float Mid = 0.5f;

            public float StartWidth = 5f;
            public float MidWidth = 5f;
            public float EndWidth = 5f;

            public float StartAlpha = 1f;
            public float MidAlpha = 0.5f;
            public float EndAlpha = 0f;

            public Color StartColor = Color.White;
            public Color MidColor = Color.White;
            public Color EndColor = Color.White;

            public SamplerState SamplerState = SamplerState.LinearClamp;
            public Vector2 ImageScale = Vector2.One;
            public SlashTrailInfo(Vector2 startPosition, Vector2[] 坐标组)
            {
                this.startPosition = startPosition;
                this.坐标组 = 坐标组;
            }

            public SlashTrailInfo(Vector2 startPosition, Vector2[] 坐标组, float Mid,
            float StartWidth, float MidWidth, float EndWidth,
            float StartAlpha, float MidAlpha, float EndAlpha,
            Color startColor, Color midColor, Color endColor,
            Vector2 ImageScale, SamplerState samplerState)
            {
                this.startPosition = startPosition;
                this.坐标组 = 坐标组;
                this.Mid = Mid;

                this.StartWidth = StartWidth;
                this.MidWidth = MidWidth;
                this.EndWidth = EndWidth;

                this.StartAlpha = StartAlpha;
                this.MidAlpha = MidAlpha;
                this.EndAlpha = EndAlpha;

                this.StartColor = startColor;
                this.MidColor = midColor;
                this.EndColor = endColor;

                this.ImageScale = ImageScale;//横向放大图片，达到循环的效果
                this.SamplerState = samplerState;
            }
        }
        //刀光类型的拖尾，以玩家为原点绘制
        public static void 顶点绘制shader(Texture2D MainTex, SlashTrailInfo TrailInfo, int 绘制次数 = 1, int 绘制模式 = 0, float uTimeX = 0, float uTimeY = 0)
        {
            bool drawPoint = false;
            // 把所有的点都生成出来，按照顺序
            List<CustomVertexInfo> bars = new List<CustomVertexInfo>();
            List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();


            for (int i = 0; i < TrailInfo.坐标组.Length - 1; i++)
            {
                //if (坐标组[i] == Vector2.Zero) break;
                if (drawPoint)
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, TrailInfo.startPosition + TrailInfo.坐标组[i] - Main.screenPosition,
                    new Rectangle(0, 0, 1, 1), Color.White, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);

                float progress = (float)i / TrailInfo.坐标组.Length;
                float progress1 = Math.Clamp(i / (TrailInfo.坐标组.Length * TrailInfo.Mid), 0, 1);
                float progress2 = (Math.Clamp(i - (TrailInfo.坐标组.Length * TrailInfo.Mid), 0f, TrailInfo.坐标组.Length) / ((float)TrailInfo.坐标组.Length * (1f - TrailInfo.Mid)));
                //Main.NewText(progress2);


                float width = width = MathHelper.Lerp(TrailInfo.MidWidth, TrailInfo.EndWidth, progress2);
                if (progress <= TrailInfo.Mid) width = MathHelper.Lerp(TrailInfo.StartWidth, TrailInfo.MidWidth, progress1);

                var normalDir = TrailInfo.坐标组[i] - TrailInfo.坐标组[i + 1];
                //if (normalDir == Vector2.Zero) break;
                if (i > 0 && TrailInfo.坐标组[i + 1] == Vector2.Zero)
                {
                    normalDir = -(TrailInfo.坐标组[i - 1] - TrailInfo.坐标组[i]);
                }

                {
                    normalDir = Vector2.Normalize(new Vector2(-normalDir.Y, normalDir.X));
                    var factor = i / (float)TrailInfo.坐标组.Length * TrailInfo.ImageScale.X; //(float)坐标组.Length;
                    //var color = new Color(255, 123, 35, 255);//Color.Lerp(Color.White,Color.Red , factor);//Projectile.GetFairyQueenWeaponsColor(0f)//从头部到尾部渐变颜色
                    var w = MathHelper.Lerp(TrailInfo.StartAlpha, TrailInfo.MidAlpha, progress1);//从头部到尾部越来越透明
                    if (progress >= TrailInfo.Mid) w = MathHelper.Lerp(TrailInfo.MidAlpha, TrailInfo.EndAlpha, progress2);

                    Color color = Color.Lerp(TrailInfo.StartColor, TrailInfo.MidColor, progress1);
                    if (progress >= TrailInfo.Mid) color = Color.Lerp(TrailInfo.MidColor, TrailInfo.EndColor, progress2);

                    var trans = Main.GameViewMatrix != null ? Main.GameViewMatrix.TransformationMatrix : Matrix.Identity;
                    bars.Add(new CustomVertexInfo(TrailInfo.startPosition + TrailInfo.坐标组[i] + normalDir * width * trans.M11, color * w, new Vector3(factor, 0.5f+(0.5f/TrailInfo.ImageScale.Y), w)));//最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
                    bars.Add(new CustomVertexInfo(TrailInfo.startPosition + TrailInfo.坐标组[i] + normalDir * -width * trans.M11, color * w, new Vector3(factor, 0.5f - (0.5f / TrailInfo.ImageScale.Y), w))); //(float)Math.Sqrt(factor)
                }

            }

            if (bars.Count > 2)
            {

                // 按照顺序连接三角形
                triangleList.Add(bars[0]);

                var vertex = new CustomVertexInfo((bars[0].Position + bars[1].Position) * 0.5f, TrailInfo.StartColor,
                    new Vector3(0, 0.5f, 1));
                triangleList.Add(bars[1]);
                triangleList.Add(vertex);
                for (int i = 0; i < bars.Count - 4; i += 2)
                {
                    triangleList.Add(bars[i]);
                    triangleList.Add(bars[i + 2]);
                    triangleList.Add(bars[i + 1]);

                    triangleList.Add(bars[i + 1]);
                    triangleList.Add(bars[i + 2]);
                    triangleList.Add(bars[i + 3]);
                }
                BlendState bs = BlendState.AlphaBlend;
                if (绘制模式 == 1) bs = BlendState.Additive;
                if (绘制模式 == 2) bs = BlendState.NonPremultiplied;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, bs, TrailInfo.SamplerState, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
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
                顶点绘制.Parameters["uTransform"].SetValue(model * projection);
                顶点绘制.Parameters["uTime"].SetValue(new Vector2(uTimeX, uTimeY));
                Main.graphics.GraphicsDevice.Textures[0] = MainTex;
                顶点绘制.CurrentTechnique.Passes[0].Apply();


                for (int i = 0; i < 绘制次数; i++)
                    Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0, triangleList.Count / 3);


                Main.graphics.GraphicsDevice.RasterizerState = originalState;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }
        public static void 保存主屏幕()
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;
            sb.End();

            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null);
            gd.SetRenderTarget(Main.screenTargetSwap);//在这个上面绘制一遍原图，相当于“保存”
            gd.Clear(Color.Transparent);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
        }
        public static void 切换屏幕(RenderTarget2D render,bool AdjustScreen = true)
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, AdjustScreen? Main.GameViewMatrix.ZoomMatrix: Matrix.Identity);

            gd.SetRenderTarget(render);//在这个上面绘制一遍原图，相当于“保存”
            gd.Clear(Color.Transparent);
        }

        public static void 上色shader(Color color1,Color color2,bool smooth)
        {
            上色.Parameters["color1"].SetValue(color1.ToVector4());
            上色.Parameters["color2"].SetValue(color2.ToVector4());
            上色.Parameters["smooth"].SetValue(smooth);

            上色.CurrentTechnique.Passes["Apply1"].Apply();//开启shader
        }
        public static void 扰动shader(float strength, float uTime = 0, Texture2D noiseTex = null, bool 消融 = false, float effect = 1, Texture2D dissolveTex = null)
        {
            Texture2D tex = Perlin.Value;
            Texture2D tex2 = Perlin.Value;

            effect = MathHelper.Clamp(effect, 0, 1);

            if (noiseTex != null) tex = noiseTex;
            if (dissolveTex != null) tex2 = dissolveTex;
            扰动.Parameters["strength"].SetValue(strength);
            扰动.Parameters["tex0"].SetValue(tex);
            扰动.Parameters["uTimex"].SetValue(uTime);
            扰动.Parameters["uTimey"].SetValue(uTime);

            if (消融)
            {
                扰动.Parameters["tex1"].SetValue(tex2);
                扰动.Parameters["dissolveFactor"].SetValue(1 - effect);
                扰动.CurrentTechnique.Passes["moveAndDistort"].Apply();

            }
            else 扰动.CurrentTechnique.Passes["move"].Apply();
        }
        #region 顶点结构体
        private struct CustomVertexInfo : IVertexType
        {
            private static VertexDeclaration _vertexDeclaration = new VertexDeclaration(new VertexElement[3]
            {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
            });
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
        #endregion

        public static void 顶点绘制shader(Texture2D MainTex, Vector2[] 坐标组, Color color, float MaxWidth = 5f,float MinWidth = 0f, float startAlpha = 1f, float endAlpha = 0.1f, int 绘制次数 = 1,int 绘制模式 = 0,float uTimeX = 0)
        {
            bool drawPoint = false;
            // 把所有的点都生成出来，按照顺序
            List<CustomVertexInfo> bars = new List<CustomVertexInfo>();
            List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();
            int length = 0;
            for (int i = 0; i < 坐标组.Length; i++)
            {
                if (坐标组[i] == Vector2.Zero) continue;
                length++;
            }
            if (length <= 1) return;
            Vector2[]坐标组2 = new Vector2[length];
            length = 0;

            for (int i = 0; i < 坐标组.Length; i++)
            {
                if (坐标组[i] == Vector2.Zero) continue;
                坐标组2[length++] = 坐标组[i];
            }
            for (int i = 0; i < 坐标组2.Length-1; i++)
            {
                if (drawPoint)
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, 坐标组2[i] - Main.screenPosition,
                    new Rectangle(0, 0, 1, 1), Color.White, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);

                float width = MathHelper.Lerp(MaxWidth, MinWidth, (float)i / 坐标组2.Length);

                var normalDir = 坐标组2[i] - 坐标组2[i + 1];
                if (normalDir == Vector2.Zero) continue;
                if (坐标组2[i + 1] == Vector2.Zero)
                {
                    continue;
                }

                {
                    normalDir = Vector2.Normalize(new Vector2(-normalDir.Y, normalDir.X));
                    var factor = i / (float)坐标组2.Length; //(float)坐标组.Length;
                    //var color = new Color(255, 123, 35, 255);//Color.Lerp(Color.White,Color.Red , factor);//Projectile.GetFairyQueenWeaponsColor(0f)//从头部到尾部渐变颜色
                    var w = MathHelper.Lerp(startAlpha, endAlpha, factor);//从头部到尾部越来越透明
                    var trans = Main.GameViewMatrix != null ? Main.GameViewMatrix.TransformationMatrix : Matrix.Identity;
                    bars.Add(new CustomVertexInfo(坐标组2[i] + normalDir * width * trans.M11, color * w, new Vector3(factor+ uTimeX, 1, w)));//最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
                    bars.Add(new CustomVertexInfo(坐标组2[i] + normalDir * -width * trans.M11, color * w, new Vector3(factor+ uTimeX, 0, w))); //(float)Math.Sqrt(factor)
                }

            }

            if (bars.Count > 2)
            {

                // 按照顺序连接三角形
                triangleList.Add(bars[0]);

                var vertex = new CustomVertexInfo((bars[0].Position + bars[1].Position) * 0.5f, Color.White,
                    new Vector3(0, 0.5f, 1));
                triangleList.Add(bars[1]);
                triangleList.Add(vertex);
                for (int i = 0; i < bars.Count - 4; i += 2)
                {
                    triangleList.Add(bars[i]);
                    triangleList.Add(bars[i + 2]);
                    triangleList.Add(bars[i + 1]);

                    triangleList.Add(bars[i + 1]);
                    triangleList.Add(bars[i + 2]);
                    triangleList.Add(bars[i + 3]);
                }
                BlendState bs = BlendState.AlphaBlend;
                if (绘制模式 == 1) bs = BlendState.Additive;
                if (绘制模式 == 2) bs = BlendState.NonPremultiplied;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, bs, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
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
                顶点绘制.Parameters["uTransform"].SetValue(model * projection);
                顶点绘制.Parameters["uTime"].SetValue(-(float)Main.GameUpdateCount * 0.03f);
                Main.graphics.GraphicsDevice.Textures[0] = MainTex;
                顶点绘制.CurrentTechnique.Passes[0].Apply();


                for (int i = 0; i < 绘制次数; i++)
                    Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0, triangleList.Count / 3);


                Main.graphics.GraphicsDevice.RasterizerState = originalState;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }

        public static void 圆环顶点绘制shader(Texture2D MainTex, Vector2 center, Vector2[] 坐标组, float rotation, Color color, Color color2, float width = 5f, float alpha = 1f, int 绘制次数 = 1, int blendState = 0, float uTimeX = 0,float uTimeY = 0)
        {
            bool drawPoint = false;
            // 把所有的点都生成出来，按照顺序
            List<CustomVertexInfo> bars = new List<CustomVertexInfo>();
            List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            for (int i = 0; i < 坐标组.Length - 2; i++)
            {
                Color c = color;
                if (坐标组[i].Y < center.Y) c = color2;
                if (坐标组[i] == Vector2.Zero) continue;

                var normalDir = new Vector2(0, 1);

                int MiddlePos = 坐标组.Length / 2;
                int Lenth = Math.Abs(MiddlePos - i);
                float LenthEffect = MathHelper.Lerp(1, 0, Lenth / (float)MiddlePos);
                //normalDir *= MathHelper.Lerp(1, 0, Lenth / (float)MiddlePos);
                {
                    var factor = i / (float)坐标组.Length; //(float)坐标组.Length;
                    //var color = new Color(255, 123, 35, 255);//Color.Lerp(Color.White,Color.Red , factor);//Projectile.GetFairyQueenWeaponsColor(0f)//从头部到尾部渐变颜色
                    var w = MathHelper.Lerp(1f, 1f, factor);//从头部到尾部越来越透明
                    var trans = Main.GameViewMatrix != null ? Main.GameViewMatrix.TransformationMatrix : Matrix.Identity;

                    Vector2 UpPos = 坐标组[i] + normalDir * width / 2f * LenthEffect - center;//坐标组[i] - normalDir * width / 2f - center;
                    Vector2 DownPos = 坐标组[i] - normalDir * width / 2f * LenthEffect - center;// 坐标组[i] + normalDir * width / 2f - center;

                    UpPos = UpPos.RotatedBy(rotation);
                    DownPos = DownPos.RotatedBy(rotation);

                    if (drawPoint)
                    {
                        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, center + UpPos - Main.screenPosition,
                        new Rectangle(0, 0, 1, 1), c, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, center + DownPos - Main.screenPosition,
                        new Rectangle(0, 0, 1, 1), c, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);

                        {
                            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, 坐标组[i] - Main.screenPosition,
                            new Rectangle(0, 0, 1, 1), Color.White, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);

                            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, 坐标组[i] + new Vector2(0, 1) * width / 2f * MathHelper.Lerp(1, 0, Lenth / (float)MiddlePos) - Main.screenPosition,
                            new Rectangle(0, 0, 1, 1), c, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);
                            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, 坐标组[i] - new Vector2(0, 1) * width / 2f * MathHelper.Lerp(1, 0, Lenth / (float)MiddlePos) - Main.screenPosition,
                            new Rectangle(0, 0, 1, 1), c, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);
                        }
                    }
                    //else
                    {
                        bars.Add(new CustomVertexInfo(center + UpPos, c * w * alpha, new Vector3(factor, 1, w)));//最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
                        bars.Add(new CustomVertexInfo(center + DownPos, c * w * alpha, new Vector3(factor, 0, w))); //(float)Math.Sqrt(factor)
                    }
                    //bars.Add(new CustomVertexInfo(坐标组[i] + normalDir * width / 2f * trans.M11, c * w * alpha, new Vector3(factor, 0, w))); //(float)Math.Sqrt(factor)

                }

            }

            if (bars.Count > 2)
            {

                // 按照顺序连接三角形
                triangleList.Add(bars[0]);

                var vertex = new CustomVertexInfo((bars[0].Position + bars[1].Position) * 0.5f, Color.White,
                    new Vector3(0, 0.5f, 1));
                triangleList.Add(bars[1]);
                triangleList.Add(vertex);
                for (int i = 0; i < bars.Count - 4; i += 2)
                {
                    triangleList.Add(bars[i]);
                    triangleList.Add(bars[i + 2]);
                    triangleList.Add(bars[i + 1]);

                    triangleList.Add(bars[i + 1]);
                    triangleList.Add(bars[i + 2]);
                    triangleList.Add(bars[i + 3]);
                }
                BlendState bs = BlendState.AlphaBlend;
                if (blendState == 1) bs = BlendState.Additive;
                else if (blendState == 2) bs = BlendState.NonPremultiplied;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, bs, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
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
                顶点绘制.Parameters["uTransform"].SetValue(model * projection);
                顶点绘制.Parameters["uTime"].SetValue(-(float)Main.GameUpdateCount * 0.03f);
                Main.graphics.GraphicsDevice.Textures[0] = MainTex;
                顶点绘制.CurrentTechnique.Passes[0].Apply();


                for (int i = 0; i < 绘制次数; i++)
                    Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0, triangleList.Count / 3);


                Main.graphics.GraphicsDevice.RasterizerState = originalState;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }
    

        public static void 任意矩形shader(Texture2D MainTex,Vector2[][] 坐标组, float rotation, Color color, Color color2, float width = 5f, float alpha = 1f, int 绘制次数 = 1, int blendState = 0, float uTimeX = 0, float uTimeY = 0)
        {
            bool drawPoint = false;
            // 把所有的点都生成出来，按照顺序
            List<CustomVertexInfo> bars = new List<CustomVertexInfo>();
            List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            for(int i=0;i< 坐标组.Length; i++)
            {
                for(int j =0;j< 坐标组[i].Length; j++)
                {
                    Vector2[] nowArray = 坐标组[i];
                    if (nowArray[j] == Vector2.Zero) continue;
                    Color c = Color.White;
                    if (drawPoint)
                    {
                        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, nowArray[j] - Main.screenPosition,
                        new Rectangle(0, 0, 1, 1), c, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);
                    }
                    float factor = MathHelper.Lerp(0, 1, j / (float)nowArray.Length)+ uTimeX;
                    float factor2 = MathHelper.Lerp(0, 1, i / (float)坐标组.Length)+uTimeY;
                    bars.Add(new CustomVertexInfo(nowArray[j], c  * alpha, new Vector3(factor, factor2, 1)));//最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
                }
            }

            if (bars.Count > 2)
            {
                for (int i = 0; i < 坐标组.Length-1; i++)  // 29 因为每个正方形需要访问 vec[i+1]
                {
                    for (int j = 0; j < 坐标组[i].Length-1; j++)
                    {
                        int n = 坐标组.Length;
                        int topLeft = i * n + j;        // 左上
                        int topRight = i * n + j + 1;   // 右上
                        int bottomLeft = (i + 1) * n + j;   // 左下
                        int bottomRight = (i + 1) * n + j + 1;
                        // 第一个三角形 (左上, 右上, 左下)
                        triangleList.Add(bars[topLeft]);
                        triangleList.Add(bars[topRight]);
                        triangleList.Add(bars[bottomLeft]);

                        // 第二个三角形 (右上, 右下, 左下)
                        triangleList.Add(bars[topRight]);
                        triangleList.Add(bars[bottomRight]);
                        triangleList.Add(bars[bottomLeft]);
                    }
                }



                BlendState bs = BlendState.AlphaBlend;
                if (blendState == 1) bs = BlendState.Additive;
                else if (blendState == 2) bs = BlendState.NonPremultiplied;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, bs, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
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
                顶点绘制.Parameters["uTransform"].SetValue(model * projection);
                顶点绘制.Parameters["uTime"].SetValue(-(float)Main.GameUpdateCount * 0.03f);
                Main.graphics.GraphicsDevice.Textures[0] = MainTex;
                顶点绘制.CurrentTechnique.Passes[0].Apply();


                for (int i = 0; i < 绘制次数; i++)
                    Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0, triangleList.Count / 3);


                Main.graphics.GraphicsDevice.RasterizerState = originalState;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }
        public static void 绘制球体(Texture2D MainTex, Vector2 Center,  float radius, Color color, float alpha = 1f, int 绘制次数 = 1, int blendState = 0, float uTimeX = 0,float uTimeY=0,float disStrength = 0,Vector2?扰动偏移 = null,float 消融 = 0,float 消融边缘 = 0,Texture2D disTex=null, Vector2? 消融偏移 = null)
        {
            List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();
            triangleList.Add(new CustomVertexInfo((Center - new Vector2(radius, radius)), color, new Vector3(-1, 1, alpha)));
            triangleList.Add(new CustomVertexInfo((Center - new Vector2(radius, -radius)), color, new Vector3(-1, -1, alpha)));
            triangleList.Add(new CustomVertexInfo((Center - new Vector2(-radius, -radius)), color, new Vector3(1, -1, alpha)));

            triangleList.Add(new CustomVertexInfo((Center - new Vector2(radius, radius)), color, new Vector3(-1, 1, alpha)));
            triangleList.Add(new CustomVertexInfo((Center - new Vector2(-radius, -radius)), color, new Vector3(1, -1, alpha)));
            triangleList.Add(new CustomVertexInfo((Center - new Vector2(-radius, radius)), color, new Vector3(1, 1, alpha)));
            
            RasterizerState originalState = Main.graphics.GraphicsDevice.RasterizerState;
            // 干掉注释掉就可以只显示三角形栅格
            RasterizerState rasterizerState = new RasterizerState();
            //rasterizerState.CullMode = CullMode.None;
            //rasterizerState.FillMode = FillMode.WireFrame;
            //Main.graphics.GraphicsDevice.RasterizerState = rasterizerState;

            var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
            var model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0)) ;
            if (扰动偏移 == null) 扰动偏移 = Vector2.Zero;
            Vector2 move =(Vector2)扰动偏移;
            if(消融偏移==null) 消融偏移 = Vector2.Zero;
            Vector2 move2 = (Vector2)(消融偏移);
            // 把变换和所需信息丢给shader
            Sphere.Parameters["uTransform"].SetValue(model * projection);
            Sphere.Parameters["circleCenter"].SetValue(new Vector3(0, 0, -2));
            Sphere.Parameters["radiusOfCircle"].SetValue(1f);
            Sphere.Parameters["uTime"].SetValue(new Vector2(uTimeX, uTimeY));
            Sphere.Parameters["strength"].SetValue(disStrength);
            Sphere.Parameters["uTime2"].SetValue(move);
            Sphere.Parameters["dissolveTime"].SetValue(消融);
            Sphere.Parameters["dissolveEdge"].SetValue(消融边缘);
            Sphere.Parameters["uTime3"].SetValue(move2);

            if (disTex == null) disTex = PerlinX.Value;

            Main.graphics.GraphicsDevice.Textures[0] = MainTex;

            Main.graphics.GraphicsDevice.Textures[1] = PerlinX.Value;
            Main.graphics.GraphicsDevice.Textures[2] = disTex;

            Main.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            Sphere.CurrentTechnique.Passes[0].Apply();

            for (int i = 0; i < 绘制次数; i++)
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0, triangleList.Count / 3);
        }

        public static void 快捷扰动(float strength,Vector2 move)
        {
            扰动.Parameters["strength"].SetValue(strength);
            扰动.Parameters["tex0"].SetValue(PerlinX.Value);
            扰动.Parameters["uTimex"].SetValue(move.X);
            扰动.Parameters["uTimey"].SetValue(move.Y);
            扰动.CurrentTechnique.Passes[1].Apply();

        }
        public static void 消融shader(float dissolveFactor, Vector2? uTime=null,Texture2D shape=null,
            bool useMask = false,Texture2D mask=null,
            Color? lineColor=null,float lineWidth=0)
        {
            if (uTime == null) uTime = Vector2.Zero;
            if(lineColor==null)lineColor = Color.White;
            if (shape == null) shape = PerlinX.Value;
            if(mask==null) mask = PerlinX.Value;

            消融.Parameters["dissolveFactor"].SetValue(dissolveFactor);
            消融.Parameters["uTime"].SetValue((Vector2)uTime);

            消融.Parameters["useMask"].SetValue(useMask);

            消融.Parameters["lineColor"].SetValue(((Color)lineColor).ToVector4());
            消融.Parameters["lineWidth"].SetValue(lineWidth);

            Main.graphics.GraphicsDevice.Textures[1] = shape;
            Main.graphics.GraphicsDevice.Textures[2] = mask;

            消融.CurrentTechnique.Passes[0].Apply();

        }
        public static void 球体shader(Texture2D MainTex, Vector2[][] 坐标组, float rotation, Color color, Color color2, float width = 5f, float alpha = 1f, int 绘制次数 = 1, int blendState = 0, float uTimeX = 0, float uTimeY = 0)
        {
            bool drawPoint = false;
            // 把所有的点都生成出来，按照顺序
            List<CustomVertexInfo> bars = new List<CustomVertexInfo>();
            List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            for (int i = 0; i < 坐标组.Length; i++)
            {
                for (int j = 0; j < 坐标组[i].Length; j++)
                {
                    Vector2[] nowArray = 坐标组[i];
                    if (nowArray[j] == Vector2.Zero) continue;
                    Color c = color;
                    if (drawPoint)
                    {
                        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, nowArray[j] - Main.screenPosition,
                        new Rectangle(0, 0, 1, 1), c, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);
                    }
                    float factor = MathHelper.Lerp(0, 1, j / (float)nowArray.Length) + uTimeX;
                    float factor2 = MathHelper.Lerp(0, 1, i / (float)坐标组.Length) + uTimeY;
                    bars.Add(new CustomVertexInfo(nowArray[j], c * alpha, new Vector3(factor, factor2, 1)));//最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
                }
            }

            if (bars.Count > 2)
            {
                for (int i = 0; i < 坐标组.Length - 1; i++)  // 29 因为每个正方形需要访问 vec[i+1]
                {
                    for (int j = 0; j < 坐标组[i].Length - 1; j++)
                    {
                        int n = 坐标组.Length;
                        int topLeft = i * n + j;        // 左上
                        int topRight = i * n + j + 1;   // 右上
                        int bottomLeft = (i + 1) * n + j;   // 左下
                        int bottomRight = (i + 1) * n + j + 1;
                        // 第一个三角形 (左上, 右上, 左下)
                        triangleList.Add(bars[topLeft]);
                        triangleList.Add(bars[topRight]);
                        triangleList.Add(bars[bottomLeft]);

                        // 第二个三角形 (右上, 右下, 左下)
                        triangleList.Add(bars[topRight]);
                        triangleList.Add(bars[bottomRight]);
                        triangleList.Add(bars[bottomLeft]);
                    }
                }
                BlendState bs = BlendState.AlphaBlend;
                if (blendState == 1) bs = BlendState.Additive;
                else if (blendState == 2) bs = BlendState.NonPremultiplied;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, bs, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
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
                顶点绘制.Parameters["uTransform"].SetValue(model * projection);
                顶点绘制.Parameters["uTime"].SetValue(-(float)Main.GameUpdateCount * 0.03f);
                Main.graphics.GraphicsDevice.Textures[0] = MainTex;
                顶点绘制.CurrentTechnique.Passes[0].Apply();


                for (int i = 0; i < 绘制次数; i++)
                    Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0, triangleList.Count / 3);


                Main.graphics.GraphicsDevice.RasterizerState = originalState;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }

        public static float FrameTime(float 最小值, float 最大值, int 所需时间)
        {
            float divide = 所需时间 / 最大值;
            float result = (Main.GameUpdateCount % 所需时间);
            if (result > 所需时间 / 2f)
            {
                result = 所需时间 - result;
            }
            //Main.NewText(result);
            float result2 = MathHelper.Lerp(最小值,最大值,result/(所需时间 / 2f));
            return result2;

        }
        public static float FrameTime(float 最小值, float 最大值, int 所需时间,int 判定时间)
        {
            float divide = 所需时间 / 最大值;
            float result = (判定时间 % 所需时间);
            if (result > 所需时间 / 2f)
            {
                result = 所需时间 - result;
            }
            //Main.NewText(result);
            float result2 = MathHelper.Lerp(最小值, 最大值, result / (所需时间 / 2f));
            return result2;

        }
    }
}
