using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Buffs;

public sealed class ConcentrationBuff : ModBuff
{
    public override string LocalizationCategory => "Alchemy.Buffs";
    public override string Texture => $"Terraria/Images/Buff_{BuffID.Archery}";

    public override void Update(Player player, ref int buffIndex) =>
        player.GetModPlayer<AlchemyBuffPlayer>().ConcentrationActive = true;
}
