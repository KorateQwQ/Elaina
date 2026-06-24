using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.IO;
using System;
using 伊蕾娜.System;

namespace 伊蕾娜.Projectiles.MagicIce
{
    public class 冰魔法ModPlayer : ModPlayer
    {
        public int damage()
        {
            var 魔力飞弹 = Player.GetModPlayer<EXPmodplayer>();
            int lv = 魔力飞弹.GetLv()*3;
            float dam = MathHelper.Lerp(50, 500, lv * lv / 900f) * Player.GetTotalDamage(DamageClass.Magic).Additive;
            if (critChance() > 100) dam *= critChance() / 100f;
            return (int)dam;
        }
        public float critChance()
        {
            var 火魔法 = Player.GetModPlayer<EXPmodplayer>();
            float chance = 火魔法.GetLv() * 5 + Player.GetTotalCritChance(DamageClass.Magic);
            return chance;

        }
        public int mana()
        {
            var 水魔法 = Player.GetModPlayer<EXPmodplayer>();
            float manaReduce = 100 * MathHelper.Lerp(1, 0.1f, 水魔法.GetLv() * 水魔法.GetLv() / 100f);
            return (int)manaReduce;//100-10点蓝

        }
        public override void FrameEffects()
        {
            //Main.NewText(damage() + " " + critChance() + " " + mana());
            //Player.GetModPlayer<魔力飞弹modplayer>().熟练度=10;
            //Player.GetModPlayer<FlameModplayer>().熟练度 = 10;
            //Player.GetModPlayer<WaterModplayer>().熟练度 = 10;
            base.FrameEffects();
        }
    }
    public class 冰雾 : ModProjectile
    {
        Vector2 towards = Vector2.Zero;
        Vector2 oldtowards;
        Asset<Texture2D> MainColor;
        Asset<Texture2D> MainShape;
        Asset<Texture2D> MaskColor;
        public 冰粒子子[] iceList = new 冰粒子子[1000];
        int amount = 0;
        public float[] 冰冻计数器 = new float[201];

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(towards);

        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            towards = reader.ReadVector2();
        }
        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            amount = 0;
            base.SetDefaults();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (MainColor == null) MainColor = Mod.Assets.Request<Texture2D>("Projectiles/颜色");
            if (MainShape == null) MainShape = Mod.Assets.Request<Texture2D>("Projectiles/形状");
            if (MaskColor == null) MaskColor = Mod.Assets.Request<Texture2D>("Projectiles/渐变");
            List<CustomVertexInfo> bars = new();
            List<CustomVertexInfo> triangleList = new();
            //Main.NewText(123);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
            DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            for (int i = 0; i < amount; i++)
            {
                if (iceList[i] != null && iceList[i].active)
                {
                    iceList[i].update();
                }
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public void 冻结矩形区域(int x, int y)
        {
            for (int i = x - 1; i <= x + 1; i++)
            {
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (i > 0 && j > 0)
                    {
                        if (!Main.tile[i, j].HasTile && Main.tile[i, j].LiquidType == 0 && Main.tile[i, j].LiquidAmount > 0)
                        {
                            //Main.NewText(Main.tile[x, y].LiquidAmount);
                            Vector2 position = new(i * 16, j * 16);
                            Vector2 v = Vector2.Normalize(Projectile.Center - position) * 15;
                            //if (Main.tile[i, j].LiquidAmount > 60)
                            {
                                //Main.tile[i, j].TileType = 161;
                                WorldGen.PlaceTile(i, j, 161);
                                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, i, j, 0f, 0, 0, 0);
                                for (int k = 0; k < 30; k++)
                                {
                                    Dust d = Dust.NewDustDirect(position, 16, 16, DustID.IceTorch, 0, -10, 255, Color.White, 2);
                                }

                                if (Main.myPlayer == Projectile.owner)
                                {
                                    if (Main.netMode == 1)
                                        NetMessage.sendWater(i, j);
                                    else Liquid.AddWater(i, j);
                                }
                            }
                        }
                    }
                }
            }
        }
        public void 判定施加冻结(NPC target)
        {
            float 体积 = target.width * target.height;
            //Main.NewText(target.width + " " + target.height + " " + 体积);
            target.velocity *= 0.95f;
            float 体积倍率影响 = MathHelper.Lerp(0, 10, 体积 / 40000f);
            float 基础所需冻结 = MathHelper.Lerp(0, 1000, target.lifeMax / 100000f);
            float 所需冻结 = 体积倍率影响 * 基础所需冻结;
            if (target.boss) 所需冻结 *= 5;
            //Main.NewText(target+" "+ 冰冻计数器[(int)Projectile.ai[1]] +" / "+(int)所需冻结);
            //float 冰冻所需计数器=MathHelper.
            //target.AddBuff(ModContent.BuffType<Buffs.冻结>(), 500);

        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];//读取主人位置
            if (amount >= 1000) amount = 0;
            if (player.channel && Projectile.ai[0] != 1)
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    //Main.tile[x, y].LiquidType.Equals(LiquidID.Water) ;
                    towards = Main.MouseWorld - player.MountedCenter;
                    if (oldtowards != towards) Projectile.netUpdate = true;

                    /*if (Projectile.ai[1] >= 0)
					{
						if (Main.npc[(int)Projectile.ai[1]].active&& (Main.npc[(int)Projectile.ai[1]].position-Projectile.Center).Length()<1200)
                        {
							//冰冻计数器[(int)Projectile.ai[1]]++;
							//Main.NewText(冰冻计数器[(int)Projectile.ai[1]] + " " + Projectile.ai[1]+" "+ Main.npc[(int)Projectile.ai[1]]);
							//判定施加冻结(Main.npc[(int)Projectile.ai[1]]);
						}
						Projectile.ai[1] = -1;
					}*/
                }
                Projectile.Center = player.MountedCenter + (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
                if (player.direction < 0) Projectile.Center = player.MountedCenter - (player.itemRotation + player.fullRotation).ToRotationVector2() * 38;
                player.itemTime = 2;
                player.itemAnimation = 2;
                player.direction = towards.X < 0 ? -1 : 1;//玩家朝向根据鼠标
                Projectile.rotation = Vector2.Normalize(towards).ToRotation();
                Projectile.timeLeft = 180;
                player.itemRotation = (float)Math.Atan2(Projectile.rotation.ToRotationVector2().Y * player.direction, Projectile.rotation.ToRotationVector2().X * player.direction);//武器朝向
                Vector2 v = Vector2.Normalize(towards) * 15 + Main.rand.NextVector2Circular(5, 5);
                v = Vector2.Normalize(v) * 15;
                if (Main.GameUpdateCount % 2 == 0)
                {
                    //amount++;
                    if (iceList[amount] == null || !iceList[amount].active)
                        iceList[amount++] = new 冰粒子子(Projectile.Center, v, Projectile.owner, Mod.Assets.Request<Texture2D>("Projectiles/MagicIce/冰粒子"), Projectile.whoAmI);
                    //Projectile.NewProjectileDirect(null, Projectile.Center, v, ModContent.ProjectileType<冰粒子子>(), 1, 0.5f, Projectile.owner);
                }
            }
            else
            {
                Projectile.ai[0] = 1;
                Projectile.netUpdate = true;
            }
        }

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            base.ModifyDamageHitbox(ref hitbox);
        }

        public class 冰粒子子
        {
            Asset<Texture2D> texture;
            public bool[] onhit = new bool[1000];
            public Vector2 center;
            public bool active = false;
            public Vector2 velocity;
            public int owner;
            public int projowner;
            int frameCounter = 0;
            public int frame = 0;
            public float rotation = 0;
            int x, y;
            public 冰粒子子(Vector2 center, Vector2 velocity, int owner, Asset<Texture2D> texture, int projowner)
            {
                this.center = center;
                this.velocity = velocity;
                this.owner = owner;
                this.texture = texture;
                active = true;
                this.projowner = projowner;
            }

            public void update()
            {
                if (!active) return;
                center += velocity;
                x = (int)(center.X / 16f);
                y = (int)(center.Y / 16f);
                frameCounter++;
                if (frameCounter % 2 == 0)
                {
                    frame += 1;
                    frameCounter = 0;
                }
                if (frame >= 36)
                {
                    frame = 0;
                    active = false;
                }
                //if (frame %  == 0)
                {
                    {
                        foreach (NPC npc in Main.npc)
                        {
                            if (npc.active && !npc.friendly && !npc.immortal && !npc.dontTakeDamage && onhit[npc.whoAmI] == null | onhit[npc.whoAmI] == false)
                            {
                                if ((npc.position - center).Length() < 50)
                                {
                                    //npc.stri
                                    //if (!npc.immortal) npc.life -= 1;
                                    // npc.StrikeNPC(0, 2, 0, false, true, false);
                                    onhit[npc.whoAmI] = true;
                                    //Main.projectile[projowner].ai[1] = npc.whoAmI;
                                    //if (!npc.immortal) npc.GetGlobalNPC<Count>().count++;
                                    //npc.velocity = Vector2.Zero;
                                    if (Main.myPlayer == owner)
                                    {
                                        //npc.StrikeNPC(1, (int)伊蕾娜.HitType.HitByIce - 2, 0);
                                        //npc.SimpleStrikeNPC(1, (int)伊蕾娜.HitType.HitByIce - 2);
                                        NPC.HitInfo hitInfo = new();
                                        hitInfo.Knockback = 6;
                                        hitInfo.Damage = 0;
                                        hitInfo.Crit = false;
                                        npc.GetGlobalNPC<Count>().OnHitByProjectile(npc, null, hitInfo, 0);


                                        //npc.buffImmune[ModContent.BuffType<Buffs.冻结.冻结计数>()] = false;
                                        //if(!npc.HasBuff<Buffs.冻结.冻结计数>())
                                        //npc.AddBuff(ModContent.BuffType<Buffs.冻结.冻结计数>(), 30);
                                    }
                                    //Main.player[owner].addDPS(1);
                                    //CombatText.NewText(npc.getRect(), CombatText.DamagedHostile, 3);
                                }
                            }
                        }
                    }
                }

                //if (Main.myPlayer==owner)

                if (x > 0 && y > 0)
                {
                    冻结矩形区域(x, y);
                    if (Main.tile[x, y].HasTile && Main.tileSolid[Main.tile[x, y].TileType])
                    {
                        active = false;
                    }
                }
                rotation = velocity.ToRotation();
                draw();
            }
            public void draw()
            {
                Vector2 origin = texture.Size() / new Vector2(6f, 6f) * 0.5f;//除3相当于以图片中心为position.除以X,Y,此时为从上往下，从左往右的帧图
                int frameWidth = texture.Width() / 6;//图片总高度除以长度，得到每张图的长度
                int frameHeight = texture.Height() / 6;//图片总高度除以高度，得到每张图的高度
                int startX = frameWidth * (frame % 6);//每一帧的起始坐标X
                int startY = frameHeight * (frame / 6);//每一帧的起始坐标Y
                Rectangle sourceRectangle = new(startX, startY, frameWidth, frameHeight);//坐标x，坐标y，图片长度，图片高度，得到一张完整图片
                SpriteEffects spriteEffects = SpriteEffects.None;//调整图片方向，当弹幕方向不是1时水平翻转图片
                Main.spriteBatch.Draw(texture.Value, center - Main.screenPosition, sourceRectangle, new Color(200, 224, 233, 255), rotation, origin, 3f, spriteEffects, 0f);

            }

            public void 冻结矩形区域(int x, int y)
            {
                for (int i = x - 1; i <= x + 1; i++)
                {
                    for (int j = y - 1; j <= y + 1; j++)
                    {
                        if (i > 0 && j > 0)
                        {
                            if (!Main.tile[i, j].HasTile && Main.tile[i, j].LiquidType == 0 && Main.tile[i, j].LiquidAmount > 0)
                            {
                                //Main.NewText(Main.tile[x, y].LiquidAmount);
                                Vector2 position = new(i * 16, j * 16);
                                Vector2 v = Vector2.Normalize(center - position) * 15;
                                //if (Main.tile[i, j].LiquidAmount > 60)
                                {
                                    //Main.tile[i, j].TileType = 161;
                                    if (Main.myPlayer == owner)
                                    {
                                        WorldGen.PlaceTile(i, j, 161, false, true, owner);
                                        NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 1, i, j, 161);//1增加0删除ij位置161砖块类型messagebuffer，最后一位placestyle可不加
                                                                                                                    //NetMessage.SendObjectPlacment(-1, i, j, data.type, data.style, data.alternate, data.random, direction);
                                    }
                                    for (int k = 0; k < 30; k++)
                                    {
                                        //Dust.NewDust(position + Main.rand.NextVector2Circular(-20, 20), 16, 16, DustID.Snow, 0, 10, 255, Color.White, 2);
                                        Dust d = Dust.NewDustDirect(position, 16, 16, DustID.IceTorch, 0, -5, 255, Color.White, 1);
                                    }
                                    active = false;
                                }
                            }
                        }
                    }
                }
            }
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
