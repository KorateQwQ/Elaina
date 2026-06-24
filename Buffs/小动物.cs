using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.Buffs
{
    public class 小动物 : ModBuff
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Critter magic");
            //DisplayName.AddTranslation(7, "小动物魔法");
            // Description.SetDefault("You are a critter now (*^▽^*)");
            //Description.AddTranslation(7, "你现在是一只小动物 (*^▽^*)");
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = false;
        }
        
        public override void Update(Player player, ref int buffIndex)
        {
            //player.buffTime[buffIndex] = 10;
            //if (player.whoAmI == 0) Main.NewText(player.buffTime[buffIndex]);
            if (player.mount.Active) player.mount.Dismount(player);
        }
        /*
         * 小动物药水逻辑：
         * 射弹命中后，施加小动物buff，并通过射弹给予一个合理的动物id，这个id为buff时间
         * 全客户端检测到buff的生成，并通过此buff检测动物id，若为-1，则随机生成一个合理的动物id
         */
    }
}
