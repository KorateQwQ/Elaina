using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Buffs;

namespace 伊蕾娜.ElainaModAlchemy.item.Potions;

public sealed class ConcentrationPotion : AlchemyPotion
{
    public override string EntryId => "focus";
    protected override int PotionBuffType => ModContent.BuffType<ConcentrationBuff>();
}
