using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using 伊蕾娜.Projectiles;
using 伊蕾娜.Buffs;
using 伊蕾娜.Items.魔法书;
using static 伊蕾娜.伊蕾娜;
using Terraria.Localization;

namespace 伊蕾娜
{
    public class Count : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public float count = 0;
        public float oldcount = 0;
        float 所需冻结=1000;
        public bool 冻结 = false;
        public int 冻结时间 = 0;
        public int parentid = -1;
        public int []childid=new int[201];
        public int childamount = 0;
        public int 冻结下降速率 = 1;
        public int 短暂冻结 = 0;
        public bool hitbyMissle = false;
        static public  Effect npcFrozonEffect;
        static Asset<Texture2D> IceIcon1;
        static Asset<Texture2D> IceIcon2;
        public int 冻结槽=0;
        public float 添加冻结延迟 = 0;
        public int 冻结下降延迟 = 0;
        public int 冻结buffCD = 0;
        public Rectangle FrozenFrame;
        public Vector2 FrozenRandonShader = Vector2.Zero;
        public int 水魔法引爆延迟=-1;
        public int 水魔法存量 = 0;
        public bool MarkSelfAsChild = false;
        public Vector2 pos;
        public bool Sync = false;
        public int spawn = -1;
        public int lightning = 0;
        public int 蒸发cd = 0;
        public override void Load()
        {

            base.Load();
        }

