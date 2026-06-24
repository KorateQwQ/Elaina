using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.Mounts;

namespace 伊蕾娜.Buffs
{
    public class 扫帚buff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("扫帚");
            // Description.SetDefault("骑着扫帚飞行");
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }


        public override void Update(Player player, ref int buffIndex)
        {

            player.mount.SetMount(ModContent.MountType<扫帚>(), player);
            player.buffTime[buffIndex] = 10;
            player.mount._fatigue = 0;//坐骑的疲劳值始终为0
            player.mount._flyTime = 320;//坐骑的飞行时间
        }
    }
}
