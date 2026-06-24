using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System;
using Terraria.ModLoader.IO;
using Terraria.Audio;
using 伊蕾娜.炼金;
using 伊蕾娜.System;

namespace 伊蕾娜.Projectiles.MagicMissile
{
    public class 五重飞弹 : KLProjectile
    {
        Asset<Texture2D> texture;
        Asset<Texture2D> texture2;
        SoundStyle 生成 = (new SoundStyle($"伊蕾娜/Projectiles/MagicMissile/生成", 1, SoundType.Sound)) with
        {
            Volume = 0.1f,
            MaxInstances = 5,
            Pitch = 0,
            PitchVariance = 0.3f,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PlayOnlyIfFocused = true,
        };
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 14;
            // DisplayName.SetDefault("魔力飞弹");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void SetDefaults()
        {

            //Projectile.penetrate = 1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;//瓷砖碰撞
                                           //projectile.timeLeft=30;
                                           //projectile.extraUpdates=1;
            Projectile.width = 3;
            Projectile.height = 3;
            Projectile.damage = 50;
            //Projectile.timeLeft = 800;
            //Projectile.extraUpdates = ;
            Projectile.alpha = 0;

        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            if (texture == null) texture = Mod.Assets.Request<Texture2D>("Projectiles/MagicMissile/五重飞弹");
            Vector2 origin = texture.Size() * 0.5f;//除3相当于以图片中心为position
            origin.Y /= 14;//动图还要除以帧数
            int frameHeight = texture.Height() / Main.projFrames[Projectile.type];//图片总高度除以帧数，得到每张图的高度
            int startY = frameHeight * Projectile.frame;//每一帧的起始坐标Y
            Rectangle sourceRectangle = new(0, startY, texture.Width(), frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
            Main.spriteBatch.Draw(texture.Value, Projectile.Center - Main.screenPosition, sourceRectangle, new Color(255, 255, 255, 255), 0, origin, 1f, 0, 0f);

            {
                Vector2[] 坐标组;
                float 长度 = Projectile.ai[0];
                //if (Projectile.ai[0] > 30)
                {
                    长度 = 10;
                    坐标组 = Projectile.oldPos;// smooth(Projectile.oldPos, (int)长度);
                }


                Effect DefaultEffect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/tail").Value;
                Asset<Texture2D> MainColor = Mod.Assets.Request<Texture2D>("Projectiles/粉色拖尾");
                Asset<Texture2D> MainShape = Mod.Assets.Request<Texture2D>("Projectiles/拖尾");
                Asset<Texture2D> MaskColor = Mod.Assets.Request<Texture2D>("Projectiles/渐变");

                // 把所有的点都生成出来，按照顺序
                List<CustomVertexInfo> bars = new();
                List<CustomVertexInfo> triangleList = new();
                for (int i = 0; i < 坐标组.Length; ++i)
                {
                    if (坐标组[i] == Vector2.Zero) break;
                    //spriteBatch.Draw(Main.magicPixel, projectile.oldPos[i] - Main.screenPosition,
                    //    new Rectangle(0, 0, 1, 1), Color.White, 0f, new Vector2(0.5f, 0.5f), 5f, SpriteEffects.None, 0f);

                    float width = MathHelper.Lerp(5f, 1f, (float)i / 坐标组.Length);
                    var normalDir = Vector2.Normalize(player.velocity);
                    if (i >= 1) normalDir = 坐标组[i - 1] - 坐标组[i];
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

                    var vertex = new CustomVertexInfo((bars[0].Position + bars[1].Position) * 0.5f + Vector2.Normalize(player.velocity) * 5, Color.White,
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
            return false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }
        public override void OnKill(int timeLeft)
        {
            var p = Main.player[Projectile.owner].GetModPlayer<魔力飞弹modplayer>();
            p.飞弹[(int)Projectile.ai[0]] = false;
        }
        public override void AI()
        {

            Lighting.AddLight(Projectile.Center, 255f / 200f, 119f / 200f, 215f / 200f);
            Projectile.timeLeft = 30;
            Player player = Main.player[Projectile.owner];
            var expmodplayer = player.GetModPlayer<EXPmodplayer>();

            var p = player.GetModPlayer<魔力飞弹modplayer>();
            p.飞弹[(int)Projectile.ai[0]] = true;
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 3 == 0)
            {
                Projectile.frame += 1;
                //Main.NewText(Projectile.ai[0]);
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 14)
            {
                Projectile.frame = 11;
            }
            if (player.dead)
            {//主人死后消除射弹
                Projectile.Kill();
                return;
            }
            Projectile.Center = player.Center + p.position[(int)Projectile.ai[0]] + new Vector2(0, player.gfxOffY);
            if (Projectile.ai[0] < 4 && Projectile.frame == 8 && Projectile.frameCounter == 1 && !p.飞弹[(int)Projectile.ai[0] + 1])
            {
                p.飞弹[(int)Projectile.ai[0] + 1] = true;
                player.manaRegenDelay = 90;
                SoundEngine.PlaySound(生成, player.position);
                if (player.whoAmI == Main.myPlayer)//弹幕主人控制连续五个弹幕的生成
                {
                    switch (Projectile.ai[0])
                    {
                        case 0:
                            if (Projectile.frame < 8)
                            {
                                player.itemTime = (int)(60 * p.咏唱时间);
                                player.itemAnimation = (int)(60 * p.咏唱时间);
                            }
                            Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), player.Center + p.position[(int)Projectile.ai[0] + 1], new Vector2(0f, 0f), Projectile.type, 50, 10, player.whoAmI, 1);
                            break;
                        case 1:
                            Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), player.Center + p.position[(int)Projectile.ai[0] + 1], new Vector2(0f, 0f), Projectile.type, 50, 10, player.whoAmI, 2);

                            break;
                        case 2:
                            Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), player.Center + p.position[(int)Projectile.ai[0] + 1], new Vector2(0f, 0f), Projectile.type, 50, 10, player.whoAmI, 3);

                            break;
                        case 3:
                            Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), player.Center + p.position[(int)Projectile.ai[0] + 1], new Vector2(0f, 0f), Projectile.type, 50, 10, player.whoAmI, 4);
                            break;
                    }
                }
            }
            if (player.whoAmI == Main.myPlayer)//索敌
            {
                Projectile.ai[1] = Projectile.FindTargetWithLineOfSight(1000f);
                Projectile.netUpdate = true;
            }
            NPC target = null;
            if (Projectile.ai[1] >= 0) target = Main.npc[(int)Projectile.ai[1]];
            if (target != null && target.active && !target.friendly && !target.dontTakeDamage && Projectile.frame > 8)
            {
                Vector2 targetVec = target.Center - Projectile.Center;
                targetVec.Normalize();
                targetVec *= 15f;
                if (player.CheckMana(player.HeldItem, (int)(5*player.manaCost), true))
                {
                    float 伤害系数 = player.GetTotalDamage(DamageClass.Magic).Additive;
                    player.manaRegenDelay = 90;
                    Projectile proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.position, targetVec, ModContent.ProjectileType<魔力飞弹>(), (int)(expmodplayer.GetMagicMissleDamage() * 伤害系数), p.knockback, player.whoAmI, -1f, 1);
                    //proj.CritChance = Projectile.CritChance;
                    Projectile.Kill();
                }
            }
            //Main.NewText(Projectile.CritChance);


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
    public class 魔力飞弹modplayer : ModPlayer
    {
        public bool[] 飞弹 = [false, false, false, false, false];
        public Vector2[] position =
        [
            new Vector2(75,0),
            new Vector2((float)(75f/Math.Sqrt(2)),(float)(-75f/Math.Sqrt(2))),
            new Vector2(0,-75),
            new Vector2((float)(-75f/Math.Sqrt(2)),(float)(-75f/Math.Sqrt(2))),
            new Vector2(-75,0)
        ];
        public int damage = 12;
        public int knockback = 2;
        public int 熟练度 = 0;
        public int exp = 0;
        public int maxexp = 0;
        public int mana = 2;//6
        public int[] 魔力飞弹信息;
        public float 咏唱时间 = 1f;
        public int 最大熟练度 = 5;
        public Vector2 screenPosition;
        public Vector2 targetScreenPosition;
        public Vector2 screenVelocity;
        public override void SaveData(TagCompound tag)
        {
            tag["魔力飞弹信息"] = new int[7] { damage, knockback, 熟练度, exp, maxexp, mana, 最大熟练度 };
        }
        public override void ModifyScreenPosition()
        {
            if (ElainaModplayer.MissileCamera&& screenPosition != Vector2.Zero)
            {
                if (targetScreenPosition == Vector2.Zero)
                {
                    targetScreenPosition = Player.Center;
                }
                if ((targetScreenPosition - Main.MouseWorld).Length() > 30)
                {
                    Vector2 targetVec = Main.MouseWorld - targetScreenPosition;
                    targetVec.Normalize();
                    // 目标向量是朝向目标的大小为20的向量
                    targetVec *= 50f;
                    // 朝向npc的单位向量*20 + 3.33%偏移量
                    screenVelocity = targetVec / 3f;
                    //screenVelocity.Normalize();
                    //screenVelocity *= 20f;
                    targetScreenPosition += screenVelocity;
                }
                else
                {
                    targetScreenPosition = Main.MouseWorld;
                }
                Vector2 res = targetScreenPosition - Player.Center;
                if (res.Length() > 700) res = Vector2.Normalize(res) * 700;


                Main.screenPosition += res;

            }
            base.ModifyScreenPosition();
        }
        public override void LoadData(TagCompound tag)
        {
            魔力飞弹信息 = (int[])tag["魔力飞弹信息"];
            damage = 魔力飞弹信息[0];
            knockback = 魔力飞弹信息[1];
            熟练度 = 魔力飞弹信息[2];
            exp = 魔力飞弹信息[3];
            maxexp = 魔力飞弹信息[4];
            mana = 魔力飞弹信息[5];
            if (魔力飞弹信息.Length == 7)
            {
                if (魔力飞弹信息[6] > 0)
                    最大熟练度 = 魔力飞弹信息[6];
            }

            //butterflychance = (int)tag["butterflychance"];
        }
        public override void FrameEffects()
        {
            //熟练度 = 10;//二级轻松蜂后,五级猪鲨级别
            if (熟练度 >= 3) 咏唱时间 = MathHelper.Lerp(1, 0.5f, (熟练度 - 3f) / 2f);
            if (咏唱时间 < 0.2) 咏唱时间 = 0.2f;
            maxexp = (int)MathHelper.Lerp(100f, 10000f, (float)(熟练度 * 熟练度) / 100);
            damage = (int)MathHelper.Lerp(15f, 70f, 熟练度 * 熟练度 / 100f);
            /*if (Main.mouseLeft && Main.mouseLeftRelease)
			{
				熟练度--;
				//maxexp = (int)MathHelper.Lerp(100f, 10000f, (float)Math.Sqrt(熟练度) / (float)Math.Sqrt(5));
				maxexp = (int)MathHelper.Lerp(100f, 10000f, (float)(熟练度* 熟练度) / 16);
				damage = (int)MathHelper.Lerp(15f, 50f, (float)(熟练度) / 6f);
				Main.NewText(熟练度 + " "+maxexp+" "+damage);
			}

			if (Main.mouseRight && Main.mouseRightRelease)
			{
				熟练度++;
				//maxexp = (int)MathHelper.Lerp(100f, 10000f, (float)Math.Sqrt(熟练度) / (float)Math.Sqrt(5));
				maxexp = (int)MathHelper.Lerp(100f, 10000f, (float)(熟练度 * 熟练度) / 16);
				damage = (int)MathHelper.Lerp(15f, 50f, (float)(熟练度 * 熟练度) / 25f);
				Main.NewText(熟练度 + " " + maxexp + " " + damage);
			}*/
        }
        public void lvUp(int exp)
        {
            if (Main.hardMode) 最大熟练度 = Player.GetModPlayer<炼金modplayer>().炼金最大lv;
            else 最大熟练度 = 5;
            if (熟练度 < 最大熟练度)
            {
                this.exp += exp;
                if (this.exp > maxexp)
                {
                    this.exp = 0;
                    熟练度++;
                }
            }
        }
    }
}
