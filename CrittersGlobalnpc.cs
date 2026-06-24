using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

using Terraria.ModLoader.IO;
using 伊蕾娜;

public class CrittersGlobalnpc : GlobalNPC
{
    public override bool InstancePerEntity => true;

    public Vector2 velocity = Vector2.Zero;
    public int direction = 1;
    public int player = -1;
    public bool IfCountrolByPlayer = false;
    public bool act = false;
    public int hp = 0;
    int ai0 = 0;
    float maxX = 0;
    float maxY = 0;
    float Jumpspeed = 0;
    public bool sound = false;
    public bool enterworld = false;//她妈的，这个东西执行在load之后？？？
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        //if (npc.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer)
        {
            binaryWriter.WriteVector2(velocity);
            binaryWriter.Write(direction);
            binaryWriter.Write(player);
            binaryWriter.Write(IfCountrolByPlayer);
            binaryWriter.Write(act);
            binaryWriter.Write(hp);
            binaryWriter.Write(sound);
        }


    }
    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        //if (npc.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer)
        {
            velocity = binaryReader.ReadVector2();
            direction = binaryReader.ReadInt32();
            player = binaryReader.ReadInt32();
            IfCountrolByPlayer = binaryReader.ReadBoolean();
            act = binaryReader.ReadBoolean();
            hp = binaryReader.ReadInt32();
            sound = binaryReader.ReadBoolean();
        }

    }
    public bool isRabbit(int type)
    {
        if (type == NPCID.GoldBunny | type == NPCID.Bunny)
            return true;
        else return false;
    }
    public override void LoadData(NPC npc, TagCompound tag)
    {
        /*IfCountrolByPlayer = tag.GetBool("IfCountrolByPlayer");
        player = tag.GetInt("player");
        hp = tag.GetInt("hp");
        npc.townNPC = tag.GetBool("townNpc");
        enterworld = false;
        if (IfCountrolByPlayer)
        {
            npc.life = hp;
            npc.lifeMax = Main.player[player].statLifeMax2;
        }*/
        base.LoadData(npc, tag);
    }
    public override void SaveData(NPC npc, TagCompound tag)
    {
        /*tag["IfCountrolByPlayer"] = IfCountrolByPlayer;
        tag["player"] = player;
        tag["hp"] = hp;
        tag["townNpc"] = npc.townNPC;*/

        base.SaveData(npc, tag);
    }
    public override bool NeedSaving(NPC npc)
    {
        return base.NeedSaving(npc);
    }
    void 同步小动物ai0(Player p,int ai0)
    {
        this.ai0 = ai0;
        if (Main.netMode == NetmodeID.SinglePlayer) return;
        ModPacket packet = Mod.GetPacket();
        packet.Write((byte)伊蕾娜.伊蕾娜.MessageType.小动物ai0);
        packet.Write(player);
        packet.Write(ai0);
        packet.Write(sound);
        packet.Send(-1, player);
    }


    public override void AI(NPC npc)
    {   //if(IfCountrolByPlayer) Main.NewText(npc.type);

        if (IfCountrolByPlayer)//此npc为玩家控制
        {
            npc.townNPC = false;
            //Main.NewText("111"+npc.type+" " + Main.player[player].name);
            if (npc.type == NPCID.TownBunny || npc.type == NPCID.TownCat || npc.type == NPCID.TownDog)//玩家的兔兔会说话
            {
                NPCID.Sets.IsTownPet[npc.type] = false;
            }
            if (player >= 0 && Main.player[player] != null && Main.player[player].active && !Main.player[player].dead)
            {
                npc.netUpdate = true;
                var p = Main.player[player];
                /*
                 * if (!enterworld)
                {
                    //if (Main.netMode == 2) Console.WriteLine(npc.life + " " + hp + " " + npc.lifeMax + "enterworld" + enterworld);
                    //else Main.NewText(npc.life + " " + hp + " " + npc.lifeMax + "enterworld" + enterworld);
                    npc.life = hp;
                    npc.lifeMax = Main.player[player].statLifeMax2;
                    enterworld = true;
                    //npc.type = p.GetModPlayer<ElainaModplayer>().CritterType;
                    //Main.NewText(npc.type + " " + p.GetModPlayer<ElainaModplayer>().CritterType);
                }
                else 
                 */

                if (hp != npc.life)
                {
                    //if (Main.netMode == 2) Console.WriteLine(npc.life + " " + hp+" "+npc.lifeMax+"enterworld"+ enterworld);
                    //else Main.NewText(npc.life + " " + hp + " " + npc.lifeMax + "enterworld" + enterworld);
                    if (hp > npc.life)
                    {
                        act = false;
                    }

                    hp = npc.life;
                    p.statLife = npc.life;
                }

                p.velocity = npc.velocity;
                if (Main.myPlayer != p.whoAmI) p.velocity = Vector2.Zero;
                p.position = npc.position;//玩家位置跟随此npc
                p.GetModPlayer<ElainaModplayer>().screenposition = npc.position;
                p.GetModPlayer<ElainaModplayer>().isACritter = true;
                p.GetModPlayer<ElainaModplayer>().CritterType = npc.type;

                p.npcTypeNoAggro[npc.type]=true;
                p.aggro = -10000;
                p.invis = true;
                npc.life = p.statLife;
                npc.lifeMax = p.statLifeMax2;
                npc.catchItem = -1;//没有物品可以抓取此npc
                //npc.ForcePartyHatOn = false;//戴派对帽子
                if (p.HasBuff<伊蕾娜.Buffs.小动物>())//保持小动物buff，通过此状态维持玩家消失且不能正常控制自己角色
                {
                    p.buffTime[p.FindBuffIndex(ModContent.BuffType<伊蕾娜.Buffs.小动物>())] = 15;
                }
                else  //玩家取消buff，小动物状态解除
                {
                    npc.active = false;
                    npc.life = 0;
                    if (Main.netMode != 1)
                    {
                        NetMessage.SendData(23, -1, -1, null, npc.whoAmI);
                    }
                    p.velocity.Y = -5;
                }

                if (npc.active && p != null && p.active && !p.dead&&p.active)//开始控制小动物
                {

                    if (npc.aiStyle == 66) npc.ai[1] = 0;
                    //if (npc.aiStyle==16 || npc.aiStyle == 7|| npc.aiStyle == 68)
                    {
                        ControlGroundCritters(npc);
                    }
                }
            }
            else
            {
                npc.active = false;
            }

        }
    }

    public void Sync(NPC npc,bool canfly)
    {
        Main.player[player].fallStart = (int)npc.position.Y;
        if (!canfly)
        {
            if (velocity.Y < 0)//跳跃时，受重力速度逐渐降低
            {
                velocity.Y += 0.2f;
                if (velocity.Y != npc.velocity.Y)
                {
                    npc.velocity.Y = velocity.Y;//同步速度
                    if (Main.netMode != 1) NetMessage.SendData(23, -1, -1, null, npc.whoAmI);
                }
            }
        }
        else
        {
            if (!Main.player[player].controlDown) velocity.Y += 0.05f;
            if (velocity.Y != npc.velocity.Y)
            {
                npc.velocity.Y = velocity.Y;//同步速度
                if (Main.netMode != 1) NetMessage.SendData(23, -1, -1, null, npc.whoAmI);
            }
        }
        if (npc.velocity.X != velocity.X)//同步速度
        {
            npc.velocity.X = velocity.X;
            if (Main.netMode != 1) NetMessage.SendData(23, -1, -1, null, npc.whoAmI);//if (Main.netMode == 1 && Main.myPlayer == player) 
        }
        npc.direction = direction;
    }

    public override bool? CanBeHitByItem(NPC npc, Player player, Item item)
    {
        if (npc.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer) return false;

        return base.CanBeHitByItem(npc, player, item);
    }
    public override bool? CanChat(NPC npc)
    {
        if (npc.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer) return false;

        return base.CanChat(npc);
    }
    public override bool? CanBeCaughtBy(NPC npc, Item item, Player player)
    {
        if (npc.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer) return false;
        return base.CanBeCaughtBy(npc, item, player);
    }
    public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
    {
        if (npc.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer && projectile.friendly) return false;

        return base.CanBeHitByProjectile(npc, projectile);
    }
    public override void FindFrame(NPC npc, int frameHeight)
    {
        if (npc.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer)
        {
            if ((npc.type == NPCID.Duck|| npc.type ==NPCID.Seagull || npc.type == NPCID.DuckWhite) &&npc.wet) npc.frame.Y = frameHeight * 1;
        }
        base.FindFrame(npc, frameHeight);
    }
    
    
    public void ControlGroundCritters(NPC npc)
    {
        Player p = Main.player[player];

        //所有陆地被动行走或两栖动物
        //Main.NewText(npc.velocity);
        float maxspeedX = 1;
        float maxspeedY = 8.6f;
        float swimspeedX = 0;
        bool canswim = false;
        float swimdash = 0;
        bool iffish = false;
        bool canfly = false;
        bool cansound = false;
        SoundStyle soundid = SoundID.Duck;
        if (npc.type == NPCID.Squirrel || npc.type == NPCID.SquirrelGold || npc.type == NPCID.SquirrelRed)
        {
            maxspeedX = 1.6f;
        }
        if (npc.type == NPCID.Mouse || npc.type == NPCID.Rat || npc.type == NPCID.GoldMouse)
        {
            cansound = true;
            maxspeedX = 2; maxspeedY = 7.2f;
            soundid = SoundID.Critter;
        }
        if (npc.type == NPCID.TurtleJungle || npc.type == NPCID.Turtle || npc.type == NPCID.SeaTurtle)
        {
            //Main.NewText(npc.breathCounter + " " + npc.breath+" "+p.breath+" "+p.breathCD);
            npc.breathCounter=0;
            p.breathCD = 0;
            p.breath = p.breathMax;
            maxspeedX = 0.5f; maxspeedY = 6f;
            swimspeedX = 2.2f;
            if (npc.type == NPCID.SeaTurtle)
            {
                swimspeedX = 2.7f;
                canswim = true;
            }
        }
        if (npc.type == NPCID.Frog|| npc.type == NPCID.GoldFrog)
        {
            npc.breathCounter = 0;
            p.breathCD = 0;
            p.breath = p.breathMax;
            npc.ai[0] = 0;
            maxspeedX = 1f; maxspeedY = 6f;
            swimspeedX = 10f;
            swimdash = 15f;
            cansound = true;
            soundid = SoundID.Frog;
        }
        if (npc.type == NPCID.Goldfish || npc.type == NPCID.GoldfishWalker || npc.type == NPCID.Dolphin|| npc.type == NPCID.GoldGoldfish || npc.type == NPCID.GoldGoldfishWalker)
        {
            npc.breathCounter = 0;
            p.breathCD = 0;
            p.breath = p.breathMax;
            iffish = true;
            maxspeedX = 1f; maxspeedY = 3f;
            swimspeedX = 2.2f;
            canswim = true;
        }
        if (npc.type == NPCID.Duck|| npc.type == NPCID.Seagull|| npc.type == NPCID.DuckWhite)
        {
            npc.breathCounter = 0;
            p.breathCD = 0;
            p.breath = p.breathMax;
            cansound = true;
            if (npc.wet)
            {
                npc.frameCounter = 0;
                npc.ai[0] = 0;
                npc.velocity.Y = 0.0f;
                npc.noGravity = true;
            }
            else npc.noGravity = false;
            maxspeedX = 1f; maxspeedY = 6f;
            swimspeedX = 2.2f;
            //npc.velocity.Y = 0;
            if ( Main.player[player].controlJump)
            {
                canfly = true;
                npc.type += 1;
                canswim = false;
                npc.velocity.Y = -5.0f;
                velocity.Y = -5;
                npc.ai[0] = 1;
            }
        }
        if(npc.type == NPCID.Duck2 || npc.type == NPCID.Seagull2 || npc.type == NPCID.DuckWhite2)
        {
            npc.breathCounter = 0;
            p.breathCD = 0;
            p.breath = p.breathMax;
            cansound = true;
            maxspeedX = 5f; maxspeedY = 5f;
            swimspeedX = 0f;
            canfly = true;
            npc.ai[0] = 1;
            if (npc.velocity.Y == 0 || npc.wet)
            {
                velocity.Y = 0;
                canfly = false;
                npc.type -=1;
                npc.ai[0] = 1;
                npc.aiStyle = 7;
                canswim = false;
                npc.rotation = 0;
            }
        }
        if(npc.type == NPCID.LadyBug|| npc.type == NPCID.GoldLadyBug|| npc.type == NPCID.Lavafly|| npc.type == NPCID.LightningBug)
        {
            canfly = true;
            maxspeedX = 1f; maxspeedY = 2f;
        }
        if (npc.type == NPCID.Butterfly || npc.type == NPCID.EmpressButterfly || npc.type == NPCID.HellButterfly || npc.type == NPCID.GoldButterfly)
        {
            canfly = true;
            maxspeedX = 3f; maxspeedY = 3f;
            if(npc.type== NPCID.EmpressButterfly)
            {
                //if (npc.ai[2]> 58)
                if (npc.ai[2] > 58) npc.ai[2] = 0;
            }
        }
        if (npc.type == NPCID.Bird || npc.type == NPCID.BirdBlue|| npc.type == NPCID.BirdRed || npc.type == NPCID.GoldBird)
        {
            cansound = true;
            canfly = true;
            maxspeedX = 5f; maxspeedY = 5f;
            npc.ai[0] = 0;
            soundid = SoundID.Bird;
        }
        if (npc.type == NPCID.Seagull | npc.type == NPCID.Seagull + 1) soundid = SoundID.Seagull;

        Sync(npc, canfly);
        //Main.NewText(act);
        if (npc.type == NPCID.TownBunny || npc.type == NPCID.TownCat|| npc.type == NPCID.TownDog)
        {   
            if (npc.ai[0] != 7 & npc.ai[0] != 22 & npc.ai[0] != 21 && npc.ai[0] != 20) act = false;
            if (!act) npc.ai[0] = 0;//&& player != Main.myPlayer&& Main.netMode!=NetmodeID.Server
            if (act)
            {
                npc.velocity.X = 0;
            }
            if (npc.ai[0] == 21| npc.ai[0] ==20)
            {
                if (p.controlLeft | p.controlRight | p.controlJump)
                {
                    act = false;
                }
                else npc.ai[1]++;
            }
        }
        if (canswim&&npc.wet)
        {
            p.breath += 1;
            p.gills = true;
            npc.breath++;
        }

        if ((npc.velocity.Y == 0 || iffish || canfly || (npc.wet && swimspeedX != 0)) && !act)
        {

            if (npc.type == NPCID.TownBunny || npc.type == NPCID.TownCat || npc.type == NPCID.TownDog)//玩家点按后改变自身属性，由服务器同步
            {
                if (player == Main.myPlayer && !act&& !Main.isMouseLeftConsumedByUI&& !Main.LocalPlayer.mouseInterface)
                {
                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        int ai0 = 22;
                        if (npc.type == NPCID.TownDog) ai0 = 20;
                        npc.ai[0] = ai0;//7说话，22吃饭？1走路，0停止，4听别人说话（坐着不动），21躺在地上,
                                        //20看着屏幕？？,6放烟花，18站起来看着玩家，19看着玩家
                        npc.ai[1] = 100;
                        act = true;
                        同步小动物ai0(p, ai0);
                    }
                    else if(Main.mouseMiddle && Main.mouseMiddleRelease)
                    {
                        int ai0 = 7;
                        npc.ai[0] = ai0;//7说话，22吃饭？1走路，0停止，4听别人说话（坐着不动），21躺在地上,
                                        //20看着屏幕？？,6放烟花，18站起来看着玩家，19看着玩家
                        npc.ai[1] = 260;
                        act = true;
                        同步小动物ai0(p, ai0);
                    }
                    else if(Main.mouseRight && Main.mouseRightRelease)
                    {
                        int ai0 = 21;
                        if (npc.type == NPCID.TownCat && Main.rand.Next(2) == 1) ai0 = 20;
                        npc.ai[0] = ai0;//7说话，22吃饭？1走路，0停止，4听别人说话（坐着不动），21躺在地上,
                                        //20看着屏幕？？,6放烟花，18站起来看着玩家，19看着玩家
                        npc.ai[1] = 500;
                        act = true;
                        同步小动物ai0(p, ai0);
                    }

                }

            }
            if(cansound&&player == Main.myPlayer && Main.mouseLeft && Main.mouseLeftRelease && !Main.LocalPlayer.mouseInterface)
            {
                    sound = true;
                    同步小动物ai0(p, 0);
            }
            if (sound)
            {
                {
                    SoundStyle 声音 = soundid with
                    {
                        Volume = 0.5f,
                        MaxInstances = 1,
                        SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
                        PlayOnlyIfFocused = false,
                    };
                    SoundEngine.PlaySound(声音, npc.position);
                    velocity.X = 0;
                    //velocity.Y = 0;
                    sound = false;
                }
            }
            if (Main.player[player].controlLeft)
            {

                if (swimdash != 0 && npc.wet)
                {
                    if (velocity.X == 0) velocity.X = -swimdash;
                }
                else if (velocity.X > -maxspeedX || (swimspeedX != 0 && npc.wet && velocity.X > -swimspeedX))
                {
                    velocity.X += -0.2f;
                }
                if (velocity.X < 0) direction = -1;
            }
            else if (Main.player[player].controlRight)
            {

                if (swimdash != 0 && npc.wet)
                {
                    if (velocity.X == 0) velocity.X = swimdash;
                }
                else if (velocity.X < maxspeedX || (swimspeedX != 0 && npc.wet && velocity.X < swimspeedX))
                {
                    velocity.X += 0.2f;
                }
                if (velocity.X > 0) direction = 1;
            }
            else
            {
                if (Math.Abs(velocity.X) < 0.2f) velocity.X = 0;
                velocity.X *= 0.9f;
            }
            if (swimdash != 0 && npc.wet)
            {
                if (velocity.X != 0) velocity.X *= 0.9f;
                if (Math.Abs(velocity.X) < 0.2f) velocity.X = 0;

            }
            if (Main.player[player].controlJump && !canfly)
            {
                float speedmax = maxspeedY;
                if (swimspeedX != 0 && npc.wet) speedmax /= 2;
                if (Jumpspeed < speedmax) Jumpspeed += 0.5f;
            }
            else if (!canfly)
            {   if(Jumpspeed==0) velocity.Y = npc.velocity.Y;

            }//else if(npc.type!=NPCID.Duck)velocity.Y = npc.velocity.Y;
            if (!canfly&&Jumpspeed != 0&& !Main.player[player].controlJump)
            {

                velocity.Y = -Jumpspeed;
                Jumpspeed = 0;
            }
            if (canfly)
            {
                if (Main.player[player].controlDown)
                {
                    if (velocity.Y < maxspeedY)
                    {
                        velocity.Y += 0.2f;
                    }
                    
                }
                else if(Main.player[player].controlJump || Main.player[player].controlUp)
                {
                    if (velocity.Y > -maxspeedY) velocity.Y -= 0.2f;

                }
                else
                {
                    velocity.Y *= 0.8f;
                }
            }
            if (canswim)
            {
                if (npc.wet)
                {
                    if (Main.player[player].controlDown)
                    {
                        if (velocity.Y < swimspeedX)
                        {
                            velocity.Y = 2.7f;
                        }
                    }
                    else if (Main.player[player].controlJump || Main.player[player].controlUp)
                    {
                        if (velocity.Y > -swimspeedX)
                        {
                            velocity.Y = -2.7f;
                        }
                    }
                    else velocity.Y = npc.velocity.Y;

                    if (npc.type == NPCID.Dolphin)
                    {
                        int x = (int)npc.position.X/16;
                        int y = (int)npc.position.Y/16-1;
                        Tile t = Main.tile[x, y];
                        if(t != null && t.LiquidAmount == 0)
                        {
                            if (Main.player[player].controlJump)
                            {
                                npc.ai[0] = -1;
                                npc.ai[2] = 1;
                                velocity.Y = -Main.rand.Next(4,10);
                            }
                            if (Main.player[player].controlUp)
                            {
                                SoundStyle 海豚叫 = SoundID.Dolphin with 
                                {
                                    Volume = 0.5f,
                                    MaxInstances = 1,
                                    SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
                                    PlayOnlyIfFocused = true,
                                };
                                SoundEngine.PlaySound(海豚叫, npc.position);
                                npc.ai[0] = -1;
                                npc.ai[2] = 2;
                                velocity.X = 0;
                                velocity.Y = 0;
                            }
                        }

                    }
                }
                else
                {
                    if (Main.player[player].controlJump && npc.velocity.Y == 0)
                    {
                        velocity.Y = -maxspeedY;
                    }
                    else
                    {
                        velocity.Y = npc.velocity.Y;
                    }
                    
                }
                if (velocity.Y != npc.velocity.Y)
                {
                    npc.velocity.Y = velocity.Y;//同步速度
                    if (Main.netMode != 1) NetMessage.SendData(23, -1, -1, null, npc.whoAmI);
                }
            }
        }
        if (Math.Abs(velocity.Y) < 0.15f) velocity.Y = 0;
        if(npc.type == NPCID.TownBunny || npc.type == NPCID.TownCat || npc.type == NPCID.TownDog)
        {
            if (npc.ai[0] == 21&& npc.type == NPCID.TownBunny || (npc.ai[0] == 20&& npc.type == NPCID.TownCat))//躺着
            {
                if (Main.GameUpdateCount % 20 == 0)
                {
                    p.HealEffect(1);
                    p.statLife += 1;
                }

            }
            if(npc.ai[0] == 22 && npc.type == NPCID.TownBunny)
            {
                if (npc.ai[1] == 10)
                {
                    p.HealEffect(50);
                    p.statLife += 50;
                }
            }
        }

    }
    //npc.aiStyle = 65;
}
