using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameInput;
using Terraria.ModLoader.IO;
using ReLogic.Content;
using Terraria.GameContent;
using 伊蕾娜.Buffs;
using 伊蕾娜.System;

namespace 伊蕾娜.Mounts
{
    public class 扫帚 : ModMount
    {
        Asset<Texture2D> texture;

        private List<Vector2> oldPosition;
        private Vector2[] oldPos;
        public int 解除下落速度限制 = 0;
        public override void SetStaticDefaults()
        {
            MountData.buff = ModContent.BuffType<扫帚buff>();//mod.BuffType("扫帚Buff");//ModContent.BuffType<buff.扫帚Buff>();
                                                           //控制贴图位置
            MountData.playerYOffsets = [0];
            MountData.xOffset = 1;
                
            MountData.yOffset = -2;
            MountData.totalFrames = 1;
            //基本属性
            MountData.spawnDust = 226;//召唤用的粒子
            MountData.spawnDustNoGravity = true;
            MountData.heightBoost = 0;//坐骑提升的身高
            MountData.flightTimeMax = 320;
            MountData.fatigueMax = 500;//飞行时间，疲劳值
            MountData.fallDamage = 0f;
            MountData.usesHover = false;//悬停
            MountData.runSpeed = 3f;
            //base.mountData.dashSpeed = 10f;
            MountData.acceleration = 0.1f;//加速度
                                          //base.mountData.jumpHeight = 5;
                                          //base.mountData.jumpSpeed = 15f;
            MountData.blockExtraJumps = true;//禁止额外跳跃
            if (Main.netMode != NetmodeID.Server)
            {
                MountData.textureWidth = MountData.backTexture.Width();
                MountData.textureHeight = MountData.backTexture.Height();
                //mountData.textureWidth = mountData.frontTexture.Width;
                //mountData.textureHeight = mountData.frontTexture.Height;

            }
            base.SetStaticDefaults();
        }
        public override bool UpdateFrame(Player mountedPlayer, int state, Vector2 velocity)
        {
            oldPos ??= new Vector2[30];
            
            for (int i = oldPos.Length - 1; i > 0; --i)
                oldPos[i] = oldPos[i - 1];
            oldPos[0] =mountedPlayer.MountedCenter;
            
            Vector2 move = new Vector2(-30 * mountedPlayer.direction, 15).RotatedBy(mountedPlayer.fullRotation);

            Vector2 position = mountedPlayer.MountedCenter.Floor() /*+ new Vector2(0, mountedPlayer.gfxOffY) + move*/;
            // 初始化oldPosition列表（如果为null）
            oldPosition ??= new List<Vector2>();
            
            // 将当前玩家位置插入到列表开头（最新数据在第一位）
            oldPosition.Insert(0,position );
            
            //Lighting.AddLight(position, new Color(255, 160, 239,155).ToVector3());
            // 限制列表长度为30，超出时移除最旧的位置（列表末尾）
            if (oldPosition.Count > 30)
            {
                oldPosition.RemoveAt(oldPosition.Count - 1);
            }
            return false;
        }
        public override bool Draw(List<DrawData> playerDrawData, int drawType, Player drawPlayer, ref Texture2D texture, ref Texture2D glowTexture, ref Vector2 drawPosition, ref Rectangle frame, ref Color drawColor, ref Color glowColor, ref float rotation, ref SpriteEffects spriteEffects, ref Vector2 drawOrigin, ref float drawScale, float shadow)
        {
            /*Texture2D trail =ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/T_Trail73", AssetRequestMode.ImmediateLoad).Value;

            if (oldPosition is { Count: > 3 })
            {
                TrailEffect(trail,oldPosition.ToArray(), new Color(255, 160, 239,155), new Color(255, 160, 239,155),12,0,
                    0.3f,0,blendState:1,uTime:new Vector2(((float)-Main.timeForVisualEffects%30)/30f,0));
            }
            EndBeginDraw();*/
            return base.Draw(playerDrawData, drawType, drawPlayer, ref texture, ref glowTexture, ref drawPosition, ref frame, ref drawColor, ref glowColor, ref rotation, ref spriteEffects, ref drawOrigin, ref drawScale, shadow);
        }
        public override void UpdateEffects(Player player)
        {
            MountData.yOffset = -4;
            //MountData.usesHover = false;
            扫帚ModPlayer v = player.GetModPlayer<扫帚ModPlayer>();
            float cost = MathHelper.Lerp(5, 1, v.熟练度 / 10f);
            player.fallStart = (int)(player.position.Y / 16f);
            //Main.NewText((int)(player.velocity.Length() / 3f * cost)+" "+ player.velocity.Length());

            if (player.CheckMana(player.HeldItem, (int)(player.velocity.Length() / 3f * cost * player.manaCost), false) && player.GetModPlayer<ElainaModplayer>().Elaina)
            {
                player.controlJump = false;
                MountData.runSpeed = v.最大速度();
                player.maxFallSpeed = v.最大速度();
                MountData.acceleration = v.加速度();
                player.mount._flyTime = 320;
                player.mount._fatigue = 0;
                player.gravity = 0;
                if (player.velocity.Y != 0)
                {
                    if (player.direction > 0)
                    {
                        if (player.velocity.Y <= 0)//往右上方
                        {
                            player.fullRotation = MathHelper.Lerp(0, -0.15f, -player.velocity.Y / 5f);
                            if (player.fullRotation < -0.15f) player.fullRotation = -0.15f;
                            if (player.fullRotation > 0) player.fullRotation = 0;
                        }
                        else if (player.velocity.Y > 0)//往右下方
                        {
                            player.fullRotation = MathHelper.Lerp(0, 0.15f, player.velocity.Y / 5f);
                            if (player.fullRotation > 0.15f) player.fullRotation = 0.15f;
                        }

                    }
                    else
                    {
                        if (player.velocity.Y <= 0)//往右上方
                        {
                            player.fullRotation = MathHelper.Lerp(0, 0.15f, -player.velocity.Y / 5f);
                            if (player.fullRotation > 0.15f) player.fullRotation = 0.15f;
                            if (player.fullRotation < 0) player.fullRotation = 0;
                        }
                        else if (player.velocity.Y > 0)//往右下方
                        {
                            player.fullRotation = MathHelper.Lerp(0, -0.15f, player.velocity.Y / 5f);
                            if (player.fullRotation < -0.15f) player.fullRotation = -0.15f;
                        }
                    }


                }
                {
                    if (Main.GameUpdateCount % 70 == 0)
                    {
                        if (v.加速cd > 0) cost *= 3;
                        if (!player.CheckMana(player.HeldItem, (int)(player.velocity.Length() / 3f * cost *player.manaCost), true))
                        {
                            player.gravity = 0.4f;
                        }
                        else
                        {
                            //player.manaRegenDelay = 60;
                            v.lvUp((int)cost);
                        }
                    }

                }

                {
                    if (v.加速)
                    {
                        {
                            解除下落速度限制 = 30;
                            for (int i = 0; i < 50; i++)
                            {

                                Vector2 velocity = player.velocity * 200f + new Vector2(Main.rand.NextFloatDirection() * 40f, Main.rand.NextFloatDirection() * 40f);
                                velocity = Vector2.Normalize(velocity) * 30;
                                Dust d = Dust.NewDustDirect(player.position, 40, 40, DustID.Cloud,
                                0, 0, 0, Color.White, 1.5f);
                                //d.noGravity = true;
                            }
                            player.velocity = Vector2.Zero;
                        }
                        //Main.NewText($"开始加速");
                    }
                    else if (v.加速cd > 20)
                    {
                        MountData.runSpeed = v.最大冲刺速度();
                        player.maxFallSpeed = v.最大冲刺速度();
                    }
                    if (player.GetModPlayer<ElainaModplayer>().Elaina)
                    {   
                        NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, Main.myPlayer);
                        tryup(player, v);
                        trydown(player, v);
                        tryslowDown(player, v);
                    }
                    else player.gravity = 1;
                    if (解除下落速度限制 > 0)
                    {
                        解除下落速度限制--;
                        player.maxFallSpeed = v.最大冲刺速度() * 10f;
                    }
                }
            }
            else
            {
                player.gravity = 1;
                tryslowDown(player, v);
            }

