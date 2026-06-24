using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System;
using Terraria.Audio;
using Terraria.GameContent;

namespace 伊蕾娜.Projectiles.Lightning
{
    public class 雷云 : ModProjectile
    {
        public override string Texture => "伊蕾娜/Projectiles/形状2.1";

        float count = 0;
        int 闪电 = 0;
        Vector2 闪电坐标 = Vector2.Zero;
        Asset<Texture2D> texture3;
        float time = 0;
        bool start = false;
        public override void Load()
        {

            base.Load();
        }
        public override void SetDefaults()
        {
            if (texture3 == null) texture3 = TextureAssets.Projectile[Projectile.type];
            Projectile.penetrate = -1; // 穿透数量
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;//瓷砖碰撞
            Projectile.friendly = false;
            //projectile.timeLeft=30;
            //projectile.extraUpdates=1;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.damage = 20;
            Projectile.timeLeft = 3600;
            Projectile.knockBack = 2;

            Projectile.alpha = 0;
        }
        public override void OnKill(int timeLeft)
        {
            time = 0;
            Main.player[Projectile.owner].GetModPlayer<天雷modplayer>().雷云 = false;
            ModContent.GetInstance<天黑控制>().startDark = 0;
            ModContent.GetInstance<天黑控制>().endDark = 300;
            if (Main.myPlayer == Projectile.owner) 亮度同步(Projectile.owner, 0, 300, false);

        }
        public void 亮度同步(int playerid, int startDark, int endDark, bool 雷云)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)伊蕾娜.MessageType.雷云);
                packet.Write(playerid);
                packet.Write(startDark);//1主机告诉客机要扣篮//2客机告诉主机没蓝了//3客机告诉主机有蓝
                packet.Write(endDark);
                packet.Write(雷云);
                packet.Send(-1, playerid);
            }

        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (闪电 > 0)
            {
                Player player = Main.player[Projectile.owner];


                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
                Vector2 origin = texture3.Size() * 0.5f;
                Rectangle sourceRectangle = new(0, 0, 500, 500);
                float 渐变 = MathHelper.Lerp(1, 0.5f, 闪电 / 120f);
                Vector2 scale = new(5);
                scale /= 渐变;
                Color c = new(166, 235, 255, 70);
                c.A = (byte)MathHelper.Lerp(0, 70f, 闪电 / 120f);
                //Main.NewText(111);
                Vector2 坐标 = new(闪电坐标.X, 0);
                Main.spriteBatch.Draw(texture3.Value, 坐标, sourceRectangle,
                        c, 0, origin, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }
            return false;
        }
        public override void AI()
        {
            if (time<300) time++;
            if (!start)
            {
                if (Projectile.ai[0] == 1)
                {
                    Projectile.extraUpdates += 1;
                    Projectile.timeLeft = 7200;
                }

                start = true;
            }
            Player player = Main.player[Projectile.owner];

            float extramana = MathHelper.Lerp(1, 25, time / 300f);
            if (Main.GameUpdateCount % 2 == 0)
            {
                if (player.CheckMana(player.HeldItem, (int)(extramana * player.manaCost), true))
                {
                    player.manaRegenDelay = 10;
                }
                else Projectile.Kill();
            }
            if (Main.rand.Next(0, 100) > 98)
            {
                Vector2 position = Vector2.Zero + new Vector2(Main.rand.NextFloat(0, Main.screenWidth), 0f);
                float distance = Math.Abs(position.X - Main.screenWidth / 2);
                float sound = MathHelper.Lerp(1, 0.1f, distance / 500f);
                SoundStyle t = SoundID.Thunder with
                {
                    Volume = sound,
                    PlayOnlyIfFocused = true,
                    MaxInstances = 3,
                    SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
                };
                //t.Type = SoundType.Sound;
                //t.Volume = 0.5f;
                SoundEngine.PlaySound(t, Projectile.Center + new Vector2(0, -700));

                if (闪电 == 0)
                {
                    闪电 = 120;
                    闪电坐标 = position;
                    Main.player[Main.myPlayer].GetModPlayer<天雷modplayer>().雷击距离 = new Vector3(position, 30);
                }
            }
            else if (闪电 > 0) 闪电--;
            if (!Main.player[Main.myPlayer].GetModPlayer<天雷modplayer>().雷云)
            {
                Projectile.Kill();
            }
            Projectile.Center = Main.player[Projectile.owner].Center;
            if (Main.myPlayer == Projectile.owner && Projectile.timeLeft < 6600)
            {
                Projectile.ai[0]--;
                float distanceMax = 1250f;
                if (Projectile.ai[0] <= 0)//cd到了，准备雷击
                {

                    foreach (NPC target in Main.npc)
                    {
                        if (Projectile.ai[0] <= 0)
                        {
                            if (target.active && !target.friendly && !target.dontTakeDamage && !target.immortal)//拥有攻击目标
                            {
                                if ((target.wet || target.HasBuff(BuffID.Wet)) && Main.rand.Next(0, 10) < 7)
                                {
                                    Projectile.ai[0] = Main.rand.NextFloat(60, 120);//随机cd,短
                                    闪电 = 120;
                                    闪电坐标 = target.Center - Main.screenPosition;

                                    Projectile.NewProjectile(null, target.Center, new Vector2(0, 10), ModContent.ProjectileType<雷击点>(), 0, Projectile.knockBack, Projectile.owner, Main.rand.NextFloat(0, 5));
                                    count = 0;
                                    //continue;
                                }
                                if (count >= 2)//保底追踪
                                {
                                    //Main.NewText("保底锁定");
                                    Projectile.ai[0] = Main.rand.NextFloat(30, 120);//随机cd,短
                                    闪电 = 120;
                                    闪电坐标 = target.Center - Main.screenPosition;
                                    Projectile.NewProjectile(null, target.Center, new Vector2(0, 10), ModContent.ProjectileType<雷击点>(), 0, Projectile.knockBack, Projectile.owner, Main.rand.NextFloat(0, 5));
                                    count = 0;
                                }
                                else
                                {
                                    if (Main.rand.Next(0, 10) > 7)//小概率追踪敌人
                                    {
                                        //Main.NewText("随机锁定");
                                        Projectile.ai[0] = Main.rand.NextFloat(60, 180);//随机cd,长
                                        闪电 = 120;
                                        闪电坐标 = target.Center - Main.screenPosition;
                                        Projectile.NewProjectile(null, target.Center, new Vector2(0, 10), ModContent.ProjectileType<雷击点>(), 0, Projectile.knockBack, Projectile.owner, Main.rand.NextFloat(0, 5));
                                        count = 0;
                                    }
                                    else//随机攻击，增加保底
                                    {
                                        //Main.NewText("随机空地");
                                        count++;
                                        Projectile.ai[0] = Main.rand.NextFloat(60, 100);//随机cd,短
                                        Vector2 randPosition = new(Main.rand.NextFloat(-625, 625), 0f);
                                        闪电 = 120;
                                        闪电坐标 = Projectile.Center + randPosition - Main.screenPosition;
                                        Projectile.NewProjectile(null, Projectile.Center + randPosition, new Vector2(0, 10), ModContent.ProjectileType<雷击点>(), 0, Projectile.knockBack, Projectile.owner, Main.rand.NextFloat(0, 5));
                                    }
                                }
                            }
                        }
                        else break;
                    }
                    if (Projectile.ai[0] <= 0)
                    {//没有攻击目标
                        {
                            //Main.NewText("没有敌人");
                            count++;
                            Projectile.ai[0] = Main.rand.NextFloat(30, 200);
                            Vector2 randPosition = new(Main.rand.NextFloat(-625, 625), 0f);
                            闪电 = 120;
                            闪电坐标 = Projectile.Center + randPosition - Main.screenPosition;
                            Projectile.NewProjectile(null, Projectile.Center + randPosition, new Vector2(0, 10), ModContent.ProjectileType<雷击点>(), 0, Projectile.knockBack, Projectile.owner, Main.rand.NextFloat(0, 5));
                        }
                    }
                }
            }

        }
    }
    public class 天黑控制 : ModSystem
    {
        public float startDark = 0;
        public float endDark = 0;
        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            //Main.NewText(MathHelper.Lerp(1, 10, (float)Math.Pow(2400,2) / 1440000f));
            if (startDark > 0)
            {
                //Main.NewText(tileColor + "  " + backgroundColor);
                Color goalColor = new(15, 15, 15, 255);
                tileColor = Color.Lerp(goalColor, tileColor, startDark / 300f);
                backgroundColor = Color.Lerp(goalColor, backgroundColor, startDark / 300f);
                //Main.NewText(tileColor + "  " + backgroundColor);
                if (startDark > 1)
                {
                    startDark--;
                }
                //if(startDark==1) Main.dayTime = false;
            }
            else if (endDark > 0)
            {
                endDark--;
                Color goalColor = new(15, 15, 15, 255);
                tileColor = Color.Lerp(tileColor, goalColor, endDark / 300f);
                backgroundColor = Color.Lerp(backgroundColor, goalColor, endDark / 300f);
                //if(endDark==1) Main.dayTime = true;
            }
            base.ModifySunLightColor(ref tileColor, ref backgroundColor);
        }
    }
    public class 天雷modplayer : ModPlayer
    {
        public bool 雷云 = false;
        public Vector3 雷击距离 = Vector3.Zero;
        public int 疲劳值 = 0;
        public override void ModifyScreenPosition()
        {
            if (Player.GetModPlayer<ElainaModplayer>().isACritter)
            {
                Player.position = Player.GetModPlayer<ElainaModplayer>().screenposition;
            }
            if (雷云) 疲劳值 = 300;
            if (疲劳值 > 0) 疲劳值--;
            if (雷击距离.Z > 0)
            {
                Vector2 length = new(雷击距离.X, 雷击距离.Y);
                Vector2 随机震动 = Main.rand.NextVector2Circular(MathHelper.Lerp(20, 0, length.Length() / 1000f), MathHelper.Lerp(20, 0, length.Length() / 1000f));
                Main.screenPosition += 随机震动;
                //Main.NewText(随机震动);
                雷击距离.Z--;
            }
            base.ModifyScreenPosition();
        }
    }
}
