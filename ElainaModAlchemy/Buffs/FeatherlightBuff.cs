using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Buffs;

public sealed class FeatherlightBuff : ModBuff
{
    public override string LocalizationCategory => "Alchemy.Buffs";

    public override void Update(Player player, ref int buffIndex) =>
        player.GetModPlayer<AlchemyBuffPlayer>().FeatherlightActive = true;
}
