global using 伊蕾娜.Projectiles;
global using KL.Projectiles;
global using static KL.Drawing.DrawHelper;
global using static KL.Extensions.GamePlayStatic;
global using System.Collections.Generic;
global using Microsoft.Xna.Framework.Graphics;
global using ReLogic.Content;
global using Terraria.ModLoader;

using System;
using System.IO;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using 伊蕾娜.Buffs;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.Items.accessories;
using 伊蕾娜.Projectiles.Lightning;
using 伊蕾娜.炼金;



namespace 伊蕾娜
{
    public class 伊蕾娜 : Mod
    {
        private class DisplayNameUpdater : ModSystem
        {
            public override void OnLocalizationsLoaded()
            {
                typeof(Mod).GetProperty("DisplayName")?.SetValue(ModContent.GetInstance<伊蕾娜>(), Language.GetTextValue($"Mods.伊蕾娜.ModName"));
            }
        }
        public static 伊蕾娜 Instance => ModContent.GetInstance<伊蕾娜>();

        public static RenderTarget2D render;
        public static RenderTarget2D render2;
        public int worldtime = 0;
        
        public static Mod ElainaModInstance;
        public override void Load()
        {
            ElainaModInstance = this;
            //On.Terraria.Main.CalculateDamageNPCsTake += Main_NewDamage;
            //Terraria.On_Player.itemcheck
            On_PlayerDrawLayers.DrawPlayer_03_PortableStool += Elaina_PortableStool;
            //Terraria.On_NPC.strikenpc += Main_NewStrikeNpc;
            //On_PlayerDrawLayers.DrawSittingLegs += Elaina_sittinglegs;
            On_PlayerDrawLayers.DrawSittingLegs += Elaina_sittinglegs2;
            Terraria.Graphics.Effects.On_FilterManager.EndCapture += FilterManager_EndCapture;//原版绘制场景的最后部分——滤镜。在这里运用render保证不会与原版冲突
            Main.OnResolutionChanged += Main_OnResolutionChanged;
            On_NPC.Transform += Elaina_NewTransform;
            On_Rain.Update += On_Rain_Update;

            EquipLoader.AddEquipTexture(this, "伊蕾娜/Items/Armors/新版身体/Elaina2body", EquipType.Body, null, "Elaina2");
            EquipLoader.AddEquipTexture(this, "伊蕾娜/Items/Armors/新版身体/Elaina2head", EquipType.Head, null, "Elaina2");
            //EquipLoader.AddEquipTexture(this, "伊蕾娜/Items/Armors/新版身体/ElainaUiHead", EquipType.Head, null, "ElainaUiHead");
            EquipLoader.AddEquipTexture(this, "伊蕾娜/Items/Armors/新版身体/Elaina2legs", EquipType.Legs, null, "Elaina2");
            
            
            base.Load();
        }

        private void Elaina_sittinglegs(Terraria.DataStructures.On_PlayerDrawLayers.orig_DrawSittingLegs orig, ref Terraria.DataStructures.PlayerDrawSet drawinfo, Texture2D textureToDraw, Color matchingColor, int shaderIndex, bool glowmask)
        {
            if (drawinfo.drawPlayer.GetModPlayer<ElainaModplayer>().Elaina)
            {
                drawinfo.drawPlayer.GetModPlayer<ElainaModplayer>().sittingmount = true;
                drawinfo.drawPlayer.legFrame.Y = 336;
                return;
            }
            //orig(ref drawinfo, textureToDraw, matchingColor, shaderIndex, glowmask);
        }
        
        private void Elaina_sittinglegs2(On_PlayerDrawLayers.orig_DrawSittingLegs orig, ref PlayerDrawSet drawinfo, Texture2D textureToDraw, Color matchingColor, int shaderIndex, bool glowmask, EquipType? equipType)
        {
            if (drawinfo.drawPlayer.GetModPlayer<ElainaModplayer>().Elaina)
            {
                drawinfo.drawPlayer.GetModPlayer<ElainaModplayer>().sittingmount = true;
                drawinfo.drawPlayer.legFrame.Y = 336;
                return;
            }
            orig(ref drawinfo, textureToDraw, matchingColor, shaderIndex, glowmask, equipType);
        }


