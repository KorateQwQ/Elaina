using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using KL.Extensions;
using KL.SkillSystem;
using KL.Utils.Net;
using ReLogic.OS;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using 伊蕾娜.Config;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.Skills.AshenWitch;
using 伊蕾娜.ElainaModSkills.Skills.MagicBarrier;
using 伊蕾娜.Items;
using 伊蕾娜.Items.accessories;
using 伊蕾娜.ReProjs;

namespace 伊蕾娜
{

    public partial class ElainaModplayer : KLModPlayer
    {
        public bool Elaina = false;
        // 旧角色沿用原先的专属资源槽；此后按该角色保存时的实际选择恢复。
        public bool UseElainaResourceDisplay = true;
        public bool hide = false;
        public bool hat = false;
        public bool onLight = false;
        public bool 武器流光 = false;
        public int sleep = 0;
        public float 掉落帽子 = 0;
        public static bool water = true;
        public static bool MissileCamera = true;
        private int test = 10086;
        float testrotation = 0;
        public bool sittingmount = false;
        public bool BecomeAnimal = false;
        public int CritterID = NPCID.GoldBunny;
        public Vector2 screenposition;
        public bool isACritter = false;
        public int CritterType = NPCID.Bunny;
        public bool NeedToBeCritter = false;
        
        //public int butterflychance = 0;
        //public bool origin = true;
        SoundStyle 死亡1 = (new SoundStyle($"伊蕾娜/Sounds/死亡_1", 1, SoundType.Sound)) with
        {
            Volume = 1f,
            MaxInstances = 5,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PlayOnlyIfFocused = true,
        };
        SoundStyle 死亡2 = (new SoundStyle($"伊蕾娜/Sounds/死亡_2", 1, SoundType.Sound)) with
        {
            Volume = 1f,
            MaxInstances = 5,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PlayOnlyIfFocused = true,
        };
        SoundStyle 受伤 = (new SoundStyle($"伊蕾娜/Sounds/受伤_", 2, SoundType.Sound)) with
        {
            Volume = 1f,
            MaxInstances = 1,
            SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
            PlayOnlyIfFocused = true,
        };
        SoundStyle 睡觉 = (new SoundStyle($"伊蕾娜/Sounds/睡觉", 1, SoundType.Sound)) with
        {
            Volume = 1f,
            MaxInstances = 1,
            SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
            PlayOnlyIfFocused = true,
        };
        
        public void 判定动作状态()
        {
            /*Player.GetModPlayer<Homura.Manager.人物modplayer>().Homura = false;
            Player.GetModPlayer<Homura.Manager.人物modplayer>().Mahua = false;
            Player.GetModPlayer<Homura.Manager.蓝量modplayer>().BlockMana = 0;
            Player.GetModPlayer<Homura.UI.SoulGemModPlayer>().Soul = 0;*/

            if (Elaina && !Player.dead)
            {
                Player.legPosition = new Vector2(0, 0);

                if (Player.bodyFrame.Y >= 56 && Player.bodyFrame.Y < 392 && Player.legFrame.Y >= 392)//正在跑步的同时手里持有物品
                {

                    if (Player.bodyFrame.Y < 392)
                    {
                        if (Player.direction > 0)
                        {
                            Player.legPosition = new Vector2(-3, 0);
                        }
                        else
                        {
                            Player.legPosition = new Vector2(3, 0);
                        }
                    }
                }
                if (Player.legFrame.Y >= 392)
                {
                    test++;
                    if (Player.legFrame.Y != testrotation)
                    {
                        float v = Math.Abs(Player.velocity.X);
                        int vgap = (int)MathHelper.Lerp(10, 2, v / 7);
                        if (test < vgap)
                        {
                            if (Player.legFrame.Y == 392)
                            {
                                Player.legFrame.Y = 1064;
                            }
                            else
                            {
                                Player.legFrame.Y -= 56;
                            }
                            if (Player.bodyFrame.Y >= 392)
                            {
                                if (Player.bodyFrame.Y == 392)
                                {
                                    Player.bodyFrame.Y = 1064;
                                }
                                else
                                {
                                    Player.bodyFrame.Y -= 56;
                                }
                            }

                        }
                        else
                        {
                            test = 0;
                            testrotation = Player.legFrame.Y;
                        }
                    }
                }
            }
        }
        public override IEnumerable<Item> AddStartingItems(bool mediumCoreDeath)
        {
            if (Elaina)
            {
                if (!mediumCoreDeath) UseElainaResourceDisplay = true;
                Player.ConsumedManaCrystals = 4;
                Player.GetModPlayer<ElainaModplayer>().Elaina = true;
                Player.GetModPlayer<EXPmodplayer>().Reset();
                Player.GetModPlayer<ElainaSkillModPlayer>().LearnInitialSkills();
                return
                [
                    new Item(ModContent.ItemType<ElainaHat>()),
                    new Item(ModContent.ItemType<ElainaHat>()),
                    new Item(ModContent.ItemType<ElainaWand>()),
                    new Item(ModContent.ItemType<扫帚坐骑>()),
                ];
            }
            return [];
        }
        public override void OnEnterWorld()
        {
            ElainaSkillManager.RestoreResourceDisplay(this);
            if (Elaina&&Main.myPlayer==Player.whoAmI)
            {
                if (!Main.dedServ&&Platform.IsWindows)
                {
                    string title = "Wandering Witch: The Journey of Elaina".ZHlan("泰拉瑞亚: 魔女之旅");
                    Platform.Get<IWindowService>().SetUnicodeTitle(Main.instance.Window, title);
                }
            }
            RPC("NewPlayerIn",[Main.myPlayer,Elaina]);

        }

