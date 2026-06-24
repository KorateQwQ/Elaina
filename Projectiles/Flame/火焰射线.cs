using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.Managers;

namespace 伊蕾娜.Projectiles.Flame
{
    public class 火焰射线 : KLProjectile
    {
        private const float MaxDistance = 1700f;
        static Effect DefaultEffect;
        static Asset<Texture2D> texture;
        static Asset<Texture2D> texture3;
        static Asset<Texture2D> MainColor;
        static Asset<Texture2D> MainShape;
        static Asset<Texture2D> MaskColor;
        static Asset<Texture2D> noisetexture;

        static Asset<Texture2D> newFlameTex;

        static Effect 两端虚化shader;
        static Effect 扰动shader;

        Vector2 towards = Vector2.Zero;
        public float maxDistance = MaxDistance;
        public int 启动 = 13;
        public float 生成火焰间隔 = 3f;
        public 粒子[] 粒子组 = new 粒子[60];


        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(towards);

        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            towards = reader.ReadVector2();
        }
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
            //DisplayName.SetDefault("火焰射线");
        }
        public override void SetDefaults()
        {
            if (DefaultEffect == null)
                DefaultEffect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/tail").Value;

            if (MainColor == null)
                MainColor = Mod.Assets.Request<Texture2D>("Projectiles/颜色");
            if (MainShape == null)
                MainShape = Mod.Assets.Request<Texture2D>("Projectiles/形状");
            if (MaskColor == null)
                MaskColor = Mod.Assets.Request<Texture2D>("Projectiles/渐变");

            if(newFlameTex==null)newFlameTex= Mod.Assets.Request<Texture2D>("Projectiles/光柱");

            Projectile.penetrate = -1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;//瓷砖碰撞
            Projectile.friendly = false;
            //projectile.timeLeft=30;
            //projectile.extraUpdates=1;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.damage = 100;
            Projectile.timeLeft = 40;
            Projectile.knockBack = 2;
            //Projectile.CritChance = (int)Main.player[Projectile.owner].GetTotalCritChance(DamageClass.Magic);
            //Projectile.extraUpdates = ;
            Projectile.alpha = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.hide = true;
        }

        public override void PostDraw(Color lightColor)//绘制手部光线
        {

        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }
        public override void OnKill(int timeLeft)
        {
            {
                SoundStyle flamesound = new("伊蕾娜/Projectiles/Flame/火焰射线释放音效");
                flamesound.SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest;
                flamesound.Volume = 0f;
                SoundEngine.PlaySound(flamesound);
            }
            {
                SoundStyle flamesound = new("伊蕾娜/Projectiles/Flame/射线施法音效");
                flamesound.SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest;
                flamesound.Volume = 0f;
                SoundEngine.PlaySound(flamesound);
            }
            Main.player[Projectile.owner].itemAnimation = 0;
            Main.player[Projectile.owner].itemTime = 0;
            if (Main.myPlayer == Projectile.owner)
            {
                Main.mouseLeftRelease = true;
            }

        }
        
        public override bool PreDraw(ref Color lightColor)
        {
            var player = Main.player[Projectile.owner];
            Effect DrawEffect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/Draw", AssetRequestMode.ImmediateLoad).Value;

            List<CustomVertexInfo> bars = new();
            List<CustomVertexInfo> triangleList = new();
            if (启动 <= 0)
            {
                Vector2 unit = towards;
                int amount = (int)(maxDistance+120);
                Vector2[] 坐标组 = new Vector2[amount];
                for (int i = 0;i< 坐标组.Length; i++)
                {
                    坐标组[i] = Projectile.Center + unit * (坐标组.Length-i-80);
                }
                DrawManager.顶点绘制shader(newFlameTex.Value, 坐标组,new Color(255,100,0,0), 150,50f,  1, 1, 3, 0, (Main.GameUpdateCount%20)/20f);
                DrawManager.顶点绘制shader(MaskColor.Value, 坐标组, new Color(255, 100, 0,0), 130* maxDistance / MaxDistance, 20f * maxDistance / MaxDistance, 1, 0.7f, 2, 0, (Main.GameUpdateCount % 20) / 20f);

                #region 不想看到的过去的构式
                if (启动>=-7)启动-=2;
                Projectile.ai[0] = -1;
                if (启动 < -7)
                {
                    SoundStyle flamesound = SoundID.Item34 with
                    {
                        Volume = 0.5f,
                        MaxInstances = 20,
                        SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
                    };//new SoundStyle("伊蕾娜/Projectiles/Flame/持续施法");
                      //flamesound.MaxInstances = 1;
                      //flamesound.SoundLimitBehavior = SoundLimitBehavior.IgnoreNew;
                    SoundEngine.PlaySound(flamesound);
                }
                Projectile.friendly = true;
                float 渐变参数 = 30 - (float)Main.GameUpdateCount % 60;//60到 -60
                渐变参数 = Math.Abs(渐变参数);
                float 渐变火焰大小 = MathHelper.Lerp(1f, 1.5f, 渐变参数 / 30);
                if (texture == null)
                {
                    texture = Mod.Assets.Request<Texture2D>("Projectiles/Flame/火焰射线2");
                }
                int step = 20;
                float alpha = 255 - Projectile.alpha;

                Vector2 origin = texture.Size() * 0.5f;//除3相当于以图片中心为position
                origin.Y /= 4;//动图还要除以帧数
                int frameHeight = texture.Height() / Main.projFrames[Projectile.type];//图片总高度除以帧数，得到每张图的高度
                int startY = frameHeight * Projectile.frame;//每一帧的起始坐标Y
                unit.Normalize();
                float 反转了 = 1f;
                if (Projectile.frame % 2 == 0)
                    反转了 = 0;
                float 火焰帧数 = Main.GameUpdateCount % 20f;
                //if (Main.GameUpdateCount % 300 == 0) 反转了++;
                for (int i = 0; i < maxDistance; i += step)
                {
                    {//顶点信息
                        float width = MathHelper.Lerp(20f, 35f, (float)i / 1200);

                        var normalDir = Projectile.Center - player.MountedCenter;
                        normalDir = Vector2.Normalize(new Vector2(-normalDir.Y, normalDir.X));
                        var factor = i / 800f; //(float)坐标组.Length;

                        factor *= MathHelper.Lerp(3, 6, 火焰帧数 / 20f);
                        var color = new Color(180, 255, 255, 255);//Color.Lerp(Color.White,Color.Red , factor);//Projectile.GetFairyQueenWeaponsColor(0f)//从头部到尾部渐变颜色
                                                                  //var w = MathHelper.Lerp(1f, 0.05f, factor);//从头部到尾部越来越透明
                        var trans = Main.GameViewMatrix != null ? Main.GameViewMatrix.TransformationMatrix : Matrix.Identity;

                        bars.Add(new CustomVertexInfo(Projectile.Center + unit * i + normalDir * width * trans.M11, color, new Vector3(factor, 反转了, 1)));//最后一项纹理坐标.从左到右factor,从上顶点到下顶点1,0
                        bars.Add(new CustomVertexInfo(Projectile.Center + unit * i + normalDir * -width * trans.M11, color, new Vector3(factor, 1 - 反转了, 1))); //(float)Math.Sqrt(factor)
                    }
                    //开始绘制激光本体
                    /*Vector2 scale = new(1, 0.1f);
                    scale.Y = MathHelper.Lerp(0.35f, 1f, i / 600f);

                    if (scale.Y > 1)
                        scale.Y = 1;
                    scale.Y *= 0.5f * 渐变火焰大小;
                    if (i < 20)
                    {
                        Main.spriteBatch.Draw(texture.Value, Projectile.Center + unit * i - Main.screenPosition, new Rectangle(i, startY, step, 44),
                        new Color(alpha, alpha, alpha), Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
                    }
                    else if (i < maxDistance - 20)
                    {
                        Main.spriteBatch.Draw(texture.Value, Projectile.Center + unit * i - Main.screenPosition, new Rectangle(38, startY, step, 44),
                        new Color(alpha, alpha, alpha), Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
                        //Main.spriteBatch.Draw(texture.Value, Projectile.Center + unit * i - Main.screenPosition, new Rectangle(38, startY, 1, 44),
                        //new Color(alpha, alpha, alpha), Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
                    }
                    else if (i >= maxDistance - 20)
                    {
                        //Main.NewText(1);
                        Main.spriteBatch.Draw(texture.Value, Projectile.Center + unit * i - Main.screenPosition,
                        new Rectangle(79 + i - (int)maxDistance, startY, 35, 44), new Color(alpha, alpha, alpha), Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
                    }*/
                }


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
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, DrawEffect, Main.GameViewMatrix.ZoomMatrix);
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
                        //Main.graphics.GraphicsDevice.Textures[0] = Main.magicPixel;
                        //Main.graphics.GraphicsDevice.Textures[1] = Main.magicPixel;
                        //Main.graphics.GraphicsDevice.Textures[2] = Main.magicPixel;

                        for (int i = 0; i < 1; i++)
                        {
                            DefaultEffect.CurrentTechnique.Passes[0].Apply();
                            Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0, triangleList.Count / 3);

                        }
                        Main.graphics.GraphicsDevice.RasterizerState = originalState;
                    }
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, DrawEffect, Main.GameViewMatrix.TransformationMatrix);
                if (两端虚化shader == null)
                    两端虚化shader = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/两端虚化", AssetRequestMode.ImmediateLoad).Value;
                if (noisetexture == null)
                    noisetexture = Mod.Assets.Request<Texture2D>("Projectiles/Perlin");
                if (扰动shader == null)
                    扰动shader = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/扰动", AssetRequestMode.ImmediateLoad).Value;


                //开始render
                #endregion
            }
            else
                Projectile.friendly = false;

            //Main.spriteBatch.Draw(texture.Value, Projectile.Center + unit * (maxDistance) - Main.screenPosition,
            //new Rectangle(79, startY, 35, 44), new Color(alpha, alpha, alpha), Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
            if(texture3==null)texture3 = Mod.Assets.Request<Texture2D>("Projectiles/Flame/FlameDust");
            绘制粒子(texture3);

            return false;
        }
        public void 创造粒子(Vector2 pos, Player player)
        {
            //if (Main.time % 2 == 0)
            {
                for (int i = 0; i < 60; i++)
                {
                    if (粒子组[i] == null)
                    {
                        粒子组[i] = new 粒子(pos - towards * 50, player);
                        break;
                    }
                    if (!粒子组[i].active)
                    {
                        粒子组[i] = new 粒子(pos - towards * 50, player);
                        break;
                    }
                }

            }
        }
        public void 绘制粒子(Asset<Texture2D> texture)
        {
            for (int i = 0; i < 60; i++)
            {
                if (粒子组[i] != null && 粒子组[i].active)
                {
                    粒子组[i].draw(texture);
                }
            }
        }
        public override void AI()
        {
            //Main.NewText(Projectile.ai[1] + " " + maxDistance);
            Player player = Main.player[Projectile.owner];
            //if (maxDistance < 17) Projectile.Kill();

            player.manaRegenDelay = 2;
            Lighting.AddLight(Projectile.Center - towards * 50, 2.3f, 1.1f, 4.9f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 6 == 0)
            {
                player.CheckMana(player.HeldItem, (int)(5 * player.manaCost), true);
                Projectile.frame += 1;
                if (启动 >= 0)
                    启动--;
                Projectile.frameCounter = 0;
            }
            player.manaRegenDelay = 5;

            if (Projectile.frame >= 4)
            {
                Projectile.frame = 0;
            }
            int damage = (int)(Projectile.damage * 0.3f); // ;


            if (Main.myPlayer == player.whoAmI)
            {
                Vector2 lastTowards = Main.MouseWorld - player.Center;
                lastTowards = lastTowards.SafeNormalize(lastTowards);
                
                if ( (towards-lastTowards).Length()>0.1f)
                {
                    towards = lastTowards;
                    Projectile.netUpdate = true;
                }

                {//检测瓷砖碰撞
                    float[] samples = new float[5];
                    Collision.LaserScan(Projectile.Center, towards, 15f, MaxDistance, samples);//长950，宽150的激光，v必须是速度的标准化
                    for (int i = 1; i < 5; i++)
                    {
                        if (Projectile.ai[1] > samples[i])
                        {
                            Projectile.ai[1] = samples[i];
                            Projectile.netUpdate = true;
                        }
                    }
                }
                {
                    生成火焰间隔++;
                    if (生成火焰间隔 > 3)
                        生成火焰间隔 = 0;
                    Vector2 随机矩形 = new(Main.rand.NextFloat(-18, 18), Main.rand.NextFloat(-18, 18));
                    for (Vector2 i = Vector2.Zero; i.Length() < Projectile.ai[1] + 300; i += towards * Vector2.One * 16)
                    {
                        if ((int)((Projectile.Center.X + i.X) / 16) < 0 || (int)((Projectile.Center.Y + i.Y) / 16) < 0)
                        {
                            //break;
                        }
                        Vector2 pos = new((int)((Projectile.Center.X + i.X) / 16), (int)((Projectile.Center.Y + i.Y) / 16));
                        Tile tile4 = Main.tile[(int)((Projectile.Center.X + i.X) / 16), (int)((Projectile.Center.Y + i.Y) / 16)];
                        //Main.NewText(!tile4.IsActuated && tile4.HasTile && Main.tileSolid[tile4.TileType] && !Main.tileSolidTop[tile4.TileType]);
                        bool flag = !tile4.IsActuated && tile4.HasTile && Main.tileSolid[tile4.TileType] && !Main.tileSolidTop[tile4.TileType] && !tile4.IsHalfBlock && !tile4.LeftSlope && !tile4.RightSlope;
                        //( TileID.Sets.IsATreeTrunk[theTile.type] | theTile.type == 323)鉴定所有的树
                        //Main.NewText(Main.tileCut[tile4.TileType]);
                        //if (启动 <= 0)
                        {
                            if (启动 <= 0 && tile4 != null && Main.tileCut[tile4.TileType] && WorldGen.CanCutTile((int)pos.X, (int)pos.Y, TileCuttingContext.AttackProjectile))
                            {//割草
                                WorldGen.KillTile((int)pos.X, (int)pos.Y);
                                if (Main.netMode != 0)
                                    NetMessage.SendData(17, -1, -1, null, 0, (int)pos.X, (int)pos.Y);
                            }
                            if (Collision.WetCollision(pos * 16, 30, 30) && !Collision.honey && !Collision.LavaCollision(pos * 16, 30, 30))
                            {
                                Projectile.ai[1] = i.Length();
                                if (启动 <= 0)
                                {
                                    for (int a = (int)pos.X - 2; a < (int)pos.X + 2; a++)
                                    {
                                        for (int b = (int)pos.Y - 2; b < (int)pos.Y + 2; b++)
                                        {

                                            if (a > 0 && b > 0)
                                            {
                                                if (Main.tile[a, b].LiquidType == 0 && Main.tile[a, b].LiquidAmount > 0)
                                                {
                                                    if (Main.tile[a, b].LiquidAmount > 10)
                                                        Main.tile[a, b].LiquidAmount -= 10;
                                                    else
                                                        Main.tile[a, b].LiquidAmount = 0;
                                                    {
                                                        Dust d = Dust.NewDustDirect(pos * 16, 0, 0, 33,
                                                        Main.rand.Next(-5, 5), -15, 255, Color.White, 2.4f);
                                                        d.active = true;
                                                        //d.noGravity = true;
                                                    }
                                                    //Main.NewText(6);
                                                    if (Main.netMode == 1)
                                                        NetMessage.sendWater(a, b);
                                                    else
                                                        Liquid.AddWater(a, b);
                                                    break;
                                                }
                                                //else Main.NewText(Main.tile[a, b].LiquidType + " " + Main.tile[a, b].LiquidAmount);
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                            if (启动 <= 0 && TileID.Sets.IsATreeTrunk[tile4.TileType] | tile4.TileType == 323)//棕榈树
                            {
                                //Main.NewText(10);
                                //燃烧地面坐标 = (pos * 16 + new Vector2(8f, 0));
                                //ModContent.GetInstance<drawFlameModplayer>().burning.创造燃烧地面(pos * 16 + new Vector2(8f, 0), Projectile.owner);
                                bool flag2 = false;
                                foreach (Projectile proj in Main.projectile)
                                {
                                    if (proj.active && proj.type == ModContent.ProjectileType<地面燃烧proj>() && (proj.position - (pos * 16 + new Vector2(8f, 0))).Length() <= 0.8f)
                                    {
                                        flag2 = true;
                                        break;
                                    }
                                }
                                if (!flag2)
                                    Projectile.NewProjectile(Projectile.GetSource_FromAI(""), pos * 16 + new Vector2(8f, 0), Vector2.Zero, ModContent.ProjectileType<地面燃烧proj>(), damage, (float)伊蕾娜.HitType.HitByFlame, Projectile.owner);

                            }
                            if (启动 <= 0 && maxDistance != 1200)
                            {
                                if (flag)
                                {
                                    if (!Collision.WetCollision(Projectile.Center + i, 10, 10) && !Collision.honey && !Collision.LavaCollision(Projectile.Center + i, 10, 10))//判断是否有水，如果有水这个值是true
                                    {
                                        //没水时创造燃烧地面
                                        //ModContent.GetInstance<drawFlameModplayer>().burning.创造燃烧地面(pos * 16 + new Vector2(8f, 0), Projectile.owner);
                                        bool flag2 = false;
                                        foreach (Projectile proj in Main.projectile)
                                        {
                                            if (proj.active && proj.type == ModContent.ProjectileType<地面燃烧proj>() && (proj.position - (pos * 16 + new Vector2(8f, 0))).Length() <= 0.8f)
                                            {
                                                flag2 = true;
                                                break;
                                            }
                                        }
                                        if (!flag2)
                                            Projectile.NewProjectile(Projectile.GetSource_FromAI(""), pos * 16 + new Vector2(8f, 0), Vector2.Zero, ModContent.ProjectileType<地面燃烧proj>(), damage, (float)伊蕾娜.HitType.HitByFlame, Projectile.owner);
                                        if (i.Length() <= 0 && 生成火焰间隔 == 1)
                                        {
                                            生成火焰间隔 = 0;
                                            //创造火焰坐标 = 随机矩形 + Projectile.Center + towards * maxDistance;
                                            Projectile.NewProjectile(Projectile.GetSource_FromAI(""), 随机矩形 + Projectile.Center + towards * maxDistance, Vector2.Zero, ModContent.ProjectileType<火焰燃烧>(), 0, (float)伊蕾娜.HitType.HitByFlame, Projectile.owner, Main.rand.Next(2, 8));
                                            //ModContent.GetInstance<drawFlameModplayer>().flameui.创造火焰(随机矩形 + Projectile.Center + towards * maxDistance);
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        if (tile4.LiquidType == 0 && tile4.LiquidAmount > 0)
                                        {
                                            int a = (int)pos.X;
                                            int b = (int)pos.Y;
                                            tile4.LiquidAmount = 0;
                                            if (a > 0 && b > 0)
                                            {
                                                if (Main.netMode == 1)
                                                    NetMessage.sendWater(a, b);
                                                else
                                                    Liquid.AddWater(a, b);
                                            }
                                        }
                                    }
                                }
                                //

                                if (i.Length() <= 0 && 生成火焰间隔 == 1)
                                {
                                    生成火焰间隔 = 0;
                                    //创造火焰坐标 = 随机矩形 + Projectile.Center + towards * maxDistance;
                                    Projectile.NewProjectile(Projectile.GetSource_FromAI(""), 随机矩形 + Projectile.Center + towards * maxDistance, Vector2.Zero, ModContent.ProjectileType<火焰燃烧>(), 0, (float)伊蕾娜.HitType.HitByFlame, Projectile.owner, Main.rand.Next(2, 8));
                                    //ModContent.GetInstance<drawFlameModplayer>().flameui.创造火焰(随机矩形 + Projectile.Center + towards * maxDistance);
                                }
                            }
                        }
                    }
                    //Projectile.NewProjectileDirect(null, 随机矩形+Projectile.Center + towards * maxDistance, new Vector2(0f, -6), ModContent.ProjectileType<升腾火焰>(), 0, 0, player.whoAmI);
                    //	Dust d1 = Dust.NewDustPerfect(随机矩形 + Projectile.Center + towards * maxDistance, ModContent.DustType<Dusts.升腾火焰粒子>(), null, 0, Color.White, 1f);//ModContent.DustType<Dusts.feidandust>()
                    //var dFM = player.GetModPlayer<drawFlameModplayer>();
                    //dFM.创造火焰(随机矩形+Projectile.Center + towards * maxDistance);
                    //ModContent.GetInstance<drawFlameModplayer>().创造火焰(随机矩形 + Projectile.Center + towards * maxDistance);

                }

            }


            //联机判定火焰();

            //if (player.channel)
            {
                //player.heldProj = Projectile.whoAmI;
                Projectile.timeLeft = 5;
                player.itemTime = 2;
                player.itemAnimation = 2;
                player.direction = towards.X < 0 ? -1 : 1;//玩家朝向根据鼠标
                Projectile.Center = towards * 120 + player.MountedCenter;
                Projectile.rotation = towards.ToRotation();
                player.itemRotation = (float)Math.Atan2(Projectile.rotation.ToRotationVector2().Y * player.direction, Projectile.rotation.ToRotationVector2().X * player.direction);//武器朝向
                maxDistance = Projectile.ai[1];

            }
            if (Main.myPlayer == Projectile.owner)
            {

                if (!Main.mouseLeft)
                {
                    Main.mouseLeftRelease = true;
                    Projectile.Kill();
                }
            }
            if (启动 == 0)
            {
                SoundStyle flamesound = new("伊蕾娜/Projectiles/Flame/火焰射线释放音效");
                flamesound.SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest;
                flamesound.Volume = 0.6f;
                SoundEngine.PlaySound(flamesound);
                启动--;
            }

            //else Projectile.Kill();
            if (Projectile.wet)
                Projectile.Kill();
            Vector2 Center = player.MountedCenter + (player.itemRotation + player.fullRotation).ToRotationVector2() * 78;
            if (player.direction < 0)
                Center = player.MountedCenter - (player.itemRotation + player.fullRotation).ToRotationVector2() * 78;
            创造粒子(Center, player);

        }
        public override bool? Colliding(Rectangle myRect, Rectangle targetRect)
        {

            var player = Main.player[Projectile.owner];
            float point = 0f;
            Vector2 length = towards;
            length *= maxDistance;
            int weight = 37;
            //Main.NewText(999);

            return Collision.CheckAABBvLineCollision(targetRect.TopLeft(), targetRect.Size(), Projectile.Center, Projectile.Center + length, weight, ref point);
        }
        public override bool? CanDamage()
        {
            return true;
            return base.CanDamage();
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
        public class 粒子
        {
            public Vector2 Position;
            public bool active = false;
            public Vector2 velocity;
            int frame = 0;
            Player player;
            public 粒子(Vector2 Pos, Player player)
            {
                Position = Pos;
                velocity = Main.rand.NextVector2Circular(15, 15)*2f;
                active = true;
                this.player = player;
            }
            public void draw(Asset<Texture2D> texture)
            {
                frame++;
                if (frame > 10)
                {
                    active = false;
                    return;
                }

                Vector2 Center = player.MountedCenter + (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
                if (player.direction < 0)
                    Center = player.MountedCenter - (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
                Position = Center + frame * velocity * 0.5f;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
                Vector2 origin2 = texture.Size() * 0.5f;//除3相当于以图片中心为position
                Vector2 scale2 = new(6.5f, 3.3f);
                scale2 *= MathHelper.Lerp(1, 0, frame / 10f);
                Color c = new(255, 50, 0);
                c.A = 15;

                Main.spriteBatch.Draw(texture.Value, Position - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()),
                c, velocity.ToRotation(), origin2, scale2, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texture.Value, Position - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()),
                c * 0.3f, velocity.ToRotation(), origin2, scale2 * 1.5f, SpriteEffects.None, 0f);

                /*Main.spriteBatch.Draw(texture.Value, Position - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()),
                        c, velocity.ToRotation()+(float)Math.PI/2f, origin2, scale2, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texture.Value, Position - Main.screenPosition, new Rectangle(0, 0, texture.Width(), texture.Height()),
                c*0.3f, velocity.ToRotation() + (float)Math.PI / 2f, origin2, scale2*1.5f, SpriteEffects.None, 0f);*/

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }

        }
    }
}
