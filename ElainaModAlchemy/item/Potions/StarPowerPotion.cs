using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

public sealed class StarPowerPotion : AlchemyPotion
{
    public override string EntryId => "starpower";
    protected override int PotionBuffType => ModContent.BuffType<StarPowerBuff>();
}
