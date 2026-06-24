using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using System;
using ReLogic.Content;
using Terraria.ModLoader.IO;
using Terraria.Graphics;
using Terraria.ID;
using 伊蕾娜.Items;
using 伊蕾娜.Items.消耗品;
using 伊蕾娜.System;

namespace 伊蕾娜.炼金
{
    public class modifyScreen : ModSystem
    {
        public int startModify = 0;
        public int endModify = 0;
        Vector2 nowZoom = Vector2.One;
        public override void ModifyTransformMatrix(ref SpriteViewMatrix Transform)
        {
            //Main.NewText(Main.GameViewMatrix.Zoom);
            if (!Main.gameMenu)
            {
                if (startModify > 0)
                {
                    if (Main.GameViewMatrix.Zoom != Vector2.One * 2)
                    {
                        Main.GameViewMatrix.Zoom = Vector2.Lerp(Vector2.One * 2, Main.GameViewMatrix.Zoom, startModify / 90f);
                        nowZoom = Main.GameViewMatrix.Zoom;
                        if (startModify > 1) startModify--;
                    }
                    else startModify = 0;
                }
                if (endModify > 0)
                {
                    //if (Main.GameViewMatrix.Zoom != nowZoom)
                    {
                        startModify = 0;
                        Main.GameViewMatrix.Zoom = Vector2.Lerp(Main.GameViewMatrix.Zoom, Vector2.One * 2, endModify / 60f);
                        nowZoom = Main.GameViewMatrix.Zoom;
                        endModify--;
                    }

                }
            }
        }
    }
    public class 炼金modplayer : ModPlayer
    {
        private Asset<Texture2D> hintTexture;//ReLogic.Content
        private Asset<Texture2D> hintTexture2;
        public bool 使用提示 = false;
        public bool 炼制提示 = false;
        public int 炼金lv = 0;
        public int 炼金最大lv = 0;
        public float 炼金exp = 24000;
        public float 炼金最大exp = 1000;
        public bool 炼制中 = false;
        public bool 搅拌中 = false;
        public Item[] itemList = new Item[100];
        public Item[] itemListClone = new Item[100];
        public int 判定间隔 = 0;
        public int 魔力间隔 = 0;
        public int 使用锅炉次数 = 5;
        public int 最大使用次数 = 5;
        public int 稀有物品克隆限制 = 0;
        public int bodyFramecounter = 0;
        public int bodyFrame = 0;
        public float 克隆能量 = 0;
        public int 克隆样本数量 = 0;
        public Item CloneItem = new();
        public Item MainItem = new();
        public Item SubItem1 = new();
        public Item SubItem2 = new();
        public byte 目标配方 = 0;
        public bool mouseOnSpot = false;
        public Tile TheSpotUsingNow;
        public Vector2 TheSpotPosition;
        public int 炸锅倒计时 = 0;
        public int 成功次数 = 0;
        public int 炸锅物品总稀有度 = 0;
        public int 炸锅物品总伤害 = 0;
        internal enum 配方 : byte
        {
            无效配方,
            究极魔力药水,
            焰火药水,
            潋滟药水,
            寒霜药水,
            雷引药水,
            小动物药水,
        }
        //int frame = 0;
        //int oldframe = 0;
        public override void ModifyManaCost(Item item, ref float reduce, ref float mult)
        {
            base.ModifyManaCost(item, ref reduce, ref mult);
        }