        public static bool iftarget(NPC target, Player player)
        {
            if (!(target.CountsAsACritter && player.dontHurtCritters) && !target.dontTakeDamage && target.active && !target.immortal && !target.friendly && !target.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer)
            {
                return true;
            }
            return false;
        }

        private void Elaina_PortableStool(On_PlayerDrawLayers.orig_DrawPlayer_03_PortableStool orig, ref PlayerDrawSet drawinfo)
        {
            if (drawinfo.drawPlayer.GetModPlayer<炼金modplayer>().搅拌中)
            {
                Texture2D value = TextureAssets.Extra[102].Value;
                Vector2 position = new Vector2((int)(drawinfo.Position.X - Main.screenPosition.X + (float)(drawinfo.drawPlayer.width / 2)), (int)(drawinfo.Position.Y - Main.screenPosition.Y + (float)drawinfo.drawPlayer.height + 28f)) + new Vector2(0, 7);
                Rectangle rectangle = value.Frame();
                Vector2 origin = rectangle.Size() * new Vector2(0.5f, 1f);
                Vector2 scale = new(1, 0.5f);
                DrawData item = new(value, position + new Vector2(0, -15), rectangle, drawinfo.colorArmorLegs, drawinfo.drawPlayer.bodyRotation, origin, scale, drawinfo.playerEffect, 0);
                item.shader = drawinfo.cPortableStool;
                drawinfo.DrawDataCache.Add(item);
                return;
            }
            orig(ref drawinfo);
        }
        
        private void On_Rain_Update(On_Rain.orig_Update orig, Rain self)
        {
            Rectangle r = new((int)self.position.X, (int)self.position.Y, 2, 2);
            foreach (var player in Main.ActivePlayers)
            {
                if (player!=null&& player.getRect().Intersects(r))
                {
                    WaterHitEffect(player);
                }
            }
            foreach (var npc in Main.ActiveNPCs)
            {
                if (npc!=null&&npc.getRect().Intersects(r))
                {
                    WaterHitEffect(npc);
                }
            }
            orig(self);
        }
        public void WaterHitEffect(NPC npc)
        {
            if (Main.netMode != 2)
            {
                if (npc.buffImmune[BuffID.Wet])
                    npc.buffImmune[BuffID.Wet] = false;
                npc.AddBuff(BuffID.Wet, 300);
                if (NpcIsOnFire(npc))
                    ClearNpcFire(npc);
            }
        }
        public void WaterHitEffect(Player npc)
        {
            if (Main.netMode != 2)
            {
                npc.AddBuff(BuffID.Wet, 300);
                if (NpcIsOnFire(npc))
                    ClearNpcFire(npc);
            }
        }
        static public bool NpcIsOnFire(NPC npc)
        {
            if (npc.HasBuff(BuffID.OnFire) || npc.HasBuff(BuffID.OnFire3) || npc.HasBuff(ModContent.BuffType<Buffs.灼烧>()))
            {
                return true;
            }
            return false;
        }
        static public bool NpcIsOnFire(Player npc)
        {
            if (npc.HasBuff(BuffID.OnFire) || npc.HasBuff(BuffID.OnFire3) || npc.HasBuff(ModContent.BuffType<Buffs.灼烧>()))
            {
                return true;
            }
            return false;
        }
        static public void ClearNpcFire(NPC npc)
        {
            if (npc.HasBuff(BuffID.OnFire))
            {
                npc.DelBuff(npc.FindBuffIndex(BuffID.OnFire));
            }
            if (npc.HasBuff(BuffID.OnFire3))
            {
                npc.DelBuff(npc.FindBuffIndex(BuffID.OnFire3));
            }
            if (npc.HasBuff(ModContent.BuffType<灼烧>()))
            {
                npc.DelBuff(npc.FindBuffIndex(ModContent.BuffType<灼烧>()));
            }
        }
        static public void ClearNpcFire(Player npc)
        {
            if (npc.HasBuff(BuffID.OnFire))
            {
                npc.DelBuff(npc.FindBuffIndex(BuffID.OnFire));
            }
            if (npc.HasBuff(BuffID.OnFire3))
            {
                npc.DelBuff(npc.FindBuffIndex(BuffID.OnFire3));
            }
            if (npc.HasBuff(ModContent.BuffType<灼烧>()))
            {
                npc.DelBuff(npc.FindBuffIndex(ModContent.BuffType<灼烧>()));
            }
        }
        private void Elaina_NewTransform(Terraria.On_NPC.orig_Transform orig, NPC self, int newType)
        {
            int player = 255;
            bool IfCountrolByPlayer = false;
            int life = 0;
            int lifemax = 0;
            bool flag = false;
            if (self.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer)
            {
                flag = true;
                player = self.GetGlobalNPC<CrittersGlobalnpc>().player;
                IfCountrolByPlayer = self.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer;
                life = Main.player[player].statLife;
                lifemax = Main.player[player].statLifeMax2;
                if (flag)
                {
                    if ((newType == NPCID.CorruptGoldfish || newType == NPCID.CrimsonGoldfish))
                        return;
                    if (newType == NPCID.CorruptBunny || newType == NPCID.CrimsonBunny)
                        return;
                    if (newType == NPCID.Duck2 || newType == NPCID.Seagull2 || newType == NPCID.DuckWhite2)
                        return;
                }
            }
            orig(self, newType);
            if (flag)
            {
                Main.player[player].AddBuff(ModContent.BuffType<Buffs.小动物>(), 20);

                self.GetGlobalNPC<CrittersGlobalnpc>().player = player;
                self.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer = IfCountrolByPlayer;
                self.life = life;
                self.lifeMax = lifemax;
            }

        }

