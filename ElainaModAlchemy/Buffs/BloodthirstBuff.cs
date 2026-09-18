using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Buffs;

public sealed class BloodthirstBuff : ModBuff
{
    public override string LocalizationCategory => "Alchemy.Buffs";
    public override string Texture => $"Terraria/Images/Buff_{BuffID.Wrath}";

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetModPlayer<AlchemyBuffPlayer>().BloodthirstActive = true;
        player.GetDamage(DamageClass.Melee) += 0.1f;
    }
}