            //if (v.熟练度 <= 4)


        }
        public void tryup(Player player, 扫帚ModPlayer v)
        {
            if (player.controlUp)
            {
                if (player.velocity.Y > -MountData.runSpeed)
                {
                    player.velocity.Y -= v.加速度();
                }
                else if (解除下落速度限制 <= 0) player.velocity.Y += v.加速度();

            }
        }
        public void trydown(Player player, 扫帚ModPlayer v)
        {
            if (player.controlDown)
            {
                if (player.velocity.Y < MountData.runSpeed)
                {
                    player.velocity.Y += v.加速度();
                }
            }
        }
        public void tryslowDown(Player player, 扫帚ModPlayer v)
        {
            int control = 0;
            if (!player.controlUp)//||player.velocity.Length()> MountData.runSpeed
            {
                control++;
                if (player.velocity.Y < 0)
                {
                    if (-player.velocity.Y > v.加速度())
                        player.velocity.Y += v.加速度();
                    else player.velocity.Y += 0.1f;
                }
            }
            if (!player.controlDown)
            {
                control++;
                if (player.velocity.Y > 0)
                {
                    if (player.velocity.Y > v.加速度()) player.velocity.Y -= v.加速度();
                    else player.velocity.Y -= 0.1f;

                }

            }
            if (!player.controlLeft)
            {
                control++;
                if (player.velocity.X < 0) player.velocity.X += v.加速度();

            }
            if (!player.controlRight)
            {
                control++;
                if (player.velocity.X > 0) player.velocity.X -= v.加速度();
            }
            if (control == 4 && player.velocity.Length() < 3)
            {
                player.fullRotation = 0;
                player.velocity = Vector2.Zero;
            }


        }
        public override void SetMount(Player player, ref bool skipDust)
        {
            base.SetMount(player, ref skipDust);
        }
        public class 扫帚ModPlayer : ModPlayer
        {
            public int exp = 0;
            public int maxexp = 100;
            public int 熟练度 = 0;
            public int 最大熟练度 = 5;
            public bool 加速 = false;
            public int 加速cd = 0;
            public int 冲刺cd = 0;
            public int[] 扫帚信息 = new int[4];

