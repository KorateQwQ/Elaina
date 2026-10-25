using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModAlchemy.Buffs;

public sealed class MoonDewElixirBuff : ModBuff
{
    public override string LocalizationCategory => "Alchemy.Buffs";

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetModPlayer<ElainaManaElixirPlayer>().Active = true;
    }
}