        void SyncParendChildren(NPC npc)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)MessageType.父子关系同步);
                packet.Write(Main.myPlayer);
                packet.Write(npc.whoAmI);
                packet.Write(parentid);
                packet.Write(childamount);
                if(childamount > 0)
                {
                    for (int i = 0; i < childamount; i++)
                    {
                        packet.Write(childid[i]);
                    }
                }

                //Console.WriteLine("parentid " + parentid + " amount: " + childamount);

                packet.Send(-1, Main.myPlayer);
            }

        }
        
        public override void ModifyHitNPC(NPC npc, NPC target, ref NPC.HitModifiers modifiers)
        {
            //modifiers.
            base.ModifyHitNPC(npc, target, ref modifiers);
        }
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {   

            //spriteBatch.End();
            //spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            if (冻结槽>0|| npc.HasBuff<冻结>())
            {

                //Console.WriteLine(npc.FullName + " server: " + Main.netMode+" parent? "+parentid);
                //Main.NewText(冻结槽+" "+ 所需冻结);
                float 进程 =1- 冻结槽 / 所需冻结;
                //Main.NewText(进程);
                Color c = Color.White;
                if (npc.HasBuff<冻结>())//上冰冻shader
                {
                    进程 = 0;
                    //c =new Color(90,192,220);

                    GraphicsDevice gd = Main.instance.GraphicsDevice;
                    /*Main.graphics.GraphicsDevice.Textures[1] = Mod.Assets.Request<Texture2D>("Projectiles/MagicIce/FrozenTexture").Value;
                    npcFrozonEffect.Parameters["widthRatio"].SetValue(TextureAssets.Npc[npc.type].Width() / 1920f);
                    npcFrozonEffect.Parameters["heightRatio"].SetValue(TextureAssets.Npc[npc.type].Height() / 900f);
                    //Main.NewText(npc.frame + " "+npc.width);
                    npcFrozonEffect.CurrentTechnique.Passes["FrozenEffect"].Apply();*/
                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                        DepthStencilState.None, RasterizerState.CullCounterClockwise, null);
                    gd.SetRenderTarget(render2);//换到新的空白画布
                    gd.Clear(Color.Transparent);//用透明清除
                    Vector2 x = npc.Center - Main.screenPosition;
                    float startX =((x.X - Main.screenWidth / 2) / Main.screenWidth)* Main.GameViewMatrix.Zoom.X;//npc在屏幕中心时，双值为0,在最左侧时，x为-0.5（使npc位于shader中心）
                    float startY = ((x.Y - Main.screenHeight / 2) / Main.screenHeight)* Main.GameViewMatrix.Zoom.Y;

                    npcFrozonEffect.Parameters["widthRatio"].SetValue(startX+ FrozenRandonShader.X);
                    npcFrozonEffect.Parameters["heightRatio"].SetValue(startY + FrozenRandonShader.Y);
                    npcFrozonEffect.CurrentTechnique.Passes["FrozenEffect2"].Apply();
                    Main.graphics.GraphicsDevice.Textures[1] = Mod.Assets.Request<Texture2D>("Projectiles/MagicIce/FrozenTexture").Value;
                    spriteBatch.Draw(render, Vector2.Zero, Color.White);//应用自己的shader并绘制npc于空白画布

                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                    gd.SetRenderTarget(Main.screenTarget);//换回屏幕renderTarget
                    gd.Clear(Color.Transparent);//用透明清除
                    spriteBatch.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);//绘制自己的rendertarget
                    spriteBatch.Draw(render2, Vector2.Zero, Color.White);//绘制自己的rendertarget
                    //Main.NewText("haha");
                }
                float scale = MathHelper.Lerp(1, 3, (npc.width * npc.height) / 10000f);
                scale = MathHelper.Clamp(scale, 1, 3);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.AnisotropicClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                Main.spriteBatch.Draw(IceIcon2.Value, npc.Center - Main.screenPosition + new Vector2(0, -npc.height), new Rectangle(0, 0, IceIcon2.Width(), IceIcon2.Height()),
                c, 0, new Vector2(25, 25), scale, SpriteEffects.None, 0f);

                Main.spriteBatch.Draw(IceIcon2.Value, npc.Center - Main.screenPosition + new Vector2(0, -npc.height), new Rectangle(0, 0, IceIcon2.Width(), (int)(IceIcon2.Height() * 进程)),
                Color.Black, 0, new Vector2(25, 25), scale, SpriteEffects.None, 0f);
                //Main.spriteBatch.Draw(IceIcon1.Value, npc.Center - Main.screenPosition + new Vector2(0, -npc.height), new Rectangle(0, 0, IceIcon1.Width(), IceIcon1.Height()),
                //Color.White, 0, new Vector2(25, 25), scale, SpriteEffects.None, 0f);

            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (IceIcon1 == null)
            {
                IceIcon1 = Mod.Assets.Request<Texture2D>("Projectiles/MagicIce/IceIcon1");
                IceIcon2 = Mod.Assets.Request<Texture2D>("Projectiles/MagicIce/IceIcon2");
            }

            count = 0f;
            
            if (npc.HasBuff<冻结>())
            {
                if (FrozenFrame != npc.frame) npc.frame = FrozenFrame;
                GraphicsDevice gd = Main.instance.GraphicsDevice;

                npcFrozonEffect = Mod.Assets.Request<Effect>("Projectiles/Effects/Content/NpcFrozen", AssetRequestMode.ImmediateLoad).Value;
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                gd.SetRenderTarget(Main.screenTargetSwap);//在这个上面绘制一遍原图，相当于“保存”
                spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);


                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                gd.SetRenderTarget(render);//等待原版绘制npc到我的render上，嘻嘻
                gd.Clear(Color.Transparent);

            }

            return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitByProjectile(npc, projectile, ref modifiers);
        }
        
        public static int 攻击类型(float ProjKnock)
        {
            switch (ProjKnock)
            {
                case (float)HitType.HitByMagicMissile:
                    return 1;
                case (float)HitType.HitByWater:
                    return 2;
                case (float)HitType.HitByFlame:
                    return 3;
                case (float)HitType.HitByIce:
                    return 4;
                case (float)HitType.HitByIce-1://强化冰锥
                    return 5;
                case (float)HitType.HitByIce-2://冰雾
                    return 6;
                case (float)HitType.HitByThunder:
                    return 7;
                case (float)HitType.HitByWind:
                    return 8;
                case (float)HitType.HitByFlame - 1://蒸发

                    return 9;
            }
            return 0;
        }
        public static void onHitEffect(NPC npc, int 攻击类型, float damage, bool crit)
        {
            if (攻击类型 > 0)//说明为伊蕾娜魔法
            {
                Color color2 = Color.White;
                string text = (int)damage + "";

                switch (攻击类型)//根据魔法种类改变伤害颜色
                {
                    case 0:
                        break;
                    case 1:
                        color2 = new Color(255, 160, 239);
                        break;
                    case 2:
                        if (Main.netMode != 2)
                        {
                            npc.AddBuff(BuffID.Wet, 300);
                            if (NpcIsOnFire(npc)) ClearNpcFire(npc);
                        }
                        color2 = new Color(30, 116, 224, 150);
                        break;
                    case 3://flame
                        color2 = new Color(221, 0, 27, 170);
                        npc.AddBuff(ModContent.BuffType<Buffs.灼烧>(), 500);
                        break;
                    case 4://冰锥
                        color2 = new Color(154, 204, 203, 150);
                        if (!npc.immortal)
                        {
                            NPC target = npc;
                            if (npc.realLife >= 0 || npc.GetGlobalNPC<Count>().parentid >= 0)
                            {
                                target = Main.npc[npc.realLife >= 0 ? npc.realLife : npc.GetGlobalNPC<Count>().parentid];
                                //Main.NewText(target.GetGlobalNPC<Count>().childamount);
                            }
                            if (target.GetGlobalNPC<Count>().冻结buffCD <= 0 && target.GetGlobalNPC<Count>().添加冻结延迟 <= 0)
                            {
                                target.GetGlobalNPC<Count>().冻结槽 += 150;
                                target.GetGlobalNPC<Count>().冻结下降延迟 = 300;
                                target.GetGlobalNPC<Count>().添加冻结延迟 = 20;
                            }
                        }
                        break;
                    case 5://强化冰锥
                        color2 = new Color(154, 204, 203, 150);
                        if (!npc.immortal)
                        {
                            NPC target = npc;
                            if (npc.realLife >= 0 || npc.GetGlobalNPC<Count>().parentid >= 0)
                            {
                                target = Main.npc[npc.realLife >= 0 ? npc.realLife : npc.GetGlobalNPC<Count>().parentid];
                            }
                            if (target.GetGlobalNPC<Count>().冻结buffCD <= 0)//可以再次冻结敌人
                            {
                                target.GetGlobalNPC<Count>().冻结槽 += 150;
                                target.GetGlobalNPC<Count>().冻结下降延迟 = 300;
                            }
                        }
                        break;
                    case 6://冰雾
                        color2 = new Color(154, 204, 203, 150);
                        if (!npc.immortal)
                        {
                            NPC target = npc;
                            if (npc.realLife >= 0 || npc.GetGlobalNPC<Count>().parentid >= 0)
                            {
                                target = Main.npc[npc.realLife >= 0 ? npc.realLife : npc.GetGlobalNPC<Count>().parentid];
                            }
                            if (target.GetGlobalNPC<Count>().冻结buffCD <= 0)//可以再次冻结敌人
                            {
                                target.GetGlobalNPC<Count>().冻结槽 += 150;
                                target.GetGlobalNPC<Count>().冻结下降延迟 = 300;
                                target.GetGlobalNPC<Count>().添加冻结延迟 = 2;
                            }
                        }
                        break;
                    case 7://雷
                        color2 = new Color(255, 207, 73, 130);
                        break;
                    case 8://风
                        color2 = new Color(107, 250, 232, 255);
                        break;
                    case 9://蒸发
                        color2 = new Color(221, 0, 27, 170);
                        if (npc.HasBuff(BuffID.Wet))
                        {
                            npc.DelBuff(npc.FindBuffIndex(BuffID.Wet));
                        }
                        string s = "Evaporation ";
                        if (Language.ActiveCulture.Name == "zh-Hans") s = "蒸发 ";
                        text = s + damage + "";
                        break;
                }
                text += (crit ? " !" : "");
                if(攻击类型!=6)
                CombatText.NewText(new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height), color2, text, crit);


            }

        }

        public override bool PreAI(NPC npc)
        {
            //if (npc.boss) Main.NewText(npc.FullName + " " + npc.type);
            if (蒸发cd > 0) 蒸发cd--;
            //npc.AddBuff(ModContent.BuffType<冻结>(), 300);
            if (Main.myPlayer == 0)
            {
                if (npc.life > 5000)
                {
                }
            }
            //hitcount= 0;
            //if (冻结槽 > 0) Main.NewText(冻结槽 + npc.FullName);
            if (npc.buffImmune[ModContent.BuffType<冻结>()])
            {
                npc.buffImmune[ModContent.BuffType<冻结>()] = false;
            }
            if (npc.boss) 所需冻结 = 3000;
            else 所需冻结 = 1000;
            if (冻结下降延迟 > 0) 冻结下降延迟--;
            if (添加冻结延迟 > 0) 添加冻结延迟-=npc.HasBuff(BuffID.Wet)?3:1;
            if (冻结buffCD > 0&&!npc.HasBuff<冻结>()) 冻结buffCD -= npc.HasBuff(BuffID.Wet) ? 3 : 1;
            //if (npc.HasBuff(BuffID.Wet) && 冻结buffCD > 300&& !npc.HasBuff<冻结>()) 冻结buffCD = 300;
            if (冻结槽 > 0)
            {   if(冻结下降延迟 == 0 )
                冻结槽--;
                if(npc.HasBuff<灼烧>()||npc.onFire||npc.onFire2||npc.onFire3)
                    冻结槽-=30;
                if (npc.HasBuff(BuffID.Wet)||npc.wet)
                {
                    冻结槽+=3;
                    冻结下降延迟++;
                }

            }

                if (冻结槽 < 0) 冻结槽 = 0;
            if (!npc.HasBuff<冻结>())
            {
                冻结 = false;
            }

            if (冻结槽 >= 所需冻结 && !npc.HasBuff<冻结>())
            {
                冻结下降延迟 = 0;
                冻结buffCD = 600;
                FrozenFrame = npc.frame;
                if (Main.netMode != 2)
                {
                    //Main.NewText(npc.FullName + "addbuff");
                    npc.AddBuff(ModContent.BuffType<冻结>(), 300);

                }

                if (Main.netMode != 2)
                {
                    if (npc.realLife >= 0 || parentid >= 0)
                    {
                        //Main.NewText(npc.whoAmI + "啊哈！" + npc.realLife);
                        if (npc.realLife >= 0)
                        {
                            if (npc.whoAmI != npc.realLife && Main.npc[npc.realLife].GetGlobalNPC<Count>().冻结槽 != 10000)
                            {
                                Main.npc[npc.realLife].GetGlobalNPC<Count>().冻结槽 = 10000;
                                //NetMessage.SendData(23, -1, -1, null, Main.npc[npc.realLife].whoAmI);
                            }
                        }
                        else if (Main.npc[parentid].GetGlobalNPC<Count>().冻结槽!=10000)
                        {
                            //Console.WriteLine("!!addbuffto" + Main.npc[parentid].FullName);
                            Main.npc[parentid].GetGlobalNPC<Count>().冻结槽 = 10000;
                            //NetMessage.SendData(23, -1, -1, null, Main.npc[parentid].whoAmI);

                        }

                    }
                    if (childamount > 0)
                    {
                        //Main.NewText("芜湖！");
                        for (int i = 0; i < childamount; i++)
                        {
                            if (Main.npc[childid[i]] != null && Main.npc[childid[i]].active)
                            {
                                if (Main.npc[childid[i]].GetGlobalNPC<Count>().冻结槽 != 10000)
                                {
                                    //Console.WriteLine("addbuffto" + Main.npc[childid[i]].FullName);
                                    Main.npc[childid[i]].GetGlobalNPC<Count>().冻结槽 = 10000;
                                    //NetMessage.SendData(23, -1, -1, null, Main.npc[childid[i]].whoAmI);
                                }
                            }
                        }
                    }

                }
            }
            if (!冻结 && npc.HasBuff<冻结>())
            {
                //Main.NewText("冻结！");
                //Console.WriteLine("冻结！" + Main.netMode);
                冻结 = true;
                FrozenFrame = npc.frame;
                pos = npc.position;
                冻结槽 = 0;
                FrozenRandonShader = Main.rand.NextVector2Square(0, 1);
            }
            //Main.NewText(冻结buffCD);
            if (npc.HasBuff<冻结>())
            {
                冻结槽 = 0;
                npc.position = pos;
                npc.frameCounter--;
                //npc.velocity.X = 0;
                //if (npc.velocity.Y < 0) npc.velocity.Y = 0;
                return false;
            }
            if (冻结槽 < 0) 冻结槽 = 0;
            //else
            if (冻结槽 > 0 && npc.realLife < 0 && npc.velocity.Length() > 5)
            {
                //Main.NewText(target.width + " " + target.height + " " + 体积);
                float 减缓速度 = MathHelper.Lerp(1, 0.2f, 冻结槽 / 所需冻结);
                Vector2 目标速度 = npc.velocity;
                if (减缓速度 > 1) 减缓速度 = 1;
                if (减缓速度 < 0.2) 减缓速度 = 0.2f;
                目标速度 *= 减缓速度;
                if (npc.velocity.Length() > 目标速度.Length() && 冻结槽 > 0)
                {
                    npc.frameCounter--;
                    //Main.NewText(target.velocity);
                    npc.position -= (npc.velocity - 目标速度);
                    //target.velocity = (target.velocity*15 + 目标速度*15)/30;
                }
            }
            return base.PreAI(npc);
        }
        public override void FindFrame(NPC npc, int frameHeight)
        {
            /*if(npc.life>5000&&Main.GameUpdateCount%60==0)//if (npc.netUpdate)
            {
            }*/
            if(spawn>0)spawn--;
            if (npc.active)
            {   
                if(Sync ||spawn==0||npc.netUpdate||npc.netUpdate2)
                SyncParendChildren(npc);
                Sync = false;
                spawn = -1;
            }
            if (npc.HasBuff<冻结>())
            {
                //npc.position -= npc.velocity;
                //npc.velocity.X = 0;
                //if (npc.velocity.Y < 0) npc.velocity.Y = 0;
            }
            base.FindFrame(npc, frameHeight);
        }
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            base.ModifyNPCLoot(npc, npcLoot);
        }
        public override void AI(NPC npc)
        {
            if (lightning > 0) lightning--;
            //if (!target.boss&&target.life>1000) Main.NewText(target.FullName);

                /*if (target.realLife >= 0&&!MarkSelfAsChild)
                {
                    MarkSelfAsChild= true;
                    if (Main.npc[target.realLife].whoAmI != target.whoAmI)
                    {
                        Main.npc[target.realLife].GetGlobalNPC<Count>().childid[Main.npc[target.realLife].GetGlobalNPC<Count>().childamount++] = target.whoAmI;
                        Main.NewText(target.FullName);
                    }
                }*/

                //if(target.life>4000)
                // Main.NewText(target.FullName + " " + target.realLife);
                //if (childamount > 0) Main.NewText(Main.npc[childid[2]].FullName);
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            
            if (source is EntitySource_Parent&&Main.netMode!=1 )
            {
                //Console.WriteLine("parent" + npc.FullName + " server?" + Main.netMode);

                // 在这里先创建好一个EntitySource_Parent实例，方便后面调用
                EntitySource_Parent parentSource = source as EntitySource_Parent;
                // 如果生成这个source的是一个NPC
                // 而且这个NPC是一个Boss
                // parentSource.Entity as NPC 用于获取这个NPC，这样我们才能用boss这个字段
                if (parentSource.Entity is NPC&& npc.realLife<0)
                {
                    NPC gnpc = (parentSource.Entity as NPC);
                    if (npc.lifeMax> gnpc.lifeMax/8f)
                    {
                        //if (Main.netMode != 2) Main.NewText("parent" + gnpc.FullName + " child " + npc);
                        //Console.WriteLine("parent" + npc.FullName + " server?" + Main.netMode);

                        //Main.NewText(npc.FullName+" "+ npc.lifeMax+" "+ gnpc.FullName+" "+ gnpc.lifeMax+" "+ npc.realLife);
                        npc.GetGlobalNPC<Count>().parentid = gnpc.whoAmI;
                        gnpc.GetGlobalNPC<Count>().childid[gnpc.GetGlobalNPC<Count>().childamount++] = npc.whoAmI;
                        gnpc.GetGlobalNPC<Count>().spawn = 5;
                    }
                }
            }
        }
        
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            /*Color frozen = new Color(0, 233, 233, 255);
            if (count > 0) drawColor = Color.Lerp(Color.White, frozen, count / 所需冻结);
            if (npc.HasBuff<冻结>())
            {
                float 体积 = npc.width * npc.height;
                float scale = MathHelper.Lerp(0.8f, 3, 体积 / 20000f);
                if (scale > 3) scale = 3;
                if (scale < 0.5) scale = 0.5f;
                //if(npc.boss)Main.NewText(scale);
                Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.IceTorch,
                0, 0, 0, Color.White, scale);
                drawColor = frozen;

            }*/


        }
    }

}