        void NewPlayerIn(int newPlayerId, bool isElaina)
        {
            if(newPlayerId<0||newPlayerId>=Main.player.Length)return;
            Main.player[newPlayerId].GetModPlayer<ElainaModplayer>().Elaina = isElaina;
            //PrintText($"GetMessagePlayer:{Player.whoAmI} NewPlayerID: {newPlayerId} isElaina: {isElaina}");
            
            //已存在服务器的玩家告知新玩家自己是伊蕾娜
            if(Main.LocalPlayer.GetModPlayer<ElainaModplayer>().Elaina&&newPlayerId!=Main.myPlayer)RPC("MulticastElaina",[Main.myPlayer]);
        }

        void MulticastElaina(int elainaId)
        {
            if(elainaId<0||elainaId>=Main.player.Length)return;
            Main.player[elainaId].GetModPlayer<ElainaModplayer>().Elaina = true;
            //PrintText($"GetMessagePlayer:{Player.whoAmI} MulticastElaina: {elainaId} isElaina");
        }
        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot) => !isACritter && base.CanBeHitByNPC(npc, ref cooldownSlot);

        //private bool debugProj = false;
        public override void ResetEffects()
        {
            /*if (!debugProj && Player.active)
            {
                Console.WriteLine($"MyID{Main.myPlayer} Projectile count:"+ ProjectileLoader.ProjectileCount);
                for (int i = 0; i < ProjectileLoader.ProjectileCount; i++)
                {
                    ModProjectile proj = ModContent.GetModProjectile(i);

                    if(proj!=null) Console.WriteLine($"Mod: {proj.Mod.Name} Projectile Name :{proj.Name} Projectile Type: {proj.Type}");
                }
                debugProj = true;
            }*/
            //Console.WriteLine("WhoAmI"+Main.myPlayer+ " ProjectileCount " + ProjectileLoader.ProjectileCount +" ItemCount "+ItemLoader.ItemCount);
            
            
            if (isACritter)
            {
                动物状态();
            }
            else
            {
                Player.width = 20;
            }
            if (Player.active)
                isACritter = false;
            if (Elaina && Player.active)
            {
                Player.manaFlower = true;
                Player.wearsRobe = false;
                hat = false;

            }
            base.ResetEffects();
        }
        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            {
                /*ModPacket packet = Mod.GetPacket();
                packet.Write((byte)伊蕾娜.MessageType.Elaina);
                packet.Write(Player.whoAmI);
                packet.Write(Elaina);
                packet.Write("Request");
                packet.Send(toWho, fromWho);*/
            }
            if (newPlayer)
            {
                if (isACritter)
                {
                    Console.WriteLine("Firstenter by " + Main.myPlayer);
                    ModPacket packet = Mod.GetPacket();
                    packet.Write((byte)伊蕾娜.MessageType.小动物状态);
                    packet.Write(Player.whoAmI);
                    packet.Write(isACritter);
                    packet.Write(CritterType);
                    packet.Send(toWho, fromWho);
                }
            }
        }

        public void SetTitle()
        {
            if (!Main.dedServ&&Platform.IsWindows)
            {
                string title = "Wandering Witch: The Journey of Elaina".ZHlan("泰拉瑞亚: 魔女之旅");
                Platform.Get<IWindowService>().SetUnicodeTitle(Main.instance.Window, title);
            }
        }

        public override void SaveData(TagCompound tag)
        {
            ElainaSkillManager.RecordResourceDisplayChoice(this);
            tag[nameof(UseElainaResourceDisplay)] = UseElainaResourceDisplay;
            tag["Elaina"] = Elaina;
            tag["isACritter"] = isACritter;
            tag["CritterType"] = CritterType;
        }
        public override void LoadData(TagCompound tag)
        {
            UseElainaResourceDisplay = !tag.ContainsKey(nameof(UseElainaResourceDisplay))
                || tag.GetBool(nameof(UseElainaResourceDisplay));
            Elaina = tag.GetBool("Elaina");
            isACritter = tag.GetBool("isACritter");
            CritterType = tag.GetInt("CritterType");
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            modifiers.DisableSound();
            base.ModifyHurt(ref modifiers);
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (Elaina)
            {
                info.SoundDisabled = true;
                SoundEngine.PlaySound(受伤, Player.Center);
            }
        }
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (Elaina)
            {
                playSound = false;

                if (Main.rand.NextBool())
                {
                    SoundEngine.PlaySound(死亡1, Player.Center);
                    if (Language.ActiveCulture.Name == "en-US")
                        damageSource = PlayerDeathReason.ByCustomReason("Let's go back...");
                    else
                        damageSource = PlayerDeathReason.ByCustomReason("还是回去吧..");
                }
                else
                {
                    SoundEngine.PlaySound(死亡2, Player.Center);
                    if (Language.ActiveCulture.Name == "en-US")
                        damageSource = PlayerDeathReason.ByCustomReason("I just want to be recognized...");
                    else
                        damageSource = PlayerDeathReason.ByCustomReason("我只是希望能够得到承认而已..");

                }
            }
            return base.PreKill(damage, hitDirection, pvp, ref playSound, ref genGore, ref damageSource);
        }


        void 动物状态()
        {
            //碰撞箱
            //if (Player.active)
            {
                Player.width = 0;
                Player.height = 0;
            }

            //禁用物品
            if (Player.mount.Active)
                Player.mount.Dismount(Player);
            Player.controlHook = false;
            Player.controlMount = false;
            Player.releaseMount = false;
            Player.controlThrow = false;
            Player.gravControl = false;
            Player.controlUseItem = false;
            Player.controlUseTile = false;
            Player.controlCreativeMenu = false;
            Player.controlSmart = false;
            if (Player.talkNPC != -1)
            {
                if (Main.npc[Player.talkNPC].GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer)
                {
                    Player.SetTalkNPC(-1);

                }
            }
        }
        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            if (Player.HasBuff<Buffs.小动物>())
            {
                r = 0;
                g = 0;
                b = 0;
                a = 0;
            }
            if (Elaina)
            {
                if (!Main.gamePaused && !Main.gameMenu && drawInfo.shadow == 0)
                {
                    判定动作状态();
                }
                if (Player.dead)
                {
                    Player.headVelocity = Vector2.Zero;
                    Player.bodyVelocity = Vector2.Zero;
                    Player.legVelocity = Vector2.Zero;
                    Player.headFrame.X = 0;
                    Player.headFrame.Y = 0;

                    Player.legFrame.Y = 56;
                    Player.legFrame.X = 40;
                    Player.bodyFrame.X = 40;
                    Player.bodyFrame.Y = 1064;
                    Player.headFrame.X = 40;
                    Player.headFrame.Y = 1064;

                    if (掉落帽子 == 0)
                        掉落帽子 = 150;
                }
                else if (掉落帽子 != 0)
                {
                    掉落帽子 = 0;
                    Player.headFrame.X = 0;
                    Player.headFrame.Y = 0;

                    Player.bodyFrame.X = 0;
                    Player.bodyFrame.Y = 0;
                    Player.legFrame.Y = 0;
                    Player.legFrame.X = 0;
                    Player.legFrame.Y = 0;
                    Player.legFrame.X = 0;
                }
                if (Player.sleeping.isSleeping)
                {
                    if (!Main.gamePaused && !Main.gameMenu && sleep == 0) SoundEngine.PlaySound(睡觉, Player.Center);
                    hat = false;
                    Player.legFrame.Y = 56;
                    Player.legFrame.X = 40;
                    Player.bodyFrame.X = 40;
                    Player.bodyFrame.Y = 1064;
                    Player.headFrame.X = 40;
                    Player.headFrame.Y = 1064;
                    if (sleep == 0)
                        sleep = 1;
                }
                if (!Player.sleeping.isSleeping && sleep == 1)
                {
                    sleep = 0;
                    Player.headFrame.X = 0;
                    Player.headFrame.Y = 0;

                    Player.bodyFrame.X = 0;
                    Player.bodyFrame.Y = 0;
                    Player.legFrame.Y = 0;
                    Player.legFrame.X = 0;
                    Player.legFrame.Y = 0;
                    Player.legFrame.X = 0;
                }
                Player.manaFlower = true;
            }
        }
        public override void FrameEffects()
        {
            if (Elaina)
            {
            }
            if (Main.myPlayer == Player.whoAmI)
            {
                if (Elaina)
                {
                    Player.wearsRobe = true;
                    Player.manaFlower = true;
                    if (ElainaModplayer.IsWand(Player.HeldItem.type))
                    {
                        if (!武器流光 && (Player.itemTime > 0 || onLight))
                        {
                            //Projectile.NewProjectileDirect(Entity.GetSource_FromThis(), Player.position, Vector2.Zero, ModContent.ProjectileType<武器流光>(), 0, 0, Player.whoAmI, 0, p.chooseSkill);
                            //武器流光 = true;
                        }
                    }
                    //onLight = false;
                    if (!onLight)
                    {
                        //Main.NewText(9999);
                        if (Main.mouseMiddle && Main.mouseMiddleRelease && ElainaModplayer.IsWand(Player.HeldItem.type))
                        {
                            onLight = true;
                            if (Player.direction > 0)
                            {
                                Player.itemLocation.X = Player.Center.X - 2f;
                                ;
                                Player.itemLocation.Y = Player.Center.Y - 10f;//+30f;
                                Player.itemRotation = -MathHelper.Pi / 3;
                                if (Player.mount.Active)
                                    Player.itemLocation.Y -= Player.mount.HeightBoost / 2;
                            }
                            else
                            {
                                Player.itemLocation.X = Player.Center.X - 24f;
                                Player.itemLocation.Y = Player.Center.Y - 10f;//+30f;
                                Player.itemRotation = MathHelper.Pi / 3;
                                if (Player.mount.Active)
                                    Player.itemLocation.Y -= Player.mount.HeightBoost / 2;
                            }

                            Projectile.NewProjectile(Entity.GetSource_FromThis(), Player.position, Vector2.Zero, ModContent.ProjectileType<Light>(), 0, 0, Player.whoAmI);
                        }

                    }

                }
            }
        }

        public override void UpdateEquips()
        {            
            //Player.buffImmune[BuffID.ManaSickness] = true;
            base.UpdateEquips();
        }

        public override void PostUpdate()
        {
            base.PostUpdate();
        }

        public override bool? CanHitNPCWithProj(Projectile proj, NPC target)
        {
            if (Elaina)
            {
                if((target.type == NPCID.Butterfly || target.type == NPCID.HellButterfly))return false;
            }

            return null;
        }

        public override bool CanHitNPC(NPC target)
        {
            if (Elaina)
            {
                if ((target.type == NPCID.Butterfly || target.type == NPCID.HellButterfly)) return false;
            }

            return true;
        }
        public static bool IsWand(int type) => type == ModContent.ItemType<ElainaWand>();
    }
}
