using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;

namespace 伊蕾娜.Buffs;

/// <summary>开心：小幅提升魔法伤害与旅行速度；保留原类型名以兼容存档。</summary>
public class 塔の西 : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.pvpBuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetDamage(DamageClass.Magic) += 0.03f;
        player.GetModPlayer<AlchemyBuffPlayer>().HappyActive = true;
    }
}
