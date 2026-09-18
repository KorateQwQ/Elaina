using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;

namespace 伊蕾娜.ElainaModAlchemy.item;

public sealed class Painkiller : AlchemyPotion
{
    protected override int PotionBuffType => ModContent.BuffType<PainkillerBuff>();
    protected override int DurationTicks => 30 * 60;
}