            public override void FrameEffects()
            {
                //熟练度 = 9;
                熟练度 = Player.GetModPlayer<EXPmodplayer>().GetLv();
                if (冲刺cd > 0) 冲刺cd--;
                base.FrameEffects();
            }
            public override void SaveData(TagCompound tag)
            {
            }

            public override void LoadData(TagCompound tag)
            {

                //butterflychance = (int)tag["butterflychance"];
            }
            public float 最大速度()
            {

                return MathHelper.Lerp(7, 10, 熟练度 / 10f);
            }
            public float 最大冲刺速度()
            {

                return MathHelper.Lerp(10, 20, 熟练度 / 10f);
            }
            public float 刹车速度()
            {
                return MathHelper.Lerp(0.1f, 1, 熟练度 / 10f);
            }
            public float 加速度()
            {
                if (加速) return 最大冲刺速度() * 1.5f;
                return MathHelper.Lerp(0.1f, 1, 熟练度 / 10f);
            }
            public override void ProcessTriggers(TriggersSet triggersSet)
            {
                if (Player.mount.Active && Player.mount.Type == ModContent.MountType<扫帚>())
                {
                    //if (熟练度 > 4)
                    {
                        if (Config.KeyBind.BroomAcceleration.Current)
                        {
                            加速cd++;
                        }
                        else if (Config.KeyBind.BroomAcceleration.JustReleased && 加速cd < 20&& 冲刺cd==0)
                        {
                            //if (Player.CheckMana(player.HeldItem,50,false))
                            {
                                if (熟练度 < 10)
                                {
                                    if (Player.CheckMana(Player.HeldItem, (int)(50*Player.manaCost), true))
                                    {
                                        加速 = true;
                                    }
                                }
                                else
                                {
                                    加速 = true;
                                }
                                if (加速)
                                {
                                    冲刺cd = (int)MathHelper.Lerp(180, 30, 熟练度 / 10f);
                                }

                            }
                        }
                        else
                        {
                            加速cd = 0;
                            加速 = false;
                        }
                    }
                }

                //if(KeyBind.扫帚加速)
            }
            public void lvUp(int exp)
            {
            }
        }
    }
}