        private void FilterManager_EndCapture(Terraria.Graphics.Effects.On_FilterManager.orig_EndCapture orig, Terraria.Graphics.Effects.FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            if (render == null)
                CreateRender();
            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }



        private void Main_OnResolutionChanged(Vector2 obj)
        {
            CreateRender();
        }

        private void CreateRender()
        {
            render = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
                false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            render2 = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
                false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }




        public enum SkillType : int
        {
            MagicMissile,
            Water,
            Flame,
            Ice,
            Heal,
            Thunder,
            Wind,
        }
        internal static Dictionary<SkillType, Color> skillColor = new()
        {
            {SkillType.MagicMissile,SkillDamageColor(HitType.HitByMagicMissile)},
            {SkillType.Water,SkillDamageColor(HitType.HitByWater)},
            {SkillType.Flame,SkillDamageColor(HitType.HitByFlame) },
            {SkillType.Ice,SkillDamageColor(HitType.HitByIce)},
            {SkillType.Heal,Color.Green},
            {SkillType.Thunder,SkillDamageColor(HitType.HitByThunder)},
            {SkillType.Wind,SkillDamageColor(HitType.HitByWind)},
        };

        public enum HitType : int
        {
            HitByMagicMissile = -10086,
            HitByWater = -114514,
            HitByFlame = -1919810,
            HitByIce = -4008123,
            HitByThunder = -999,
            HitByWind = -990614,
            HitByEvaporation = -1919811
        }
        internal enum MessageType : byte
        {
            炼金状态,
            同步技能,
            同步伤害,
            Elaina,
            魔力共享,
            雷云,
            小动物ai0,
            父子关系同步,
            小动物状态,
            妮可名单,
        }


        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            MessageType msgType = (MessageType)reader.ReadByte();
            int playernumber = reader.ReadInt32();
            switch (msgType)
            {
                case MessageType.炼金状态:

                    bool 炼制中 = reader.ReadBoolean();
                    bool 搅拌中 = reader.ReadBoolean();
                    if (Main.netMode == NetmodeID.Server)
                    {
                        ModPacket packet = GetPacket();
                        packet.Write((byte)MessageType.炼金状态);
                        packet.Write(playernumber);
                        packet.Write(炼制中);
                        packet.Write(搅拌中);
                        packet.Send(-1, whoAmI);
                    }
                    else
                    {
                        炼金modplayer examplePlayer = Main.player[playernumber].GetModPlayer<炼金modplayer>();
                        examplePlayer.炼制中 = 炼制中;
                        examplePlayer.搅拌中 = 搅拌中;
                        if (炼制中)
                            Main.player[playernumber].velocity = Vector2.Zero;
                        if (!炼制中 || !搅拌中)
                        {
                            Main.player[playernumber].bodyFrame.Y = 0;
                            Main.player[playernumber].bodyFrame.X = 0;
                        }

                    }

                    // SyncPlayer will be called automatically, so there is no need to forward this data to other clients.
                    break;
                case MessageType.小动物状态:
                    {
                        var player = Main.player[playernumber];
                        bool isaCritter = reader.ReadBoolean();
                        int CritterType = reader.ReadInt32();
                        player.GetModPlayer<ElainaModplayer>().isACritter = true;
                        player.GetModPlayer<ElainaModplayer>().CritterType = CritterType;
                        if (Main.netMode == NetmodeID.Server)
                        {
                            Console.WriteLine("player " + playernumber + " isaCritter" + isaCritter + " CritterType" + CritterType);
                            if (isaCritter)
                            {
                                player.AddBuff(ModContent.BuffType<Buffs.小动物>(), 20);
                                int n = NPC.NewNPC(null, (int)player.Center.X, (int)player.Center.Y, CritterType);
                                Main.npc[n].GetGlobalNPC<CrittersGlobalnpc>().player = player.whoAmI;
                                Main.npc[n].GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer = true;
                                Main.npc[n].lifeMax = player.statLifeMax2;
                                Main.npc[n].life = player.statLife;
                                Main.npc[n].GetGlobalNPC<CrittersGlobalnpc>().hp = player.statLifeMax2;
                                Main.npc[n].GetGlobalNPC<CrittersGlobalnpc>().enterworld = true;
                                ModPacket packet = GetPacket();
                                packet.Write((byte)MessageType.小动物状态);
                                packet.Write(playernumber);
                                packet.Write(isaCritter);
                                packet.Write(CritterType);
                                packet.Send(-1, whoAmI);
                            }
                        }
                        else
                        {
                            if (isaCritter)
                            {
                                player.AddBuff(ModContent.BuffType<Buffs.小动物>(), 20);
                                int n = NPC.NewNPC(null, (int)player.Center.X, (int)player.Center.Y, CritterType);
                                Main.npc[n].GetGlobalNPC<CrittersGlobalnpc>().player = player.whoAmI;
                                Main.npc[n].GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer = true;
                                Main.npc[n].lifeMax = player.statLifeMax2;
                                Main.npc[n].life = player.statLife;
                                Main.npc[n].GetGlobalNPC<CrittersGlobalnpc>().hp = player.statLifeMax2;
                                Main.npc[n].GetGlobalNPC<CrittersGlobalnpc>().enterworld = true;
                            }
                        }

                    }
                    break;
                case MessageType.Elaina:
                    {
                        if (Main.netMode == NetmodeID.Server)
                        {
                            bool ifElaina = reader.ReadBoolean();
                            ElainaModplayer examplePlayer = Main.player[playernumber].GetModPlayer<ElainaModplayer>();
                            examplePlayer.Elaina = ifElaina;
                            string text = reader.ReadString();

                            if (text == "Send")
                            {
                                ModPacket packet = GetPacket();
                                packet.Write((byte)MessageType.Elaina);
                                packet.Write(playernumber);//告诉他我是谁
                                packet.Write(ifElaina);
                                packet.Write("Send");
                                packet.Send(-1, whoAmI);
                            }
                            foreach (NPC npc in Main.npc)
                            {
                                if (npc.active)
                                {
                                    if (npc.GetGlobalNPC<Count>().parentid >= 0 || npc.GetGlobalNPC<Count>().childamount > 0)
                                    {//此npc具有父子关系
                                        npc.GetGlobalNPC<Count>().Sync = true;
                                    }
                                }
                            }

                        }
                        else
                        {

                            bool ifElaina = reader.ReadBoolean();
                            string text = reader.ReadString();
                            if (text == "Request")
                            {//收到别人的请求, 因此把数据发给他
                                ModPacket packet = GetPacket();
                                packet.Write((byte)MessageType.Elaina);
                                packet.Write(Main.myPlayer);//告诉他我是谁
                                packet.Write(Main.player[Main.myPlayer].GetModPlayer<ElainaModplayer>().Elaina);
                                packet.Write("Send");
                                packet.Send(playernumber, Main.myPlayer);
                            }
                            else if (text == "Send")
                            {
                                Main.player[playernumber].GetModPlayer<ElainaModplayer>().Elaina = ifElaina;
                            }
                            //ElainaModplayer examplePlayer = Main.player[playernumber].GetModPlayer<ElainaModplayer>();
                            //examplePlayer.Elaina = reader.ReadBoolean();
                            //Main.NewText(examplePlayer.Elaina);
                        }

                    }
                    break;
                case MessageType.魔力共享:
                    {
                        int targetid = reader.ReadInt32();
                        int typeid = reader.ReadInt32();
                        if (Main.netMode == NetmodeID.Server)
                        {
                            bool 蓝量不足 = reader.ReadBoolean();
                            int 试图扣蓝 = reader.ReadInt32();
                            if (typeid == 1)
                            {
                                蓝量共享 examplePlayer = Main.player[playernumber].GetModPlayer<蓝量共享>();
                                ModPacket packet = GetPacket();
                                packet.Write((byte)MessageType.魔力共享);
                                packet.Write(playernumber);
                                packet.Write(targetid);
                                packet.Write(typeid);
                                packet.Write(蓝量不足);
                                packet.Write(试图扣蓝);
                                packet.Send(-1, whoAmI);
                            }
                            if (typeid == 2)
                            {
                                蓝量共享 examplePlayer = Main.player[targetid].GetModPlayer<蓝量共享>();
                                examplePlayer.客机蓝量不足 = true;
                                ModPacket packet = GetPacket();
                                packet.Write((byte)MessageType.魔力共享);
                                packet.Write(playernumber);
                                packet.Write(targetid);
                                packet.Write(typeid);
                                packet.Write(蓝量不足);
                                packet.Write(试图扣蓝);
                                packet.Send(-1, whoAmI);
                            }
                            if (typeid == 3)
                            {
                                ModPacket packet = GetPacket();
                                packet.Write((byte)MessageType.魔力共享);
                                packet.Write(playernumber);
                                packet.Write(targetid);
                                packet.Write(typeid);
                                packet.Write(蓝量不足);
                                packet.Write(试图扣蓝);
                                packet.Send(-1, whoAmI);
                            }

                        }
                        else
                        {

                            bool 蓝量不足 = reader.ReadBoolean();
                            int 试图扣蓝 = reader.ReadInt32();
                            if (Main.myPlayer == targetid)
                            {
                                if (typeid == 1)
                                {
                                    //Main.NewText("试图扣篮");
                                    蓝量共享 examplePlayer = Main.player[playernumber].GetModPlayer<蓝量共享>();
                                    if (!Main.player[targetid].CheckMana(试图扣蓝, true))
                                    {
                                        examplePlayer.客机蓝量不足 = true;
                                        Main.player[targetid].GetModPlayer<蓝量共享>().蓝量不足 = true;
                                        ModPacket packet = GetPacket();
                                        packet.Write((byte)MessageType.魔力共享);
                                        packet.Write(targetid);
                                        packet.Write(playernumber);
                                        packet.Write(2);
                                        packet.Write(蓝量不足);
                                        packet.Write(试图扣蓝);
                                        packet.Send(-1, whoAmI);
                                    }
                                }

                                if (typeid == 2)
                                {
                                    蓝量共享 examplePlayer = Main.player[targetid].GetModPlayer<蓝量共享>();
                                    examplePlayer.客机蓝量不足 = true;

                                }
                            }
                            if (typeid == 3)
                            {
                                蓝量共享 examplePlayer = Main.player[Main.myPlayer].GetModPlayer<蓝量共享>();
                                examplePlayer.客机蓝量不足 = false;
                            }

                            //ElainaModplayer examplePlayer = Main.player[playernumber].GetModPlayer<ElainaModplayer>();
                            //examplePlayer.Elaina = reader.ReadBoolean();
                            //Main.NewText(examplePlayer.Elaina);
                        }
                    }
                    break;
                case MessageType.雷云:
                    int startDark = reader.ReadInt32();
                    int endDark = reader.ReadInt32();
                    bool ifc = reader.ReadBoolean();
                    if (Main.netMode == NetmodeID.Server)
                    {
                        Main.player[playernumber].GetModPlayer<天雷modplayer>().雷云 = ifc;
                        {
                            ModPacket packet = GetPacket();
                            packet.Write((byte)MessageType.雷云);
                            packet.Write(playernumber);//告诉他我是谁
                            packet.Write(startDark);
                            packet.Write(endDark);
                            packet.Write(ifc);
                            packet.Send(-1, whoAmI);
                        }
                    }
                    else
                    {
                        Main.player[Main.myPlayer].GetModPlayer<天雷modplayer>().雷云 = ifc;
                    }
                    break;
                case MessageType.小动物ai0:
                    {
                        int ai0 = reader.ReadInt32();
                        bool sound = reader.ReadBoolean();
                        if (Main.netMode == NetmodeID.Server)
                        {
                            foreach (NPC npc in Main.npc)
                            {
                                if (npc.active && npc.GetGlobalNPC<CrittersGlobalnpc>().IfCountrolByPlayer)
                                {
                                    if (npc.GetGlobalNPC<CrittersGlobalnpc>().player == playernumber)
                                    {
                                        if (ai0 > 0)
                                            npc.GetGlobalNPC<CrittersGlobalnpc>().act = true;
                                        npc.ai[0] = ai0;
                                        if (npc.ai[0] == 22)
                                        {
                                            npc.ai[1] = 100;
                                        }
                                        else if (npc.ai[0] == 7)
                                        {
                                            npc.ai[1] = 260;
                                        }
                                        npc.GetGlobalNPC<CrittersGlobalnpc>().sound = sound;
                                        NetMessage.SendData(23, -1, -1, null, npc.whoAmI);
                                        //Console.WriteLine(npc.ai[0] + " " + npc.ai[1] + " " + npc.ai[3]);
                                    }
                                }
                            }
                        }
                    }

                    break;
                case MessageType.父子关系同步:
                    {
                        int i;
                        int npcId = reader.ReadInt32();
                        int parentid = reader.ReadInt32();
                        int childamount = reader.ReadInt32();
                        int[] child = new int[201];
                        if (childamount > 0)
                        {
                            for (i = 0; i < childamount; i++)
                            {
                                child[i] = reader.ReadInt32();
                                //Console.WriteLine("parentid " + parentid + " amount: " + childamount+" childname: " + Main.npc[child[i]].FullName);
                            }
                        }
                        //Console.WriteLine("parentid "+parentid+" amount: "+ childamount);
                        NPC npc = Main.npc[npcId];
                        if (npc.active && Main.player[Main.myPlayer].active)
                        {
                            npc.GetGlobalNPC<Count>().parentid = parentid;
                            npc.GetGlobalNPC<Count>().childamount = childamount;
                            if (childamount > 0)
                            {
                                for (i = 0; i < childamount; i++)
                                {
                                    //Main.NewText(npc.FullName + " parent: " + parentid + " childamount " + childamount + "child " + Main.npc[child[i]].FullName);

                                    npc.GetGlobalNPC<Count>().childid[i] = child[i];
                                    NPC Children = Main.npc[child[i]];
                                    if (Children.active)
                                        Children.GetGlobalNPC<Count>().parentid = npc.whoAmI;
                                }
                            }

                        }

                    }
                    break;
                case MessageType.妮可名单:
                    {
                        string NpcFullName = reader.ReadString();
                        {
                            foreach (Player player in Main.player)
                            {
                                if (player.GetModPlayer<ElainaModplayer>().Nikeh)
                                {
                                    foreach (Item item in player.armor)
                                    {
                                        if (item.type == ModContent.ItemType<妮可的冒险谭>())
                                        {
                                            player.GetModPlayer<ElainaModplayer>().TheBossJustDefeat = NpcFullName;
                                        }
                                    }
                                }
                            }
                        }

                    }
                    break;
                default:
                    break;
            }
        }


        static public Color SkillDamageColor(HitType skilltype)
        {

            switch (skilltype)//根据魔法种类改变伤害颜色
            {
                case HitType.HitByMagicMissile:
                    return new Color(255, 160, 239);
                case HitType.HitByWater:
                    return new Color(30, 116, 224, 100);
                case HitType.HitByFlame://flame
                    return new Color(255, 107, 42, 155);
                case HitType.HitByIce:
                    return new Color(178, 234, 255, 100);
                case HitType.HitByThunder:
                    return new Color(255, 207, 73, 130);
                case HitType.HitByWind:
                    return new Color(107, 250, 232, 255);
            }
            return Color.White;
        }



    }
}