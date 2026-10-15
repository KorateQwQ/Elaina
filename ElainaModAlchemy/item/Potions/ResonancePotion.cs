using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

public sealed class ResonancePotion : AlchemyPotion
{
    public override string EntryId => "resonance";
    protected override int PotionBuffType => ModContent.BuffType<ResonanceBuff>();
}
