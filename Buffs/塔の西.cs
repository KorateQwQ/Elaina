using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ID;

namespace 伊蕾娜.Buffs
{
    public class 塔の西 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("开心");
            // Description.SetDefault("捡到钱了好耶");
            Main.buffNoSave[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
            Main.pvpBuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.manaRegenDelay > 5) player.manaRegenDelay = 5;
            player.manaRegen++;
            //player.statMana++;
            //player.buffTime[buffIndex]++;
            //player.GetDamage<GenericDamageClass>() += 0.1f;

            base.Update(player, ref buffIndex);
        }
        public override void Update(NPC npc, ref int buffIndex)
        {

            base.Update(npc, ref buffIndex);
        }
        public class 捡钱 : GlobalItem
        {
            public override bool InstancePerEntity => true;
            public bool 战利品 = false;
            public int pickupcd = 0;
            public override void Update(Item item, ref float gravity, ref float maxFallSpeed)
            {

                /*if (pickupcd > 0)
                {
                    pickupcd--;
                }*/
                base.Update(item, ref gravity, ref maxFallSpeed);
            }

            public override void OnSpawn(Item item, IEntitySource source)
            {
                /*if (source != null)
                {
                    Main.NewText(source.ToString());
                }
                //if(source is EntitySource_DropAsItem || source is EntitySource_Death || source is EntitySource_Loot)
                */
                /*{
                    Main.NewText(111);
                    战利品 = true;
                }*/
                if (source is EntitySource_Misc PlayerDropItemCheck)
                {
                    item.GetGlobalItem<捡钱>().战利品 = false;
                    //Main.NewText("1111"+item.GetGlobalItem<捡钱>().战利品);
                }
                else
                {
                    战利品 = true;
                    // Main.NewText("wuwu");
                }
                base.OnSpawn(item, source);
            }
            public override bool OnPickup(Item item, Player player)
            {
                //Main.NewText(战利品);
                if (pickupcd <= 0)
                {
                    if (player.GetModPlayer<ElainaModplayer>().Elaina)
                    {
                        if (item.type == ItemID.CopperCoin || item.type == ItemID.SilverCoin || item.type == ItemID.GoldCoin || item.type == ItemID.PlatinumCoin)
                        {
                            if (item.GetGlobalItem<捡钱>().战利品)
                            {
                                pickupcd = 60;
                                player.statMana += 10;
                                item.GetGlobalItem<捡钱>().战利品 = false;
                                CombatText.NewText(player.getRect(), CombatText.HealMana, 10);
                                player.AddBuff(ModContent.BuffType<塔の西>(), 600);
                                //Main.NewText("好耶");
                            }
                            else
                            {
                                //Main.NewText("坏耶");
                            }

                        }
                    }
                }

                return base.OnPickup(item, player);
            }

        }
    }
}