        public override void ResetEffects()
        {
            if (Player.GetModPlayer<ElainaModplayer>().Elaina)
            {
                if (炸锅倒计时 > 0)
                {
                    //Main.NewText(炸锅倒计时 +" "+ 克隆能量);
                    炸锅倒计时--;
                    if (Player.whoAmI == Main.myPlayer && 炸锅倒计时 == 1)
                    {
                        炸锅倒计时 = 0;
                        成功次数 = 0;
                        if (搅拌中) 退出炼制();
                        Projectile proj = Projectile.NewProjectileDirect(Entity.GetSource_FromThis(), TheSpotPosition * 16, Vector2.Zero, ModContent.ProjectileType<爆炸>(), 50, 2, Player.whoAmI, 炸锅物品总稀有度, 炸锅物品总伤害);
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj.whoAmI);

                    }
                }
                //Main.NewText(Main.mouseItem.type);
                mouseOnSpot = false;
                //if (Main.hardMode) 炼金最大lv = 10;
                //Main.NewText(Main.HoverItem);
                if (Main.myPlayer == Player.whoAmI)
                {
                    if (Main.mouseRight && Main.mouseRightRelease)
                    {
                        //Main.NewText($"{炼制中} || {搅拌中} || {ModContent.GetInstance<modifyScreen>().startModify == 1}" +
                           // $" 5 {!使用提示} 6{!炼制提示} 7{!Main.LocalPlayer.mouseInterface}");
                    
                    }
                    


                    if (Main.mouseRight && Main.mouseRightRelease && (炼制中 || 搅拌中 || ModContent.GetInstance<modifyScreen>().startModify == 1) &&
                                    !使用提示 && !炼制提示 && !Main.LocalPlayer.mouseInterface)
                    {
                        退出炼制();

                    }
                }

                if (炼制中 || 搅拌中)
                {
                    if (炼制中)
                    {
                        if (bodyFramecounter++ == 7)
                        {
                            bodyFrame++;
                            bodyFramecounter = 0;
                        }
                        if (bodyFrame > 10) bodyFrame = 0;
                    }
                    else if (搅拌中)
                    {
                        Player.portableStoolInfo.SetStats(7, 7, 7);
                        Player.controlUp = true;
                        Player.portableStoolInfo.IsInUse = true;
                        if (bodyFramecounter++ == 7)
                        {
                            bodyFrame++;
                            bodyFramecounter = 0;
                        }
                        if (bodyFrame > 12) bodyFrame = 0;
                        生成物品();
                    }
                    禁用玩家();

                }
                else
                {

                    bodyFrame = 0;
                    bodyFramecounter = 0;
                    if (ModContent.GetInstance<modifyScreen>().startModify == 1 && Main.myPlayer == Player.whoAmI)
                    {
                        退出炼制();
                    }
                }
            }
            
        }
        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {

        }
        public override void SaveData(TagCompound tag)
        {
            tag["炼金最大lv"] = 炼金最大lv;
            tag["炼金最大exp"] = 炼金最大exp;
            tag["炼金exp"] = 炼金exp;
            tag["炼金lv"] = 炼金lv;
        }

        public override void LoadData(TagCompound tag)
        {
            炼金最大lv = (int)tag["炼金最大lv"];
            炼金最大exp = (float)tag["炼金最大exp"];
            炼金exp = (float)tag["炼金exp"];
            炼金lv = (int)tag["炼金lv"];

        }


        public override void PreUpdate()
        {

        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (info.Damage > 0 && (炼制中 || 搅拌中)) 退出炼制();
        }
        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            if (Player.GetModPlayer<ElainaModplayer>().Elaina && Player.whoAmI == Main.myPlayer && mouseOnSpot)
            {

                绘制提示();
            }
            else
            {
                使用提示 = false;
            }

            //if (炼制中)
            //Main.NewText(炼制提示);

