using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.Buffs
{
    public class 潋滟buff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Water Magic Power");
            //DisplayName.AddTranslation(7, "水花萦绕");
            // Description.SetDefault("20% increased Water magic damage");
            //Description.AddTranslation(7, "水魔法伤害提高20%");
            Main.buffNoSave[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
        }
        /*
         * 小动物药水逻辑：
         * 射弹命中后，施加小动物buff，并通过射弹给予一个合理的动物id，这个id为buff时间
         * 全客户端检测到buff的生成，并通过此buff检测动物id，若为-1，则随机生成一个合理的动物id
         */
    }

}
