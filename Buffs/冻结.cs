using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.Buffs
{
    public class 冻结 : ModBuff
    {


        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("冻结");
            // Description.SetDefault("无法行动");
            Main.buffNoSave[Type] = false;
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.pvpBuff[Type] = true;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            //if (npc.boss) npc.buffTime[buffIndex]--;
            /*if (npc.buffTime[buffIndex] == 499)&& !npc.GetGlobalNPC<modifyNPC>().update) {
                //npc.GetGlobalNPC<modifyNPC>().frozenFrame = npc.frame;
                //npc.GetGlobalNPC<modifyNPC>().update = true;
                //Main.NewText("记录:  "+npc.frame);
            }*/
            //npc.GetGlobalNPC<Count>().count=0;
            //if (npc.boss) npc.buffTime[buffIndex] -= 6;
            //npc.dontTakeDamage = true;
            //Main.NewText(npc.buffTime[buffIndex]);


            //Main.NewText(npc.buffTime[buffIndex]);
            {
                //Main.NewText(npc.buffTime[buffIndex]);
            }

            //npc.color = new Color(0, 233, 233, 255);


        }
        public class 冻结计数 : ModBuff
        {


            public override void SetStaticDefaults()
            {
                // DisplayName.SetDefault("寒冷");
                // Description.SetDefault("难以行动");
                Main.buffNoSave[Type] = false;
                Main.debuff[Type] = true;
                Main.buffNoTimeDisplay[Type] = false;
                Main.pvpBuff[Type] = true;
            }
            public override bool ReApply(NPC npc, int time, int buffIndex)
            {
                if (!npc.immortal)
                {
                    npc.GetGlobalNPC<Count>().冻结下降速率 = 0;
                    npc.GetGlobalNPC<Count>().count += 5;
                    if (npc.HasBuff(BuffID.Wet) || npc.wet) npc.GetGlobalNPC<Count>().count += 5;
                }
                npc.buffTime[buffIndex] = time;
                return false;
            }
            public override void Update(NPC npc, ref int buffIndex)
            {
                 /*NPC target = npc;
                 if (npc.realLife >= 0 || npc.GetGlobalNPC<Count>().parentid >= 0)
                 {
                     target = Main.npc[npc.realLife >= 0 ? npc.realLife : npc.GetGlobalNPC<Count>().parentid];
                 }
                 if (!target.HasBuff<冻结>())
                 {
                     target.GetGlobalNPC<Count>().冻结槽 += 20;
                     target.GetGlobalNPC<Count>().冻结下降延迟 = 300;
                 }*/
                /*if (npc.buffTime[buffIndex] == 499)&& !npc.GetGlobalNPC<modifyNPC>().update) {
                    //npc.GetGlobalNPC<modifyNPC>().frozenFrame = npc.frame;
                    //npc.GetGlobalNPC<modifyNPC>().update = true;
                    //Main.NewText("记录:  "+npc.frame);
                }*/
                //npc.GetGlobalNPC<Count>().count=0;
                //if (npc.boss) npc.buffTime[buffIndex] -= 6;
                //npc.dontTakeDamage = true;
                //Main.NewText(npc.buffTime[buffIndex]);


                //Main.NewText(npc.buffTime[buffIndex]);
                {
                    //Main.NewText(npc.buffTime[buffIndex]);
                }

                //npc.color = new Color(0, 233, 233, 255);i
            }
        }
    }
}