            //使用锅炉次数 = 5;
            //Main.NewText(MathHelper.Lerp(60, 1800, (14) * (14) / 100f));
            /*if (Player.active && drawInfo.shadow == 0)
            {
                //Main.NewText(1);
                //oldframe = frame;
                Tile t = Main.tile[0, 0];
                int i = 0;
                int j = 0;

                if (Main.myPlayer == Player.whoAmI && Main.MouseWorld.X > 0 && Main.MouseWorld.Y > 0 && Vector2.Distance(Main.MouseWorld, Player.Center) < 50)
                {
                    i = (int)(Main.MouseWorld.X / 16f);
                    j = (int)(Main.MouseWorld.Y / 16f);
                    if (i - 1 > 0 && j - 1 > 0) t = Main.tile[i, j];
                }
                使用提示 = false;
                炼制提示 = false;
                if (Player.GetModPlayer<ElainaModplayer>().Elaina && Player.whoAmI == Main.myPlayer)
                {
                    if (t.TileType == TileID.CookingPots && t.TileFrameX > 18)
                    {//鼠标处于锅上且足够接近时的所有选项：1使用提示2搅拌提示3放入物品
                     //Main.NewText("yes");
                        绘制提示();//根据烹饪锅的状态来提示
                        if (Main.mouseRight && Main.mouseRightRelease)
                        {
                            使用锅炉(i, j);//检测到鼠标右键则开始使用锅炉
                        }
                    }
                    else
                    {
                        if (Main.mouseLeft && Main.mouseLeftRelease && itemListClone[0] != null)
                            foreach (Item item in itemListClone)
                            {
                                if (item != null) ; //Main.NewText(item + " " + item.rare);
                                else break;
                            }
                        if (Main.mouseMiddle && Main.mouseMiddleRelease)
                        {
                            itemListClone = new Item[100];
                            itemNo = 0;
                        }
                    }

                }
            }*/

        }

        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            if (搅拌中)
            {
                Player.portableStoolInfo.SetStats(7, 7, 7);
                drawInfo.Position -= new Vector2(0, 8);
            }
            if (炼制中 || 搅拌中)
            {
                //Main.OffsetsPlayerOffhand[drawInfo.drawPlayer.bodyFrame.Y / 56].Y = 20;
                drawInfo.compFrontArmFrame.Y = 1000;
                drawInfo.compBackArmFrame.Y = 1000;
                drawInfo.drawPlayer.legFrame.Y = 336;
                drawInfo.drawPlayer.bodyFrame.X = 40;
                drawInfo.drawPlayer.bodyFrame.Y = 56;
                //if (搅拌中) drawInfo.drawPlayer.portableStoolInfo.IsInUse = false;
                //Main.NewText(drawInfo.drawPlayer.bodyFrame.Y);
                //Main.NewText(drawInfo.torsoOffset+" "+drawInfo.helmetOffset);
            }
            base.ModifyDrawInfo(ref drawInfo);
        }

        public void 禁用玩家()
        {
            //Main.NewText(CloneItem.stack);
            Player.controlRight = false;
            Player.releaseRight = true;
            Player.controlLeft = false;
            Player.releaseLeft = true;
            Player.controlJump = false;
            Player.controlUp = false;
            Player.controlDown = false;
            Player.controlHook = false;
            Player.controlMount = false;
            Player.releaseMount = false;
            Player.controlThrow = false;
            Player.gravControl = false;
            Player.controlUseItem = false;
            //Player.controlUseTile = false;
        }
        public void 退出炼制()
        {
            炼制物品UI.Visible = false;
            配方表UI.Visible = false;
            if(炸锅倒计时>120) 炸锅倒计时 = 0;

            成功次数 = 0;
            目标配方 = 0;
            CloneItem = new Item();
            MainItem = new Item();
            SubItem1 = new Item();
            SubItem2 = new Item();
            克隆样本数量 = 0;
            {//退出炼制状态
                ModContent.GetInstance<modifyScreen>().endModify = 60;
                Player.bodyFrame.Y = 0;
                Player.bodyFrame.X = 0;
                炼制中 = false;
                判定间隔 = 0;
                if (搅拌中)
                {
                    搅拌中 = false;
                }

                if (Main.netMode == 1)
                {
                    //Main.NewText("主机发送" + 炼制中);
                    ModPacket packet = Mod.GetPacket();
                    packet.Write((byte)伊蕾娜.MessageType.炼金状态);
                    packet.Write(Player.whoAmI);
                    packet.Write(炼制中);
                    packet.Write(搅拌中);
                    packet.Send(-1, -1);
                }
            }
        }
        public void 使用锅炉()
        {

            Player.velocity = Vector2.Zero;

            if (使用提示 && !搅拌中)
            {

                炼制物品UI.Visible = true;

                //if (使用锅炉次数 > 0)
                {
                    Player.direction = 1;

                    炼制中 = true;
                    if (ModContent.GetInstance<modifyScreen>().startModify == 0) ModContent.GetInstance<modifyScreen>().startModify = 90;

                    if (Main.netMode == 1)
                    {
                        //Main.NewText("主机发送" + 炼制中);
                        ModPacket packet = Mod.GetPacket();
                        packet.Write((byte)伊蕾娜.MessageType.炼金状态);
                        packet.Write(Player.whoAmI);
                        packet.Write(炼制中);
                        packet.Write(搅拌中);
                        packet.Send(-1, -1);
                    }
                    //搅拌中 = true;
                    if (Player.mount.Active)
                    {
                        Player.mount.Dismount(Player);
                    }

                    //Player.gravDir = 1;
                }
                /*else
                {
                    if (Language.ActiveCulture.Name == "en-US")
                    {
                        Main.NewText("I'm already too tired today");
                    }
                    else Main.NewText("今天已经太累了");
                }*/
            }


            if (炼制提示)
            {
                if (ModContent.GetInstance<炼制物品UISystem>().UI.inv[1].IsAir)
                {
                    if (Language.ActiveCulture.Name == "en-US")
                    {
                        Main.NewText("The air is not enough to make something");
                    }
                    else Main.NewText("空气可做不出东西来");

                }
                else if (ModContent.GetInstance<炼制物品UISystem>().UI.inv[0].type == ItemID.Obsidian ||
                    ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].type == ItemID.Obsidian ||
                    (克隆能量 > 0 && ModContent.GetInstance<炼制物品UISystem>().UI.inv[0].IsAir && ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].IsAir))
                {
                    if (增加能量() | (克隆能量 > 0))
                    {
                        CloneItem = new Item(ModContent.GetInstance<炼制物品UISystem>().UI.inv[1].type, ModContent.GetInstance<炼制物品UISystem>().UI.inv[1].stack);
                        //CloneItem.stack += ModContent.GetInstance<炼制物品UISystem>().UI.inv[1].stack;
                        //CloneItem.type = ModContent.GetInstance<炼制物品UISystem>().UI.inv[1].type;
                        //CloneItem.rare = ModContent.GetInstance<炼制物品UISystem>().UI.inv[1].rare;
                        ModContent.GetInstance<炼制物品UISystem>().UI.inv[1].TurnToAir();
                        if (CloneItem.stack > 1) Main.item[Player.QuickSpawnItem(null, CloneItem.type, CloneItem.stack - 1)].noGrabDelay = 30;
                        炼制物品UI.Visible = false;
                        搅拌中 = true;
                        炼制中 = false;
                        //使用锅炉次数--;
                        Player.velocity = Vector2.Zero;
                        bool Equippable = CloneItem.DamageType.Type != 0 | CloneItem.accessory |//是武器或饰品
                        CloneItem.bodySlot >= 0 | CloneItem.headSlot >= 0 | CloneItem.legSlot >= 0; //是装备
                        bool Material = CloneItem.material && CloneItem.type != ItemID.Obsidian && CloneItem.rare >= 0;//&& CloneItem.type != ItemID.FallenStar
                        if (Equippable || !Material)//不可炼制
                        {
                            //Main.NewText(Equippable + "6"+ CloneItem.material);
                            /*{
                                if (Language.ActiveCulture.Name == "en-US")
                                {
                                    Main.NewText("I can't seem to make this item...");
                                }
                                else Main.NewText("似乎无法炼制这个物品..");
                            }*/
                            炸锅();
                        }
                        if (Main.netMode == 1)
                        {
                            ModPacket packet = Mod.GetPacket();
                            packet.Write((byte)伊蕾娜.MessageType.炼金状态);
                            packet.Write(Player.whoAmI);
                            packet.Write(炼制中);
                            packet.Write(搅拌中);
                            packet.Send(-1, -1);
                        }
                    }
                }
                else if (!ModContent.GetInstance<炼制物品UISystem>().UI.inv[0].IsAir || !ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].IsAir)//查询配方
                {

                    MainItem = new Item(ModContent.GetInstance<炼制物品UISystem>().UI.inv[1].type, ModContent.GetInstance<炼制物品UISystem>().UI.inv[1].stack);
                    ModContent.GetInstance<炼制物品UISystem>().UI.inv[1].TurnToAir();
                    目标配方 = 查询配方();
                    //SubItem1.TurnToAir();
                    //if(SubItem2!=null)
                    //SubItem2.TurnToAir();
                    炼制物品UI.Visible = false;
                    搅拌中 = true;
                    炼制中 = false;
                    Player.velocity = Vector2.Zero;
                    if (Main.netMode == 1)
                    {
                        ModPacket packet = Mod.GetPacket();
                        packet.Write((byte)伊蕾娜.MessageType.炼金状态);
                        packet.Write(Player.whoAmI);
                        packet.Write(炼制中);
                        packet.Write(搅拌中);
                        packet.Send(-1, -1);
                    }
                }


            }

        }

        public void 绘制提示()
        {
            if (!炼制中)
            {
                使用提示 = true;
                /*string s1 = "炼金等级: ";
                string s2 = "下一等级需要经验: ";
                if (Language.ActiveCulture.Name == "en-US")
                {
                    s1 = "Alchemy level: ";
                    s2 = "Experience required for the next level: ";
                }
                string lv = s1 + 炼金lv.ToString()
                , exp = s2 + 炼金exp.ToString() + "/" + 炼金最大exp.ToString();
                if (炼金lv >= 炼金最大lv) exp = "max" + "/" + "max";
                Utils.DrawBorderString(Main.spriteBatch, lv,
                    Main.MouseWorld - Main.screenPosition + new Vector2(-30, 30), Color.Gold, 1f, 0f, 0f, -1);
                Utils.DrawBorderString(Main.spriteBatch, exp,
                    Main.MouseWorld - Main.screenPosition + new Vector2(-30, 60), Color.White, 1f, 0f, 0f, -1);*/

                if (hintTexture == null) hintTexture = Mod.Assets.Request<Texture2D>("炼金/使用提示");
                Main.spriteBatch.Draw(
                    hintTexture.Value, // The texture to render.
                    Main.MouseWorld - Main.screenPosition, // Position to render at.
                    null, // Source rectangle.
                    Color.White, // Color.
                    0, // Rotation.
                    hintTexture.Size() * 0.5f, // Origin. Uses the texture's center.
                    1f, // Scale.
                    SpriteEffects.None, // SpriteEffects.
                    0 // 'Layer'. This is always 0 in Terraria.
                );
            }
            else if (炼制中)
            {
                if (Main.mouseItem.IsAir)//没有物品则搅拌提示
                {
                    炼制提示 = true;

                    if (hintTexture2 == null) hintTexture2 = Mod.Assets.Request<Texture2D>("炼金/炼制提示");
                    Main.spriteBatch.Draw(
                        hintTexture2.Value, // The texture to render.
                        Main.MouseWorld - Main.screenPosition, // Position to render at.
                        new Rectangle(0, 12 * 40, 40, 40), // Source rectangle.
                        Color.White, // Color.
                        0, // Rotation.
                        hintTexture.Size() * 0.5f, // Origin. Uses the texture's center.
                        1f, // Scale.
                        SpriteEffects.None, // SpriteEffects.
                        0 // 'Layer'. This is always 0 in Terraria.
                    );
                }
            }
        }
        public bool 增加能量()
        {
            bool success = false;
            if (ModContent.GetInstance<炼制物品UISystem>().UI.inv[0].type == ItemID.Obsidian)
            {
                克隆能量 += 100 * ModContent.GetInstance<炼制物品UISystem>().UI.inv[0].stack;
                ModContent.GetInstance<炼制物品UISystem>().UI.inv[0].TurnToAir();
                success = true;
            }
            else if (ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].type == ItemID.Obsidian)
            {
                克隆能量 += 100 * ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].stack;
                ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].TurnToAir();
                success = true;
            }
            return success;

        }
        void 炸锅()
        {
            炸锅倒计时 = 180;
            炸锅物品总稀有度 += Math.Abs(MainItem.rare);
            炸锅物品总稀有度 += Math.Abs(SubItem1.rare);
            炸锅物品总稀有度 += Math.Abs(SubItem2.rare);
            炸锅物品总伤害 += MainItem.damage;
            炸锅物品总伤害 += SubItem1.damage;
            炸锅物品总伤害 += SubItem2.damage;
        }
        public void 克隆物品(Item item)
        {
            float 稀有度影响时间 = MathHelper.Lerp(30, 1800, item.rare * item.rare / 100f);
            if (稀有度影响时间 > 1800) 稀有度影响时间 = 1800;
            float k = MathHelper.Clamp(item.maxStack * item.maxStack / 998001f, 0, 1);
            float 最大堆叠影响时间 = MathHelper.Lerp(1f, 0.1f, k);
            float 炼制时间 = 稀有度影响时间 * 最大堆叠影响时间;

            if (判定间隔++ > 炼制时间)
            {
                if (克隆能量 > 0)
                {
                    if (Player.CheckMana(Player.HeldItem, (int)(10*Player.manaCost*最大堆叠影响时间), true))
                    {
                        Player.manaRegenDelay = 120;
                        判定间隔 = 0;//道具越稀有，可最大堆叠数量越少，则炼制越慢
                                 //int newItem= Item.NewItem(null,(int)Player.position.X, (int)Player.position.Y, Player.width, Player.height, itemList[0].type, 1, false, 0, false, false);
                        if (!(炸锅倒计时 > 0))
                        {
                            克隆能量 -= 10 * MathHelper.Lerp(1, 25, item.rare * item.rare / 100f);
                            if (克隆能量 >= 0)
                            {
                                bool SpawnItem = true;
                                if (item.rare > Player.GetModPlayer<EXPmodplayer>().GetLv())//越级炼制，有概率成功，成功率会随成功次数不断降低
                                {
                                    float 概率 = (10 + 成功次数 *  (item.rare- Player.GetModPlayer<EXPmodplayer>().GetLv()));
                                    if (概率 > 30) 概率 = 30;
                                    if (Main.rand.Next(100) > 概率)//成功
                                    {
                                        成功次数++;
                                    }
                                    else
                                    {
                                        SpawnItem = false;
                                        炸锅();
                                    }
                                }
                                if (SpawnItem)
                                {
                                    int i = Player.QuickSpawnItem(null, item.type, 1);
                                    Main.item[i].noGrabDelay = 100;
                                    Main.item[i].velocity = new Vector2(Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, -3));
                                }
                            }
                            else
                            {
                                克隆能量 += 10 * MathHelper.Lerp(1, 25, item.rare * item.rare / 100f);
                                if (Language.ActiveCulture.Name == "en-US")
                                {
                                    Main.NewText("Insufficient Obsidian");
                                }
                                else Main.NewText("黑曜石不足");

                                退出炼制();
                            }
                        }
                        //int item = Main.item[Item.NewItem(null, (int)Player.position.X, (int)Player.position.Y, Player.width, Player.height, itemList[0].type, itemList[0].stack, false, 0, false, false)].noGrabDelay = 60;
                        //if (Main.netMode == 1) NetMessage.SendData(21, -1, -1, null, item, 0f, 0f, 0f, 0, 0, 0);
                        if (克隆能量 < 0) 克隆能量 = 0;
                    }
                }
                else
                {
                    if (Language.ActiveCulture.Name == "en-US")
                    {
                        Main.NewText("Insufficient Obsidian");
                    }
                    else Main.NewText("黑曜石不足");

                    退出炼制();
                }
            }
        }

        public void 生成物品()
        {
            //克隆物品
            if (CloneItem.stack > 0)
            {
                克隆物品(CloneItem);
            }
            else
            {
                试图炼制配方(目标配方);
            }
        }
        public byte 查询配方()
        {
            int i = 0;
            if (!ModContent.GetInstance<炼制物品UISystem>().UI.inv[0].IsAir)
            {
                SubItem1 = new Item(ModContent.GetInstance<炼制物品UISystem>().UI.inv[0].type, ModContent.GetInstance<炼制物品UISystem>().UI.inv[0].stack);
                ModContent.GetInstance<炼制物品UISystem>().UI.inv[0].TurnToAir();
            }
            else {
                SubItem1 = new Item(ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].type, ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].stack);
                ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].TurnToAir();

            }
            if (!ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].IsAir)
            {
                if (ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].type == SubItem1.type)//说明两个栏物品一样，有脑残兄弟们
                {
                    SubItem1.stack += ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].stack;
                    ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].TurnToAir();
                }
                else
                {
                    SubItem2 = new Item(ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].type, ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].stack);
                    ModContent.GetInstance<炼制物品UISystem>().UI.inv[2].TurnToAir();
                }
            }
            switch (MainItem.type) //此处subitem1一定有物品，2不一定，2如果有物品说明双配方
            {
                case ItemID.LesserManaPotion:
                    if((SubItem1.type== ItemID.DirtBlock || SubItem1.type == ItemID.StoneBlock || SubItem1.type == ItemID.ClayBlock || SubItem1.type == ItemID.MudBlock) && SubItem2.IsAir)
                    {
                        return (byte)配方.究极魔力药水;
                    }
                    else return (byte)配方.无效配方;
                case ItemID.MagicPowerPotion://魔能药水
                    if (SubItem2.IsAir)
                    {
                        return (byte)配方.无效配方;
                    }//此时一定有两种配方
                    if(SubItem1.type ==ItemID.FallenStar || SubItem2.type == ItemID.FallenStar)
                    {
                        int type = SubItem1.type;
                        if(SubItem1.type == ItemID.FallenStar) type = SubItem2.type;
                        switch (type)
                        {
                            case ItemID.Fireblossom:
                                return (byte)配方.焰火药水;
                            case ItemID.Coral:
                                return (byte)配方.潋滟药水;
                            case ItemID.Shiverthorn:
                                return (byte)配方.寒霜药水;
                        }
                        return (byte)配方.无效配方;
                    }
                    break;
                case ItemID.GenderChangePotion:
                    if (!SubItem2.IsAir)
                        return (byte)配方.无效配方;
                    if (SubItem1.type == ItemID.FallenStar)
                    {
                        return (byte)配方.小动物药水;
                    }
                    break;
            }
            if (MainItem.type == ModContent.ItemType<潋滟药水>()
                || MainItem.type == ModContent.ItemType<焰火药水>()
                || MainItem.type == ModContent.ItemType<寒霜药水>())
            {
                if (MainItem.type + SubItem1.type + SubItem2.type == ModContent.ItemType<潋滟药水>()
                    + ModContent.ItemType<焰火药水>()
                    + ModContent.ItemType<寒霜药水>()) return (byte)配方.雷引药水;
            }
            //Main.NewText(SubItem1.Name + " " + SubItem1.stack+" "+ SubItem2.Name + " " + SubItem2.stack);
            return (byte)配方.无效配方;
        }
        public void 试图炼制配方(byte 配方类型)
        {
            if (炸锅倒计时 <= 0)
            {
                int finalItemType = -1;
                bool SpawnItem = false;
                bool 正确比例差 = false;
                int 主配方需求量 = 0;
                int 副配方1需求量 = 0;
                int 副配方2需求量 = 0;
                int i = -1;
                Player.manaRegenDelay = 60;
                if (判定间隔++ > 60 - Player.GetModPlayer<EXPmodplayer>().GetLv() * 3)
                {
                    switch (配方类型)
                    {
                        case (byte)配方.究极魔力药水:
                            {
                                if (SubItem1.stack >= 3 && MainItem.stack >= 1)//找到合适副材料且数量充足
                                {
                                    if (Player.CheckMana(Player.HeldItem, (int)(10*Player.manaCost), true))//耗蓝制作物品
                                    {
                                        SpawnItem = true;
                                        float 比例差 = SubItem1.stack / MainItem.stack;//正常比例为3
                                        if (比例差 == 3) 正确比例差 = true;
                                        判定间隔 = 0;
                                        finalItemType = ModContent.ItemType<究极魔力药水>();
                                        主配方需求量 = 1;
                                        副配方1需求量 = 3;
                                    }
                                }
                                else
                                {
                                    退出炼制();
                                }
                            }
                            break;
                        case (byte)配方.潋滟药水:
                            if (SubItem1.stack >= 1 && MainItem.stack >= 1&& SubItem2.stack>=1)
                            {
                                if (Player.CheckMana(Player.HeldItem, (int)(10 * Player.manaCost), true))//耗蓝制作物品
                                {
                                    SpawnItem = true;//必要
                                    正确比例差 = true;//必要
                                    if (SubItem1.stack / MainItem.stack != 10) 正确比例差 = false;
                                    if (SubItem2.stack / MainItem.stack != 10) 正确比例差 = false;
                                    判定间隔 = 0;//必要
                                    finalItemType = ModContent.ItemType<潋滟药水>();//必要
                                    主配方需求量 = 1;//必要
                                    副配方1需求量 = 10;//必要
                                    副配方2需求量 = 10;
                                }
                            }
                            else 退出炼制();
                            break;
                        case (byte)配方.焰火药水:
                            if (SubItem1.stack >= 1 && MainItem.stack >= 1 && SubItem2.stack >= 1)
                            {
                                if (Player.CheckMana(Player.HeldItem, (int)(10 * Player.manaCost), true))//耗蓝制作物品
                                {
                                    SpawnItem = true;

                                    正确比例差 = true;
                                    if (SubItem1.stack / MainItem.stack != 10) 正确比例差 = false;
                                    if (SubItem2.stack / MainItem.stack != 10) 正确比例差 = false;
                                    判定间隔 = 0;
                                    finalItemType = ModContent.ItemType<焰火药水>();
                                    主配方需求量 = 1;
                                    副配方1需求量 = 10;
                                    副配方2需求量 = 10;
                                }
                            }
                            else 退出炼制();

                            break;
                        case (byte)配方.寒霜药水:
                            if (SubItem1.stack >= 1 && MainItem.stack >= 1 && SubItem2.stack >= 1)
                            {
                                if (Player.CheckMana(Player.HeldItem, (int)(10 * Player.manaCost), true))//耗蓝制作物品
                                {
                                    SpawnItem = true;
                                    正确比例差 = true;
                                    if (SubItem1.stack / MainItem.stack != 10) 正确比例差 = false;
                                    if (SubItem2.stack / MainItem.stack != 10) 正确比例差 = false;
                                    判定间隔 = 0;
                                    finalItemType = ModContent.ItemType<寒霜药水>();
                                    主配方需求量 = 1;
                                    副配方1需求量 = 10;
                                    副配方2需求量 = 10;
                                }
                            }
                            else 退出炼制();

                            break;
                        case (byte)配方.雷引药水:
                            if (SubItem1.stack >= 1 && MainItem.stack >= 1 && SubItem2.stack >= 1)
                            {
                                if (Player.CheckMana(Player.HeldItem, (int)(10 * Player.manaCost), true))//耗蓝制作物品
                                {
                                    SpawnItem = true;
                                    正确比例差 = true;
                                    if (SubItem1.stack / MainItem.stack != 1) 正确比例差 = false;
                                    if (SubItem2.stack / MainItem.stack != 1) 正确比例差 = false;
                                    判定间隔 = 0;
                                    finalItemType = ModContent.ItemType<雷引药水>();
                                    主配方需求量 = 1;
                                    副配方1需求量 = 1;
                                    副配方2需求量 = 1;
                                }
                            }
                            else 退出炼制();
                            break;
                        case (byte)配方.小动物药水:
                            if (SubItem1.stack >= 10 && MainItem.stack >= 1)
                            {
                                if (Player.CheckMana(Player.HeldItem, (int)(10 * Player.manaCost), true))//耗蓝制作物品
                                {
                                    SpawnItem = true;
                                    正确比例差 = true;
                                    if (SubItem1.stack / MainItem.stack != 10) 正确比例差 = false;
                                    判定间隔 = 0;
                                    finalItemType = ModContent.ItemType<小动物药水>();
                                    主配方需求量 = 1;
                                    副配方1需求量 = 10;
                                }
                            }
                            else 退出炼制();

                            break;
                        case (byte)配方.无效配方:
                            if (MainItem.damage > 0 || SubItem1.damage > 0 || SubItem2.damage > 0)
                            {
                                SpawnItem = false;
                                炸锅();
                                break;
                            }
                            int rand = Main.rand.Next(10);
                            if (rand < 6)//6成概率炸了
                            {
                                炸锅();
                                break;
                            }
                            if (rand == 6)
                            {
                                break;
                            }
                            int range = 0;
                            if (SubItem1.stack >= 1) range++;
                            if (SubItem2.stack >= 1) range++;

                            rand = Main.rand.Next(range);
                            if (rand == 0)
                            {
                                主配方需求量 = 1;
                                SpawnItem = true;
                                finalItemType = MainItem.type;
                            }
                            if (rand == 1)
                            {
                                副配方1需求量 = 1;
                                SpawnItem = true;
                                finalItemType = SubItem1.type;
                            }
                            if (rand == 2)
                            {
                                副配方2需求量 = 1;
                                SpawnItem = true;
                                finalItemType = SubItem2.type;
                            }
                            break;
                    }
                }

                if (SpawnItem)//成功消耗魔力开始生成物品
                {
                    /*if (!正确比例差)//配方比不对
                    {

                        int rand = Main.rand.Next(10);
                        if (rand == 0)//十分之一的几率炸锅
                        {
                            SpawnItem = false;
                            炸锅();
                        }
                        if (rand == 1) SpawnItem = false;//十分之一不生成
                        if (rand == 2)//十分之一出主物品或副物品
                        {
                            SpawnItem = false;
                            i = Player.QuickSpawnItem(null, MainItem.type, 1);
                        }
                        if (rand == 3)//十分之一出主物品
                        {
                            SpawnItem = false;
                            i = Player.QuickSpawnItem(null, MainItem.type, 1);

                        }
                        if (rand == 4)//十分之一出副物品
                        {
                            SpawnItem = false;
                            if(副配方2需求量==0)
                                i = Player.QuickSpawnItem(null, SubItem1.type, 1);
                            else
                            {
                                if (Main.rand.NextBool())
                                {
                                    i = Player.QuickSpawnItem(null, SubItem1.type, 1);
                                }
                                else i = Player.QuickSpawnItem(null, SubItem2.type, 1);
                            }
                        }
                    }*/
                    if (SpawnItem)
                    {
                        MainItem.stack -= 主配方需求量;
                        SubItem1.stack -= 副配方1需求量;
                        if(副配方2需求量>0) SubItem2.stack -= 副配方2需求量;
                        //itemListClone[主物品位置 - 1].stack--;
                        Player.manaRegenDelay = 120;
                        i = Player.QuickSpawnItem(null, finalItemType, 1); 
                    }

                }
                if (i != -1)
                {
                    Main.item[i].noGrabDelay = 30;
                    Main.item[i].velocity = new Vector2(Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, -3));
                }
            }

        }


        public class 炼金等级上限 : GlobalItem
        {
            public override void OnSpawn(Item item, IEntitySource source)
            {
                base.OnSpawn(item, source);
            }
            public override bool CanPickup(Item item, Player player)
            {
                var p = player.GetModPlayer<炼金modplayer>();

                if (p.搅拌中)
                {
                    return false;
                }
                return base.CanPickup(item, player);
            }

        }


    }
}
